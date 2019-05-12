namespace PathApi.Server.Tests.GrpcApi
{
    using Google.Protobuf.WellKnownTypes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PathApi.Server.GrpcApi.V1;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="ServerApi"/> class.
    /// </summary>
    [TestClass]
    public sealed class ServerApiTest
    {
        [TestMethod]
        public async Task GetHealth()
        {
            var api = new ServerApi();
            Assert.IsNotNull(await api.GetHealth(new Empty(), null));
        }
    }
}