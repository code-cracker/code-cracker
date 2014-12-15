Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports System.Threading
Imports System.Collections.Immutable

Namespace TestHelper

    ' Class for turning strings into documents And getting the diagnostics on them. 
    ' All methods are Shared.
    Partial Public MustInherit Class DiagnosticVerifier

        Private Shared ReadOnly CorlibReference = MetadataReference.CreateFromAssembly(GetType(Object).Assembly)
        Private Shared ReadOnly SystemCoreReference = MetadataReference.CreateFromAssembly(GetType(Enumerable).Assembly)
        Private Shared ReadOnly VisualBasicSymbolsReference = MetadataReference.CreateFromAssembly(GetType(VisualBasicCompilation).Assembly)
        Private Shared ReadOnly CodeAnalysisReference = MetadataReference.CreateFromAssembly(GetType(Compilation).Assembly)

        Friend Shared DefaultFilePathPrefix As String = "Test"
        Friend Shared CSharpDefaultFileExt As String = "cs"
        Friend Shared VisualBasicDefaultExt As String = "vb"
        Friend Shared CSharpDefaultFilePath As String = DefaultFilePathPrefix & 0 & "." & CSharpDefaultFileExt
        Friend Shared VisualBasicDefaultFilePath As String = DefaultFilePathPrefix & 0 & "." & VisualBasicDefaultExt
        Friend Shared TestProjectName As String = "TestProject"

#Region " Get Diagnostics "

        ''' <summary>
        ''' Given classes in the form of strings, their language, And an IDiagnosticAnlayzer to apply to it, return the diagnostics found in the string after converting it to a document.
        ''' </summary>
        ''' <param name="sources">Classes in the form of strings</param>
        ''' <param name="language">The language the soruce classes are in</param>
        ''' <param name="analyzer">The analyzer to be run on the sources</param>
        ''' <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        Private Shared Function GetSortedDiagnostics(sources As String(), language As String, analyzer As DiagnosticAnalyzer) As Diagnostic()
            Return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, language))
        End Function

        ''' <summary>
        ''' Given an analyzer And a document to apply it to, run the analyzer And gather an array of diagnostics found in it.
        ''' The returned diagnostics are then ordered by location in the source document.
        ''' </summary>
        ''' <param name="analyzer">The analyzer to run on the documents</param>
        ''' <param name="documents">The Documents that the analyzer will be run on</param>
        ''' <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        Protected Shared Function GetSortedDiagnosticsFromDocuments(analyzer As DiagnosticAnalyzer, documents As Document()) As Diagnostic()

            Dim projects = New HashSet(Of Project)()
            For Each document In documents
                projects.Add(document.Project)
            Next

            Dim diagnostics = New List(Of Diagnostic)()
            For Each project In projects

                Dim compilation = project.GetCompilationAsync().Result
                Dim driver = AnalyzerDriver.Create(compilation, ImmutableArray.Create(analyzer), Nothing, compilation, CancellationToken.None)
                Dim discarded = compilation.GetDiagnostics
                Dim diags = driver.GetDiagnosticsAsync.Result
                For Each diag In diags

                    If diag.Location = Location.None OrElse diag.Location.IsInMetadata Then

                        diagnostics.Add(diag)
                    Else

                        For i = 0 To documents.Length - 1

                            Dim document = documents(i)
                            Dim tree = document.GetSyntaxTreeAsync().Result
                            If tree Is diag.Location.SourceTree Then

                                diagnostics.Add(diag)
                            End If
                        Next
                    End If
                Next
            Next

            Dim results = SortDiagnostics(diagnostics)
            diagnostics.Clear()

            Return results
        End Function

        ''' <summary>
        ''' Sort diagnostices by location in source document
        ''' </summary>
        ''' <param name="diagnostics">The list of Diagnostics to be sorted</param>
        ''' <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        Private Shared Function SortDiagnostics(diagnostics As IEnumerable(Of Diagnostic)) As Diagnostic()
            Return diagnostics.OrderBy(Function(d) d.Location.SourceSpan.Start).ToArray()
        End Function

#End Region

#Region " Set up compilation And documents"
        ''' <summary>
        ''' Given an array of strings as soruces And a language, turn them into a project And return the documents And spans of it.
        ''' </summary>
        ''' <param name="sources">Classes in the form of strings</param>
        ''' <param name="language">The language the source code is in</param>
        ''' <returns>An array fo rDocuments produced from the source strings</returns>
        Private Shared Function GetDocuments(sources As String(), language As String) As Document()

            If language <> LanguageNames.CSharp AndAlso language <> LanguageNames.VisualBasic Then
                Throw New ArgumentException("Unsupported Language")
            End If

            For i = 0 To sources.Length - 1
                Dim fileName As String = If(language = LanguageNames.CSharp, "Test" & i & ".cs", "Test" & i & ".vb")
            Next

            Dim Project = CreateProject(sources, language)
            Dim documents = Project.Documents.ToArray()

            If sources.Length <> documents.Length Then
                Throw New SystemException("Amount of sources did not match amount of Documents created")
            End If

            Return documents
        End Function

        ''' <summary>
        ''' Create a Document from a string through creating a project that contains it.
        ''' </summary>
        ''' <param name="source">Classes in the form of a string</param>
        ''' <param name="language">The language the source code Is in</param>
        ''' <returns>A Document created from the source string</returns>
        Protected Shared Function CreateDocument(source As String, Optional language As String = LanguageNames.CSharp) As Document
            Return CreateProject({source}, language).Documents.First()
        End Function

        ''' <summary>
        ''' Create a project using the inputted strings as sources.
        ''' </summary>
        ''' <param name="sources">Classes in the form of strings</param>
        ''' <param name="language">The language the source code is in</param>
        ''' <returns>A Project created out of the Douments created from the source strings</returns>
        Private Shared Function CreateProject(sources As String(), Optional language As String = LanguageNames.CSharp) As Project

            Dim fileNamePrefix As String = DefaultFilePathPrefix
            Dim fileExt As String = If(language = LanguageNames.CSharp, CSharpDefaultFileExt, VisualBasicDefaultExt)

            Dim projectId As projectId = projectId.CreateNewId(debugName:=TestProjectName)

            Dim Solution = New CustomWorkspace() _
                               .CurrentSolution _
                               .AddProject(projectId, TestProjectName, TestProjectName, language) _
                               .AddMetadataReference(projectId, CorlibReference) _
                               .AddMetadataReference(projectId, SystemCoreReference) _
                               .AddMetadataReference(projectId, VisualBasicSymbolsReference) _
                               .AddMetadataReference(projectId, CodeAnalysisReference)

            Dim count As Integer = 0

            For Each source In sources
                Dim newFileName = fileNamePrefix & count & "." & fileExt
                Dim documentId As documentId = documentId.CreateNewId(projectId, debugName:=newFileName)
                Solution = Solution.AddDocument(documentId, newFileName, SourceText.From(source))
                count += 1
            Next

            Return Solution.GetProject(projectId)
        End Function
#End Region
    End Class
End Namespace

