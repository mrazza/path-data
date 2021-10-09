namespace PathApi.Server.GrpcApi.V1
{
    using Google.Protobuf.WellKnownTypes;
    using Google.Type;
    using Grpc.Core;
    using PathApi.Server.PathServices;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TrainStatus = PathApi.V1.GetUpcomingTrainsResponse.Types.UpcomingTrain.Types.Status;

    /// <summary>
    /// gRPC service implementation for the Routes service.
    /// </summary>
    internal sealed class RoutesApi : Routes.RoutesBase, IGrpcApi
    {
        private const int DEFAULT_PAGE_SIZE = 250;
        private readonly IPathDataRepository pathDataRepository;

        /// <summary>
        /// Constructs a new instance of the <see cref="RoutesApi"/>.
        /// </summary>
        /// <param name="pathDataRepository">The repository to use when looking up static path data.</param>
        public RoutesApi(IPathDataRepository pathDataRepository)
        {
            this.pathDataRepository = pathDataRepository;
        }

        /// <summary>
        /// Binds the Routes service to this implementation.
        /// </summary>
        /// <returns>The <see cref="ServerServiceDefinition"/> for this service that can be registered with a server.</returns>
        public ServerServiceDefinition BindService()
        {
            return Routes.BindService(this);
        }

        /// <summary>
        /// Handles the ListRoutes request.
        /// </summary>
        public override async Task<ListRoutesResponse> ListRoutes(ListRoutesRequest request, ServerCallContext context)
        {
            int offset = PaginationHelper.GetOffset(request.PageToken);
            int pageSize = request.PageSize == 0 ? DEFAULT_PAGE_SIZE : request.PageSize;

            ListRoutesResponse response = new ListRoutesResponse();
            var routes = await this.GetAllRoutes();
            response.Routes.Add(routes.Skip(offset).Take(pageSize));
            if (routes.Count > offset + pageSize)
            {
                response.NextPageToken = PaginationHelper.GetPageToken(offset + pageSize);
            }
            return response;
        }

        /// <summary>
        /// Handles the GetRoute request.
        /// </summary>
        public override async Task<RouteData> GetRoute(GetRouteRequest request, ServerCallContext context)
        {
            if (request.Route == Route.Unspecified)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Invalid route supplied."));
            }

            try
            {
                return (await GetAllRoutes()).Where(route => route.Route == request.Route).First();
            }
            catch (InvalidOperationException)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Requested route not found."));
            }
        }

        private async Task<List<RouteData>> GetAllRoutes()
        {
            List<RouteData> routes = new List<RouteData>();
            var routeData = await this.pathDataRepository.GetRoutes();
            routes = routeData.GroupBy(routeDataEntry => routeDataEntry.Route)
                    .Select(route => this.ToRouteData(route)).ToList();
            return routes;
        }

        private RouteData ToRouteData(IEnumerable<RouteLine> lines)
        {
            var firstLine = lines.First();
            if (!lines.All(line => line.Route == firstLine.Route))
            {
                throw new ArgumentException("All lines must be for the same route.");
            }

            RouteData routeData = new RouteData()
            {
                Route = firstLine.Route,
                Id = firstLine.Id,
                Name = firstLine.LongName,
                Color = firstLine.Color
            };
            routeData.Lines.Add(lines.Select(line => new RouteData.Types.RouteLine()
            {
                DisplayName = line.DisplayName,
                Headsign = line.Headsign,
                Direction = RouteMappings.RouteDirectionToDirection[line.Direction]
            }));
            return routeData;
        }
    }
}
