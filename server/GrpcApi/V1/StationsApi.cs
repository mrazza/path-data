namespace PathApi.Server.GrpcApi.V1
{
    using Google.Protobuf.WellKnownTypes;
    using Google.Type;
    using Grpc.Core;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrainStatus = PathApi.V1.GetUpcomingTrainsResponse.Types.UpcomingTrain.Types.Status;

    /// <summary>
    /// gRPC service implementation for the Stations service.
    /// </summary>
    internal sealed class StationsApi : Stations.StationsBase, IGrpcApi
    {
        private const int DEFAULT_PAGE_SIZE = 250;
        private readonly IRealtimeDataRepository realtimeDataRepository;
        private readonly IPathDataRepository pathDataRepository;

        /// <summary>
        /// Constructs a new instance of the <see cref="StationsApi"/>.
        /// </summary>
        /// <param name="flags">Flags instance containing the app configuration.</param>
        /// <param name="realtimeDataRepository">The repository to use when looking up realtime data.</param>
        /// <param name="pathDataRepository">The repository to use when looking up static path data.</param>
        public StationsApi(IRealtimeDataRepository realtimeDataRepository, IPathDataRepository pathDataRepository)
        {
            this.realtimeDataRepository = realtimeDataRepository;
            this.pathDataRepository = pathDataRepository;
        }

        /// <summary>
        /// Binds the Stations service to this implementation.
        /// </summary>
        /// <returns>The <see cref="ServerServiceDefinition"/> for this service that can be registered with a server.</returns>
        public ServerServiceDefinition BindService()
        {
            return Stations.BindService(this);
        }

        /// <summary>
        /// Handles the GetUpcomingTrains request.
        /// </summary>
        public override async Task<GetUpcomingTrainsResponse> GetUpcomingTrains(GetUpcomingTrainsRequest request, ServerCallContext context)
        {
            if (request.Station == Station.Unspecified)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Invalid station supplied."));
            }

            var response = new GetUpcomingTrainsResponse();
            foreach (var entry in (await this.realtimeDataRepository.GetRealtimeData(request.Station)).Select(data => this.ToUpcomingTrain(data)))
            {
                response.UpcomingTrains.Add(entry);
            }

            return response;
        }

        /// <summary>
        /// Handles the ListStations request.
        /// </summary>
        public override async Task<ListStationsResponse> ListStations(ListStationsRequest request, ServerCallContext context)
        {
            int offset = PaginationHelper.GetOffset(request.PageToken);
            int pageSize = request.PageSize == 0 ? DEFAULT_PAGE_SIZE : request.PageSize;

            ListStationsResponse response = new ListStationsResponse();
            List<StationData> stations = new List<StationData>();
            foreach (var station in (System.Enum.GetValues(typeof(Station)) as Station[]).Where((station) => station != Station.Unspecified))
            {
                try
                {
                    var stops = await this.pathDataRepository.GetStops(station);
                    stations.Add(this.ToStation(station, stops));
                }
                catch (Exception ex)
                {
                    Log.Logger.Here().Warning(ex, "Failed to load expected station {station}.", station);
                }
            }
            response.Stations.Add(stations.Skip(offset).Take(pageSize));
            if (stations.Count > offset + pageSize)
            {
                response.NextPageToken = PaginationHelper.GetPageToken(offset + pageSize);
            }
            return response;
        }

        /// <summary>
        /// Handles the GetStation request.
        /// </summary>
        public override async Task<StationData> GetStation(GetStationRequest request, ServerCallContext context)
        {
            if (request.Station == Station.Unspecified)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Invalid station supplied."));
            }

            var stops = await this.pathDataRepository.GetStops(request.Station);
            return this.ToStation(request.Station, stops);
        }

        private StationData ToStation(Station station, List<Stop> stops)
        {
            var parentStop = stops.Where(stop => stop.LocationType == LocationType.Station).Single();
            var stationData = new StationData()
            {
                Station = station,
                Id = parentStop.Id,
                Name = parentStop.Name,
                Coordinates = new LatLng()
                {
                    Latitude = parentStop.Latitude,
                    Longitude = parentStop.Longitude
                },
                Timezone = parentStop.Timezone,
            };
            stationData.Platforms.Add(stops.Where(stop => stop.LocationType == LocationType.Platform)
                .Select(platform => new StationData.Types.Area()
                {
                    Id = platform.Id,
                    Name = platform.Name,
                    Coordinates = new LatLng()
                    {
                        Latitude = platform.Latitude,
                        Longitude = platform.Longitude
                    }
                }));
            stationData.Entrances.Add(stops.Where(stop => stop.LocationType == LocationType.Entrance)
                .Select(platform => new StationData.Types.Area()
                {
                    Id = platform.Id,
                    Name = platform.Name,
                    Coordinates = new LatLng()
                    {
                        Latitude = platform.Latitude,
                        Longitude = platform.Longitude
                    }
                }));

            return stationData;
        }

        private GetUpcomingTrainsResponse.Types.UpcomingTrain ToUpcomingTrain(RealtimeData realtimeData)
        {
            TrainStatus status = TrainStatus.OnTime;

            if (realtimeData.ArrivalTimeMessage.Trim().Equals("0 min", StringComparison.InvariantCultureIgnoreCase))
            {
                status = TrainStatus.ArrivingNow;
            }
            else if (realtimeData.ArrivalTimeMessage.Trim().Equals("Delayed", StringComparison.InvariantCultureIgnoreCase))
            {
                status = TrainStatus.Delayed;
            }

            var upcomingTrain = new GetUpcomingTrainsResponse.Types.UpcomingTrain()
            {
                LineName = realtimeData.Headsign,
                Headsign = realtimeData.Headsign,
                ProjectedArrival = this.ToTimestamp(realtimeData.ExpectedArrival),
                LastUpdated = this.ToTimestamp(realtimeData.LastUpdated),
                Status = status
            };
            if (realtimeData.Route != null)
            {
                upcomingTrain.Route = realtimeData.Route.Route;
                upcomingTrain.RouteDisplayName = realtimeData.Route.DisplayName;
                upcomingTrain.Direction = RouteMappings.RouteDirectionToDirection[realtimeData.Route.Direction];
            }
            upcomingTrain.LineColors.Add(realtimeData.LineColors);
            return upcomingTrain;
        }

        private Timestamp ToTimestamp(DateTime dateTime)
        {
            return new Timestamp()
            {
                Seconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds()
            };
        }
    }
}
