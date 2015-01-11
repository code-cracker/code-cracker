$rootDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$packagesDir = "$rootDir\packages"
$openCoverExe = "$packagesDir\OpenCover.4.5.3607-rc27\OpenCover.Console.exe"
$xunitConsoleExe = "$packagesDir\xunit.runners.2.0.0-beta5-build2785\tools\xunit.console.x86.exe"
$testDll = "CodeCracker.Test.CSharp.dll"
$testDir = "$PSScriptRoot\CodeCracker.Test\bin\Debug"
$logDir = "$rootDir\log"
$outputXml = "$logDir\CodeCoverageResults.xml"
$reportGeneratorExe = "$packagesDir\ReportGenerator.2.0.4.0\ReportGenerator.exe"
$coverageReportDir = "$logDir\codecoverage\"
$converallsNetExe = "$packagesDir\coveralls.io.1.1.73-beta\tools\coveralls.net.exe"

$allPaths = $openCoverExe, $xunitConsoleExe, $testDir, $logDir, $reportGeneratorExe

function testPath($paths) {
    $notFound = @()
    foreach($path in $paths) {
        if ((Test-Path $path) -eq $false)
        {
            $notFound += $path
        }
    }
    $notFound
}

if ((Test-Path $logDir) -eq $false)
{
    Write-Host -ForegroundColor DarkBlue "Creating log directory $logDir"
    mkdir $logDir | Out-Null
}

$notFoundPaths = testPath $allPaths
if ($notFoundPaths.length -ne 0) {
    Write-Host -ForegroundColor DarkRed "Paths not found: "
    foreach($path in $notFoundPaths) {
        Write-Host -ForegroundColor DarkRed "    $path"
    }
    return
}

Write-Host -ForegroundColor DarkBlue "Running Code Coverage"
. $openCoverExe -register:user "-target:$xunitConsoleExe" "-targetargs:$testDll -noshadow" "-filter:+[CodeCracker*]* -[CodeCracker.Test*]*" "-output:$outputXml" -coverbytest:*.Test.*.dll "-targetdir:$testDir" -log:All
Write-Host -ForegroundColor DarkBlue "Exporting code coverage report"
. $reportGeneratorExe -verbosity:Info -reports:$outputXml -targetdir:$coverageReportDir
if ($env:COVERALLS_REPO_TOKEN -ne $null) {
    Write-Host -ForegroundColor DarkBlue "Uploading coverage report to Coveralls.io"
    . $converallsNetExe --opencover $outputXml
}