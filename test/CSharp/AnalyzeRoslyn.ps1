$ErrorActionPreference = "Stop"
$baseDir =  "$([System.IO.Path]::GetTempPath())$([System.Guid]::NewGuid().ToString().Substring(0,8))"
$projectDir =  "$baseDir\roslyn"
$logDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\log")
$logFile = "$logDir\roslyn.log"
$analyzerDll = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\CSharp\CodeCracker\bin\Debug\CodeCracker.CSharp.dll")
$gitPath = "https://github.com/dotnet/roslyn.git"
$gitPath = "d:\proj\roslyn"

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

echo "Cloning roslyn"
git clone -q $gitPath $projectDir
$itemsInProj = ls $projectDir
if ($itemsInProj -eq $null -or $itemsInProj.Length -eq 0)
{
    echo "Unable to clone, exiting."
    exit 2
}
git checkout 0dca10d517b4c43972ef124e2dc1a82ef0021da1

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

echo "Restoring dependencies"
msbuild "$projectDir\BuildAndTest.proj" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /t:RestorePackages /p:Configuration=""
if ($LASTEXITCODE -ne 0)
{
    echo "Not possible to restore build tools, stopping."
    return
}

$slns = ls "$projectDir\*.sln" -Recurse
echo "Building..."
foreach($sln in $slns)
{
    echo "Building $($sln.FullName)..."
    msbuild $sln.FullName /t:rebuild /v:detailed /p:Configuration="Debug" >> $logFile
}
$ccBuildErrors = cat $logFile | Select-String "info AnalyzerDriver: The Compiler Analyzer 'CodeCracker"
if ($ccBuildErrors -ne $null)
{
    echo "Errors found (see $logFile):"
    foreach($ccBuildError in $ccBuildErrors)
    {
        Write-Host -ForegroundColor DarkRed "$($ccBuildError.LineNumber) $($ccBuildError.Line)" 
    }
}