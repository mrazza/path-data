namespace PathApi.Server.PathServices
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;

    /// <summary>
    /// A self-updating repository that provides access to the latest version of the static PATH data.
    /// </summary>
    internal interface IPathDataRepository
    {
        /// <summary>
        /// An event that is triggered when the PATH data snapshot is downloaded or updated.
        /// </summary>
        event EventHandler<EventArgs> OnDataUpdate;

        /// <summary>
        /// Gets the encrypted service bus key from the PATH database.
        /// </summary>
        /// <returns>A task returning the encrypted service bus key.</returns>
        Task<string> GetServiceBusKey();

        /// <summary>
        /// Gets the encrypted JWT token broker URL from the PATH database.
        /// </summary>
        /// <returns>A task returning the encrypted JWT token broker URL.</returns>
        Task<string> GetTokenBrokerUrl();

        /// <summary>
        /// Gets the encrypted token value from the PATH database.
        /// </summary>
        /// <returns>A task returning the encrypted token value.</returns>
        Task<string> GetTokenValue();

        /// <summary>
        /// Gets information about the specified station.
        /// </summary>
        /// <returns>A task returning the station information for the specified station.</returns>
        Task<List<Stop>> GetStops(Station station);

        /// <summary>
        /// Gets all the routes (specifically RouteLines).
        /// </summary>
        /// <returns>A task returning a collection of RouteLines.</returns>
        Task<List<RouteLine>> GetRoutes();

        /// <summary>
        /// Gets a route from the specified headsign name and color pair.
        /// </summary>
        /// <returns>A task returning the route for the specified train.</returns>
        Task<RouteLine> GetRouteFromTrainHeadsign(string headsignName, IEnumerable<string> headsignColors);
    }
}