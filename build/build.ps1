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

$paket=[System.IO.Path]::Combine($solutionDir, ".paket", "paket.exe")
$fake=[System.IO.Path]::Combine($solutionDir, "packages", "FAKE", "tools", "fake.exe")

Write-Host -ForegroundColor Green "*** Restoring packages in $solutionDir ***"
& "$paket" restore

Write-Host -ForegroundColor Green "*** Building target $Target ($Configuration) in $solutionDir ***"
& "$fake" "$buildScript" "$Target" --logfile "$buildLog" Configuration="$Configuration" Platform="$Platform"
