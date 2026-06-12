##Demo App

create app using minimal api with following endpoints
- /city to retuns cities from Demo.Cities
- /city/usa to retuns USA cities from Demo.Cities
- /city/?name=Chicago to return coordionats and population using OpenMeteo.Client call. Coordionats and population should be cached in SQLite database to prevent api call.

## Implementation
- use qa versions of internal NuGets if it is available
