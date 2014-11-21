pushd "%~dp0"
set currDir=%~dp0
set currDir=%currDir:~0,-1%
"%currDir%\packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe" pack "%currDir%\src\CodeCracker\CodeCracker.csproj" -Properties "Configuration=Debug;Platform=AnyCPU" -Symbols -OutputDirectory "%currDir%"
popd