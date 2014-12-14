param([String]$testClass)
$Global:lastRun = $(date)
$testDllDirPath = "$PSScriptRoot\test\CSharp\CodeCracker.Test\bin\Debug\"
$testDllFileName = "CodeCracker.Test.dll"
$Global:testDllFullFileName = "$testDllDirPath$testDllFileName"
$Global:xunitConsole = "$PSScriptRoot\packages\xunit.runners.2.0.0-beta5-build2785\tools\xunit.console.x86.exe"

function global:DebounceXunit {
    try {
        if (($(date) - $script:lastRun).TotalMilliseconds -lt 2000) {
            return
        }
        $Global:lastRun = $(date)
        If ($Global:testClass) {
            Start-Process $Global:xunitConsole -ArgumentList "`"$Global:testDllFullFileName`" -class $Global:testClass" -NoNewWindow
        } Else {
            Start-Process $Global:xunitConsole -ArgumentList "`"$Global:testDllFullFileName`"" -NoNewWindow
        }
    }
    catch
    {
        Write-Host $_.Exception.Message
    }
}

Write-Host "Watching $testDllDirPath"
If ($testClass) {
    If ($testClass.StartsWith("CodeCracker.Test") -eq $false) {
        $testClass = "CodeCracker.Test.$testClass"
    }
    Write-Host "Only for $testClass"
}
$Global:testClass = $testClass

try {
    $watcher = New-Object System.IO.FileSystemWatcher
    $watcher.Path = $testDllDirPath
    $watcher.Filter = $testDllFileName
    $watcher.IncludeSubdirectories = $false
    $watcher.EnableRaisingEvents = $true
    $watcher.NotifyFilter = [System.IO.NotifyFilters]::LastWrite
    $changed = Register-ObjectEvent $watcher "Changed" -Action { DebounceXunit }
}
catch {
  Write-Host $_.Exception.Message
}
#if we do that, then we don't get any console output:
#Write-Host "Press any key to continue ..."
#[System.Console]::ReadKey()
#Unregister-Event $changed.Id
