namespace PathApi.Server.PathServices
{
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository containing the realtime predicted arrivals of PATH trains.
    /// </summary>
    internal interface IRealtimeDataRepository
    {
        /// <summary>
        /// Gets the latest realtime train arrival data for the specified station.
        /// </summary>
        /// <param name="station">The station to get realtime arrival data for.</param>
        /// <returns>A collection of arriving trains.</returns>
        Task<IEnumerable<RealtimeData>> GetRealtimeData(Station station);
    }
}