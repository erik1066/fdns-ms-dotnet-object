# FDNS Object Microservice
A microservice for providing an abstraction layer over a NoSQL database engine, with CRUD operations mapped to HTTP verbs. Arbitrary Json is supported without requiring typed objects, enabling reusability across applications. The Object Service is one of three microservices that form the FDNS Data Lake. This repository represents an unofficial re-implementation of the U.S. Centers for Disease Control and Prevention's [Object microservice](https://github.com/CDCgov/fdns-ms-object) using [ASP.NET Core 2.1](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-2.1?view=aspnetcore-2.1) instead of [Java Spring](https://spring.io/).

## Purpose

Use when you want higher-level microservices to be decoupled from database-specific implementation details and/or when you want to present data scientists a consistent API contract.

Benefits: Using `fdns-ms-dotnet-object` ensures the underlying database engine can change (to a degree) without requiring rework on higher-level microservices and avoids coupling higher-level microservices to complex ORMs and database-specific SDKs. For analysts and data scientists, they can avoid the need to find, learn, and then use many APIs from across the organization to do their work; instead, there is a single, consistent API.

## Quick-start guide and developer docs
[USAGE.md](docs/USAGE.md) explains how to containerize the microservice, debug it, run its unit tests, and contains a quick-start guide for interacting with the microservice once it's running. It also explains how to use OAuth2 scopes to provide course-grained authorization around the microservice's API.

## License
The repository utilizes code licensed under the terms of the Apache Software License and therefore is licensed under ASL v2 or later.

This source code in this repository is free: you can redistribute it and/or modify it under the terms of the Apache Software License version 2, or (at your option) any later version.

This source code in this repository is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the Apache Software License for more details.

You should have received a copy of the Apache Software License along with this program. If not, see https://www.apache.org/licenses/LICENSE-2.0.html.

The source code forked from other open source projects will inherit its license.