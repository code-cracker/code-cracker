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
    $nugetPackagesExe = "$packagesDir\NuGet.CommandLine.4.6.2\tools\NuGet.exe"
    $nugetExe = if (Test-Path $nugetPackagesExe) { $nugetPackagesExe } else { 'nuget' }
    $nupkgPathCS = "$rootDir\src\CSharp\CodeCracker.CSharp.{0}.nupkg"
    $nupkgPathVB = "$rootDir\src\VisualBasic\CodeCracker.VisualBasic.{0}.nupkg"
    $xunitConsoleExe = "$packagesDir\xunit.runner.console.2.3.1\tools\net452\xunit.console.x86.exe"
    $openCoverExe = "$packagesDir\OpenCover.4.6.519\tools\OpenCover.Console.exe"
    $dllCS = "CodeCracker.CSharp.dll"
    $dllVB = "CodeCracker.VisualBasic.dll"
    $dllCommon = "CodeCracker.Common.dll"
    $testDllCS = "CodeCracker.Test.CSharp.dll"
    $testDllVB = "CodeCracker.Test.VisualBasic.dll"
    $testDirCS = "$testDir\CSharp\CodeCracker.Test\bin\Release"
    $testDirVB = "$testDir\VisualBasic\CodeCracker.Test\bin\Release"
    $projectDirVB = "$srcDir\VisualBasic\CodeCracker"
    $projectFileVB = "$projectDirVB\CodeCracker.vbproj"
    $releaseDirVB = "$projectDirVB\bin\Release"
    $projectDirCS = "$srcDir\CSharp\CodeCracker"
    $projectFileCS = "$projectDirCS\CodeCracker.csproj"
    $releaseDirCS = "$projectDirCS\bin\Release"
    $logDir = "$rootDir\log"
    $outputXml = "$logDir\CodeCoverageResults.xml"
    $reportGeneratorExe = "$packagesDir\ReportGenerator.3.1.2\tools\ReportGenerator.exe"
    $coverageReportDir = "$logDir\codecoverage\"
    $coverallsNetExe = "$packagesDir\coveralls.io.1.4.2\tools\coveralls.net.exe"
    $ilmergeExe = "$packagesDir\ilmerge.2.14.1208\tools\ILMerge.exe"
    $isRelease = $isAppVeyor -and (($env:APPVEYOR_REPO_BRANCH -eq "release") -or ($env:APPVEYOR_REPO_TAG -eq "true"))
    $isPullRequest = $env:APPVEYOR_PULL_REQUEST_NUMBER -ne $null
    $tempDir = Join-Path "$([System.IO.Path]::GetTempPath())" "CodeCracker"
    # msbuild hack necessary until https://github.com/psake/psake/issues/201 is fixed:
    $msbuild64 = Resolve-Path "$(if (${env:ProgramFiles(x86)}) { ${env:ProgramFiles(x86)} } else { $env:ProgramFiles } )\Microsoft Visual Studio\2017\*\MSBuild\15.0\Bin\msbuild.exe"
    $msbuild32 = Resolve-Path "$(if (${env:ProgramFiles(x86)}) { ${env:ProgramFiles(x86)} } else { $env:ProgramFiles } )\Microsoft Visual Studio\2017\*\MSBuild\15.0\Bin\msbuild.exe"
    $msbuild = if ($msbuild64) { $msbuild64 } else { $msbuild32 }
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
Task Build-Only-CS -depends Build-MSBuild-CS, ILMerge-CS
Task Build-MSBuild-CS {
    if ($isAppVeyor) {
        Exec { . $msbuild $solutionFileCS /m /verbosity:minimal /p:Configuration=ReleaseNoVsix /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" }
    } else {
        Exec { . $msbuild $solutionFileCS /m /verbosity:minimal /p:Configuration=ReleaseNoVsix }
    }
}
Task Build-Only-VB -depends Build-MSBuild-VB, ILMerge-VB
Task Build-MSBuild-VB {
    if ($isAppVeyor) {
        Exec { . $msbuild $solutionFileVB /m /verbosity:minimal /p:Configuration=ReleaseNoVsix /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" }
    } else {
        Exec { . $msbuild $solutionFileVB /m /verbosity:minimal /p:Configuration=ReleaseNoVsix }
    }
}

Task ILMerge-VB { ILMerge $releaseDirVB $dllVB $projectFileVB $projectDirVB }
Task ILMerge-CS { ILMerge $releaseDirCS $dllCS $projectFileCS $projectDirCS }

function ILMerge($releaseDir, $dll, $projectFile, $projectDir) {
    Write-Host "IL Merge:"
    $mergedDir = $tempDir
    if (!(Test-Path $mergedDir)) { mkdir "$mergedDir" }
    $inputDll = "$releaseDir\$dll"
    $inputDllCommon = "$releaseDir\$dllCommon"
    $pdbCommon = Change-Extension $inputDllCommon "pdb"
    if (Test-Path $inputDllCommon) {
        if ((ls $inputDllCommon).LastWriteTime -gt (ls $inputDll).LastWriteTime) {
            # common is newer, but no changes on main dll
            Write-Host "Common dll is newer than $inputDll, stopping IL merge."
            return
        }
    } else {
        # no common dll, can't merge
        Write-Host "Can't find common dll, stopping IL merge."
        return
    }
    $mergedDll = "$mergedDir\$dll"
    [xml]$proj = cat $projectFile
    $libs = @()
    foreach ($ref in $proj.Project.ItemGroup.Reference.HintPath) {
        $dir += [System.IO.Path]::GetDirectoryName("$projectDir\$ref")
        $libs += "/lib:`"$([System.IO.Path]::GetDirectoryName("$projectDir\$ref"))`" "
    }
    Exec { . $ilmergeExe $libs /out:"$mergedDll" "$inputDll" "$inputDllCommon" }
    $releaseMergedDir = "$releaseDir\merged"
    if (!(Test-Path $releaseMergedDir)) { mkdir $releaseMergedDir | Out-Null }
    cp $mergedDll "$releaseMergedDir\" -Force
    Write-Host "  $dll -> $releaseMergedDir\$dll"
    $mergedPdb = Change-Extension $mergedDll "pdb"
    cp $mergedPdb "$releaseMergedDir\" -Force
    $pdb = (ls $mergedPdb).Name
    Write-Host "  $pdb -> $releaseMergedDir\$pdb"
}

function Change-Extension ($filename, $extension) {
    Join-Path "$([System.IO.Path]::GetDirectoryName($filename))" "$([System.IO.Path]::GetFileNameWithoutExtension($filename)).$extension"
}

Task Clean {
    Exec { . $msbuild $solutionFileCS /t:Clean /v:quiet }
    Exec { . $msbuild $solutionFileVB /t:Clean /v:quiet }
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

Task Update-Nuspec -precondition { return $isAppVeyor -and ($isRelease -ne $true) } -depends Update-Nuspec-CSharp, Update-Nuspec-VB
Task Update-Nuspec-CSharp -precondition { return $isAppVeyor -and ($isRelease -ne $true) } {
    UpdateNuspec $nuspecPathCS "C#"
}
Task Update-Nuspec-VB -precondition { return $isAppVeyor -and ($isRelease -ne $true) } {
    UpdateNuspec $nuspecPathVB "VB"
}

Task Pack-Nuget -precondition { return $isAppVeyor } -depends Pack-Nuget-Csharp, Pack-Nuget-VB
Task Pack-Nuget-CSharp -precondition { return $isAppVeyor } {
    PackNuget "C#" "$rootDir\src\CSharp" $nuspecPathCS $nupkgPathCS
}
Task Pack-Nuget-VB -precondition { return $isAppVeyor } {
    PackNuget "VB" "$rootDir\src\VisualBasic" $nuspecPathVB $nupkgPathVB
}
Task Pack-Nuget-Force -depends Pack-Nuget-Csharp-Force, Pack-Nuget-VB-Force
Task Pack-Nuget-Csharp-Force {
    PackNuget "C#" "$rootDir\src\CSharp" $nuspecPathCS $nupkgPathCS
}
Task Pack-Nuget-VB-Force {
    PackNuget "VB" "$rootDir\src\VisualBasic" $nuspecPathVB $nupkgPathVB
}

Task Count-Analyzers {
    $count = $(ls $rootDir\src\*.cs -Recurse | ? { $_.Name.contains('Analyzer') } | ? { !((cat $_) -match 'abstract class') }).count
    Write-Host "Found $count C# Analyzers"
    $count = $(ls $rootDir\src\*.cs -Recurse | ? { $_.Name.contains('CodeFix') } | ? { !((cat $_) -match 'abstract class') }).count
    Write-Host "Found $count C# Code Fixes"
    $count = $(ls $rootDir\src\*.cs -Recurse | ? { $_.Name.contains('FixAll') } | ? { !((cat $_) -match 'abstract class') }).count
    Write-Host "Found $count C# Code Fixes All"
    $count = $(ls $rootDir\src\*.vb -Recurse | ? { $_.Name.contains('Analyzer') } | ? { !((cat $_) -match 'mustinherit class') }).count
    Write-Host "Found $count VB Analyzers"
    $count = $(ls $rootDir\src\*.vb -Recurse | ? { $_.Name.contains('CodeFix') } | ? { !((cat $_) -match 'mustinherit class') }).count
    Write-Host "Found $count VB Code Fixes"
    $count = $(ls $rootDir\src\*.vb -Recurse | ? { $_.Name.contains('FixAll') } | ? { !((cat $_) -match 'mustinherit class') }).count
    Write-Host "Found $count VB Code Fixes All"
}

Task Update-ChangeLog {
    # invoke-psake default.ps1 -tasklist update-changelog -parameters @{"token"="<token>"}
    echo $token
    return
    Exec {
        github_changelog_generator code-cracker/code-cracker --no-pull-requests --no-issues-wo-labels --exclude-labels "Can't repro","update readme",decision,docs,duplicate,question,invalid,wontfix,Duplicate,Question,Invalid,Wontfix  -t $token
    }
}

Task Echo { echo echo }

function PackNuget($language, $dir, $nuspecFile, $nupkgFile) {
    Write-Host "Packing nuget for $language..."
    [xml]$xml = cat "$nuspecFile"
    $nupkgFile = $nupkgFile -f $xml.package.metadata.version
    . $nugetExe pack "$nuspecFile" -OutputDirectory "$dir"
    $nuspecFileName = (ls $nuspecFile).Name
    Write-Host "  $nuspecFileName ($language/$($xml.package.metadata.version)) -> $nupkgFile"
    if ($isAppVeyor) {
        Write-Host "Pushing nuget artifact for $language..."
        appveyor PushArtifact $nupkgFile
        Write-Host "Nupkg pushed for $language!"
    }
}

function UpdateNuspec($nuspecPath, $language) {
      write-host "Updating version in nuspec file for $language to $buildNumber"
      [xml]$xml = cat $nuspecPath
      $xml.package.metadata.version+="-z$buildNumber"
      write-host "Nuspec version will be $($xml.package.metadata.version)"
      $xml.Save($nuspecPath)
      write-host "Nuspec saved for $language!"
}

function RestorePkgs($sln) {
    Write-Host "Restoring $sln..." -ForegroundColor Green
    Retry {
        . $nugetExe restore "$sln" -NonInteractive -ConfigFile "$rootDir\nuget.config"
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
        Exec { . $coverallsNetExe --opencover $outputXml --full-sources }
    }
}