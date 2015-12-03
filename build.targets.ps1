Properties {
    $rootDir = Split-Path $psake.build_script_file
    $solutionFileCS = "$rootDir\CodeCracker.CSharp.sln"
    $solutionFileVB = "$rootDir\CodeCracker.VisualBasic.sln"
    $srcDir = "$rootDir\src"
    $testDir = "$rootDir\test"
    $isAppVeyor = $env:APPVEYOR -eq $true
    $slns = ls "$rootDir\*.sln"
    $packagesDir = "$rootDir\packages"
    $buildNumber = [Convert]::ToInt32($env:APPVEYOR_BUILD_NUMBER).ToString("0000")
    $nuspecPathCS = "$rootDir\src\CSharp\CodeCracker\CodeCracker.nuspec"
    $nuspecPathVB = "$rootDir\src\VisualBasic\CodeCracker\CodeCracker.nuspec"
    $nuspecPathJoint = "$rootDir\src\CodeCracker.nuspec"
    $nugetExe = "$packagesDir\NuGet.CommandLine.2.8.5\tools\NuGet.exe"
    $nupkgPathCS = "$rootDir\src\CSharp\CodeCracker.CSharp.{0}.nupkg"
    $nupkgPathVB = "$rootDir\src\VisualBasic\CodeCracker.VisualBasic.{0}.nupkg"
    $nupkgPathJoint = "$rootDir\CodeCracker.{0}.nupkg"
    $xunitConsoleExe = "$packagesDir\xunit.runner.console.2.1.0\tools\xunit.console.x86.exe"
    $openCoverExe = "$packagesDir\OpenCover.4.6.166\tools\OpenCover.Console.exe"
    $testDllCS = "CodeCracker.Test.CSharp.dll"
    $testDllVB = "CodeCracker.Test.VisualBasic.dll"
    $testDirCS = "$testDir\CSharp\CodeCracker.Test\bin\Debug"
    $testDirVB = "$testDir\VisualBasic\CodeCracker.Test\bin\Debug"
    $logDir = "$rootDir\log"
    $outputXml = "$logDir\CodeCoverageResults.xml"
    $reportGeneratorExe = "$packagesDir\ReportGenerator.2.3.5.0\tools\ReportGenerator.exe"
    $coverageReportDir = "$logDir\codecoverage\"
    $converallsNetExe = "$packagesDir\coveralls.io.1.3.4\tools\coveralls.net.exe"
    $isRelease = $isAppVeyor -and ($env:APPVEYOR_REPO_BRANCH -eq "release")
    $isPullRequest = $env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Task Default -Depends Build, Test

Task Rebuild -Depends Clean, Build

Task Restore {
    Foreach($sln in $slns) {
        RestorePkgs $sln
    }
}

Task Prepare-Build -depends Restore, Update-Nuspec

Task Build -depends Prepare-Build, Build-Only
Task Build-CS -depends Prepare-Build, Build-Only-CS
Task Build-VB -depends Prepare-Build, Build-Only-VB

Task Build-Only -depends Build-Only-CS, Build-Only-VB
Task Build-Only-CS {
    if ($isAppVeyor) {
        Exec { msbuild $solutionFileCS /m /verbosity:minimal /p:Configuration=DebugNoVsix /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" }
    } else {
        Exec { msbuild $solutionFileCS /m /verbosity:minimal /p:Configuration=DebugNoVsix }
    }
}
Task Build-Only-VB {
    if ($isAppVeyor) {
        Exec { msbuild $solutionFileVB /m /verbosity:minimal /p:Configuration=DebugNoVsix /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" }
    } else {
        Exec { msbuild $solutionFileVB /m /verbosity:minimal /p:Configuration=DebugNoVsix }
    }
}

Task Clean {
    Exec { msbuild $solutionFileCS /t:Clean /v:quiet }
    Exec { msbuild $solutionFileVB /t:Clean /v:quiet }
}

Task Set-Log {
    if ((Test-Path $logDir) -eq $false)
    {
        Write-Host -ForegroundColor DarkBlue "Creating log directory $logDir"
        mkdir $logDir | Out-Null
    }
}

Task Test-Acceptance -depends Test {
    . "$rootDir\test\CSharp\AnalyzeCoreFx.ps1"
}

Task Test -depends Set-Log {
    RunTestWithCoverage "$testDirCS\$testDllCS", "$testDirVB\$testDllVB"
}
Task Test-VB -depends Set-Log {
    RunTestWithCoverage "$testDirVB\$testDllVB"
}
Task Test-CSharp -depends Set-Log {
    RunTestWithCoverage "$testDirCS\$testDllCS"
}

Task Test-No-Coverage -depends Test-No-Coverage-CSharp, Test-No-Coverage-VB
Task Test-No-Coverage-VB {
    RunTest "$testDirVB\$testDllVB"
}
Task Test-No-Coverage-CSharp {
    RunTest "$testDirCS\$testDllCS"
}

Task Update-Nuspec -precondition { return $isAppVeyor -and ($isRelease -ne $true) } -depends Update-Nuspec-Joint
Task Update-Nuspec-Joint -precondition { return $isAppVeyor -and ($isRelease -ne $true) } -depends Update-Nuspec-CSharp, Update-Nuspec-VB {
    UpdateNuspec $nuspecPathJoint "joint package"
}
Task Update-Nuspec-CSharp -precondition { return $isAppVeyor -and ($isRelease -ne $true) } {
    UpdateNuspec $nuspecPathCS "C#"
}
Task Update-Nuspec-VB -precondition { return $isAppVeyor -and ($isRelease -ne $true) } {
    UpdateNuspec $nuspecPathVB "VB"
}

Task Pack-Nuget -precondition { return $isAppVeyor } -depends Pack-Nuget-Joint
Task Pack-Nuget-Joint -precondition { return $isAppVeyor } -depends Pack-Nuget-Csharp, Pack-Nuget-VB {
    PackNuget "Joint package" "$rootDir" $nuspecPathJoint $nupkgPathJoint
}
Task Pack-Nuget-CSharp -precondition { return $isAppVeyor } {
    PackNuget "C#" "$rootDir\src\CSharp" $nuspecPathCS $nupkgPathCS
}
Task Pack-Nuget-VB -precondition { return $isAppVeyor } {
    PackNuget "VB" "$rootDir\src\VisualBasic" $nuspecPathVB $nupkgPathVB
}

function PackNuget($language, $dir, $nuspecFile, $nupkgFile) {
    Write-Host "Packing nuget for $language..."
    [xml]$xml = cat $nuspecFile
    $nupkgFile = $nupkgFile -f $xml.package.metadata.version
    Write-Host "Nupkg path is $nupkgFile"
    . $nugetExe pack $nuspecFile -Properties "Configuration=Debug;Platform=AnyCPU" -OutputDirectory $dir
    ls $nupkgFile
    Write-Host "Nuget packed for $language!"
    Write-Host "Pushing nuget artifact for $language..."
    appveyor PushArtifact $nupkgFile
    Write-Host "Nupkg pushed for $language!"
}

function UpdateNuspec($nuspecPath, $language) {
      write-host "Updating version in nuspec file for $language to $buildNumber"
      [xml]$xml = cat $nuspecPath
      $xml.package.metadata.version+="-$buildNumber"
      write-host "Nuspec version will be $($xml.package.metadata.version)"
      $xml.Save($nuspecPath)
      write-host "Nuspec saved for $language!"
}

function RestorePkgs($sln) {
    Write-Host "Restoring $sln..." -ForegroundColor Green
    Retry {
        . $nugetExe restore $sln
        if ($LASTEXITCODE) { throw "Nuget restore for $sln failed." }
    }
}

function Retry {
     Param (
        [parameter(Position=0,Mandatory=1)]
        [ScriptBlock]$cmd,
        [parameter(Position=1,Mandatory=0)]
        [int]$times = 3
    )
    $retrycount = 0
    while ($retrycount -lt $times){
        try {
            & $cmd
            if (!$?) {
                throw "Command failed."
            }
            return
        }
        catch {
            Write-Host -ForegroundColor Red "Failed: ($($_.Exception.Message)), retrying."
        }
        $retrycount++
    }
    throw "Command '$($cmd.ToString())' failed."
}

function TestPath($paths) {
    $notFound = @()
    foreach($path in $paths) {
        if ((Test-Path $path) -eq $false)
        {
            $notFound += $path
        }
    }
    $notFound
}

function RunTest($fullTestDllPath) {
    if ($isAppVeyor) {
        . $xunitConsoleExe $fullTestDllPath -appveyor -nologo -quiet
    } else {
        . $xunitConsoleExe $fullTestDllPath -nologo -quiet
    }
}

function RunTestWithCoverage($fullTestDllPaths) {
    $notFoundPaths = TestPath $openCoverExe, $xunitConsoleExe, $reportGeneratorExe
    if ($notFoundPaths.length -ne 0) {
        Write-Host -ForegroundColor DarkRed "Paths not found: "
        foreach($path in $notFoundPaths) {
            Write-Host -ForegroundColor DarkRed "    $path"
        }
        throw "Paths for test executables not found"
    }
    $targetArgs = ""
    Foreach($fullTestDllPath in $fullTestDllPaths) {
        $targetArgs += $fullTestDllPath + " "
    }
    $targetArgs = $targetArgs.Substring(0, $targetArgs.Length - 1)
    $appVeyor = ""
    if ($isAppVeyor) {
        $appVeyor = " -appveyor"
    }
    $arguments = '-register:user', "`"-target:$xunitConsoleExe`"", "`"-targetargs:$targetArgs $appVeyor -noshadow -parallel none -nologo`"", "`"-filter:+[CodeCracker*]* -[CodeCracker.Test*]*`"", "`"-output:$outputXml`"", '-coverbytest:*.Test.*.dll', '-log:All', '-returntargetcode'
    Exec { . $openCoverExe $arguments }
    Write-Host -ForegroundColor DarkBlue "Exporting code coverage report"
    Exec { . $reportGeneratorExe -verbosity:Info -reports:$outputXml -targetdir:$coverageReportDir }
    if ($env:COVERALLS_REPO_TOKEN -ne $null) {
        Write-Host -ForegroundColor DarkBlue "Uploading coverage report to Coveralls.io"
        Exec { . $converallsNetExe --opencover $outputXml --full-sources }
    }
}