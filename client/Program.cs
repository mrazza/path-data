namespace PathApi.Client
{
    using System;
    using Grpc.Core;
    using PathApi.V1;

    class Program
    {
        static void Main(string[] args)
        {
            Channel channel = new Channel("path.grpc.razza.dev:443", ChannelCredentials.Insecure);
            var client = new Stations.StationsClient(channel);
            var rclient = new Routes.RoutesClient(channel);
            var reply = client.GetUpcomingTrains(new GetUpcomingTrainsRequest()
            {
                Station = Station.GroveStreet
            });
            Console.WriteLine(reply.ToString());
            Console.WriteLine();

            var reply2 = client.GetStation(new GetStationRequest()
            {
                Station = Station.GroveStreet
            });
            Console.WriteLine(reply2.ToString());
            Console.WriteLine();

            var reply3 = client.ListStations(new ListStationsRequest());
            Console.WriteLine(reply3.ToString());
            Console.WriteLine();

            var reply4 = rclient.ListRoutes(new ListRoutesRequest());
            Console.WriteLine(reply4.ToString());
            Console.WriteLine();

            var reply5 = rclient.GetRoute(new GetRouteRequest()
            {
                Route = Route.Jsq33
            });
            Console.WriteLine(reply5.ToString());
            Console.WriteLine();

            channel.ShutdownAsync().Wait();
        }
    }
}
