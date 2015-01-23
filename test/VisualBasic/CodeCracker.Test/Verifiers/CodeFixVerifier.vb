Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports System.Collections.Generic
Imports System.Threading
Imports Xunit

Namespace TestHelper
    ''' <summary>
    ''' Superclass of all Unit tests made for diagnostics with codefixes.
    ''' Contains methods used to verify correctness of codefixes
    ''' </summary>
    Partial Public MustInherit Class CodeFixVerifier
        Inherits DiagnosticVerifier
        ''' <summary>
        ''' Returns the codefix being tested (C#) - to be implemented in non-abstract class
        ''' </summary>
        ''' <returns>The CodeFixProvider to be used for CSharp code</returns>
        Protected Overridable Function GetCSharpCodeFixProvider() As CodeFixProvider
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns the codefix being tested (VB) - to be implemented in non-abstract class
        ''' </summary>
        ''' <returns>The CodeFixProvider to be used for VisualBasic code</returns>
        Protected Overridable Function GetBasicCodeFixProvider() As CodeFixProvider
            Return Nothing
        End Function

        ''' <summary>
        ''' Called to test a C# codefix when applied on the inputted string as a source
        ''' </summary>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Protected Async Function VerifyCSharpFixAsync(oldSource As String, newSource As String, Optional codeFixIndex As Integer? = Nothing, Optional allowNewCompilerDiagnostics As Boolean = False) As Task
            Await VerifyFixAsync(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics)
        End Function

        ''' <summary>
        ''' Called to test a VB codefix when applied on the inputted string as a source
        ''' </summary>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Protected Async Function VerifyBasicFixAsync(oldSource As String, newSource As String, Optional codeFixIndex As Integer? = Nothing, Optional allowNewCompilerDiagnostics As Boolean = False) As Task
            Await VerifyFixAsync(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics)
        End Function

        ''' <summary>
        ''' General verifier for codefixes.
        ''' Creates a Document from the source string, then gets diagnostics on it And applies the relevant codefixes.
        ''' Then gets the string after the codefix Is applied And compares it with the expected result.
        ''' Note: If any codefix causes New diagnostics To show up, the test fails unless allowNewCompilerDiagnostics Is Set To True.
        ''' </summary>
        ''' <param name="language">The language the source code Is in</param>
        ''' <param name="analyzer">The analyzer to be applied to the source code</param>
        ''' <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic Is found</param>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Private Async Function VerifyFixAsync(language As String, analyzer As DiagnosticAnalyzer, codeFixProvider As CodeFixProvider, oldSource As String, newSource As String, codeFixIndex As Integer?, allowNewCompilerDiagnostics As Boolean) As Task

            Dim document = CreateDocument(oldSource, language)
            Dim analyzerDiagnostics = Await GetSortedDiagnosticsFromDocumentsAsync(analyzer, New Document() {document})
            Dim compilerDiagnostics = Await GetCompilerDiagnosticsAsync(document)
            Dim attempts = analyzerDiagnostics.Length

            For i = 0 To attempts - 1
                Dim actions = New List(Of CodeAction)()
                Dim context = New CodeFixContext(document, analyzerDiagnostics(0), Sub(a, d) actions.Add(a), CancellationToken.None)
                codeFixProvider.ComputeFixesAsync(context).Wait()

                If Not actions.Any() Then
                    Exit For
                End If

                If (codeFixIndex IsNot Nothing) Then
                    document = Await ApplyFixAsync(document, actions.ElementAt(codeFixIndex))
                    Exit For
                End If

                document = Await ApplyFixAsync(document, actions.ElementAt(0))
                analyzerDiagnostics = Await GetSortedDiagnosticsFromDocumentsAsync(analyzer, New Document() {document})

                Dim newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, Await GetCompilerDiagnosticsAsync(document))

                'check if applying the code fix introduced any New compiler diagnostics
                If Not allowNewCompilerDiagnostics AndAlso newCompilerDiagnostics.Any() Then
                    ' Format And get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace))
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, Await GetCompilerDiagnosticsAsync(document))

                    Dim test = Await GetStringFromDocumentAsync(document)

                    Assert.True(False,
                        String.Format("Fix introduced new compiler diagnostics:{2}{0}{2}{2}New document:{2}{1}{2}",
                            String.Join(vbNewLine, newCompilerDiagnostics.Select(Function(d) d.ToString())),
                            document.GetSyntaxRootAsync().Result.ToFullString(), vbNewLine))
                End If

                'check if there are analyzer diagnostics left after the code fix
                If Not analyzerDiagnostics.Any() Then
                    Exit For
                End If
            Next

            'after applying all of the code fixes, compare the resulting string to the inputted one
            Dim actual = Await GetStringFromDocumentAsync(document)
            Assert.Equal(newSource, actual)
        End Function
    End Class
End Namespace