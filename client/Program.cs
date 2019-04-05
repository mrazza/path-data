using System;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PathApi.V1;

namespace PathApi.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //Channel channel = new Channel("107.178.211.196:80", ChannelCredentials.Insecure);
            Channel channel = new Channel("127.0.0.1:5001", ChannelCredentials.Insecure);
            var client = new Stations.StationsClient(channel);



            Console.WriteLine("Request");
            var reply2 = client.GetHealth(new Empty());//client.ListUpcomingTrainsAsync(new ListUpcomingTrainsRequest()).ResponseAsync.Result;
            Console.WriteLine(reply2.ToString());
            Console.WriteLine("Done");

            Console.WriteLine("Request");
            var reply = client.GetUpcomingTrains(new GetUpcomingTrainsRequest()
            {
                Station = Station.GroveStreet
            });//client.ListUpcomingTrainsAsync(new ListUpcomingTrainsRequest()).ResponseAsync.Result;
            Console.WriteLine(reply.ToString());
            Console.WriteLine("Done");

            channel.ShutdownAsync().Wait();
        }
    }
}
