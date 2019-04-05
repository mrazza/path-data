namespace PathApi.Server.PathServices
{
    using System;
    using System.Collections.Generic;

    internal sealed class RealtimeData
    {
        public DateTime ExpectedArrival { get; set; }
        public string ArrivalTimeMessage { get; set; }
        public List<string> LineColors { get; set; }
        public string HeadSign { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}