namespace PathApi.Server.Tests.PathServices
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Azure;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Unit tests for the <see cref="ServiceBusRealtimeDataRepository"/> class.
    /// </summary>
    [TestClass]
    public sealed class ServiceBusRealtimeDataRepositoryTest
    {
        private Mock<IServiceBusFactory> mockServiceBusFactory;
        private Mock<ManagementClient> mockManagementClient;
        private ServiceBusRealtimeDataRepository realtimeDataRepository;
        private FakePathDataRepository fakePathDataRepository;
        private ConcurrentDictionary<string, (ISubscriptionClient client, Subscription subscription)> createdSubscriptions;

        [TestInitialize]
        public void Setup()
        {
            this.mockServiceBusFactory = new Mock<IServiceBusFactory>();
            this.mockManagementClient = new Mock<ManagementClient>("Endpoint=sb://not-real.servicebus.windows.net/;SharedAccessKeyName=very-fake;SharedAccessKey=access-key");
            this.mockManagementClient.SetupAllProperties();
            this.fakePathDataRepository = new FakePathDataRepository();
            this.createdSubscriptions = new ConcurrentDictionary<string, (ISubscriptionClient client, Subscription subscription)>();

            this.mockServiceBusFactory.Setup(fact => fact.CreateManagementClient(It.IsAny<string>())).Returns(this.mockManagementClient.Object);
            this.mockManagementClient
                .Setup(client =>
                    client.CreateSubscriptionAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .Returns<string, string, CancellationToken>((topic, subscriber, token) =>
                {
                    var sub = this.CreateSubscription();
                    this.createdSubscriptions.AddOrUpdate(topic, sub, (key, old) => sub);
                    return Task.FromResult<SubscriptionDescription>(null);
                });
            this.mockServiceBusFactory
                .Setup(fact => fact.CreateSubscriptionClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string>((connection, topic, subscriber) =>
                {
                    var sub = this.createdSubscriptions[topic];
                    return sub.client;
                });

            this.realtimeDataRepository = new ServiceBusRealtimeDataRepository(
                this.fakePathDataRepository, new Flags(), this.mockServiceBusFactory.Object);
        }

        [TestMethod]
        public async Task ReceivesEvents()
        {
            this.fakePathDataRepository.TriggerUpdate();

            var realtimeData = await this.realtimeDataRepository.GetRealtimeData(Station.GroveStreet);
            CollectionAssert.AreEqual(new RealtimeData[0], realtimeData.ToList());

            this.SendMessage(Station.GroveStreet, Direction.ToNj, new ServiceBusRealtimeDataRepository.RealtimeMessage()
            {
                SecondsToArrival = 10,
                ArrivalTimeMessage = "10 secs",
                LineColor = "AAAAAA",
                Headsign = "Grove Street",
                LastUpdated = new DateTime(1999, 1, 1)
            });

            realtimeData = await this.realtimeDataRepository.GetRealtimeData(Station.GroveStreet);
            var expected = new RealtimeData[]
            {
                new RealtimeData()
                {
                    ArrivalTimeMessage = "10 secs",
                    LineColors = new List<string>() { "AAAAAA" },
                    Headsign = "Grove Street",
                    LastUpdated = new DateTime(1999, 1, 1),
                    ExpectedArrival = new DateTime(1999, 1, 1).AddSeconds(10),
                    DataExpiration = realtimeData.First().DataExpiration
                }
            };

            CollectionAssert.AreEqual(expected, realtimeData.ToList());
        }

        [TestMethod]
        public async Task HandlesUpdate()
        {
            this.fakePathDataRepository.TriggerUpdate();

            var oldSubs = this.createdSubscriptions;
            this.createdSubscriptions = new ConcurrentDictionary<string, (ISubscriptionClient client, Subscription subscription)>();

            this.fakePathDataRepository.TriggerUpdate();

            Assert.AreNotEqual(0, oldSubs.Count);
            foreach (var sub in oldSubs.Values)
            {
                Assert.IsTrue(sub.subscription.IsClosed);
            }

            this.SendMessage(Station.GroveStreet, Direction.ToNj, new ServiceBusRealtimeDataRepository.RealtimeMessage()
            {
                SecondsToArrival = 10,
                ArrivalTimeMessage = "10 secs",
                LineColor = "AAAAAA",
                Headsign = "Grove Street",
                LastUpdated = new DateTime(1999, 1, 1)
            });

            var realtimeData = await this.realtimeDataRepository.GetRealtimeData(Station.GroveStreet);
            Assert.AreEqual(1, realtimeData.Count());
        }

        [TestMethod]
        public async Task Disposes()
        {
            this.fakePathDataRepository.TriggerUpdate();

            this.realtimeDataRepository.Dispose();

            Assert.AreNotEqual(0, this.createdSubscriptions.Count);
            foreach (var sub in this.createdSubscriptions.Values)
            {
                Assert.IsTrue(sub.subscription.IsClosed);
            }
        }

        private void SendMessage(Station station, Direction direction, ServiceBusRealtimeDataRepository.RealtimeMessage message)
        {
            var serviceBusMessage = new ServiceBusRealtimeDataRepository.ServiceBusMessage()
            {
                Target = "",
                Messages = new List<ServiceBusRealtimeDataRepository.RealtimeMessage>()
                {
                    message
                }
            };
            this.createdSubscriptions[StationMappings.StationToShortName[station]].subscription.SendMessage(new Message()
            {
                Label = direction.ToString(),
                Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serviceBusMessage))
            });
        }

        private (ISubscriptionClient client, Subscription subscription) CreateSubscription()
        {
            var mockSubcriptionClient = new Mock<ISubscriptionClient>(MockBehavior.Strict);
            var subscription = new Subscription();
            mockSubcriptionClient
                .Setup(client =>
                    client.RegisterMessageHandler(
                        It.IsAny<Func<Message, CancellationToken, Task>>(),
                        It.IsAny<MessageHandlerOptions>()))
                .Callback<Func<Message, CancellationToken, Task>, MessageHandlerOptions>(
                    (handler, options) =>
                    {
                        subscription.SetMessageHandler(handler);
                    });
            mockSubcriptionClient.Setup(client => client.CloseAsync()).Returns(() =>
            {
                subscription.IsClosed = true;
                return Task.CompletedTask;
            });
            return (mockSubcriptionClient.Object, subscription);
        }

        private sealed class Subscription
        {
            private Func<Message, CancellationToken, Task> handler;

            public bool IsClosed { get; set; } = false;

            public async void SendMessage(Message message)
            {
                await this.handler.Invoke(message, new CancellationToken());
            }

            public void SetMessageHandler(Func<Message, CancellationToken, Task> handler)
            {
                this.handler = handler;
            }
        }

        private sealed class FakePathDataRepository : IPathDataRepository
        {
            public event EventHandler<EventArgs> OnDataUpdate;

            public void TriggerUpdate()
            {
                if (this.OnDataUpdate != null)
                {
                    this.OnDataUpdate.Invoke(this, new EventArgs());
                }
            }

            public Task<RouteLine> GetRouteFromTrainHeadsign(string headsignName, IEnumerable<string> headsignColors)
            {
                throw new NotImplementedException();
            }

            public Task<List<RouteLine>> GetRoutes()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetServiceBusKey()
            {
                return Task.FromResult("ovK3azy9In0GQLB9y5DjwxiObyBwalySZLofNssvHSQPsI4zwwczvtztPC50yWj4f7jww991EIn9qrKFivTdDfKUDgKjOUoZRv/UGO5EyC/MFs8mTvfC8d7Jbqv6DxSgAcNRbcXGYF0OKHBKvSE2vARZkEnzSf1jcQqxd3DPATN/g08ZIW0gxhVp2BXKYdYpiiq7+pWIObjfBHB0uR4taXY2YOliHbyNV5CosF91NK0tiJV+xxwcvPURH/82auB0fTQvvUZnkaaxUzq+9Pjlv3JfdeAFM/1PmTPN/Mfnz3aZ/2bOUYUX4gciBUNxprcsGQGQJivpjteaD2C+PkEGWcpB0btYYLdSkuVYnV/LFzrRZ/9eqLPkfz20yhl41eANgrGNPdNl2wEEBTAUjw+OeZJsJZIAcWu4vByW98oaYM0=");
            }

            public Task<List<Stop>> GetStops(Station station)
            {
                throw new NotImplementedException();
            }
        }
    }
}