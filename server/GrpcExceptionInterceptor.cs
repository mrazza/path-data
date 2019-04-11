namespace PathApi.Server
{
    using System;
    using System.Threading.Tasks;
    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using PathApi.Server.GrpcApi;
    using Serilog;

    internal sealed class GrpcExceptionInterceptor : Interceptor
    {
        private readonly Type type;

        public GrpcExceptionInterceptor(IGrpcApi service)
        {
            this.type = service.GetType();
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation.Invoke(request, context);
            }
            catch (Exception ex)
            {
                Log.Logger.Here().Error(ex, $"Unexpected error in gRPC handler for {type.ToString()}.");
                throw;
            }
        }
    }
}