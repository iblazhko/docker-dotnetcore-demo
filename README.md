# docker-dotnetcore

This repository represents a serie of steps one may take to build a scalable
multi-component system implemented in .NET Core and all hosted in Docker
environment.

Aim is to have a realistic demo, but still simple enough to be able to present
it in one hour session.

## System Overview

For the purpose of this demo, we'll build a system that consists of the
following parts:

- **API**. A very simple API that provides access to collection of
  `key-value` pairs.
- **Command-line client**. Non-interactive client that continuously sends
  commands to the API.
- **MongoDB** will be used as database server to store our `key-value` pairs.
- **Elastic Search** will be storing logs from both API and the client.

All the components will be running in
[Docker](https://www.docker.com/ "Docker").

## Technology Stack

### .NET Core

REST API and the client will be implemented using .NET Core.

From [Wikipedia](https://en.wikipedia.org/wiki/.NET_Framework#.NET_Core ".NET Core")

>.NET Core is a cross-platform free and open-source managed software framework
>similar to .NET Framework. It consists of CoreCLR, a complete cross-platform
>runtime implementation of CLR, the virtual machine that manages the execution
>of .NET programs. CoreCLR comes with an improved just-in-time compiler,
>called RyuJIT. .NET Core also includes CoreFX, which is a partial fork of FCL.
>
>.NET Core's command-line interface offers an execution entry point for
>operating systems and provides developer services like compilation and
>package management.
>
>.NET Core supports four cross-platform scenarios: ASP.NET Core web apps,
>command-line apps, libraries, and Universal Windows Platform apps.
>It does not implement Windows Forms or WPF which render the standard GUI
>for desktop software on Windows. .NET Core is also modular, meaning that
>instead of assemblies, developers work with NuGet packages.
>Unlike .NET Framework, which is serviced using Windows Update, .NET Core
>relies on its package manager to receive updates.
>

.NET Core is an open-source project hosted on GitHub,
<https://github.com/dotnet/core>; and you can find overview, downloads,
learning and documentation resources at
<https://www.microsoft.com/net/core>.

.NET Core 1.0 was released on 27 June 2016, along with
Visual Studio 2015 Update 3, which enables .NET Core development.
.NET Core 1.1 was announced later that year <https://blogs.msdn.microsoft.com/dotnet/2016/11/16/announcing-net-core-1-1/>,
and was included in Visual Studio 2017.

Note that pre-release versions of .NET Core and version 1.0 included support
for JSON-based project format (`project.json`). The `project.json` is no more
in .NET Core version 1.1 and Visual Studio 2017, Microsoft decided to
go back to XML-based `.csproj` with some optimizations to reduce verbosity.

- <https://blogs.msdn.microsoft.com/dotnet/2016/11/16/announcing-net-core-tools-msbuild-alpha/>
- <https://www.stevejgordon.co.uk/project-json-replaced-by-csproj>
- <https://csharp.christiannagel.com/2017/01/10/dotnetcoreversionissues/>

### Docker

From [the horse's mouth](https://www.docker.com/what-docker "What is Docker"):

>Docker is the world’s leading software container platform.
>Developers use Docker to eliminate “works on my machine”
>problems when collaborating on code with co-workers.
>Operators use Docker to run and manage apps side-by-side
>in isolated containers to get better compute density.
>Enterprises use Docker to build agile software delivery
>pipelines to ship new features faster, more securely and
>with confidence for both Linux and Windows Server apps.

Docker is all about *containers*. A container is a piece
of software running in isolation.

Containers only bundle dependencies required to run
the software, unlike a Virtual Machine that isolates
full operating system.

## Step 1. Solution Skeleton


Create `src` and `build` directories, so that the top-level structure
looks like this:

    build\
    src\
    README.md

In command prompt, change directory to the `<project directory>\src`, and
run following commands:

    dotnet new sln --name DockerDotNetCore
    dotnet new classlib --name Infrastructure.Logging
    dotnet new webapi --name WebApi
    dotnet new xunit --name WebApi.Test.Unit
    dotnet new console --name Client

    dotnet sln .\DockerDotNetCore.sln add .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet sln .\DockerDotNetCore.sln add .\WebApi\WebApi.csproj
    dotnet sln .\DockerDotNetCore.sln add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet sln .\DockerDotNetCore.sln add .\Client\Client.csproj

    dotnet add .\WebApi\WebApi.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet add .\Client\Client.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj reference .\WebApi\WebApi.csproj
    dotnet add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj

As a result, the soluition structure will look like this:

    build\
    src\
      Client\
      Infrastructure.Logging\
      WebApi\
      WebApi.Test.Unit\

Build the solution to make sure that everything was done correctly:

    dotnet restore .\DockerDotNetCore.sln
    dotnet build .\DockerDotNetCore.sln
    dotnet test .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet publish .\DockerDotNetCore.sln

You may add a build scripts in the `build` folder to automate the steps above,
this repository uses [FAKE](http://fsharp.github.io/FAKE/ "FAKE") build script.

See tag [Step_01](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_01 "Step_01") in this repository for reference implementation.

## Step 2. Implementing WebAPI and Client

### Step 2.1 WebAPI

In this step we'll add simplest possible API implementation based on `Dictionary<string,string>`.
The purpose of this step is just to see that API starts and responds to requests, so values will not persist after API restart; we'll add persistent storage in later steps.

Modify `WebApi\Controllers\ValueController` to implement
`GET`, `POST`, `PUT`, `DELETE` operations using static
`Dictionary<string,string>` as values repository. Note that the dictionary has to be static because ASP.NET Core will create new controller instance per request.

Run the API. Change directory to `src\WebApi` and run command

    dotnet run

You should see output

    Now listening on: http://*:5000
    Application started. Press Ctrl+C to shut down.

Use `curl` or `Postman` client to test the API.

See tag [Step_02_1](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_1 "Step_02_1") in this repository for reference implementation.

### Step 2.2 Client

In this step we'll add a client that will be continuously sending
random requests to the API. The purpose of this client is to
emulate some system activity.

Add `Configuration` packages and `appsettings.json` settings file:

    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration
    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.CommandLine
    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.Json


    appsettings.json:
    {
        "ApiUrl": "http://localhost:5000/api",
        "MaxDelay": "00:00:05"
    }

Modify `Client` project to send `GET`, `POST`, `PUT`, and `DELETE`
commands periodically to the API.

Run the API. Change directory to `src\WebApi` and run command

    dotnet run

You should see output

    Now listening on: http://*:5000
    Application started. Press Ctrl+C to shut down.

Run the client. Leave the API running; in a new command prompt
change directory to `src\Client` and run command

    dotnet run

You should see that the client is sending random commands, e.g.

    REST API Random Test Client. API Url: http://localhost:5000/api
    GET http://localhost:5000/api/values
    POST http://localhost:5000/api/values
    GET http://localhost:5000/api/values
    GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
    GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
    DELETE http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac

See tag [Step_02_2](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_2 "Step_02_2") in this repository for reference implementation.

### Step 2.3 Logging

To ensure that system activity is logged consistently, add logging implementation
to `Infrastracture.Logging`; modify `WebApi` and `Client` projects to use that
implementation and log activity to console.

This repository uses [Serilog](https://serilog.net/ "Serilog") and its
[ColoredConsole sink](https://github.com/serilog/serilog-sinks-coloredconsole "Colored Console")
as underlying implementation.

    dotnet add .\Infrastructure.Logging\Infrastructure.Logging.csproj package Serilog
    dotnet add .\Infrastructure.Logging\Infrastructure.Logging.csproj package Serilog.Sinks.ColoredConsole
    dotnet add .\Client\Client.csproj package Serilog
    dotnet add .\Client\Client.csproj package Serilog.Sinks.ColoredConsole
    dotnet add .\WebApi\WebApi.csproj package Serilog
    dotnet add .\WebApi\WebApi.csproj package Serilog.Sinks.ColoredConsole

In later steps we will add [ElasticSearch sink](https://github.com/serilog/serilog-sinks-elasticsearch "ElasticSearch sink") to send logs to centralized storage.

See tag [Step_02_3](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_3 "Step_02_3") in this repository for reference implementation.

## Step 3. Docker Containers

### Step 3.1 WebAPI

In this step we'll add Docker container for the API.

Add `WebApi\Dockerfile` file to define content of the API container.

    FROM microsoft/dotnet:1.1-runtime
    WORKDIR /app
    EXPOSE 5000
    COPY _publish .
    ENTRYPOINT ["dotnet", "WebApi.dll"]

There are many ways to compose a Docker container, see
[Dockerfile reference](https://docs.docker.com/engine/reference/builder/ "Dockerfile reference")
for more information.

In this project we'll be using `dotnet publish` output, i.e. compiled binaries,
to compose API container. `Dockerfile` expects the output to be published into
`_publish` directory, this is to simplify build process — the default location
depends on the build configuration, e.g. `bin\Release\netcoreapp1.1`.

In a command prompt, change directory to `<project directory>\src\WebApi`
and run commands

    dotnet restore
    dotnet build
    dotnet publish --output _publish

    docker build --tag docker-dotnetcore/webapi:develop .

You should see output from Docker similar to this:

    Sending build context to Docker daemon 15.08 MB
    Step 1/5 : FROM microsoft/dotnet:1.1-runtime
    ---> 46bc96ad5288
    Step 2/5 : WORKDIR /app
    ---> Using cache
    ---> 09605205a557
    Step 3/5 : EXPOSE 5000
    ---> Using cache
    ---> 93bc4869240d
    Step 4/5 : COPY _publish .
    ---> 45d7a3e20515
    Removing intermediate container 209fd09f89fc
    Step 5/5 : ENTRYPOINT dotnet WebApi.dll
    ---> Running in 661f47cb5a3d
    ---> 3728e53b6d5f
    Removing intermediate container 661f47cb5a3d
    Successfully built 3728e53b6d5f

(your ids will be different).


    docker create --name dotnetcore_webapi_1 docker-dotnetcore/webapi:develop
    docker start --interactive dotnetcore_webapi_1

API should now be running in Docker. Press `Ctrl+C` when you need to stop it.
You can also run it in non-interactive mode and inspect logs on demand:

    docker start dotnetcore_webapi_1
    docker logs dotnetcore_webapi_1
    ...
    docker stop dotnetcore_webapi_1

See tag [Step_03_1](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_03_1 "Step_03_1") in this repository for reference implementation.

### Step 3.2 Client

In this step we'll add Docker container for the client.

Add `Client\Dockerfile` file to define content of the API container.

    FROM microsoft/dotnet:1.1-runtime
    WORKDIR /app
    EXPOSE 5000
    COPY _publish .
    COPY appsettings.json .
    ENTRYPOINT ["dotnet", "Client.dll"]

In a command prompt, change directory to `<project directory>\src\Client`
and run commands

    dotnet restore
    dotnet build
    dotnet publish --output _publish

    docker build --tag docker-dotnetcore/client:develop .
    docker create --name dotnetcore_client_1 docker-dotnetcore/client:develop
    docker start dotnetcore_client_1
    docker logs dotnetcore_client_1
    ...
    docker stop dotnetcore_client_1

See tag [Step_03_2](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_03_2 "Step_03_2") in this repository for reference implementation.

### Step 3.3 Docker Compose

In this step, we will create a system to build and run all our Docker
containers at once.

We will be using
[Docker Compose](https://docs.docker.com/compose/ "Docker Compose").
Compose is a tool for defining and running multi-container Docker
applications. With Compose, you use a YAML-based configuration file
to configure your application’s services. Then, using a single command,
you create and start all the services from your configuration.

Add file `<project directory>\src\_docker\docker-compose.yml`:

    version: "3"

    services:
        webapi:
            image: docker-dotnetcore/webapi:develop
            build:
                context: ../WebApi
                dockerfile: Dockerfile
            ports:
                - "5000:5000"
        client:
            image: docker-dotnetcore/client:develop
            build:
                context: ../Client
            dockerfile: Dockerfile
                depends_on:
                - webapi

This file defines two services, `webapi` and `client`.
Both of them will be built from `Dockerfile`, so we need to
specify project location (`context` parameter) and
the Dockerfile name (`dockerfile` parameter).

Docker images name in this demo follow pattern
`<product>/<component>:<tag>`, and we use tag `develop` to indicate
that this in a development version.

Our `Client` service depends on `API`, we need to specify
this dependency in `docker-compose.yml` using `depends_on` parameter,
so that `webapi` service starts first.

By default Compose sets up a single network for your app.
Each container for a service joins this default network
and is both reachable by other containers on that network,
and discoverable by them at a hostname identical to the container name.

This means that the `client` service can access `webapi` service
using `webapi` hostname. We need to adjust `client` application
settings. In `<project directory>\src\Client\appsettings.json`,
modify `ApiUrl` setting:

    "ApiUrl": "http://webapi:5000/api"

Build and run the application:

    cd <project directory>\src
    dotnet restore DockerDotNetCore.sln
    dotnet build DockerDotNetCore.sln
    dotnet publish DockerDotNetCore.sln --output _publish

    cd <project directory>\src\_docker
    docker-compose build
    docker-compose up

You should see both `webapi` and `client` containers running:

    Creating docker_webapi_1
    Creating docker_client_1
    Attaching to docker_webapi_1, docker_client_1
    webapi_1  | 2017-03-26 09:19:24.630 [docker-dotnetcore-webapi ] [ Information] [] WebAPI started
    client_1  | 2017-03-26 09:19:26.099 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] REST API Random Test Client. API Url: http://webapi:5000/api
    webapi_1  | Hosting environment: Development
    webapi_1  | Content root path: /app
    webapi_1  | Now listening on: http://*:5000
    webapi_1  | Application started. Press Ctrl+C to shut down.
    client_1  | 2017-03-26 09:19:26.166 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] GET http://webapi:5000/api/values
    webapi_1  | 2017-03-26 09:19:26.589 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] GET /api/values
    client_1  | 2017-03-26 09:19:29.483 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] GET http://webapi:5000/api/values
    webapi_1  | 2017-03-26 09:19:29.494 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] GET /api/values
    client_1  | 2017-03-26 09:19:34.189 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] GET http://webapi:5000/api/values
    webapi_1  | 2017-03-26 09:19:34.191 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] GET /api/values
    client_1  | 2017-03-26 09:19:39.192 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] POST http://webapi:5000/api/values
    webapi_1  | 2017-03-26 09:19:39.318 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] POST /api/values 03/26/2017 09:19:39
    client_1  | 2017-03-26 09:19:42.563 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] GET http://webapi:5000/api/values/7166a52d-cd0c-4ab0-9422-6c548e954091
    webapi_1  | 2017-03-26 09:19:42.579 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] GET /api/values/7166a52d-cd0c-4ab0-9422-6c548e954091
    client_1  | 2017-03-26 09:19:46.086 [docker-dotnetcore-client ] [ Information] ["Client.Random.Program"] DELETE http://webapi:5000/api/values/7166a52d-cd0c-4ab0-9422-6c548e954091
    webapi_1  | 2017-03-26 09:19:46.089 [docker-dotnetcore-webapi ] [ Information] ["WebApi.Controllers.ValuesController"] DELETE /api/values/7166a52d-cd0c-4ab0-9422-6c548e954091

See tag [Step_03_3](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_03_3 "Step_03_3") in this repository for reference implementation.


### Step 4 Supporting Infrastructure

### Step 4.1 MongoDb

In this step, we will add another Docker container to our system,
to run MongoDb server, and will modify API implementation to store
values in the database.

Docker Compose allows to define services based on images published
in Docker repositories. Let's add `mongo` service based on
[`mongo:latest`](https://hub.docker.com/_/mongo/ "MongoDb Docker Image") image.

Modify `<project directory>\src\_docker\docker-compose.yml`:

    version: "3"

    services:
      webapi:
        image: docker-dotnetcore/webapi:develop
        build:
          context: ../WebApi
          dockerfile: Dockerfile
        ports:
          - "5000:5000"
        depends_on:
          - mongodb
      client:
        image: docker-dotnetcore/client:develop
        build:
          context: ../Client
          dockerfile: Dockerfile
        depends_on:
          - webapi
      mongodb:
        image: mongo:latest
        ports:
          - "27017:27017"    


Note that MongoDb .NET drive is compatible with `netstandard1.5` or higher.
Check `TargetFramework` in project files to make sure that they
meet this requirement:

    <TargetFramework>netcoreapp1.1</TargetFramework>

Modify API implementation to use database instead of in-memory dictionary.

    cd <project directory>\src

    dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration
    dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration.CommandLine
    dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration.Json
    dotnet add .\WebApi\WebApi.csproj package MongoDB.Driver

In `<project directory>\src\WebApi\appsettings.json` add settings for MongoDb:

    {
      "MongoDb.ServerAddress": "mongodb",
      "MongoDb.ServerPort": "27017",
      "MongoDb.DatabaseName": "docker-dotnetcore",
      "MongoDb.UserName": "",
      "MongoDb.UserPassword": ""
    }

Note that we are using `mongodb` as a server address. This needs to match MongoDb service
name in `docker-compose.yml`.

Modify `<project directory>\src\WebApi\Startup.cs`
to read MongoDb settings, and 
`<project directory>\src\WebApi\Controllers\ValuesController.cs`
to use MongoDb database.

See tag [Step_04_1](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_04_1 "Step_04_1") in this repository for reference implementation.
