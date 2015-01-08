Option Strict On
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Newtonsoft.Json

Public Module TestHelpers
#Region " Set up compilation and documents "
    Private Const DefaultFilePathPrefix = "Test"
    Private Const CSharpDefaultFileExt = "cs"
    Private Const CSharpDefaultFilePath = DefaultFilePathPrefix & "0." & CSharpDefaultFileExt
    Private Const VisualBasicDefaultExt = "vb"
    Private Const VisualBasicDefaultFilePath = DefaultFilePathPrefix & "0." & VisualBasicDefaultExt
    Private Const TestProjectName = "TestProject"

    Private ReadOnly CorlibReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(Object).Assembly)
    Private ReadOnly SystemCoreReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(Enumerable).Assembly)
    Private ReadOnly RegexReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(System.Text.RegularExpressions.Regex).Assembly)
    Private ReadOnly BasicSymbolsReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(Microsoft.VisualBasic.Collection).Assembly)
    Private ReadOnly CodeAnalysisReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(Compilation).Assembly)
    Private ReadOnly JsonNetReference As MetadataReference = MetadataReference.CreateFromAssembly(GetType(JsonConvert).Assembly)

    Public Function GetDocuments(sources() As String, language As String) As Document()
        If language <> LanguageNames.CSharp AndAlso language <> LanguageNames.VisualBasic Then
            Throw New ArgumentException("Unsupported Language")
        End If

        For i = 0 To sources.Length - 1
            Dim filaname = IIf(language = LanguageNames.CSharp, "test" & i.ToString() & ".cs", "Test" & i.ToString() & ".vb")
        Next

        Dim project = CreateProject(sources, language)
        Dim documents = project.Documents.ToArray()

        If sources.Length <> documents.Length Then
            Throw New SystemException("Amount of sources did not match amount of Documents created")
        End If

        Return documents
    End Function

    Public Function CreateProject(sources() As String, Optional language As String = LanguageNames.VisualBasic) As Project
        Dim workspace As CustomWorkspace = Nothing
        Return CreateProject(sources, workspace, language)
    End Function

    Public Function CreateProject(sources() As String, workspace As CustomWorkspace, Optional language As String = LanguageNames.VisualBasic) As Project
        Dim fileNamePrefix = DefaultFilePathPrefix
        Dim fileExt = If(language = LanguageNames.CSharp, CSharpDefaultFileExt, VisualBasicDefaultExt)

        Dim projId = ProjectId.CreateNewId(TestProjectName)
        workspace = New TestCustomWorkspace()
        Dim projInfo = ProjectInfo.Create(projId, VersionStamp.Create, TestProjectName, TestProjectName, language,
                                          metadataReferences:=ImmutableList.Create(CorlibReference, SystemCoreReference, RegexReference, BasicSymbolsReference, CodeAnalysisReference, JsonNetReference))

        workspace.AddProject(projInfo)

        Dim count = "0"
        For Each source In sources
            Dim newFileName = fileNamePrefix & count & "." & fileExt
            workspace.AddDocument(projId, newFileName, SourceText.From(source))
        Next

        Return workspace.CurrentSolution.GetProject(projId)
    End Function
#End Region
End Module
