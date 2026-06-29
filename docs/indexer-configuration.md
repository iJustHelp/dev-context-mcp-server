# Indexer Configuration
 
## appsettings.json

```json
 "DevContextMcp": {
    "DatabasePath": "../../../../../database/docs.db",
    "IndexerSource": {
      "NugetsPath": "../../../../../demo/data/indexer/nugets"
    },
    "NugetPackages": [
      {
        "Name": "publicNuget",
        "Environment": "public",
        "ServiceIndex": "https://api.nuget.org/v3/index.json",
        "MaxPackages": 100
      },
      {
        "Name": "prodNuget",
        "Environment": "prod",
        "ServiceIndex": "../../../../../demo/data/nuget-repos/prod",
        "MaxPackages": 100
      },
      {
        "Name": "qaNuget",
        "Environment": "qa",
        "ServiceIndex": "../../../../../demo/data/nuget-repos/qa",
        "MaxPackages": 100
      }
    ],
    "Indexing": {
      "MaxPackageBytes": 104857600,
      "MaxDocumentBytes": 20971520,
      "MaxArchiveEntries": 10000,
      "MaxExtractedBytes": 524288000,
      "MaxCompressionRatio": 200,
      "MaxDocumentChars": 4000,
      "PackageDownloadTimeout": "00:02:00"
    }
  }
```

where:

- `DatabasePath`: path to the SQLite database file used by the indexer.

- `IndexerSource`: source configuration
  - `NugetsPath`: root folder containing NuGet JSON configuration files

- `NugetPackages`: list of configured NuGet sources by `Environment`
  - `Name`: unique identifier for the NuGet source.
  - `Environment`: environment slug for the source used in library IDs and package selection.
  - `ServiceIndex`: NuGet v3 service endpoint URI or local folder path containing `.nupkg` files.
  - `MaxPackages`: maximum number of package policy entries that may be applied to this source.

- `Indexing`: list of parameters for the indexing process
    - `MaxPackageBytes`: maximum allowed size for a downloaded package archive.
    - `MaxDocumentBytes`: maximum allowed size for a documentation file.
    - `MaxArchiveEntries`: maximum number of entries allowed inside an archive.
    - `MaxExtractedBytes`: maximum total bytes extracted from archives or documents during indexing.
    - `MaxCompressionRatio`: maximum allowed compression ratio for archive entries.
    - `MaxDocumentChars`: maximum number of characters extracted from a document for indexing.
    - `PackageDownloadTimeout`: maximum time allowed to download a package.

## NuGet Configuration

Each indexed NuGet source should have a JSON configuration file.

```json
{
  "Environment": "public",
  "PackageId": "Formula.SimpleRepo",
  "MaxVersionsPerPackage": 10,
}
```

where:

- `Environment`: is one of the values defined in `NugetPackages` in `appsettings.json` (for example: `public`, `prod`, `qa`).
- `PackageId`: full NuGet package name.
- `MaxVersionsPerPackage`: maximum number of versions for indexing of the package.

Configuration is the source of truth: the package files present for an environment
define exactly what is indexed. To remove a package, delete its JSON file. On the
next successful run the indexer prunes any package stored for that source whose id
is no longer in the configuration (including when an environment has no package
files at all). Deletions are skipped for a run whose feed discovery fails, so an
unreachable feed never wipes already-indexed data.
