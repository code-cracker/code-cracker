param($installPath, $toolsPath, $package, $project)

$analyzerPath = join-path $toolsPath "analyzers"
$analyzerFilePath = join-path $analyzerPath "CodeCracker.VisualBasic.dll"

$project.Object.AnalyzerReferences.Remove("$analyzerFilePath")