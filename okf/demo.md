---
type: Reference
title: Demo Apps & Data
description: The sample NuGet apps and prebuilt feeds/manifests used to exercise indexing and retrieval end to end.
resource: demo
tags: [demo, sample, nuget, fixtures]
timestamp: 2026-07-15T00:00:00Z
---

# Demo Apps & Data

The `demo/` folder provides a self-contained, controlled dataset so indexing and
retrieval can be exercised without private feeds. It backs the checked-in demo
configuration described in [Indexer Configuration](indexer-configuration.md).

# Schema

## Demo apps — `demo/nuget-apps/`

| App | What it is |
|-----|-----------|
| **Demo.Cities** | A dependency-free .NET 10 library packaged as a NuGet (`ICityService`/`CityService` + DI extension). Exists as `prod/` and `qa/` variants; the qa variant adds `IUsaCityService`/`UsaCityService` and evolves across versions. A controlled package to index and query. |
| **OpenMeteo.Api.Client** | A NuGet-packaged, NSwag-generated typed HTTP client for the Open-Meteo Geocoding API, with a DI registration extension and its own test project. Represents a real-world generated API-client package. |

## Demo data — `demo/data/`

| Location | Contents |
|----------|----------|
| `demo/data/nuget-repos/prod/` | Prebuilt `.nupkg` feed: Demo.Cities 1.0.0, OpenMeteo.Api.Client 1.0.0. |
| `demo/data/nuget-repos/qa/` | Prebuilt `.nupkg` feed: Demo.Cities 1.1.0, 1.1.1, 2.0.1, 2.1.1. |
| `demo/data/indexer/nugets/{prod,qa,public}/` | Per-environment indexer policy manifests, e.g. `qa/Demo.Cities.json` = `{ "Environment": "qa", "PackageId": "Demo.Cities" }`; also `prod/OpenMeteo.Api.Client.json`, `public/Formula.SimpleRepo.json`. |

Together these demonstrate multi-environment and multi-version resolution. The
demo database is produced by running the indexer against this data — see
[Operations](operations.md).

# Citations

[1] [demo/](../demo/)
[2] [README.md](../README.md)
