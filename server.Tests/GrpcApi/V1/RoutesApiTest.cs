namespace PathApi.Server.Tests.GrpcApi
{
    using Grpc.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using PathApi.Server.GrpcApi.V1;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="RoutesApi"/> class.
    /// </summary>
    [TestClass]
    public sealed class RoutesApiTest
    {
        private Mock<IPathDataRepository> mockPathRepository;
        private RoutesApi routesApi;

        [TestInitialize]
        public void Setup()
        {
            this.mockPathRepository = new Mock<IPathDataRepository>(MockBehavior.Strict);
            this.mockPathRepository.Setup(repo => repo.GetRoutes()).ReturnsAsync(new List<RouteLine>()
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
                });
            this.routesApi = new RoutesApi(mockPathRepository.Object);
        }

        [TestMethod]
        public async Task ListRoutes()
        {
            var response = await this.routesApi.ListRoutes(new ListRoutesRequest(), null);
            Assert.AreEqual(new ListRoutesResponse()
            {
                Routes =
                {
                    new RouteData()
                    {
                        Route = Route.Jsq33Hob,
                        Id = "1024",
                        Name = "Journal Square - 33rd Street (via Hoboken)",
                        Color = "ff9900",
                        Lines =
                        {
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "33rd Street (via Hoboken) - Journal Square",
                                Headsign = "Journal Square via Hoboken",
                                Direction = Direction.ToNj
                            },
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                                Headsign = "33rd via Hoboken",
                                Direction = Direction.ToNy
                            }
                        }
                    },
                    new RouteData()
                    {
                        Route = Route.Hob33,
                        Id = "859",
                        Name = "Hoboken - 33rd Street",
                        Color = "4d92fb",
                        Lines =
                        {
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "33rd Street - Hoboken",
                                Headsign = "Hoboken",
                                Direction = Direction.ToNj
                            }
                        }
                    }
                }
            }, response);
        }

        [TestMethod]
        public async Task ListRoutes_Paginated()
        {
            var response = await this.routesApi.ListRoutes(new ListRoutesRequest()
            {
                PageSize = 1
            }, null);
            var expectedResponse = new ListRoutesResponse()
            {
                Routes =
                {
                    new RouteData()
                    {
                        Route = Route.Jsq33Hob,
                        Id = "1024",
                        Name = "Journal Square - 33rd Street (via Hoboken)",
                        Color = "ff9900",
                        Lines =
                        {
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "33rd Street (via Hoboken) - Journal Square",
                                Headsign = "Journal Square via Hoboken",
                                Direction = Direction.ToNj
                            },
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                                Headsign = "33rd via Hoboken",
                                Direction = Direction.ToNy
                            }
                        }
                    }
                }
            };
            Assert.AreEqual(expectedResponse.Routes, response.Routes);
            Assert.IsNotNull(response.NextPageToken);
            Assert.AreNotEqual(string.Empty, response.NextPageToken);

            response = await this.routesApi.ListRoutes(new ListRoutesRequest()
            {
                PageSize = 1,
                PageToken = response.NextPageToken
            }, null);
            expectedResponse = new ListRoutesResponse()
            {
                Routes =
                {
                    new RouteData()
                    {
                        Route = Route.Hob33,
                        Id = "859",
                        Name = "Hoboken - 33rd Street",
                        Color = "4d92fb",
                        Lines =
                        {
                            new RouteData.Types.RouteLine()
                            {
                                DisplayName = "33rd Street - Hoboken",
                                Headsign = "Hoboken",
                                Direction = Direction.ToNj
                            }
                        }
                    }
                }
            };
            Assert.AreEqual(expectedResponse.Routes, response.Routes);
            Assert.AreEqual(string.Empty, response.NextPageToken);
        }

        [TestMethod]
        public async Task GetRoute()
        {
            var response = await this.routesApi.GetRoute(new GetRouteRequest()
            {
                Route = Route.Jsq33Hob
            }, null);
            var expectedResponse = new RouteData()
            {
                Route = Route.Jsq33Hob,
                Id = "1024",
                Name = "Journal Square - 33rd Street (via Hoboken)",
                Color = "ff9900",
                Lines =
                {
                    new RouteData.Types.RouteLine()
                    {
                        DisplayName = "33rd Street (via Hoboken) - Journal Square",
                        Headsign = "Journal Square via Hoboken",
                        Direction = Direction.ToNj
                    },
                    new RouteData.Types.RouteLine()
                    {
                        DisplayName = "Journal Square - 33rd Street (via Hoboken)",
                        Headsign = "33rd via Hoboken",
                        Direction = Direction.ToNy
                    }
                }
            };

            Assert.AreEqual(expectedResponse, response);
        }

        [TestMethod]
        public async Task GetRoute_Unspecified()
        {
            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () => await this.routesApi.GetRoute(new GetRouteRequest()
            {
                Route = Route.Unspecified
            }, null));
            Assert.AreEqual(StatusCode.NotFound, exception.StatusCode);
        }

        [TestMethod]
        public async Task GetRoute_NotFound()
        {
            var exception = await Assert.ThrowsExceptionAsync<RpcException>(async () => await this.routesApi.GetRoute(new GetRouteRequest()
            {
                Route = Route.NwkWtc
            }, null));
            Assert.AreEqual(StatusCode.NotFound, exception.StatusCode);
        }
    }
}