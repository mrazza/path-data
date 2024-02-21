namespace PathApi.Server.PathServices
{
    using Microsoft.AspNetCore.SignalR.Client;
    using Newtonsoft.Json;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository containing the realtime predicted arrivals of PATH trains sourced from SignalR hubs.
    /// </summary>
    internal sealed class SignalRRealtimeDataRepository : IRealtimeDataRepository, IDisposable
    {
        private readonly TimeSpan KEEP_ALIVE_INTERVAL = TimeSpan.FromSeconds(5);
        private readonly IPathDataRepository pathDataRepository;
        private readonly IPathApiClient pathApiClient;
        private readonly ConcurrentDictionary<(Station, RouteDirection), HubConnection> hubConnections;
        private readonly ConcurrentDictionary<(Station, RouteDirection), ImmutableList<RealtimeData>> realtimeData;

        /// <summary>
        /// Constructs a new instance of the <see cref="SignalRRealtimeDataRepository"/>.
        /// </summary>
        public SignalRRealtimeDataRepository(IPathDataRepository pathDataRepository, IPathApiClient pathApiClient)
        {
            this.pathDataRepository = pathDataRepository;
            this.pathApiClient = pathApiClient;
            this.hubConnections = new ConcurrentDictionary<(Station, RouteDirection), HubConnection>();
            this.realtimeData = new ConcurrentDictionary<(Station, RouteDirection), ImmutableList<RealtimeData>>();

            this.pathDataRepository.OnDataUpdate += this.PathSqlDbUpdated;
        }

        /// <summary>
        /// Gets the latest realtime train arrival data for the specified station.
        /// </summary>
        /// <param name="station">The station to get realtime arrival data for.</param>
        /// <returns>A collection of arriving trains.</returns>
        public Task<IEnumerable<RealtimeData>> GetRealtimeData(Station station)
        {
            var allData = this.GetRealtimeData(station, RouteDirection.ToNY).Union(this.GetRealtimeData(station, RouteDirection.ToNJ));
            var freshData = allData.Where(dataPoint => dataPoint.DataExpiration > DateTime.UtcNow);
            if (allData.Count() != freshData.Count())
            {
                var staledData = allData.Except(freshData);
                foreach (var staledDataPoint in staledData)
                    Log.Logger.Here().Warning("Staled data detected for S:{station} R:{route} with timestamp {updatedDataLastUpdated}, force reconnect maybe needed", station, staledDataPoint.Route.DisplayName, staledDataPoint.LastUpdated);

                Log.Logger.Here().Information("Recreating SignalR hubs following staled data detection...");
                Task.Run(this.CreateHubConnections).Wait();
            }
            return Task.FromResult(freshData);
        }

        private IEnumerable<RealtimeData> GetRealtimeData(Station station, RouteDirection direction)
        {
            Log.Logger.Here().Debug("Getting realtime data for {station}-{direction}...", station, direction);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var emptyRealtimeData = ImmutableList.Create<RealtimeData>();
            var realtimeDataResult = this.realtimeData.GetValueOrDefault((station, direction), emptyRealtimeData);
            stopWatch.Stop();
            if (realtimeDataResult.Count() != 0)
            {
                Log.Logger.Here().Debug("Got {count} realtime dataPoint(s) for {station}-{direction}", realtimeDataResult.Count(), station, direction);
            } else
            {
                Log.Logger.Here().Information("Got no realtime dataPoint for {station}-{direction}, this might indicate a problem either on the server or the client side", station, direction);
            }
            Log.Logger.Here().Information("Get realtime data for {station}-{direction} took {timespan:G}", station, direction, stopWatch.Elapsed);
            return realtimeDataResult;
        }

        private void PathSqlDbUpdated(object sender, EventArgs args)
        {
            Log.Logger.Here().Information("Recreating SignalR hubs following a PATH DB update...");
            Task.Run(this.CreateHubConnections).Wait();
        }

        private async Task CreateHubConnections()
        {
            await this.CloseExistingHubConnections();

            var tokenBrokerUrl = Decryption.Decrypt(await this.pathDataRepository.GetTokenBrokerUrl());
            var tokenValue = Decryption.Decrypt(await this.pathDataRepository.GetTokenValue());

            await Task.WhenAll(StationMappings.StationToSignalRTokenName.SelectMany(station =>
                RouteDirectionMappings.RouteDirectionToDirectionKey.Select(direction => this.CreateHubConnection(tokenBrokerUrl, tokenValue, station.Key, direction.Key))));
        }

        private async Task CreateHubConnection(string tokenBrokerUrl, string tokenValue, Station station, RouteDirection direction)
        {
            SignalRToken token;

            try
            {
                Log.Logger.Here().Information("Creating new SignalR hub connection for S:{station} D:{direction}", station, direction);
                token = await this.pathApiClient.GetToken(tokenBrokerUrl, tokenValue, station, direction);

                var connection = new HubConnectionBuilder()
                    .WithUrl(token.Url, c => c.AccessTokenProvider = () => Task.FromResult(token.AccessToken))
                    .WithAutomaticReconnect(new RetryPolicy())
                    .Build();

                connection.KeepAliveInterval = this.KEEP_ALIVE_INTERVAL;

                connection.On<string, string>("SendMessage", (_, json) =>
                    this.ProcessNewMessage(station, direction, json)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult());

                connection.Closed += async (e) => {
                    if (!this.hubConnections.ContainsKey((station, direction)))
                    {
                        return;
                    }

                    if (e != null)
                    {
                        Log.Logger.Here().Warning(e, "SignalR connection was closed as a result of an exception");
                    }

                    // Disable warning: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                    await Task.CompletedTask;
                };

                try
                {
                    await connection.StartAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Here().Warning(ex, "SignalR connection failed to start for {station}-{direction}...", station, direction);
                }

                this.hubConnections.AddOrUpdate((station, direction), connection, (key, existingConnection) =>
                {
                    return connection;
                });
            }
            catch (Exception ex)
            {
                Log.Logger.Here().Error(ex, "Attempt to create a new SignalR Hub connection for {station}-{direction} unexpectedly failed.", station, direction);
            }
        }

        private async Task ProcessNewMessage(Station station, RouteDirection direction, string jsonBlob)
        {
            try
            {
                var messageBody = JsonConvert.DeserializeObject<SignalRMessage>(jsonBlob);
                DateTime expiration = DateTime.UtcNow.AddMinutes(2);
                try
                {
                    expiration = messageBody.Expiration.AddMinutes(2); // Add two minutes as a buffer.
                }
                catch (Exception) { /* Ignore. */ }

                Log.Logger.Here().Debug("SignalR Hub ProcessNewMessage for {station}-{direction}...", station, direction);

                var newImmtubaleData = (await Task.WhenAll(messageBody.Messages.Select(async realtimeMessage =>
                {
                    var realtimeData = new RealtimeData()
                    {
                        ExpectedArrival = realtimeMessage.LastUpdated.ToUniversalTime().AddSeconds(realtimeMessage.SecondsToArrival),
                        ArrivalTimeMessage = realtimeMessage.ArrivalTimeMessage,
                        Headsign = realtimeMessage.Headsign,
                        LastUpdated = realtimeMessage.LastUpdated.ToUniversalTime(),
                        LineColors = realtimeMessage.LineColor.Split(',').Where(color => !string.IsNullOrWhiteSpace(color)).ToList(),
                        DataExpiration = expiration.ToUniversalTime()
                    };

                    RouteLine route = null;
                    try
                    {
                        route = await this.pathDataRepository.GetRouteFromTrainHeadsign(realtimeData.Headsign, realtimeData.LineColors);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Here().Warning(ex, "Failed to lookup route during realtime message update.");
                    }
                    realtimeData.Route = route;
                    return realtimeData;
                }))).ToImmutableList();

                this.realtimeData.AddOrUpdate((station, direction), newImmtubaleData, (key, oldImmutableData) => {
                    var latestNewDataPointLastUpdated = DateTimeOffset.FromUnixTimeSeconds(0).DateTime; // 1970 epoch
                    foreach (var newDataPoint in newImmtubaleData) {
                        if (newDataPoint.LastUpdated > latestNewDataPointLastUpdated)
                        {
                            latestNewDataPointLastUpdated = newDataPoint.LastUpdated;
                        }
                        if (newDataPoint.DataExpiration <= DateTime.UtcNow)
                        {
                            Log.Logger.Here().Warning("Staled dataPoint received for S:{station} D:{direction} with timestamp {lastUpdated} expires at {expiration}", station, direction, newDataPoint.LastUpdated, newDataPoint.DataExpiration);
                        }
                    }
                    
                    var updatedImmutableData = newImmtubaleData;
                    var oldDataNewerThanNewDataLastUpdatedCount = oldImmutableData.Where(oldDataPoint => oldDataPoint.LastUpdated > latestNewDataPointLastUpdated).Count();
                    if (oldDataNewerThanNewDataLastUpdatedCount > 0)
                    {
                        Log.Logger.Here().Warning("{count} dataPoint(s) in oldData are newer than newData for S:{station} D:{direction}, keeping oldData instead", oldDataNewerThanNewDataLastUpdatedCount, station, direction);
                        updatedImmutableData = oldImmutableData;
                    }
                    var filteredUpdatedImmutableData = updatedImmutableData.Where(updatedDataPoint => updatedDataPoint.DataExpiration > DateTime.UtcNow).ToImmutableList();
                    if (filteredUpdatedImmutableData.Count() != updatedImmutableData.Count())
                    {
                        Log.Logger.Here().Warning("{removedCount} dataPoint(s) out of {totalCount} in updatedData are removed for S:{station} D:{direction} as they are expired", updatedImmutableData.Count() - filteredUpdatedImmutableData.Count(), updatedImmutableData.Count(), station, direction);
                    } else
                    {
                        // return existing data will improve performance
                        filteredUpdatedImmutableData = updatedImmutableData;
                    }
                    return filteredUpdatedImmutableData;
                });
            }
            catch (Exception ex)
            {
                Log.Logger.Here().Error(ex, "Unexpected error reading a SignalR message for {station}-{direction}.", station, direction);
            }
        }

        private async Task CloseExistingHubConnections()
        {
            // Materialize the connections so we can clear the dictionary before disconnecting.
            // Otherwise, we will reconnect before reinitializing the connection (potentially
            // causing a loop if the token changes).
            var connections = this.hubConnections.Values.ToArray();
            this.hubConnections.Clear();

            await Task.WhenAll(connections.Select(async (client) => await client.DisposeAsync()));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.pathDataRepository.OnDataUpdate -= this.PathSqlDbUpdated;
                    Task.Run(this.CloseExistingHubConnections).Wait();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion

        /// <summary>
        /// Message received by the Service Bus client (JSON encoded).
        /// </summary>
        /// <remarks>
        /// This is only publicly exposed for testing purposes.
        /// </remarks>
        public sealed class SignalRMessage
        {
            public string Target { get; set; }
            public DateTime Expiration { get; set; }
            public List<RealtimeMessage> Messages { get; set; }
        }

        /// <summary>
        /// Message received by the Service Bus client (JSON encoded).
        /// </summary>
        /// <remarks>
        /// This is only publicly exposed for testing purposes.
        /// </remarks>
        public sealed class RealtimeMessage
        {
            public int SecondsToArrival { get; set; }
            public string ArrivalTimeMessage { get; set; }
            public string LineColor { get; set; }
            public string SecondaryColor { get; set; }
            public string ViaStation { get; set; }
            public string Headsign { get; set; }
            public DateTime LastUpdated { get; set; }
            public DateTime DepartureTime { get; set; }
        }
    }
}
