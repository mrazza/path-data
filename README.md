# PATH Data API

This repository contains the contract and server-side implementation of an API that exposes data about the Port Authority Trans-Hudson Rapid Transit System.

This software is not endorsed nor supported by the Port Authority of New York and New Jersey.

# Using This Software

Prefer to use the publically exposed API (below) rather than running this software yourself. Due to Azure Service Bus subscriber limits, running this application without a static subscriber ID or with many subscriber IDs could consume the available topic subscriber quota. For this reason, running this software independently is not recommended. See [this article](https://medium.com/@mrazza/programmatic-path-real-time-arrival-data-5d0884ae1ad6#ab14) for more information.

# Public APIs

The APIs provided by this service can be found at:
- HTTP: https://path.api.razza.dev/...
- gRPC: path.grpc.razza.dev (running on the default port, 443)

## List Stations

HTTP: `https://path.api.razza.dev/v1/stations`

```
{
  "stations": [
    {
      "station": "NEWARK",
      "id": "26733",
      "name": "Newark",
      "coordinates": {
        "latitude": 40.73454,
        "longitude": -74.16375
      },
      "platforms": [
        // ...
      ],
      "entrances": [
        // ...
      ],
      "timezone": "America/New_York"
    },
    // ...
  ]
}
```

## Get Station

HTTP: `https://path.api.razza.dev/v1/stations/{station_name}` where `{station_name}` is one of:

```
newark
harrison
journal_square
grove_street
exchange_place
world_trade_center
newport
hoboken
christopher_street
ninth_street
fourteenth_street
twenty_third_street
thirty_third_street
```

HTTP: `https://path.api.razza.dev/v1/stations/harrison`

```
{
  "station": "HARRISON",
  "id": "26729",
  "name": "Harrison",
  "coordinates": {
    "latitude": 40.73942,
    "longitude": -74.15587
  },
  "platforms": [
    {
      "id": "781720",
      "name": "Harrison",
      "coordinates": {
        "latitude": 40.73942,
        "longitude": -74.15587
      }
    },
    {
      "id": "781721",
      "name": "Harrison",
      "coordinates": {
        "latitude": 40.73942,
        "longitude": -74.15587
      }
    }
  ],
  "entrances": [
    {
      "id": "782492",
      "name": "Harrison",
      "coordinates": {
        "latitude": 40.739,
        "longitude": -74.1558
      }
    },
    {
      "id": "782493",
      "name": "Harrison",
      "coordinates": {
        "latitude": 40.7395,
        "longitude": -74.1559
      }
    }
  ],
  "timezone": "America/New_York"
}
```

## Realtime Arrivals

HTTP: `https://path.api.razza.dev/v1/stations/<station_name>/realtime`

```
{
  "upcomingTrains": [
    {
      "lineColors": [
        "#65C100"
      ],
      "projectedArrival": "2019-04-13T01:56:00Z",
      "lastUpdated": "2019-04-13T01:52:05Z",
      "status": "ON_TIME",
      "headsign": "Hoboken",
      "route": "HOB_WTC",
      "routeDisplayName": "World Trade Center - Hoboken",
      "direction": "TO_NJ"
    },
    {
      "lineColors": [
        "#65C100"
      ],
      "projectedArrival": "2019-04-13T02:11:00Z",
      "lastUpdated": "2019-04-13T01:52:05Z",
      "status": "ON_TIME",
      "headsign": "Hoboken",
      "route": "HOB_WTC",
      "routeDisplayName": "World Trade Center - Hoboken",
      "direction": "TO_NJ"
    },
    {
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-13T02:01:00Z",
      "lastUpdated": "2019-04-13T01:52:05Z",
      "status": "ON_TIME",
      "headsign": "Newark",
      "route": "NWK_WTC",
      "routeDisplayName": "World Trade Center - Newark",
      "direction": "TO_NJ"
    },
    {
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-13T02:16:00Z",
      "lastUpdated": "2019-04-13T01:52:05Z",
      "status": "ON_TIME",
      "headsign": "Newark",
      "route": "NWK_WTC",
      "routeDisplayName": "World Trade Center - Newark",
      "direction": "TO_NJ"
    }
  ]
}
```

# Demo

You can query the API via your web browser by navigating to a valid endpoint. For example the [9th street station realtime data](https://path.api.razza.dev/v1/stations/ninth_street/realtime).

A simple web app using the realtime arrival data can be found [here](https://jsfiddle.net/qkp7g8ze/embedded/result/).

# Versioning

New fields and features will continue to be added to `v1` of the API. No fields will be removed and no breaking changes will be made to `v1`. Any breaking changes will result in a version number increment and the previous API version will run along side the new version for at least 30 days. There are a number of external consumers of this API.
