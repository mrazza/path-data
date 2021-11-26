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
        private const string UPDATE_URL = "http://127.0.0.1:8080/dbCheck";
        private const string DATABASE_URL = "http://127.0.0.1:8080/dbDownload?sum={0}";
        private const string API_KEY = "duhbestapikey";
        private const string APP_NAME = "duhbestapp";
        private const string APP_VERSION = "duhlatestversion";
        private const string USER_AGENT = "mytesthttpclient";
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
                PathApiKey = API_KEY,
                PathAppName = APP_NAME,
                PathAppVersion = APP_VERSION,
                PathUserAgent = USER_AGENT,
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
            const string checksum = "12345";
            const string newChecksum = "abcde";
            const string newestChecksum = "efghj";
            this.fakePathApi.AddExpectedRequest(
                UPDATE_URL, (context) =>
                {
                    switch (context.Request.Headers["dbchecksum"])
                    {
                        case checksum:
                            return "{'Data': { 'DbUpdate': { 'Checksum': '" + newChecksum + "', 'Description': 'UPDATED!' }}}";
                        case newChecksum:
                            return "{'Data': { 'DbUpdate': { 'Checksum': '" + newestChecksum + "', 'Description': 'UPDATED!' }}}";
                        case newestChecksum:
                            return null;
                    }

                    throw new ArgumentException("Unexpected checksum!");
                });

            var latestChecksum = await this.pathApiClient.GetLatestChecksum(checksum);
            Assert.AreEqual(newestChecksum, latestChecksum);
        }

        [TestMethod]
        public async Task UpdatesChecksum_NullData()
        {
            const string checksum = "12345";
            const string newChecksum = "abcde";
            const string newestChecksum = "efghj";
            this.fakePathApi.AddExpectedRequest(
                UPDATE_URL, (context) =>
                {
                    switch (context.Request.Headers["dbchecksum"])
                    {
                        case checksum:
                            return "{'Data': { 'DbUpdate': { 'Checksum': '" + newChecksum + "', 'Description': 'UPDATED!' }}}";
                        case newChecksum:
                            return "{'Data': { 'DbUpdate': { 'Checksum': '" + newestChecksum + "', 'Description': 'UPDATED!' }}}";
                        case newestChecksum:
                            return "{'Data': null}";
                    }

                    throw new ArgumentException("Unexpected checksum!");
                });
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