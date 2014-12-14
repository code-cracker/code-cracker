pushd "%~dp0"
set currDir=%~dp0
set currDir=%currDir:~0,-1%
"%currDir%\packages\NuGet.CommandLine.2.8.2\tools\NuGet.exe" pack "%currDir%\src\CSharp\CodeCracker\CodeCracker.nuspec" -Properties "Configuration=Debug;Platform=AnyCPU" -OutputDirectory "%currDir%"
popd
