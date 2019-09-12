namespace PathApi.Server.PathServices
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using Newtonsoft.Json;
    using PathApi.Server.PathServices.Azure;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository containing the realtime predicted arrivals of PATH trains.
    /// </summary>
    internal sealed class ServiceBusRealtimeDataRepository : IRealtimeDataRepository, IDisposable
    {
        private readonly IPathDataRepository pathDataRepository;
        private readonly IServiceBusFactory managementClientFactory;
        private readonly ConcurrentDictionary<Station, ISubscriptionClient> subscriptionClients;
        private readonly ConcurrentDictionary<Tuple<Station, RouteDirection>, List<RealtimeData>> realtimeData;
        private readonly string serviceBusSubscriptionId;

        /// <summary>
        /// Constructs a new instance of the <see cref="ServiceBusRealtimeDataRepository"/>.
        /// </summary>
        public ServiceBusRealtimeDataRepository(IPathDataRepository pathDataRepository, Flags flags, IServiceBusFactory managementClientFactory)
        {
            this.pathDataRepository = pathDataRepository;
            this.managementClientFactory = managementClientFactory;
            this.serviceBusSubscriptionId = flags.ServiceBusSubscriptionId ?? Guid.NewGuid().ToString();
            this.pathDataRepository.OnDataUpdate += this.PathSqlDbUpdated;
            this.subscriptionClients = new ConcurrentDictionary<Station, ISubscriptionClient>();
            this.realtimeData = new ConcurrentDictionary<Tuple<Station, RouteDirection>, List<RealtimeData>>();
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
            return this.realtimeData.GetValueOrDefault(this.MakeKey(station, direction), new List<RealtimeData>());
        }

        private Tuple<Station, RouteDirection> MakeKey(Station station, RouteDirection direction)
        {
            return new Tuple<Station, RouteDirection>(station, direction);
        }

        private void PathSqlDbUpdated(object sender, EventArgs args)
        {
            Log.Logger.Here().Information("Creating Service Bus subscriptions following a PATH DB update...");
            Task.Run(this.CreateSubscriptions).Wait();
        }

        private async Task CreateSubscriptions()
        {
            await this.CloseExistingSubscriptions();

            var connectionString = Decryption.Decrypt(await this.pathDataRepository.GetServiceBusKey(), legacyKey: true);
            var managementClient = this.managementClientFactory.CreateManagementClient(connectionString);
            await Task.WhenAll(StationMappings.StationToServiceBusTopic.Select(station =>
                Task.Run(async () =>
                {
                    try
                    {
                        await managementClient.CreateSubscriptionAsync(station.Value, this.serviceBusSubscriptionId, new System.Threading.CancellationToken());
                    }
                    catch (MessagingEntityAlreadyExistsException ex)
                    {
                        Log.Logger.Here().Warning(ex, $"Attempt to create a new service bus subscription for {station} with ID {this.serviceBusSubscriptionId} failed, already exists.");
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Here().Error(ex, $"Attempt to create a new service bus subscription for {station} with ID {this.serviceBusSubscriptionId} unexpectedly failed.");
                    }

                    var client = this.managementClientFactory.CreateSubscriptionClient(connectionString, station.Value, this.serviceBusSubscriptionId);
                    client.RegisterMessageHandler(
                        async (message, token) => await this.ProcessNewMessage(station.Key, message),
                        new MessageHandlerOptions(async (args) => await this.HandleMessageError(station.Key, args))
                        {
                            MaxConcurrentCalls = 1,
                            AutoComplete = true
                        });
                    this.subscriptionClients.AddOrUpdate(station.Key, client, (ignored1, ignored2) => client);
                })));
            await managementClient.CloseAsync();
        }

        private async Task ProcessNewMessage(Station station, Message message)
        {
            try
            {
                RouteDirection direction = Enum.Parse<RouteDirection>(message.Label, true);
                DateTime expiration = DateTime.UtcNow.AddMinutes(2);
                try
                {
                    expiration = message.ExpiresAtUtc.AddMinutes(2); // Add two minutes as a buffer.
                }
                catch (Exception) { /* Ignore. */ }
                ServiceBusMessage messageBody = JsonConvert.DeserializeObject<ServiceBusMessage>(Encoding.UTF8.GetString(message.Body));
                Tuple<Station, RouteDirection> key = this.MakeKey(station, direction);

                List<RealtimeData> newData = (await Task.WhenAll(messageBody.Messages.Select(async realtimeMessage =>
                {
                    var realtimeData = new RealtimeData()
                    {
                        ExpectedArrival = realtimeMessage.LastUpdated.AddSeconds(realtimeMessage.SecondsToArrival),
                        ArrivalTimeMessage = realtimeMessage.ArrivalTimeMessage,
                        Headsign = realtimeMessage.Headsign,
                        LastUpdated = realtimeMessage.LastUpdated,
                        LineColors = realtimeMessage.LineColor.Split(',').Where(color => !string.IsNullOrWhiteSpace(color)).ToList(),
                        DataExpiration = expiration
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
                this.realtimeData.AddOrUpdate(key, newData, (ignored, oldData) => newData[0].LastUpdated > oldData[0].LastUpdated ? newData : oldData);
            }
            catch (Exception ex)
            {
                Log.Logger.Here().Error(ex, $"Unexpected error reading a service bus message for {station}.");
            }
        }

        private Task HandleMessageError(Station station, ExceptionReceivedEventArgs args)
        {
            Log.Logger.Here().Warning(args.Exception, "Unexpected exception when handling a new Service Bus message.");
            return Task.CompletedTask;
        }

        private async Task CloseExistingSubscriptions()
        {
            await Task.WhenAll(this.subscriptionClients.Values.Select(client => client.CloseAsync()));
            this.subscriptionClients.Clear();
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
                    Task.Run(this.CloseExistingSubscriptions).Wait();
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
        public sealed class ServiceBusMessage
        {
            public string Target { get; set; }
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