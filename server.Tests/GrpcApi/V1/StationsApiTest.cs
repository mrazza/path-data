namespace PathApi.Server.Tests.GrpcApi
{
    using Grpc.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using PathApi.Server.GrpcApi.V1;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="StationsApi"/> class.
    /// </summary>
    [TestClass]
    public sealed class StationsApiTest
    {
        private Mock<IPathDataRepository> mockPathRepository;
        private Mock<IRealtimeDataRepository> mockRealtimeRepository;
        private StationsApi stationsApi;

        [TestInitialize]
        public void Setup()
        {
            this.mockPathRepository = new Mock<IPathDataRepository>(MockBehavior.Loose);
            this.mockPathRepository.Setup(repo => repo.GetStops(It.IsAny<Station>())).ThrowsAsync(new KeyNotFoundException());
            this.mockPathRepository.Setup(repo => repo.GetStops(Station.Newark)).ReturnsAsync(new List<Stop>()
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
            });
            this.mockPathRepository.Setup(repo => repo.GetStops(Station.GroveStreet)).ReturnsAsync(new List<Stop>()
            {
                new Stop()
                {
                    Id = "26734",
                    Name = "Grove Street",
                    Latitude = 40.73454,
                    Longitude = -74.16375,
                    LocationType = LocationType.Station,
                    ParentStopId = "",
                    Timezone = "America/New_York"
                },
            });

            this.mockRealtimeRepository = new Mock<IRealtimeDataRepository>(MockBehavior.Strict);
            this.mockRealtimeRepository.Setup(repo => repo.GetRealtimeData(Station.GroveStreet)).ReturnsAsync(new[]
            {
                new RealtimeData()
                {
                    ExpectedArrival = new DateTime(2010, 2, 8),
                    ArrivalTimeMessage = "3 minutes",
                    LineColors = new List<string>() { "abcdef" },
                    Headsign = "Grove Street",
                    LastUpdated = new DateTime(2010, 2, 7),
                    DataExpiration = new DateTime(2010, 2, 10),
                    Route = new RouteLine()
                    {
                        Route = Route.Jsq33Hob,
                        Id = "1024",
                        LongName = "Journal Square - 33rd Street (via Hoboken)",
                        DisplayName = "33rd Street (via Hoboken) - Journal Square",
                        Headsign = "Journal Square via Hoboken",
                        Color = "ff9900",
                        Direction = RouteDirection.ToNJ
                    }
                }
            });

            this.stationsApi = new StationsApi(this.mockRealtimeRepository.Object, this.mockPathRepository.Object);
        }

        [TestMethod]
        public async Task GetUpcomingTrains()
        {
            var response = await this.stationsApi.GetUpcomingTrains(new GetUpcomingTrainsRequest()
            {
                Station = Station.GroveStreet
            }, null);
            Assert.AreEqual(new GetUpcomingTrainsResponse()
            {
                UpcomingTrains =
                {
                    new GetUpcomingTrainsResponse.Types.UpcomingTrain()
                    {
                        LineName = "Grove Street",
                        LineColors = { "abcdef" },
                        ProjectedArrival = new Google.Protobuf.WellKnownTypes.Timestamp()
                        {
                            Seconds = new DateTimeOffset(new DateTime(2010, 2, 8)).ToUnixTimeSeconds()
                        },
                        LastUpdated = new Google.Protobuf.WellKnownTypes.Timestamp()
                        {
                            Seconds = new DateTimeOffset(new DateTime(2010, 2, 7)).ToUnixTimeSeconds()
                        },
                        Status = GetUpcomingTrainsResponse.Types.UpcomingTrain.Types.Status.OnTime,
                        Headsign = "Grove Street",
                        Route = Route.Jsq33Hob,
                        RouteDisplayName = "33rd Street (via Hoboken) - Journal Square",
                        Direction = Direction.ToNj
                    }
                }
            }, response);
        }

        [TestMethod]
        public async Task GetUpcomingTrains_Unspecified()
        {
            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () => await this.stationsApi.GetUpcomingTrains(new GetUpcomingTrainsRequest()
            {
                Station = Station.Unspecified
            }, null));
            Assert.AreEqual(StatusCode.NotFound, exception.StatusCode);
        }

        [TestMethod]
        public async Task ListStations()
        {
            var response = await this.stationsApi.ListStations(new ListStationsRequest(), null);
            Assert.AreEqual(new ListStationsResponse()
            {
                Stations =
                {
                    EXPECTED_NEWARK_STATION,
                    EXPECTED_GROVE_STATION
                }
            }, response);
        }

        [TestMethod]
        public async Task ListStations_Pagination()
        {
            var response = await this.stationsApi.ListStations(new ListStationsRequest()
            {
                PageSize = 1
            }, null);
            var expected = new ListStationsResponse()
            {
                Stations =
                {
                    EXPECTED_NEWARK_STATION
                }
            };
            Assert.AreEqual(expected.Stations, response.Stations);
            Assert.IsNotNull(response.NextPageToken);
            Assert.AreNotEqual(string.Empty, response.NextPageToken);

            response = await this.stationsApi.ListStations(new ListStationsRequest()
            {
                PageSize = 1,
                PageToken = response.NextPageToken
            }, null);
            expected = new ListStationsResponse()
            {
                Stations =
                {
                    EXPECTED_GROVE_STATION
                }
            };
            Assert.AreEqual(expected.Stations, response.Stations);
            Assert.AreEqual(string.Empty, response.NextPageToken);
        }

        [TestMethod]
        public async Task GetStation()
        {
            var response = await this.stationsApi.GetStation(new GetStationRequest()
            {
                Station = Station.Newark
            }, null);
            Assert.AreEqual(EXPECTED_NEWARK_STATION, response);
        }

        [TestMethod]
        public async Task GetStation_Unknown()
        {
            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () => await this.stationsApi.GetStation(new GetStationRequest()
            {
                Station = Station.Unspecified
            }, null));
            Assert.AreEqual(StatusCode.NotFound, exception.StatusCode);
        }

        private static readonly StationData EXPECTED_NEWARK_STATION = new StationData()
        {
            Station = Station.Newark,
            Id = "26733",
            Name = "Newark",
            Coordinates = new Google.Type.LatLng()
            {
                Latitude = 40.73454,
                Longitude = -74.16375
            },
            Platforms =
                {
                    new StationData.Types.Area()
                    {
                        Id = "781718",
                        Name = "Newark",
                        Coordinates = new Google.Type.LatLng()
                        {
                            Latitude = 40.73454,
                            Longitude = -74.16375
                        }
                    },
                    new StationData.Types.Area()
                    {
                        Id = "781719",
                        Name = "Newark",
                        Coordinates = new Google.Type.LatLng()
                        {
                            Latitude = 40.73454,
                            Longitude = -74.16375
                        }
                    }
                },
            Entrances =
                {
                    new StationData.Types.Area()
                    {
                        Id = "782490",
                        Name = "Newark",
                        Coordinates = new Google.Type.LatLng()
                        {
                            Latitude = 40.7344,
                            Longitude = -74.1635
                        }
                    },
                    new StationData.Types.Area()
                    {
                        Id = "782491",
                        Name = "Newark",
                        Coordinates = new Google.Type.LatLng()
                        {
                            Latitude = 40.7343,
                            Longitude = -74.1649
                        }
                    }
                },
            Timezone = "America/New_York"
        };

        private static readonly StationData EXPECTED_GROVE_STATION = new StationData()
        {
            Station = Station.GroveStreet,
            Id = "26734",
            Name = "Grove Street",
            Coordinates = new Google.Type.LatLng()
            {
                Latitude = 40.73454,
                Longitude = -74.16375
            },
            Timezone = "America/New_York"
        };
    }
}