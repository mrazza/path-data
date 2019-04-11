namespace PathApi.Client
{
    using System;
    using Grpc.Core;
    using PathApi.V1;

    class Program
    {
        static void Main(string[] args)
        {
            Channel channel = new Channel("127.0.0.1:5001", ChannelCredentials.Insecure);
            var client = new Stations.StationsClient(channel);
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

            channel.ShutdownAsync().Wait();
        }
    }
}
