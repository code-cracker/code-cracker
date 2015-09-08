param([String]$testClass)
$Global:lastRun = $lastRun = [System.DateTime]::Now
$testDllDirPath = "$PSScriptRoot\test\CSharp\CodeCracker.Test\bin\Debug\"
$testDllFileName = "CodeCracker.Test.CSharp.dll"
$Global:testDllFullFileName = "$testDllDirPath$testDllFileName"
$Global:xunitConsole = "$PSScriptRoot\packages\xunit.runner.console.2.0.0\tools\xunit.console.x86.exe"

if ($testClass -eq "now"){
    . $Global:xunitConsole "$Global:testDllFullFileName"
    return
}

function global:DebounceXunit {
    try {
        if (([System.DateTime]::Now - $script:lastRun).TotalMilliseconds -lt 2000) {
            return
        }
        $Global:lastRun = [System.DateTime]::Now
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
        $testClass = "CodeCracker.Test.CSharp.$testClass"
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
