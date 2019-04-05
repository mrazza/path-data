namespace PathApi.Server.GrpcApi
{
    using System;
    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using PathApi.Server.PathServices;
    using PathApi.V1;
    using System.Linq;
    using Serilog;

    internal sealed class StationsApi : Stations.StationsBase, IGrpcApi
    {
        private readonly Flags flags;
        private readonly RealtimeDataRepository realtimeDataRepository;

        public StationsApi(Flags flags, RealtimeDataRepository realtimeDataRepository)
        {
            this.flags = flags;
            this.realtimeDataRepository = realtimeDataRepository;
        }

        public ServerServiceDefinition BindService()
        {
            return Stations.BindService(this);
        }

        public override async Task<GetUpcomingTrainsResponse> GetUpcomingTrains(GetUpcomingTrainsRequest request, ServerCallContext context)
        {
            if (request.Station == Station.Unspecified)
            {
                throw new ArgumentException("Invalid station supplied.");
            }

            var response = new GetUpcomingTrainsResponse();
            foreach (var entry in this.realtimeDataRepository.GetRealtimeData(request.Station).Select(data => this.ToUpcomingTrain(data)))
            {
                response.UpcomingTrains.Add(entry);
            }
            return response;
        }

        public override async Task<Empty> GetHealth(Empty request, ServerCallContext context)
        {
            return new Empty();
        }

        private GetUpcomingTrainsResponse.Types.UpcomingTrain ToUpcomingTrain(RealtimeData realtimeData)
        {
            var upcomingTrain = new GetUpcomingTrainsResponse.Types.UpcomingTrain()
            {
                LineName = realtimeData.HeadSign,
                ProjectedArrival = this.ToTimestamp(realtimeData.ExpectedArrival),
                LastUpdated = this.ToTimestamp(realtimeData.LastUpdated),
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
