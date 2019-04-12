namespace PathApi.Server.PathServices.Models
{
    using System;
    using System.Collections.Generic;
    using PathApi.V1;

    /// <summary>
    /// Data model representing a route in the PATH database.
    /// </summary>
    internal sealed class RouteLine
    {
        // The route this RouteLine represents.
        public Route Route { get; set; }

        // The GTFS ID of this route.
        public string Id { get; set; }

        // The long name (that does not account for direction) of this route.
        public string LongName { get; set; }

        // The display name for this route.
        public string DisplayName { get; set; }

        // The headsign for trains that use this route and direction.
        public string Headsign { get; set; }

        // The color of the headsign/train for this route.
        public string Color { get; set; }

        // The direction of travel along this route.
        public RouteDirection Direction { get; set; }
    }
}