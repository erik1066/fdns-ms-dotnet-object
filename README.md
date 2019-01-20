# Object Microservice: A REST backing service for database operations

The "Object" microservice is a [backing service](https://12factor.net/backing-services) with a RESTful API for NoSQL database operations. Supported operations include CRUD, search, data pipelines, aggregation, and bulk imports. MongoDB is the underlying database technology.

[![Build Status](https://travis-ci.org/erik1066/fdns-ms-dotnet-object.svg?branch=master)](https://travis-ci.org/erik1066/fdns-ms-dotnet-object)
[![Docker Pulls](https://img.shields.io/docker/pulls/biohazard501/fdns-ms-dotnet-object.svg)](https://hub.docker.com/r/biohazard501/fdns-ms-dotnet-object/)

The Object service provides the following benefits:
- Language-agnostic: Any language can send and consume HTTP requests. No Mongo SDK is required.
- Centralized data access for the organization: There's one, easy-to-remember URL
- Consistent API: Searching, data aggregation, etc. are consistent across data sources by-design
- Consistent authorization: Authorization is handled via OAuth2 scopes
- Design simplification: Developers can avoid ORMs and treat CRUD operations as HTTP requests
- Scalability: As a microservice based on .NET Core, it's efficient, fast, and horizontally scalable

> This repository represents an unofficial re-implementation of the U.S. Centers for Disease Control and Prevention's [Object microservice](https://github.com/CDCgov/fdns-ms-object) using [ASP.NET Core 2.2](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-2.2?view=aspnetcore-2.2) instead of [Java Spring](https://spring.io/).

## Get started in 30 seconds
Clone this repo and run the microservice in a Docker container in under 30 seconds (this assumes you have `make` command-line tools installed):

```bash
git clone https://github.com/erik1066/fdns-ms-dotnet-object.git
cd fdns-ms-dotnet-object
make docker-build
make docker-start
```

Then navigate to http://localhost:9090.

## Documentation
[README.md](docs/README.md) explains how to containerize the microservice, debug it, run its unit tests, and contains a quick-start guide for interacting with the microservice once it's running. It also explains how to use OAuth2 scopes to provide course-grained authorization around the microservice's API.

- [Running this microservice locally inside a container](docs/README.md#running-this-microservice-locally-inside-a-container)
- [Debugging using Visual Studio Code](docs/README.md#debugging-using-visual-studio-code)
- [Debugging unit tests using Visual Studio Code](docs/README.md#debugging-unit-tests-using-visual-studio-code)
- [Running from the command line without containerization](docs/README.md#running-from-the-command-line-without-containerization)
- [Analyzing code for quality and vulnerabilities](docs/README.md#analyzing-code-for-quality-and-vulnerabilities)
- [Readiness and liveness checks](docs/README.md#readiness-and-liveness-checks)
- [Experimenting with API operations](docs/README.md#experimenting-with-api-operations)
- [Writing code to interact with this service](docs/README.md#writing-code-to-interact-with-this-service)
- [Environment variable configuration](docs/README.md#environment-variable-configuration)
- [Quick-start guide](docs/README.md#quick-start-guide)
- [Data pipelining](docs/README.md#data-pipelining)
- [Bulk importing of Json arrays and Csv files](docs/README.md#bulk-importing-of-json-arrays-and-csv-files)
- [Authorization and Security](docs/README.md#authorization-and-security)

## License
The repository utilizes code licensed under the terms of the Apache Software License and therefore is licensed under ASL v2 or later.

This source code in this repository is free: you can redistribute it and/or modify it under the terms of the Apache Software License version 2, or (at your option) any later version.

This source code in this repository is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the Apache Software License for more details.

You should have received a copy of the Apache Software License along with this program. If not, see https://www.apache.org/licenses/LICENSE-2.0.html.

The source code forked from other open source projects will inherit its license.