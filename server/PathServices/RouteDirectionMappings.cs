namespace PathApi.Server.PathServices
{
    using PathApi.Server.PathServices.Models;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal static class RouteDirectionMappings
    {
        /// <summary>
        /// Dictionary mapping <see cref="RouteDirection"/> enums to direction string values used by PATH.
        /// </summary>
        public static readonly ReadOnlyDictionary<RouteDirection, string> RouteDirectionToDirectionKey =
            new ReadOnlyDictionary<RouteDirection, string>(new Dictionary<RouteDirection, string>()
            {
                { RouteDirection.ToNJ, "New Jersey" },
                { RouteDirection.ToNY, "New York" }
            });
    }
}
