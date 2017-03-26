#r @"tools/packages/FAKE.4.56.0/tools/FakeLib.dll"

open Fake
open Fake.Testing
open System
open System.Diagnostics

module Properties =
    let buildConfiguration = getBuildParamOrDefault "Configuration" "Release"
    let buildPlatform = getBuildParamOrDefault "Platform" "Any CPU"

    module Internal =
        let buildDir = __SOURCE_DIRECTORY__
        let solutionDir = sprintf @"%s\.." buildDir
        let sourceDir = sprintf @"%s\src" solutionDir
        let solutionFile = sprintf @"%s\DockerDotNetCore.sln" sourceDir
        let unitTestsProject = sprintf @"%s\WebApi.Test.Unit\WebApi.Test.Unit.csproj" sourceDir

        // Docker
        let dockerImagesRepository = "docker-dotnetcore"
        let dockerComposeDir = sprintf @"%s\_docker" sourceDir


module Targets =
    open Properties
    open Properties.Internal

    Target "Clean" (fun _ ->
        DotNetCli.RunCommand (fun p ->
             { p with
                 TimeOut = TimeSpan.FromMinutes 10.
             }) (sprintf "clean \"%s\"" solutionFile)
    )

    Target "Restore" (fun _ ->
        DotNetCli.Restore (fun p ->
            { p with
                Project = solutionFile
            })
    )

    Target "Build" (fun _ ->
        DotNetCli.Build (fun p ->
            { p with
                Project = solutionFile
                Configuration = buildConfiguration
            })
    )

    Target "Test" (fun _ ->
        DotNetCli.Test (fun p ->
            { p with
                Project = unitTestsProject
                Configuration = buildConfiguration
            })
    )

    Target "Publish" (fun _ ->
        DotNetCli.Publish (fun p ->
            { p with
                Project = solutionFile
                Configuration = buildConfiguration
                Output = "_publish"
            })
    )

    Target "Compose" (fun _ ->
        ExecProcess (fun info ->
            info.FileName <- ("docker-compose.exe")
            info.Arguments <- ("build")
            info.WorkingDirectory <- dockerComposeDir
        ) (System.TimeSpan.FromMinutes 10.) |> ignore
    )

    Target "Default" (fun _ ->
        () |> DoNothing
    )

// Dependencies
open Targets
"Clean"
    ==> "Restore"
    ==> "Build"
    ==> "Test"
    ==> "Publish"
    ==> "Compose"
    ==> "Default"

// Start
RunTargetOrDefault "Default"
