namespace PathApi.Server.Tests
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
        private HttpListener httpListener;
        private Dictionary<string, string> httpResponses;

        [TestInitialize]
        public void Setup()
        {
            this.httpResponses = new Dictionary<string, string>();
            this.httpListener = new HttpListener();
            this.httpListener.Prefixes.Add("http://+:8080/");
            this.httpListener.Start();
            this.httpListener.BeginGetContext(new AsyncCallback(this.HandleHttpRequest), null);
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
            this.httpListener.Stop();
        }

        [TestMethod]
        public async Task UpdatesChecksum()
        {
            var checksum = "12345";
            var newChecksum = "abcde";
            var newestChecksum = "efghj";
            this.SetExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newChecksum + "', 'description': 'UPDATED!' }}");
            this.SetExpectedRequest(
                string.Format(UPDATE_URL, checksum),
                "{'data': { 'checksum': '" + newestChecksum + "', 'description': 'UPDATED!' }}");
            this.SetExpectedRequest(
                string.Format(UPDATE_URL, newestChecksum), null);

            var latestChecksum = await this.pathApiClient.GetLatestChecksum(checksum);
            Assert.AreEqual(newestChecksum, latestChecksum);
        }

        [TestMethod]
        public async Task DownloadsDatabase()
        {
            var checksum = "12345";
            var databaseContent = "SQLITEDATAHERE";
            this.SetExpectedRequest(
                string.Format(DATABASE_URL, checksum),
                databaseContent);

            var databaseStream = await this.pathApiClient.GetDatabaseAsStream(checksum);
            Assert.AreEqual(databaseContent, new StreamReader(databaseStream).ReadToEnd());
        }

        private void SetExpectedRequest(string url, string response)
        {
            this.httpResponses[url] = response;
        }

        private void HandleHttpRequest(IAsyncResult asyncResult)
        {
            try
            {
                var context = this.httpListener.EndGetContext(asyncResult);
                if (context.Request.Headers["APIKey"] != API_KEY)
                {
                    throw new InvalidOperationException("Bad API key.");
                }

                var url = context.Request.Url.ToString();

                if (this.httpResponses.ContainsKey(url))
                {
                    if (this.httpResponses[url] == null)
                    {
                        context.Response.ContentLength64 = 0;
                        context.Response.StatusCode = 404;
                        context.Response.OutputStream.Close();
                    }
                    else
                    {
                        var response = Encoding.UTF8.GetBytes(this.httpResponses[url]);
                        context.Response.ContentLength64 = response.Length;
                        context.Response.SendChunked = false;
                        context.Response.StatusCode = 200;
                        context.Response.OutputStream.Write(response);
                        context.Response.OutputStream.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentLength64 = 0;
                    context.Response.OutputStream.Close();
                    throw new InvalidOperationException("Unexpected HTTP request! " + url);
                }
            }
            finally
            {
                this.httpListener.BeginGetContext(new AsyncCallback(this.HandleHttpRequest), null);
            }
        }
    }
}