namespace PathApi.Server.GrpcApi
{
    using Grpc.Core;

    internal interface IGrpcApi
    {
        ServerServiceDefinition BindService();
    }
}