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
                    mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    dbRepository.OnDataUpdate += (sender, args) => { updateCount++; };

                    await dbRepository.OnStartup();
                    Assert.AreEqual(1, updateCount);
                }

                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    TaskCompletionSource<object> latch = new TaskCompletionSource<object>();
                    mockApiClient.Setup(api => api.GetLatestChecksum(LATEST_CHECKSUM)).ReturnsAsync("newerChecksum");
                    mockApiClient.Setup(api => api.GetDatabaseAsStream("newerChecksum")).ReturnsAsync(databaseStream);
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
                    mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                var key = await dbRepository.GetServiceBusKey();
                Assert.AreEqual("ovK3azy9In0GQLB9y5DjwxiObyBwalySZLofNssvHSQPsI4zwwczvtztPC50yWj4f7jww991EIn9qrKFivTdDfKUDgKjOUoZRv/UGO5EyC/MFs8mTvfC8d7Jbqv6DxSgAcNRbcXGYF0OKHBKvSE2vARZkEnzSf1jcQqxd3DPATN/g08ZIW0gxhVp2BXKYdYpiiq7+pWIObjfBHB0uR4taXY2YOliHbyNV5CosF91NK0tiJV+xxwcvPURH/82auB0fTQvvUZnkaaxUzq+9Pjlv3JfdeAFM/1PmTPN/Mfnz3aZ/2bOUYUX4gciBUNxprcsGQGQJivpjteaD2C+PkEGWcpB0btYYLdSkuVYnV/LFzrRZ/9eqLPkfz20yhl41eANgrGNPdNl2wEEBTAUjw+OeZJsJZIAcWu4vByW98oaYM0=", key);
            }
        }

        [TestMethod]
        public async Task GetStops_KnownStation()
        {
            using (PathSqlDbRepository dbRepository = this.CreateSqlDbRepository())
            {
                using (var databaseStream = new FileStream(TEST_DATABASE_PATH, FileMode.Open))
                {
                    mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
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
                    mockApiClient.Setup(api => api.GetLatestChecksum(INITIAL_CHECKSUM)).ReturnsAsync(LATEST_CHECKSUM);
                    mockApiClient.Setup(api => api.GetDatabaseAsStream(LATEST_CHECKSUM)).ReturnsAsync(databaseStream);
                    await dbRepository.OnStartup();
                }

                await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await dbRepository.GetStops(Station.Unspecified));
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