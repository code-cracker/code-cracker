$ErrorActionPreference = "Stop"
# functions:

function IsNugetVersion3($theNugetExe) {
    try {
        $nugetText = . $theNugetExe | Out-String
    } catch {
        return false
    }
    [regex]$regex = '^NuGet Version: (.*)\n'
    $match = $regex.Match($nugetText)
    $version = $match.Groups[1].Value
    return $version.StartsWith(3)
}

function Get-Nuget {
    if (gcm nuget -ErrorAction SilentlyContinue) {
        if (IsNugetVersion3 'nuget') {
            $nugetExe = 'nuget'
        } else {
            Download-Nuget
            $nugetExe = $localNuget
        }
    } else {
        Download-Nuget
        $nugetExe = $localNuget
    }
}

function Download-Nuget {
    $tempNuget = "$env:TEMP\codecracker\nuget.exe"
    if (!(Test-Path "$env:TEMP\codecracker\")) {
        md "$env:TEMP\codecracker\" | Out-Null
    }
    if (Test-Path $localNuget) {
        if (IsNugetVersion3($localNuget)) { return }
    }
    if (Test-Path $tempNuget) {
        if (IsNugetVersion3($tempNuget)) {
            cp $tempNuget $localNuget
            return
        }
    }
    wget "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $tempNuget
    cp $tempNuget $localNuget
}

function Import-Psake {
    $psakeModule = "$PSScriptRoot\packages\psake.4.5.0\tools\psake.psm1"
    if ((Test-Path $psakeModule) -ne $true) {
        . "$nugetExe" restore $PSScriptRoot\.nuget\packages.config -SolutionDirectory $PSScriptRoot
    }
    Import-Module $psakeModule -force
}

# statements:

$localNuget = "$PSScriptRoot\.nuget\nuget.exe"
$nugetExe = ""
Get-Nuget
Import-Psake
if ($MyInvocation.UnboundArguments.Count -ne 0) {
    . $PSScriptRoot\psake.ps1 -taskList ($MyInvocation.UnboundArguments -join " ")
}
else {
    . $PSScriptRoot\build.ps1 Build
}

exit !($psake.build_success)