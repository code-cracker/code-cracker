pushd "%~dp0src\CodeCracker\bin\Debug"
"%~dp0\packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe" pack Diagnostic.nuspec -OutputDirectory "%~dp0\"
popd