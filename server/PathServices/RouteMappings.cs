namespace PathApi.Server.PathServices
{
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Various PATH route metadata.
    /// </summary>
    internal static class RouteMappings
    {
        /// <summary>
        /// Dictionary mapping <see cref="Route"/> enums to their GTFS IDs.
        /// </summary>
        public static readonly ReadOnlyDictionary<string, Route> DatabaseIdToRoute =
            new ReadOnlyDictionary<string, Route>(new Dictionary<string, Route>()
            {
                { "1024", Route.Jsq33Hob },
                { "859", Route.Hob33 },
                { "860", Route.HobWtc },
                { "861", Route.Jsq33 },
                { "862", Route.NwkWtc },
                { "11048", Route.NptHob }
            });

        public static readonly ReadOnlyDictionary<RouteDirection, Direction> RouteDirectionToDirection =
            new ReadOnlyDictionary<RouteDirection, Direction>(new Dictionary<RouteDirection, Direction>()
            {
                { RouteDirection.ToNJ, Direction.ToNj },
                { RouteDirection.ToNY, Direction.ToNy }
            });
    }
}