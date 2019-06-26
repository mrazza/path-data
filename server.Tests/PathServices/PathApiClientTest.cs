namespace PathApi.Server.Tests.PathServices
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PathApi.Server.PathServices;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="PathApiClient"/> class.
    /// </summary>
    [TestClass]
    public sealed class PathApiClientTest
    {
        private const string UPDATE_URL = "http://127.0.0.1:8080/dbCheck?sum={0}";
        private const string DATABASE_URL = "http://127.0.0.1:8080/dbDownload?sum={0}";
        private const string API_KEY = "duhbestapikey";
        private PathApiClient pathApiClient;
        private FakePathApi fakePathApi;

        [TestInitialize]
        public void Setup()
        {
            this.fakePathApi = new FakePathApi(8080, API_KEY);
            this.fakePathApi.StartListening();
            this.pathApiClient = new PathApiClient(new Flags()
            {
                PathCheckDbUpdateUrl = UPDATE_URL,
                PathDbDownloadUrl = DATABASE_URL,
                PathApiKey = API_KEY
            });
        }

        [TestCleanup]
        public void Shutdown()
        {
            this.fakePathApi.Dispose();
        }

        [TestMethod]
        public async Task UpdatesChecksum()
        {
            var checksum = "12345";
            var newChecksum = "abcde";
            var newestChecksum = "efghj";
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newChecksum + "', 'description': 'UPDATED!' }}");
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newestChecksum + "', 'description': 'UPDATED!' }}");
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, newestChecksum), null);

            var latestChecksum = await this.pathApiClient.GetLatestChecksum(checksum);
            Assert.AreEqual(newestChecksum, latestChecksum);
        }

        [TestMethod]
        public async Task UpdatesChecksum_NullData()
        {
            var checksum = "12345";
            var newChecksum = "abcde";
            var newestChecksum = "efghj";
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newChecksum + "', 'description': 'UPDATED!' }}");
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newestChecksum + "', 'description': 'UPDATED!' }}");
            this.fakePathApi.AddExpectedRequest(
                string.Format(UPDATE_URL, newestChecksum), "{'data': null}");

            var latestChecksum = await this.pathApiClient.GetLatestChecksum(checksum);
            Assert.AreEqual(newestChecksum, latestChecksum);
        }

        [TestMethod]
        public async Task DownloadsDatabase()
        {
            var checksum = "12345";
            var databaseContent = "SQLITEDATAHERE";
            this.fakePathApi.AddExpectedRequest(
                string.Format(DATABASE_URL, checksum),
                databaseContent);

            var databaseStream = await this.pathApiClient.GetDatabaseAsStream(checksum);
            Assert.AreEqual(databaseContent, new StreamReader(databaseStream).ReadToEnd());
        }
    }
}