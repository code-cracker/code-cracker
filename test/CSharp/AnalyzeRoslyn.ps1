$ErrorActionPreference = "Stop"
$baseDir =  "$([System.IO.Path]::GetTempPath())$([System.Guid]::NewGuid().ToString().Substring(0,8))"
$projectDir =  "$baseDir\roslyn"
$logDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\log")
$logFile = "$logDir\roslyn.log"
$analyzerDll = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\CSharp\CodeCracker\bin\Debug\CodeCracker.CSharp.dll")
$analyzerDllVB = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\VisualBasic\CodeCracker\bin\Debug\CodeCracker.VisualBasic.dll")
$gitPath = "https://github.com/dotnet/roslyn.git"
if (Test-Path "c:\proj\roslyn") {
    $gitPath = "c:\proj\roslyn"
}

echo "Saving to log file $logFile"
echo "Analyzer C# dll is $analyzerDll"
echo "Analyzer VB dll is $analyzerDllVB"

if ($analyzerDll -eq $null)
{
    echo "Analyzer C# dll not found"
    exit 1
}
if ($analyzerDllVB -eq $null)
{
    echo "Analyzer VB dll not found"
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
git clone --depth 5 -q $gitPath $projectDir
$itemsInProj = ls $projectDir
if ($itemsInProj -eq $null -or $itemsInProj.Length -eq 0)
{
    echo "Unable to clone, exiting."
    exit 1
}

echo "Adding Code Cracker to projects..."
$csprojs = ls "$projectDir\*.csproj" -Recurse
if ($csprojs -eq $null)
{
    echo "csprojs not found"
    exit 2
}
$vbprojs = ls "$projectDir\*.vbproj" -Recurse
if ($vbprojs -eq $null)
{
    echo "vbprojs not found"
    exit 3
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
foreach($proj in $vbprojs)
{
    echo "Adding analyzer to $($proj.Name)"
    [xml]$xmlProj = cat $proj
    $itemGroup = $xmlProj.CreateElement("ItemGroup", $xmlProj.Project.xmlns)
    $analyzer = $xmlProj.CreateElement("Analyzer", $xmlProj.Project.xmlns)
    $analyzer.SetAttribute("Include", $analyzerDllVB)
    $itemGroup.AppendChild($analyzer) | Out-Null
    $xmlProj.DocumentElement.AppendChild($itemGroup) | Out-Null
    $xmlProj.Save($proj.FullName)
}

echo "Restoring dependencies"
msbuild "$projectDir\BuildAndTest.proj" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /t:RestorePackages /p:Configuration=""
if ($LASTEXITCODE -ne 0)
{
    echo "Not possible to restore build tools, stopping."
    return
}

echo "Building..."
msbuild "$projectDir\src\RoslynLight.sln" /t:rebuild /v:detailed /p:Configuration="Debug" >> $logFile

$ccBuildErrors = cat $logFile | Select-String "info AnalyzerDriver: The Compiler Analyzer 'CodeCracker"
if ($ccBuildErrors -ne $null)
{
    Write-Host "Errors found (see $logFile):"
    foreach($ccBuildError in $ccBuildErrors)
    {
        Write-Host -ForegroundColor DarkRed "$($ccBuildError.LineNumber) $($ccBuildError.Line)" 
    }
    throw "Errors found on the roslyn analysis"
}