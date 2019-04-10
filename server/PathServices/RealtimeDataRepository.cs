namespace PathApi.Server.PathServices
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using Newtonsoft.Json;
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
    internal sealed class RealtimeDataRepository : IDisposable
    {
        private readonly IPathSqlDbRepository sqlDbRepository;
        private ManagementClient managementClient;
        private readonly ConcurrentDictionary<Station, SubscriptionClient> subscriptionClients;
        private readonly ConcurrentDictionary<Tuple<Station, PathDirection>, List<RealtimeData>> realtimeData;
        private readonly string serviceBusSubscriptionId;

        /// <summary>
        /// Constructs a new instance of the <see cref="RealtimeDataRepository"/>.
        /// </summary>
        /// <param name="sqlDbRepository"></param>
        /// <param name="flags"></param>
        public RealtimeDataRepository(IPathSqlDbRepository sqlDbRepository, Flags flags)
        {
            this.sqlDbRepository = sqlDbRepository;
            this.serviceBusSubscriptionId = flags.ServiceBusSubscriptionId ?? Guid.NewGuid().ToString();
            this.sqlDbRepository.OnDatabaseUpdate += this.PathSqlDbUpdated;
            this.subscriptionClients = new ConcurrentDictionary<Station, SubscriptionClient>();
            this.realtimeData = new ConcurrentDictionary<Tuple<Station, PathDirection>, List<RealtimeData>>();
        }

        /// <summary>
        /// Gets the latest realtime train arrival data for the specified station.
        /// </summary>
        /// <param name="station">The station to get realtime arrival data for.</param>
        /// <returns>A collection of arriving trains.</returns>
        public IEnumerable<RealtimeData> GetRealtimeData(Station station)
        {
            return this.GetRealtimeData(station, PathDirection.ToNY).Union(this.GetRealtimeData(station, PathDirection.ToNJ)).Where(data => data.DataExpiration > DateTime.UtcNow);
        }

        private IEnumerable<RealtimeData> GetRealtimeData(Station station, PathDirection direction)
        {
            return this.realtimeData.GetValueOrDefault(this.MakeKey(station, direction), new List<RealtimeData>());
        }

        private Tuple<Station, PathDirection> MakeKey(Station station, PathDirection direction)
        {
            return new Tuple<Station, PathDirection>(station, direction);
        }

        private void PathSqlDbUpdated(object sender, EventArgs args)
        {
            Log.Logger.Here().Information("Creating Service Bus subscriptions following a PATH DB update...");
            Task.Run(this.CreateSubscriptions).Wait();
        }

        private async Task CreateSubscriptions()
        {
            await this.CloseExistingSubscriptions();

            var connectionString = Decryption.Decrypt(await this.sqlDbRepository.GetServiceBusKey());
            this.managementClient = new ManagementClient(connectionString);
            await Task.WhenAll(StationMappings.StationToShortName.Select(station =>
                Task.Run(async () =>
                {
                    try
                    {
                        await this.managementClient.CreateSubscriptionAsync(station.Value, this.serviceBusSubscriptionId, new System.Threading.CancellationToken());
                    }
                    catch (MessagingEntityAlreadyExistsException ex)
                    {
                        Log.Logger.Here().Warning(ex, $"Attempt to create a new service bus subscription for {station} with ID {this.serviceBusSubscriptionId} failed, already exists.");
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Here().Error(ex, $"Attempt to create a new service bus subscription for {station} with ID {this.serviceBusSubscriptionId} unexpectedly failed.");
                    }

                    var client = new SubscriptionClient(connectionString, station.Value, this.serviceBusSubscriptionId);
                    client.RegisterMessageHandler(
                        async (message, token) => await this.ProcessNewMessage(station.Key, message),
                        new MessageHandlerOptions(async (args) => await this.HandleMessageError(station.Key, args))
                        {
                            MaxConcurrentCalls = 1,
                            AutoComplete = true
                        });
                    this.subscriptionClients.AddOrUpdate(station.Key, client, (ignored1, ignored2) => client);
                })));
        }

        private Task ProcessNewMessage(Station station, Message message)
        {
            try
            {
                PathDirection direction = Enum.Parse<PathDirection>(message.Label, true);
                DateTime expiration = message.ExpiresAtUtc.AddMinutes(2); // Add two minutes as a buffer.
                ServiceBusMessage messageBody = JsonConvert.DeserializeObject<ServiceBusMessage>(Encoding.UTF8.GetString(message.Body));
                Tuple<Station, PathDirection> key = this.MakeKey(station, direction);

                List<RealtimeData> newData = messageBody.Messages.Select(realtimeMessage =>
                    new RealtimeData()
                    {
                        ExpectedArrival = realtimeMessage.LastUpdated.AddSeconds(realtimeMessage.SecondsToArrival),
                        ArrivalTimeMessage = realtimeMessage.ArrivalTimeMessage,
                        HeadSign = realtimeMessage.HeadSign,
                        LastUpdated = realtimeMessage.LastUpdated,
                        LineColors = realtimeMessage.LineColor.Split(',').Where(color => !string.IsNullOrWhiteSpace(color)).ToList(),
                        DataExpiration = expiration
                    }).ToList();
                this.realtimeData.AddOrUpdate(key, newData, (ignored, oldData) => newData[0].LastUpdated > oldData[0].LastUpdated ? newData : oldData);
            }
            catch (Exception ex)
            {
                Log.Logger.Here().Error(ex, $"Unexpected error reading a service bus message for {station}.");
            }

            return Task.CompletedTask;
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
                    this.sqlDbRepository.OnDatabaseUpdate -= this.PathSqlDbUpdated;
                    Task.Run(this.CloseExistingSubscriptions).Wait();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion


        private sealed class ServiceBusMessage
        {
            public string Target { get; set; }
            public List<RealtimeMessage> Messages { get; set; }
        }

        private sealed class RealtimeMessage
        {
            public int SecondsToArrival { get; set; }
            public string ArrivalTimeMessage { get; set; }
            public string LineColor { get; set; }
            public string SecondaryColor { get; set; }
            public string ViaStation { get; set; }
            public string HeadSign { get; set; }
            public DateTime LastUpdated { get; set; }
            public DateTime DepartureTime { get; set; }
        }
    }
}