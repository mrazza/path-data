namespace PathApi.Server.GrpcApi.V1
{
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using System.Threading.Tasks;
    using Server = PathApi.V1.Server;

    /// <summary>
    // gRPC service implementation for the Server service.
    /// </summary>
    internal sealed class ServerApi : Server.ServerBase, IGrpcApi
    {
        /// <summary>
        /// Binds the Server service to this implementation.
        /// </summary>
        /// <returns>The <see cref="ServerServiceDefinition"/> for this service that can be registered with a server.</returns>
        public ServerServiceDefinition BindService()
        {
            return Server.BindService(this);
        }

        /// <summary>
        /// Health check used by ESP to ensure this server is healthy.
        /// </summary>
        /// <remarks>
        /// Just return an empty proto. Failure to return the proto will treat the instance as unhealthy and move traffic elsewhere.
        /// </remarks>
        public override Task<Empty> GetHealth(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }
    }
}
