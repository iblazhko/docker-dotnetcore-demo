Param(
    [ValidateNotNullOrEmpty()]
    [string]$Target="Default",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration="Release",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Any CPU", "x64")]
    [string]$Platform="Any CPU"
)

$buildDir=$PSScriptRoot
$buildLog=[System.IO.Path]::Combine($buildDir, "reports", "build.log")

$buildScript=[System.IO.Path]::Combine($buildDir, "build.fsx")

$solutionDir=(Get-Item $buildDir).Parent.FullName
$toolsDir=[System.IO.Path]::Combine($buildDir, "tools")
$nuget=[System.IO.Path]::Combine($toolsDir, "NuGet-4.0.0", "nuget.exe")
$nugetPackagesDir=[System.IO.Path]::Combine($toolsDir, "packages")

$fakeVersion="4.56.0"

$fake=[System.IO.Path]::Combine($nugetPackagesDir, "FAKE.$fakeVersion", "tools", "FAKE.exe")

Write-Host -ForegroundColor Green "*** Building $Configuration in $solutionDir ***"

Write-Host -ForegroundColor Green "***    Getting FAKE and build tools ***"
& "$NuGet" install FAKE -OutputDirectory $nugetPackagesDir -Version $fakeVersion -Verbosity quiet

Write-Host -ForegroundColor Green "***    FAKE it ***"
& "$fake" "$buildScript" "$Target" --logfile "$buildLog" Configuration="$Configuration" Platform="$Platform"
