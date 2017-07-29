#r @"../packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing
open System
open System.Diagnostics
open System.IO

module Properties =
    let buildConfiguration = getBuildParamOrDefault "Configuration" "Release"
    let buildPlatform = getBuildParamOrDefault "Platform" "Any CPU"

    module Internal =
        let repositoryDir = ((new DirectoryInfo(__SOURCE_DIRECTORY__)).Parent).FullName
        let sourceDir = Path.Combine(repositoryDir, "src")
        let solutionFile = Path.Combine(sourceDir, "DockerDotNetCore.sln")
        let unitTestsProject = Path.Combine(sourceDir, "WebApi.Test.Unit", "WebApi.Test.Unit.csproj")

        // Docker
        let dockerImagesRepository = "docker-dotnetcore"
        let dockerComposeDir = Path.Combine(sourceDir, "_docker")

module Targets =
    open Properties
    open Properties.Internal

    Target "Purge" (fun _ ->
        let purgeScript = sprintf "$startPath = '%s'; Get-ChildItem -Path $startPath -Filter 'bin' -Directory -Recurse | Foreach { $_.Delete($true) }; Get-ChildItem -Path $startPath -Filter 'obj' -Directory -Recurse | Foreach { $_.Delete($true) }" repositoryDir
        let powershellWrapper = sprintf """/c powershell -ExecutionPolicy Unrestricted -Command "%s" """ purgeScript

        let p = new Process()
        p.StartInfo.FileName <- "cmd.exe"
        p.StartInfo.Arguments <- powershellWrapper
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.UseShellExecute <- false
        p.Start() |> ignore

        printfn "%s" (p.StandardOutput.ReadToEnd())
    )

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
                AdditionalArgs = ["--logger"; "trx"]
                TimeOut = (System.TimeSpan.FromMinutes 5.)
            })
    )

    Target "Publish" (fun _ ->
        DotNetCli.Publish (fun p ->
            { p with
                Project = solutionFile
                Configuration = buildConfiguration
                Output = "_publish"
                TimeOut = (System.TimeSpan.FromMinutes 5.)
            })
    )

    Target "Compose" (fun _ ->
        ExecProcess (fun info ->
            info.FileName <- ("docker-compose.exe")
            info.Arguments <- ("build")
            info.WorkingDirectory <- dockerComposeDir
        ) (System.TimeSpan.FromMinutes 10.) |> ignore
    )

    Target "FullBuild" DoNothing
    Target "Default" DoNothing

// Dependencies
open Targets

"Clean"
    ==> "Restore"
    ==> "Build"

"Build" ==> "Test"
"Test" ==> "Publish"
"Compose" ==> "FullBuild"
"FullBuild" ==> "Default"

// Start
RunTargetOrDefault "Default"
