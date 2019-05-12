namespace PathApi.Server.Tests
{
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PathApi.Server;
    using PathApi.Server.GrpcApi;
    using PathApi.Test;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="GrpcServer"/> class.
    /// </summary>
    [TestClass]
    public sealed class GrpcServerTest
    {
        private const string SERVER_HOST = "127.0.0.1";
        private const int SERVER_PORT = 9999;

        private sealed class TestApi : TestService.TestServiceBase, IGrpcApi
        {
            public ServerServiceDefinition BindService()
            {
                return TestService.BindService(this);
            }

            public override Task<Empty> EmptyTest(Empty request, ServerCallContext context)
            {
                return Task.FromResult(new Empty());
            }
        }

        [TestMethod]
        public async Task RunBlocks()
        {
            var grpcServer = this.MakeGrpcServer(new IGrpcApi[0]);
            var runTask = grpcServer.Run();
            Assert.IsFalse(runTask.IsCompleted);

            await grpcServer.Stop();
            Assert.IsTrue(runTask.IsCompleted);
        }

        [TestMethod]
        public async Task StartsService()
        {
            var grpcServer = this.MakeGrpcServer(new[] { new TestApi() });
            var runTask = grpcServer.Run();

            var client = this.MakeClient();
            var result = await client.EmptyTestAsync(new Empty());

            Assert.AreEqual(new Empty(), result);
            await grpcServer.Stop();
        }

        private TestService.TestServiceClient MakeClient()
        {
            var channel = new Channel($"{SERVER_HOST}:{SERVER_PORT}", ChannelCredentials.Insecure);
            return new TestService.TestServiceClient(channel);
        }

        private GrpcServer MakeGrpcServer(IEnumerable<IGrpcApi> services)
        {
            return new GrpcServer(new Flags()
            {
                ServerHost = SERVER_HOST,
                ServerPort = SERVER_PORT
            }, services);
        }
    }
}