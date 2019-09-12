namespace PathApi.Server.PathServices
{
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// An API client that interfaces with the PATH RESTful API.
    /// </summary>
    internal interface IPathApiClient
    {
        /// <summary>
        /// Gets the latest checksum of the PATH SQLite database provided a starting checksum value.
        /// </summary>
        /// <param name="startingChecksum">The checksum used when looking for a new checksum.</param>
        /// <returns>A task returning the new checksum if an update exists; otherwise, the starting checksum.</returns>
        Task<string> GetLatestChecksum(string startingChecksum);

        /// <summary>
        /// Downloads the PATH SQLite database with the provided checksum.
        /// </summary>
        /// <param name="checksum">The checksum identifying the PATH database to download.</param>
        /// <returns>A task returning a <see cref="Stream"/> to the binary representation of the PATH database.</returns>
        Task<Stream> GetDatabaseAsStream(string checksum);

        /// <summary>
        /// Retrieves a JWT authentication token to be used to connect to the SignalR realtime hub.
        /// </summary>
        /// <param name="tokenBrokerUrl">The value of <c>rt_TokenBrokerUrl</c> for the environment.</param>
        /// <param name="tokenValue">The value of <c>rt_TokenValue</c> for the environment.</param>
        /// <param name="station">The target <see cref="Station"/>.</param>
        /// <param name="direction">The target <see cref="RouteDirection"/>.</param>
        /// <returns>Returns a <see cref="SignalRToken"/> instance containing the JWT and SignalR instance URL.</returns>
        Task<SignalRToken> GetToken(string tokenBrokerUrl, string tokenValue, Station station, RouteDirection direction);
    }
}