$ErrorActionPreference = "Stop"
$tempDir = Join-Path "$([System.IO.Path]::GetTempPath())" "CodeCracker"
if (!(Test-Path $tempDir)) { mkdir $tempDir | Out-Null }
# functions:

function IsNugetVersion3OrAbove($theNugetExe) {
    try {
        $nugetText = . $theNugetExe | Out-String
    } catch {
        return false
    }
    [regex]$regex = '^NuGet Version: (\d)\.(\d).*\n'
    $match = $regex.Match($nugetText)
    $version = $match.Groups[1].Value
    Write-Host "Nuget major version is $version"
    return [System.Convert]::ToInt32($version) -ge 3
}

function Get-Nuget {
    if (gcm nuget -ErrorAction SilentlyContinue) {
        if (IsNugetVersion3OrAbove 'nuget') {
            $script:nugetExe = 'nuget'
        } else {
            Download-Nuget
            $script:nugetExe = $localNuget
        }
    } else {
        Download-Nuget
        $script:nugetExe = $localNuget
    }
}

function Download-Nuget {
    $tempNuget = "$env:TEMP\codecracker\nuget.exe"
    if (!(Test-Path "$env:TEMP\codecracker\")) {
        md "$env:TEMP\codecracker\" | Out-Null
    }
    if (Test-Path $localNuget) {
        if (IsNugetVersion3OrAbove($localNuget)) { return }
    }
    if (Test-Path $tempNuget) {
        if (IsNugetVersion3OrAbove($tempNuget)) {
            cp $tempNuget $localNuget
            return
        }
    }
    wget "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $tempNuget
    cp $tempNuget $localNuget
}

function Import-Psake {
    $psakeModule = "$PSScriptRoot\packages\psake.4.7.0\tools\psake\psake.psm1"
    if ((Test-Path $psakeModule) -ne $true) {
        Write-Host "Restoring $PSScriptRoot\.nuget with $script:nugetExe"
        . "$script:nugetExe" restore $PSScriptRoot\.nuget\packages.config -SolutionDirectory $PSScriptRoot
    }
    Import-Module $psakeModule -force
}

function Import-ILMerge {
    $ilmergeExe = "$PSScriptRoot\packages\ilmerge.2.14.1208\tools\ILMerge.exe"
    if ((Test-Path $ilmergeExe) -ne $true) {
        Write-Host "Restoring $PSScriptRoot\.nuget with $script:nugetExe"
        . "$script:nugetExe" restore $PSScriptRoot\.nuget\packages.config -SolutionDirectory $PSScriptRoot
    }
}

# statements:

$localNuget = "$PSScriptRoot\.nuget\nuget.exe"
$nugetExe = ""
Get-Nuget
Import-Psake
Import-ILMerge
if ($MyInvocation.UnboundArguments.Count -ne 0) {
    Invoke-Expression("Invoke-psake -framework '4.6' $PSScriptRoot\psakefile.ps1 -taskList " + $MyInvocation.UnboundArguments -join " ")
}
else {
    . $PSScriptRoot\build.ps1 Build
}

exit !($psake.build_success)