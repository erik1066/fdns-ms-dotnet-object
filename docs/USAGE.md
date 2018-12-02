# How to use the FDNS Object Service

## Running locally inside a container
You will need to have the following software installed to run this microservice:

- [Docker](https://docs.docker.com/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- **Windows Users**: This project uses `Make`. Please use [Cygwin](http://www.cygwin.com/) or the [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10) for running the commands in this README.

1. Open Bash or a Bash-like terminal
1. Build the container image by running `make docker-build`
1. Start the container by running `make docker-start`
1. Open a web browser and point to [http://127.0.0.1:9090/](http://127.0.0.1:9090/)

## Debugging using Visual Studio Code

You will need to have the following software installed to debug this microservice:

- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core SDK 2.1](https://www.microsoft.com/net/download)
- [Docker](https://docs.docker.com/install/)
- [Docker Compose](https://docs.docker.com/compose/install/)

1. Open a terminal window
1. `cd` to the `fdns-ms-dotnet-object/src` folder
1. Execute `docker-compose up -d`
1. Open Visual Studio Code
1. Select **File** > **OpenFolder** and select `fdns-ms-dotnet-object/src`
1. Open Visual Studio Code's **Debug** pane (shortcut key: `CTRL`+`SHIFT`+`D`)
1. Press the green arrow at the top of the **Debug** pane
1. Open a web browser and point to https://localhost:5001

## Debugging unit tests using Visual Studio Code

1. Open Visual Studio Code
1. Select **File** > **OpenFolder** and select `fdns-ms-dotnet-object/tests`
1. Open Visual Studio Code's **Explorer** pane (shortcut key: `CTRL`+`SHIFT`+`E`)
1. Open a Test classfile from the file list
1. Select **Debug test** at the top of any of the test methods or **Debug all tests** from the top of the class definition

## Running from the command line

To run the service from the command line:

1. Open a terminal window
1. `cd` to the `fdns-ms-dotnet-object/src` folder
1. Execute `docker-compose up -d`
1. Execute `dotnet restore`
1. Execute `dotnet build`
1. Execute `dotnet run`
1. Open a web browser and point to https://localhost:5001

To run tests from the command line:

1. Open Bash or a Bash-like terminal
1. `cd` to the `fdns-ms-dotnet-object/tests` folder
1. Execute `dotnet test`

## Experimenting with API operations

We use Swagger to automatically generate a live design document based on the underlying C# source code and XML code comments. Swagger allows developers to experiment with and test the API on a running microservice. It also shows you exactly what operations this service exposes to developers. To access the Swagger documentation, add `/swagger` to the end of the service's URL in your web browser, e.g. `https://localhost:5001/swagger`.

## Writing code to interact with this service

It's strongly recommended to use an SDK to interact with the Object microservice:

- [FDNS .NET Core SDK](https://github.com/erik1066/fdns-dotnet-sdk)
- [FDNS Java SDK](https://github.com/CDCGov/fdns-java-sdk)
- [FDNS JavaScript SDK](https://github.com/CDCGov/fdns-js-sdk)

If an SDK is unavailable for your language or cannot meet a specific need, then interacting with this service can be done by writing standard HTTP calls.

## Environment variable configuration

* `OBJECT_PORT`: A configurable port the application is set to run on
* `OBJECT_FLUENTD_HOST`: The [Fluentd](https://www.fluentd.org/) hostname
* `OBJECT_FLUENTD_PORT`: The [Fluentd](https://www.fluentd.org/) port number
* `OBJECT_PROXY_HOSTNAME`: The hostname of your environment for use with Swagger UI, ex: `api.my.org`
* `OBJECT_IMMUTABLE`: This is a `;` separated list of database/collection names which are immutable collections. Ex: `bookstore/customer;coffeeshop/order`

The following environment variables can be used to configure this microservice to use your OAuth2 provider:

* `OAUTH2_ACCESS_TOKEN_URI`: This is the introspection URL of your provider, ex: `https://hydra:4444/oauth2/introspect`
* `OAUTH2_PROTECTED_URIS`: This is a path for which routes are to be restricted, ex: `/api/1.0/**`
* `OAUTH2_CLIENT_ID`: This is your OAuth 2 client id with the provider
* `OAUTH2_CLIENT_SECRET`: This is your OAuth 2 client secret with the provider
* `SSL_VERIFYING_DISABLE`: This is an option to disable SSL verification, you can disable this when testing locally but this should be set to `false` for all production systems

For more information on using OAuth2 with this microservice, see **Authorization and security** at the end of this document.

## Quick-start guide

Let's try some example CRUD operations. Open the route titled "Inserts an object with a specified ID" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, and `customer` for the collection name.

> The database and collection will be created if they don't already exist

Enter the following Json into the request body:

```json
{ "name": "Sarah", "age": 32 }
```

Press **Execute**. You will see an HTTP 201 with the following response body:

```json
{ "_id" : { "$oid" : "5b85cfe5e17dec28c0cd2aa0" }, "name" : "Sarah", "age" : 32, "id" : "1" }
```

> The `$oid` property will be different for each insert

Notice the response headers. They include a URI to the location of the newly-created object:

```
access-control-allow-credentials: true
access-control-allow-origin: https://localhost:9090
content-type: text/plain; charset=utf-8
date: Tue, 28 Aug 2018 22:42:46 GMT
location: https://localhost:9090/api/1.0/bookstore/customer/1
server: Kestrel
transfer-encoding: chunked
vary: Origin
```

Let's retrieve the object we just inserted. Open the route titled "Gets an object" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, `customer` for the collection name, and press **Execute**. We receive the same response body:

```json
{ "_id" : { "$oid" : "5b85cfe5e17dec28c0cd2aa0" }, "name" : "Sarah", "age" : 32, "id" : "1" }
```

If you change the Id to 2 and press **Execute**, notice you will receive an HTTP 404 "Not Found" response.

Let's update this record and make Sarah a little older. Open the route titled "Updates an object" on the Swagger page and press the **Try it out** button. Fill in `1` for the object's Id, `bookstore` for the database name, and `customer` for the collection name. Enter the following Json into the request body:

```json
{ "name": "Sarah", "age": 42 }
```

Press **Execute**. We receive the updated object:

```json
{ "_id" : { "$oid" : "5b85cfe5e17dec28c0cd2aa0" }, "name" : "Sarah", "age" : 42, "id" : "1" }
```

> The PUT verb that maps to a database UPDATE operation is a wholesale replacement of the object. Whatever you submit overwrites the current object in the underlying database, except for the `_id` and `id` properties, which are immutable.

Let's now try to find some records to see how to use the Find route. Before we can do this, insert the following records with `id` values of 2, 3, 4, and 5:

```json
{ "name": "John", "age": 35 }
```
```json
{ "name": "Mary", "age": 65 }
```
```json
{ "name": "Ramona", "age": 75 }
```
```json
{ "name": "Maria", "age": 42 }
```

Open the route titled Finds one or more objects that match the specified criteria" on the Swagger page and press the **Try it out** button. The `findExpression` property is the most important and the most powerful. It allows using [MongoDB-style find query sytnax](https://docs.mongodb.com/manual/reference/method/db.collection.find/), which we strongly recommend referencing to get the most out of the Object service. Let's do a simple find on everyone whose age is 42. (Both Maria and Sarah should have `age` values of 42 if you've followed all of the previous instructions.) Enter the following into the `findExpression` box:

```json
{ age: 42 }
```

Fill in `bookstore` for the database name and `customer` for the collection name. Do not fill in any of the other inputs and press **Execute**. Notice two objects are returned in an array:

```json
[{ "_id" : { "$oid" : "5b85cfe5e17dec28c0cd2aa0" }, "name" : "Sarah", "age" : 42, "id" : "1" }, { "_id" : { "$oid" : "5b85d74ae17dec28c0cd2aa4" }, "name" : "Maria", "age" : 42, "id" : "1" }]
```

Let's find out who has an age less than 45. Change the `findExpression` to the following:

```json
{ age: { $lt: { 45 } }
```

Press **Execute** and observe the following matching records are returned in a Json array:

```json
[{ "_id" : { "$oid" : "5b85cfe5e17dec28c0cd2aa0" }, "name" : "Sarah", "age" : 42, "id" : "1" }, { "_id" : { "$oid" : "5b85d73ee17dec28c0cd2aa1" }, "name" : "John", "age" : 35, "id" : "1" }, { "_id" : { "$oid" : "5b85d74ae17dec28c0cd2aa4" }, "name" : "Maria", "age" : 42, "id" : "1" }]
```

## Authorization and Security

This microservice is configurable so that it can be secured via an OAuth2 provider. Each route on the microservice is mapped to a scope. Since the database and collection names are part of the route, and since an OAuth2 token is only valid for the scopes associated with that token, then using OAuth2 and scopes are an effective way to control data access. Consider if a client application called `bookstore` has the following scopes:

```
object.bookstore.customers.read
object.bookstore.customers.insert
object.bookstore.customers.update
object.bookstore.books.read
object.bookstore.books.insert
object.bookstore.books.update
object.bookstore.orders.read
object.bookstore.orders.insert
```

The `bookstore` client application can access `GET api/1.0/bookstore/customers/1` because that route is mapped to one of the above scopes. If the `bookstore` client application instead tried to access `GET api/1.0/coffeshop/orders/15`, they would be denied becase that route is not part of the scope associated with `bookstore`'s access token. CRUD operations can also be controlled at this level such that `PUT api/1.0/coffeshop/orders/8` would be denied, as this corresponds to an UPDATE operation and the above scopes do not include UPDATE rights on the `bookstore/orders` route. Each software application that uses Object can (and should!) be given a different set of scopes to ensure no other applications can access their data.

Using scopes in this manner allows the Object microservice to store data for many different applications without presenting authorization risks across those boundaries. Note that OAuth2 also allows a "resource-owner" consent flow for individual users. This mechanism could be used to grant read-only access to a specific data collection for data scientists and analysts, who could then access the data directory via the API in their preferred statistical tool, e.g. SAS.

An OAuth2-based authorization model with per-application scopes that map to routes and HTTP verbs is part of how the Object microservice can be used across the enterprise as part of an enterprise-grade "data lake."

Note that additional Foundation Services provide OAuth2 integration with LDAP and ActiveDirectory.

__Scopes__: This application uses the following scope: `object.*`