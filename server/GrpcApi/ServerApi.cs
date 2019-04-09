namespace PathApi.Server.GrpcApi
{
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using System.Threading.Tasks;
    using Server = PathApi.V1.Server;

    internal sealed class ServerApi : Server.ServerBase, IGrpcApi
    {
        public ServerServiceDefinition BindService()
        {
            return Server.BindService(this);
        }

        public override Task<Empty> GetHealth(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }
    }
}
