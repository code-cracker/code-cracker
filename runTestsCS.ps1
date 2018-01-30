param([String]$testClass)
# $testDllDirPath = "$PSScriptRoot\test\CSharp\CodeCracker.Test\bin\Debug\"
$testDllDirPath = "test\CSharp\CodeCracker.Test\bin\Debug\"
$testDllFileName = "CodeCracker.Test.CSharp.dll"
$testDllFullFileName = "$testDllDirPath$testDllFileName"
# $xunitConsole = "$PSScriptRoot\packages\xunit.runner.console.2.2.0\tools\xunit.console.x86.exe"
$xunitConsole = "packages\xunit.runner.console.2.2.0\tools\xunit.console.x86.exe"

if (!(gcm nodemon -ErrorAction Ignore)) {
    Write-Host -ForegroundColor DarkRed 'Nodemon not found, install it with npm: `npm i -g nodemon`'
    return
}

if ($testClass -eq "now"){
    . $xunitConsole "$testDllFullFileName"
    return
}


Write-Host "Watching $testDllDirPath"
If ($testClass) {
    If ($testClass.StartsWith("CodeCracker.Test") -eq $false) {
        $testClass = "CodeCracker.Test.CSharp.$testClass"
    }
    Write-Host "Only for $testClass"
}

If ($testClass) {
    nodemon --watch $testDllFullFileName --exec "`"$xunitConsole`" `"$testDllFullFileName`" -class $testClass || exit 1"
} Else {
    nodemon --watch $testDllFullFileName --exec "`"$xunitConsole`" `"$testDllFullFileName`" || exit 1"
}