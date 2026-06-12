# Demo App

## Requirements

create app using minimal api with following endpoints
- /city to retuns cities from Demo.Cities
- /city/usa to retuns USA cities from Demo.Cities
- /city/{cityName}/location to return coordionats using OpenMeteo.Client call. 
-  /city/{cityName}/population to return population using OpenMeteo.Client call. 
 OpenMeteo.Client responsse  should be cached in SQLite database to prevent api call.

## Implementation
- use qa versions of internal NuGets if it is available.
- use SimpleRepo for dataaccess.