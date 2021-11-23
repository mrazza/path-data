namespace PathApi.Server.PathServices
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using Serilog;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// An API client that interfaces with the PATH RESTful API.
    /// </summary>
    internal sealed class PathApiClient : IPathApiClient
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();
        private readonly Flags flags;

        /// <summary>
        /// Constructs a new instance of the <see cref="PathApiClient"/>.
        /// </summary>
        /// <param name="flags">The <see cref="Flags"/> instance containing the apps configuration.</param>
        public PathApiClient(Flags flags)
        {
            this.flags = flags;
        }

        /// <summary>
        /// Gets the latest checksum of the PATH SQLite database provided a starting checksum value.
        /// </summary>
        /// <param name="startingChecksum">The checksum used when looking for a new checksum.</param>
        /// <returns>A task returning the new checksum if an update exists; otherwise, the starting checksum.</returns>
        public async Task<string> GetLatestChecksum(string startingChecksum)
        {
            string lastChecksum = startingChecksum;
            string currentChecksum;
            while (lastChecksum != (currentChecksum = await this.CheckForDbUpdate(lastChecksum)))
            {
                Log.Logger.Here().Information("PATH DB checksum updated to {currentChecksum}.", currentChecksum);
                lastChecksum = currentChecksum;
            }
            return currentChecksum;
        }

        /// <summary>
        /// Downloads the PATH SQLite database with the provided checksum.
        /// </summary>
        /// <param name="checksum">The checksum identifying the PATH database to download.</param>
        /// <returns>A task returning a <see cref="Stream"/> to the binary representation of the PATH database.</returns>
        public async Task<Stream> GetDatabaseAsStream(string checksum)
        {
            var httpRequestMessage = this.BuildGetRequest(new Uri(string.Format(this.flags.PathDbDownloadUrl, checksum)));
            var httpResponse = await SharedHttpClient.SendAsync(httpRequestMessage);

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Invalid response from PATH API when downloading DB update: {httpResponse.StatusCode}");
            }
            else
            {
                return await httpResponse.Content.ReadAsStreamAsync();
            }
        }

        public async Task<SignalRToken> GetToken(string tokenBrokerUrl, string tokenValue, Station station, RouteDirection direction)
        {
            var request = new { station = StationMappings.StationToSignalRTokenName[station], direction = RouteDirectionMappings.RouteDirectionToDirectionKey[direction] };
            var requestJson = JsonConvert.SerializeObject(request);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, tokenBrokerUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

            var httpResponse = await SharedHttpClient.SendAsync(httpRequestMessage);

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Invalid response from PATH API when requesting JWT token: {httpResponse.StatusCode}");
            }
            else
            {
                var responseJson = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SignalRToken>(responseJson);
            }
        }

        private HttpRequestMessage BuildGetRequest(Uri uri)
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = uri,
                Headers =
                {
                    { "apikey", this.flags.PathApiKey },
                    { "appname", this.flags.PathAppName },
                    { "appversion", this.flags.PathAppVersion },
                    { "user-agent", this.flags.PathUserAgent }
                }
            };
        }

        private async Task<string> CheckForDbUpdate(string checksum)
        {
            var httpRequestMessage = this.BuildGetRequest(new Uri(this.flags.PathCheckDbUpdateUrl));
            httpRequestMessage.Headers.Add("dbchecksum", checksum);
            var httpResponse = await SharedHttpClient.SendAsync(httpRequestMessage);

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return checksum;
            }
            else if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                var stringResponse = await httpResponse.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(stringResponse);
                return jsonResponse["Data"]?.ToObject<CheckDbUpdateResponse>()?.DbUpdate?.Checksum ?? checksum;
            }
            else
            {
                throw new HttpRequestException($"Invalid response from PATH API when checking for DB update: {httpResponse.StatusCode}");
            }
        }

        private sealed class CheckDbUpdateResponse
        {
            public sealed class DbUpdateInfo
            {
                public string Checksum { get; set; }
                public string Description { get; set; }
            }

            public DbUpdateInfo DbUpdate { get; set; }
        }
    }
}