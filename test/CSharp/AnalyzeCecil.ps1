$ErrorActionPreference = "Stop"
$baseDir =  "$([System.IO.Path]::GetTempPath())$([System.Guid]::NewGuid().ToString().Substring(0,8))"
$projectDir =  "$baseDir\cecil"
$logDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\log")
$logFile = "$logDir\cecil.log"
$analyzerDll = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\CSharp\CodeCracker\bin\Debug\CodeCracker.CSharp.dll")
$gitPath = "https://github.com/jbevain/cecil.git"
if (Test-Path "C:\proj\cecil") {
    $gitPath = "c:\proj\cecil"
}

echo "Saving to log file $logFile"
echo "Analyzer dll is $analyzerDll"

if ($analyzerDll -eq $null)
{
    echo "Analyzer dll not found"
    exit 1
}

if ((Test-Path $logDir) -eq $false)
{
    echo "Creating log directory $logDir"
    mkdir $logDir | Out-Null
}
echo "" > $logFile

if ((Test-Path $projectDir) -eq $false)
{
    echo "Creating project directory $projectDir"
    mkdir $projectDir | Out-Null
}

echo "Cloning cecil"
git clone -q $gitPath $projectDir
$itemsInProj = ls $projectDir
if ($itemsInProj -eq $null -or $itemsInProj.Length -eq 0)
{
    echo "Unable to clone, exiting."
    exit 2
}
#git --git-dir=$projectDir\.git --work-tree=$projectDir checkout d3cd20772c4f2cc3c7997357dfdf43417c063005

echo "Adding Code Cracker to projects..."
$csprojs = ls "$projectDir\*.csproj" -Recurse
if ($csprojs -eq $null)
{
    echo "Analyzer dll not found"
    exit 1
}

foreach($csproj in $csprojs)
{
    echo "Adding analyzer to $($csproj.Name)"
    [xml]$xmlProj = cat $csproj
    $itemGroup = $xmlProj.CreateElement("ItemGroup", $xmlProj.Project.xmlns)
    $analyzer = $xmlProj.CreateElement("Analyzer", $xmlProj.Project.xmlns)
    $analyzer.SetAttribute("Include", $analyzerDll)
    $itemGroup.AppendChild($analyzer) | Out-Null
    $xmlProj.DocumentElement.AppendChild($itemGroup) | Out-Null
    $xmlProj.Save($csproj.FullName)
}

$slns = ls "$projectDir\*.sln" -Recurse
echo "Building..."
foreach($sln in $slns)
{
    echo "Building $($sln.FullName)..."
    msbuild $sln.FullName /m /t:rebuild /v:detailed /p:Configuration="net_4_0_Debug" >> $logFile
}
$ccBuildErrors = cat $logFile | Select-String "info AnalyzerDriver: The Compiler Analyzer 'CodeCracker"
if ($ccBuildErrors -ne $null)
{
    write-host "Errors found (see $logFile):"
    foreach($ccBuildError in $ccBuildErrors)
    {
        Write-Host -ForegroundColor DarkRed "$($ccBuildError.LineNumber) $($ccBuildError.Line)" 
    }
    throw "Errors found on the cecil analysis"
}