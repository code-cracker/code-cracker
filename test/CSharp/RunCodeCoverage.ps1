$openCoverExe = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\packages\OpenCover.4.5.3607-rc27\OpenCover.Console.exe")
$xunitConsoleExe = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\packages\xunit.runners.2.0.0-beta5-build2785\tools\xunit.console.exe")
$testDll = "CodeCracker.Test.CSharp.dll"
$testDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\CodeCracker.Test\bin\Debug")
$logDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\log")
$outputXml = "$logDir\CodeCoverageResults.xml"

$allPaths = $openCoverExe, $xunitConsoleExe, $testDir, $logDir

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
    Write-Host -ForegroundColor Blue "Creating log directory $logDir"
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

. $openCoverExe -register:user -target:$xunitConsoleExe -targetargs:"$testDll -noshadow" -filter:"+[CodeCracker*]* -[CodeCracker.Test*]*" -output:$outputXml -coverbytest:*.Test.*.dll -targetdir:$testDir -log:All