if ((Test-Path $PSScriptRoot\packages\psake.4.4.2\tools\psake.psm1) -ne $true) {
    nuget restore $PSScriptRoot\.nuget\packages.config -SolutionDirectory $PSScriptRoot
}
Import-Module $PSScriptRoot\packages\psake.4.4.2\tools\psake.psm1 -force
if ($MyInvocation.UnboundArguments.Count -ne 0) {
    . $PSScriptRoot\psake.ps1 -taskList ($MyInvocation.UnboundArguments -join " ")
}
else {
    . $PSScriptRoot\build.ps1 Build
}
exit !($psake.build_success)