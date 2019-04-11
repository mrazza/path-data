# PATH Data API

This repository contains the contract and server-side implementation of an API that exposes data about the Port Authority Trans-Hudson Rapid Transit System.

This software is not endorsed nor supported by the Port Authority of New York and New Jersey.

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
      "lineName": "World Trade Center",
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-11T03:21:29Z",
      "lastUpdated": "2019-04-11T03:08:04Z",
      "status": "ON_TIME"
    },
    {
      "lineName": "World Trade Center",
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-11T03:56:29Z",
      "lastUpdated": "2019-04-11T03:08:04Z",
      "status": "ON_TIME"
    },
    {
      "lineName": "Newark",
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-11T03:19:46Z",
      "lastUpdated": "2019-04-11T03:08:04Z",
      "status": "ON_TIME"
    },
    {
      "lineName": "Newark",
      "lineColors": [
        "#D93A30"
      ],
      "projectedArrival": "2019-04-11T03:38:30Z",
      "lastUpdated": "2019-04-11T03:08:04Z",
      "status": "ON_TIME"
    }
  ]
}
```

# Demo

You can query the API via your web browser by navigating to a valid endpoint. For example the [9th street station realtime data](https://path.api.razza.dev/v1/stations/ninth_street/realtime).

A simple web app using the realtime arrival data can be found [here](https://jsfiddle.net/qkp7g8ze/embedded/result/).
