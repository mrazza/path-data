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
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository containing the realtime predicted arrivals of PATH trains sourced from SignalR hubs.
    /// </summary>
    internal sealed class SignalRRealtimeDataRepository : IRealtimeDataRepository, IDisposable
    {
        private readonly IPathDataRepository pathDataRepository;
        private readonly IPathApiClient pathApiClient;
        private readonly ConcurrentDictionary<(Station, RouteDirection), HubConnection> hubConnections;
        private readonly ConcurrentDictionary<(Station, RouteDirection), List<RealtimeData>> realtimeData;

        /// <summary>
        /// Constructs a new instance of the <see cref="SignalRRealtimeDataRepository"/>.
        /// </summary>
        public SignalRRealtimeDataRepository(IPathDataRepository pathDataRepository, IPathApiClient pathApiClient)
        {
            this.pathDataRepository = pathDataRepository;
            this.pathApiClient = pathApiClient;
            this.hubConnections = new ConcurrentDictionary<(Station, RouteDirection), HubConnection>();
            this.realtimeData = new ConcurrentDictionary<(Station, RouteDirection), List<RealtimeData>>();

            this.pathDataRepository.OnDataUpdate += this.PathSqlDbUpdated;
        }

        /// <summary>
        /// Gets the latest realtime train arrival data for the specified station.
        /// </summary>
        /// <param name="station">The station to get realtime arrival data for.</param>
        /// <returns>A collection of arriving trains.</returns>
        public Task<IEnumerable<RealtimeData>> GetRealtimeData(Station station)
        {
            return Task.FromResult(this.GetRealtimeData(station, RouteDirection.ToNY).Union(this.GetRealtimeData(station, RouteDirection.ToNJ)).Where(data => data.DataExpiration > DateTime.UtcNow));
        }

        private IEnumerable<RealtimeData> GetRealtimeData(Station station, RouteDirection direction)
        {
            return this.realtimeData.GetValueOrDefault((station, direction), new List<RealtimeData>());
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

        private async Task CreateHubConnection(string tokenBrokerUrl, string tokenValue, Station station, RouteDirection direction, int sequentialFailures = 0)
        {
            SignalRToken token;

            try
            {
                Log.Logger.Here().Information("Creating new SignalR hub connection for S:{station} D:{direction}", station, direction);
                token = await this.pathApiClient.GetToken(tokenBrokerUrl, tokenValue, station, direction);

                var connection = new HubConnectionBuilder()
                    .WithUrl(token.Url, c => c.AccessTokenProvider = () => Task.FromResult(token.AccessToken))
                    .Build();

                connection.On<string, string>("SendMessage", (_, json) =>
                    this.ProcessNewMessage(station, direction, json)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult());

                async Task RetryConnection()
                {
                    await Task.Delay(new Random().Next(1, 7) * (1000 * Math.Min(sequentialFailures + 1, 5)));
                    await this.CreateHubConnection(tokenBrokerUrl, tokenValue, station, direction, sequentialFailures + 1);
                };

                connection.Closed += async (e) =>
                {
                    if (!this.hubConnections.ContainsKey((station, direction)))
                    {
                        return;
                    }

                    if (e != null)
                    {
                        Log.Logger.Here().Warning(e, "SignalR connection was closed as a result of an exception");
                    }

                    Log.Logger.Here().Information("Recovering SignalR connection to {station}-{direction}...", station, direction);
                    await RetryConnection();
                };

                try
                {
                    await connection.StartAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Here().Warning(ex, "SignalR connection failed to start for {station}-{direction}...", station, direction);
                    await RetryConnection();
                }

                this.hubConnections.AddOrUpdate((station, direction), connection, (_, __) => connection);
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


                List<RealtimeData> newData = (await Task.WhenAll(messageBody.Messages.Select(async realtimeMessage =>
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
                }))).ToList();
                this.realtimeData.AddOrUpdate((station, direction), newData, (ignored, oldData) => newData[0].LastUpdated > oldData[0].LastUpdated ? newData : oldData);
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

            await Task.WhenAll(connections.Select(client => client.DisposeAsync()));
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