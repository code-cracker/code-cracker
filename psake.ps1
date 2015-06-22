Import-Module $PSScriptRoot\packages\psake.4.4.2\tools\psake.psm1 -force
Invoke-Expression("Invoke-psake -framework '4.5.1' build.targets.ps1 " + $MyInvocation.UnboundArguments -join " ")
exit !($psake.build_success)