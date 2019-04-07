namespace PathApi.Server.PathServices
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository that provides access to the latest version of the PATH SQLite database.
    /// </summary>
    internal interface IPathSqlDbRepository
    {
        /// <summary>
        /// An event that is triggered when the PATH SQLite database is downloaded or updated.
        /// </summary>
        event EventHandler<EventArgs> OnDatabaseUpdate;

        /// <summary>
        /// Gets the encrypted service bus key from the PATH database.
        /// </summary>
        /// <returns>A task returning the encrypted service bus key.</returns>
        Task<string> GetServiceBusKey();
    }
}