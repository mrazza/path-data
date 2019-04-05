namespace PathApi.Server.PathServices
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Serilog;

    internal sealed class PathApiClient
    {
        private readonly Flags flags;

        public PathApiClient(Flags flags)
        {
            this.flags = flags;
        }

        public async Task<string> GetLatestChecksum(string startingChecksum)
        {
            string lastChecksum = startingChecksum;
            string currentChecksum;
            while (lastChecksum != (currentChecksum = await this.CheckForDbUpdate(lastChecksum)))
            {
                Log.Logger.Here().Information($"PATH DB checksum updated to {currentChecksum}.");
                lastChecksum = currentChecksum;
            }
            return currentChecksum;
        }

        public async Task<Stream> GetDatabaseAsStream(string checksum)
        {
            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage = this.BuildGetRequest(new Uri(string.Format(this.flags.PathDbDownloadUrl, checksum)));
                var httpResponse = await httpClient.SendAsync(httpRequestMessage);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpRequestException($"Invalid response from PATH API when downloading DB update: {httpResponse.StatusCode}");
                }
                else
                {
                    return await httpResponse.Content.ReadAsStreamAsync();
                }
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
                    { "APIKey", this.flags.PathApiKey }
                }
            };
        }

        private async Task<string> CheckForDbUpdate(string checksum)
        {
            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage = this.BuildGetRequest(new Uri(string.Format(this.flags.PathCheckDbUpdateUrl, checksum)));
                var httpResponse = await httpClient.SendAsync(httpRequestMessage);

                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return checksum;
                }
                else if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var stringResponse = await httpResponse.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(stringResponse);
                    return jsonResponse["data"].ToObject<CheckDbUpdateResponse>().Checksum;
                }
                else
                {
                    throw new HttpRequestException($"Invalid response from PATH API when checking for DB update: {httpResponse.StatusCode}");
                }
            }
        }

        private sealed class CheckDbUpdateResponse
        {
            public string Checksum { get; set; }
            public string Description { get; set; }
        }
    }
}