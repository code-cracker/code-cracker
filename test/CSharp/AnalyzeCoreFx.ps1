$ErrorActionPreference = "Stop"
$baseDir =  "$([System.IO.Path]::GetTempPath())$([System.Guid]::NewGuid().ToString().Substring(0,8))"
$projectDir =  "$baseDir\corefx"
$logDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\log")
$logFile = "$logDir\corefx.log"
$analyzerDll = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\src\CSharp\CodeCracker\bin\Debug\CodeCracker.CSharp.dll")
$gitPath = "https://github.com/dotnet/corefx.git"
if (Test-Path "C:\proj\corefx") {
    $gitPath = "C:\proj\corefx"
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

echo "Cloning corefx"
git clone --depth 5 -q $gitPath $projectDir
$itemsInCoreFx = ls $projectDir
if ($itemsInCoreFx -eq $null -or $itemsInCoreFx.Length -eq 0)
{
    echo "Unable to clone corefx, exiting."
    exit 2
}

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

echo "Building..."
msbuild "$projectDir\build.proj" /t:build /v:detailed /p:Configuration="Debug" /nologo /maxcpucount /p:SkipTests="True" >> $logFile

$ccBuildErrors = cat $logFile | Select-String "info AnalyzerDriver: The Compiler Analyzer 'CodeCracker"
if ($ccBuildErrors -ne $null)
{
    Write-Host "Errors found (see $logFile):"
    foreach($ccBuildError in $ccBuildErrors)
    {
        Write-Host -ForegroundColor DarkRed "$($ccBuildError.LineNumber) $($ccBuildError.Line)" 
    }
    throw "Errors found on the corefx analysis"
}
