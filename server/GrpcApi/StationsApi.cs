namespace PathApi.Server.GrpcApi
{
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using PathApi.Server.PathServices;
    using PathApi.V1;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using TrainStatus = PathApi.V1.GetUpcomingTrainsResponse.Types.UpcomingTrain.Types.Status;

    /// <summary>
    /// gRPC service implementation for the Stations service.
    /// </summary>
    internal sealed class StationsApi : Stations.StationsBase, IGrpcApi
    {
        private readonly Flags flags;
        private readonly RealtimeDataRepository realtimeDataRepository;

        /// <summary>
        /// Constructs a new instance of the <see cref="StationsApi"/>.
        /// </summary>
        /// <param name="flags">Flags instance containing the app configuration.</param>
        /// <param name="realtimeDataRepository">The repository to use when looking up realtime data.</param>
        public StationsApi(Flags flags, RealtimeDataRepository realtimeDataRepository)
        {
            this.flags = flags;
            this.realtimeDataRepository = realtimeDataRepository;
        }

        /// <summary>
        /// Binds the Server service to this implementation.
        /// </summary>
        /// <returns>The <see cref="ServerServiceDefinition"/> for this service that can be registered with a server.</returns>
        public ServerServiceDefinition BindService()
        {
            return Stations.BindService(this);
        }

        /// <summary>
        /// Handles the GetUpcomingTrains request.
        /// </summary>
        public override Task<GetUpcomingTrainsResponse> GetUpcomingTrains(GetUpcomingTrainsRequest request, ServerCallContext context)
        {
            if (request.Station == Station.Unspecified)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Invalid station supplied."));
            }

            var response = new GetUpcomingTrainsResponse();
            foreach (var entry in this.realtimeDataRepository.GetRealtimeData(request.Station).Select(data => this.ToUpcomingTrain(data)))
            {
                response.UpcomingTrains.Add(entry);
            }
            return Task.FromResult(response);
        }

        /// <summary>
        /// Handles the ListStations request.
        /// </summary>
        public override Task<ListStationsResponse> ListStations(ListStationsRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "Method not yet implemented."));
        }

        /// <summary>
        /// Handles the GetStation request.
        /// </summary>
        public override Task<StationData> GetStation(GetStationRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, "Method not yet implemented."));
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
                LineName = realtimeData.HeadSign,
                ProjectedArrival = this.ToTimestamp(realtimeData.ExpectedArrival),
                LastUpdated = this.ToTimestamp(realtimeData.LastUpdated),
                Status = status
            };
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
