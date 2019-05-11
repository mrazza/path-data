namespace PathApi.Server.PathServices
{
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
    }
}