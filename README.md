# FDNS Object Microservice: A REST backing service for database operations

"Object" is a backing service with a simple RESTful API for NoSQL database operations. Supported operations include CRUD, search, data pipelines, aggregation, and bulk imports. MongoDB is the underlying database technology.

[![Build Status](https://travis-ci.org/erik1066/fdns-ms-dotnet-object.svg?branch=master)](https://travis-ci.org/erik1066/fdns-ms-dotnet-object)
[![Docker Pulls](https://img.shields.io/docker/pulls/biohazard501/fdns-ms-dotnet-object.svg)](https://hub.docker.com/r/biohazard501/fdns-ms-dotnet-object/)

Backing services are [factor #4 of the 12-factor app methodology](https://12factor.net/backing-services). Applied to database operations, the result is that many languages can be used to interact with a Mongo database, even those languages lacking a Mongo SDK. Analysts, data scientists, and developers can learn a simple HTTP REST API to work with the organization's data using the languages and tools of their choice. The Object service can be used as part of a data lake that can serve an entire organization - or as just one component of a smaller application.

When used as part of a data lake, the Object service provides the following benefits:
- Centralized data access for the whole organization: Everyone just needs to learn one service endpoint
- A consistent API: Searching, data aggregation, etc. are by-design going to be consistent regardless of the data source
- A consistent security protocol: All authenticiation and authorization is handled via OAuth2

The alternative to a data lake is allowing engineering teams to build their own APIs for each of their own services and systems. The result is that there will be hundreds of APIs at many different service endpoints with varying REST implementations and authentication schemes. Organizations can still enforce coherent REST standards and authentication protocols via management practices, but there is real effort in carrying out that enforcement, and the level of effort rises as the organization grows in size. A data lake built as a set of backing services eliminates these problems.

The Object service is designed as a microservice so that it can scale rapidly and efficiently when used as part of a data lake.

> This repository represents an unofficial re-implementation of the U.S. Centers for Disease Control and Prevention's [Object microservice](https://github.com/CDCgov/fdns-ms-object) using [ASP.NET Core 2.2](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-2.2?view=aspnetcore-2.2) instead of [Java Spring](https://spring.io/).


## Documentation
[USAGE.md](docs/USAGE.md) explains how to containerize the microservice, debug it, run its unit tests, and contains a quick-start guide for interacting with the microservice once it's running. It also explains how to use OAuth2 scopes to provide course-grained authorization around the microservice's API.

- [Running this microservice locally inside a container](docs/USAGE.md#running-this-microservice-locally-inside-a-container)
- [Debugging using Visual Studio Code](docs/USAGE.md#debugging-using-visual-studio-code)
- [Debugging unit tests using Visual Studio Code](docs/USAGE.md#debugging-unit-tests-using-visual-studio-code)
- [Running from the command line without containerization](docs/USAGE.md#running-from-the-command-line-without-containerization)
- [Readiness and liveness checks](docs/USAGE.md#readiness-and-liveness-checks)
- [Experimenting with API operations](docs/USAGE.md#experimenting-with-api-operations)
- [Writing code to interact with this service](docs/USAGE.md#writing-code-to-interact-with-this-service)
- [Environment variable configuration](docs/USAGE.md#environment-variable-configuration)
- [Quick-start guide](docs/USAGE.md#quick-start-guide)
- [Data pipelining](docs/USAGE.md#data-pipelining)
- [Bulk importing of Json arrays and Csv files](docs/USAGE.md#bulk-importing-of-json-arrays-and-csv-files)
- [Authorization and Security](docs/USAGE.md#authorization-and-security)

## License
The repository utilizes code licensed under the terms of the Apache Software License and therefore is licensed under ASL v2 or later.

This source code in this repository is free: you can redistribute it and/or modify it under the terms of the Apache Software License version 2, or (at your option) any later version.

This source code in this repository is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the Apache Software License for more details.

You should have received a copy of the Apache Software License along with this program. If not, see https://www.apache.org/licenses/LICENSE-2.0.html.

The source code forked from other open source projects will inherit its license.