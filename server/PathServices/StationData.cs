
using System.Collections.ObjectModel;
using System.Collections.Generic;
using PathApi.V1;

namespace PathApi.Server.PathServices
{
    internal static class StationData
    {
        public static readonly ReadOnlyDictionary<Station, string> StationToShortName = new ReadOnlyDictionary<Station, string>(new Dictionary<Station, string>()
        {
            { Station.Newark, "nwk" },
            { Station.Harrison, "har" },
            { Station.JournalSquare, "jsq" },
            { Station.GroveStreet, "grv" },
            { Station.ExchangePlace, "EXP" },
            { Station.WorldTradeCenter, "wtc" },
            { Station.Newport, "new" },
            { Station.Hoboken, "HOB" },
            { Station.ChristopherStreet, "chr" },
            { Station.NinthStreet, "09s" },
            { Station.FourteenthStreet, "14s" },
            { Station.TwentyThirdStreet, "23s" },
            { Station.ThirtyThirdStreet, "33s" }
        });
    }
}