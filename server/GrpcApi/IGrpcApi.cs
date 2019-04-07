namespace PathApi.Server.GrpcApi
{
    using Grpc.Core;

    /// <summary>
    /// Interface for all gRPC service definitions.
    /// </summary>
    internal interface IGrpcApi
    {
        /// <summary>
        /// Method that binds the respective service to this instance.
        /// </summary>
        /// <returns>The <see cref="ServerServiceDefinition"/> that can be registered with a server.</returns>
        ServerServiceDefinition BindService();
    }
}