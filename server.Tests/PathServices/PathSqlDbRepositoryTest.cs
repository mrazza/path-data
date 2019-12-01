namespace PathApi.Server.Tests.PathServices
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Unit tests for the <see cref="PathSqlDbRepository"/> class.
    /// </summary>
    [TestClass]
    public sealed class PathSqlDbRepositoryTest
    {
        private const string INITIAL_CHECKSUM = "initialChecksum";
        private const string LATEST_CHECKSUM = "lastestChecksum";
        private const string TEST_DATABASE_PATH = "PathServices/TestPath.db.zip";
        private const string SERVICE_BUS_KEY = "rt_ServiceBusEndpoint_Prod";
        private Mock<IPathApiClient> mockApiClient;

        [TestInitialize]
        public void Setup()
        {
            this.mockApiClient = new Mock<IPathApiClient>(MockBehavior.Strict);
        }

        [TestMethod]
        public async Task InitalizesDatabaseAndGetsUpdate()
        {
            var updateCount = 0;
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository(updateInterval: TimeSpan.FromSeconds(1)))
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    dbRepository.OnDataUpdate += (sender, args) => { updateCount++; };

                    await dbRepository.OnStartup();
                    Assert.AreEqual(1, updateCount);
                }

                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    TaskCompletionSource<object> latch = new TaskCompletionSource<object>();
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(LATEST_CHECKSUM)).ReturnsAsync("newerChecksum");
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream("newerChecksum")).ReturnsAsync(databaseStream);
                    dbRepository.OnDataUpdate += (sender, args) => { latch.SetResult(null); };

                    await latch.Task;
                    Assert.AreEqual(2, updateCount);
                }
            }
        }

        [TestMethod]
        public async Task GetServiceBusKey()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var key = await dbRepository.GetServiceBusKey();
                Assert.AreEqual("ovK3azy9In0GQLB9y5Djw+VbfXLIpIrP+b1dxBkjCVGCBMTW/5PAnPIjz3GltF3sqPKq1CEhW78qEnMwWDieQZKYgOP8G9Mz54PgPpM3Q/ILVwk1B9FMA9LrSGgw+GO6kOXCXl4oUwAMx+nEVPBSl9EJhB/iCYsgtNZQiZuGD5qhUBqJuKQ6wMWLciA5zkvztmmYZlL3/VHLL4JiJsr3omTeG1OjQ/sMfUlrg6IgKmjXBwbZd9ezxfZrZedy1EQ2J076NM81iC9Szuy0YsMH9ReR2I1Yr6PSUXL3D6Twezu9QcSPnpDJc3XWUdThDOiJM9chogHcQl6fua3fg9bRhGOS4giyzOParxBTeGelczmlIUzlQpwOHxzFT0QxM8HmUwJYDfzoMij3FkDiyi47+3jnFshEYWO+aZOTiXGd8S4=", key);
            }
        }

        [TestMethod]
        public async Task GetStops_KnownStation()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var stops = await dbRepository.GetStops(Station.Newark);
                CollectionAssert.AreEquivalent(new List<Stop>()
                {
                    new Stop()
                    {
                        Id = "26733",
                        Name = "Newark",
                        Latitude = 40.73454,
                        Longitude = -74.16375,
                        LocationType = LocationType.Station,
                        ParentStopId = "",
                        Timezone = "America/New_York"
                    },
                    new Stop()
                    {
                        Id = "781718",
                        Name = "Newark",
                        Latitude = 40.73454,
                        Longitude = -74.16375,
                        LocationType = LocationType.Platform,
                        ParentStopId = "26733",
                        Timezone = ""
                    },
                    new Stop()
                    {
                        Id = "781719",
                        Name = "Newark",
                        Latitude = 40.73454,
                        Longitude = -74.16375,
                        LocationType = LocationType.Platform,
                        ParentStopId = "26733",
                        Timezone = ""
                    },
                    new Stop()
                    {
                        Id = "782490",
                        Name = "Newark",
                        Latitude = 40.7344,
                        Longitude = -74.1635,
                        LocationType = LocationType.Entrance,
                        ParentStopId = "26733",
                        Timezone = ""
                    },
                    new Stop()
                    {
                        Id = "782491",
                        Name = "Newark",
                        Latitude = 40.7343,
                        Longitude = -74.1649,
                        LocationType = LocationType.Entrance,
                        ParentStopId = "26733",
                        Timezone = ""
                    }
                }, stops);
            }
        }

        [TestMethod]
        public async Task GetStops_UnknownStation()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await dbRepository.GetStops(Station.Unspecified));
            }
        }

        [TestMethod]
        public async Task GetRoutes()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var routes = await dbRepository.GetRoutes();
                CollectionAssert.IsSubsetOf(new List<RouteLine>()
                {
                    new RouteLine()
                    {
                        Route = Route.Jsq33Hob,
                        Id = "1024",
                        LongName = "Journal Square - 33rd Street (via Hoboken)",
                        DisplayName = "33rd Street (via Hoboken) - Journal Square",
                        Headsign = "Journal Square via Hoboken",
                        Color = "ff9900",
                        Direction = RouteDirection.ToNJ
                    },
                    new RouteLine()
                    {
                        Route = Route.Jsq33Hob,
                        Id = "1024",
                        LongName = "Journal Square - 33rd Street (via Hoboken)",
                        DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                        Headsign = "33rd via Hoboken",
                        Color = "ff9900",
                        Direction = RouteDirection.ToNY
                    },
                    new RouteLine()
                    {
                        Route = Route.Hob33,
                        Id = "859",
                        LongName = "Hoboken - 33rd Street",
                        DisplayName = "33rd Street - Hoboken",
                        Headsign = "Hoboken",
                        Color = "4d92fb",
                        Direction = RouteDirection.ToNJ
                    }
                }, routes);
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_Found()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var route = await dbRepository.GetRouteFromTrainHeadsign("Hoboken", new[] { "4d92fb" });
                Assert.AreEqual(new RouteLine()
                {
                    Route = Route.Hob33,
                    Id = "859",
                    LongName = "Hoboken - 33rd Street",
                    DisplayName = "33rd Street - Hoboken",
                    Headsign = "Hoboken",
                    Color = "4d92fb",
                    Direction = RouteDirection.ToNJ
                }, route);
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_Found_HeadsignNormalization()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var route = await dbRepository.GetRouteFromTrainHeadsign("33rd Street via Hoboken", new[] { "ff9900" });
                Assert.AreEqual(new RouteLine()
                {
                    Route = Route.Jsq33Hob,
                    Id = "1024",
                    LongName = "Journal Square - 33rd Street (via Hoboken)",
                    DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                    Headsign = "33rd via Hoboken",
                    Color = "ff9900",
                    Direction = RouteDirection.ToNY
                }, route);
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_Found_SpecialHeadsign()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository(specialHeadsignMappings: new[] { "shit headsign=33rd via Hoboken" }))
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var route = await dbRepository.GetRouteFromTrainHeadsign("shit headsign", new[] { "ff9900" });
                Assert.AreEqual(new RouteLine()
                {
                    Route = Route.Jsq33Hob,
                    Id = "1024",
                    LongName = "Journal Square - 33rd Street (via Hoboken)",
                    DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                    Headsign = "33rd via Hoboken",
                    Color = "ff9900",
                    Direction = RouteDirection.ToNY
                }, route);
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_Found_ColorNormalization()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var route = await dbRepository.GetRouteFromTrainHeadsign("Hoboken", new[] { "#4D92FB" });
                Assert.AreEqual(new RouteLine()
                {
                    Route = Route.Hob33,
                    Id = "859",
                    LongName = "Hoboken - 33rd Street",
                    DisplayName = "33rd Street - Hoboken",
                    Headsign = "Hoboken",
                    Color = "4d92fb",
                    Direction = RouteDirection.ToNJ
                }, route);
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_NotFound()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await dbRepository.GetRouteFromTrainHeadsign("NotARealHeadsign", new[] { "NotAColor" }));
            }
        }

        [TestMethod]
        public async Task GetRouteFromTrainHeadsign_MissingArguments()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    this.mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    this.mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await dbRepository.GetRouteFromTrainHeadsign("", new string[0]));
                await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await dbRepository.GetRouteFromTrainHeadsign("asd", new string[0]));
            }
        }

        private PathSqlDbRepository CreateSqlDbRepository(TimeSpan? updateInterval = null, IEnumerable<string> specialHeadsignMappings = null)
        {
            return new PathSqlDbRepository(new Flags()
            {
                InitialPathDbChecksum = INITIAL_CHECKSUM,
                SpecialHeadsignMappings = specialHeadsignMappings ?? new string[0],
                SqlUpdateCheckFrequencySecs = (int)(updateInterval ?? TimeSpan.FromDays(1)).TotalSeconds,
                ServiceBusConfigurationKeyName = SERVICE_BUS_KEY
            }, mockApiClient.Object);
        }
    }
}