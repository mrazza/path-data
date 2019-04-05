namespace PathApi.Server
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using PathApi.Server.GrpcApi;
    using PathApi.V1;
    using Serilog;

    internal sealed class GrpcServer
    {
        private readonly Server grpcServer;
        private readonly TaskCompletionSource<object> latch;
        private readonly Flags flags;
        private readonly IEnumerable<IGrpcApi> services;


        public GrpcServer(Flags flags, IEnumerable<IGrpcApi> services)
        {
            this.flags = flags;
            this.services = services;
            this.grpcServer = new Server()
            {
                Ports = { new ServerPort(flags.ServerHost, flags.ServerPort, ServerCredentials.Insecure) }
            };
            foreach (var service in services)
            {
                this.grpcServer.Services.Add(service.BindService());
            }
            this.latch = new TaskCompletionSource<object>();
        }

        public async Task Run()
        {
            Log.Logger.Here().Information("Starting gRPC server for services:");
            foreach (var service in this.services)
            {
                Log.Logger.Here().Information($"---> {service}");
            }

            this.grpcServer.Start();
            Log.Logger.Here().Information($"============================================================");
            Log.Logger.Here().Information($"Server started on {flags.ServerHost}:{flags.ServerPort}!");
            Log.Logger.Here().Information($"============================================================");
            await this.latch.Task;
        }

        public async Task Stop()
        {
            Log.Logger.Here().Information("Stopping gRPC server...");
            await this.grpcServer.ShutdownAsync();
            Log.Logger.Here().Information("gRPC server shutdown.");
            this.latch.SetResult(null);
        }
    }
}