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

            channel.ShutdownAsync().Wait();
        }
    }
}
