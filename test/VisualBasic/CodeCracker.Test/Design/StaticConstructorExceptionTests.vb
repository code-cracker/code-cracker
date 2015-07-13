Imports CodeCracker.VisualBasic.Design
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.IO
Imports Xunit

Namespace Design
    Public Class StaticConstructorExceptionTests
        Inherits CodeFixVerifier(Of StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider)

        <Fact>
        Public Async Function WarningIfExceptionIsThrownInsideStaticConstructor() As Task
            Const test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

            Dim expected = New DiagnosticResult With {
                .Id = DiagnosticId.StaticConstructorException.ToDiagnosticId(),
                .Message = "Don't throw exceptions inside static constructors.",
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 4, 9)}
            }
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function NotWarningWhenNoExceptionIsThrownInsideStaticConstructor() As Task
            Const test = "
Public Class TestClass
    Public Sub New()
        Throw New System.Exception()
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function StaticConstructorWithoutException() As Task
            Const test = "
Public Class TestClass
    Shared Sub New()
        
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)

        End Function

        <Fact>
        Public Async Function InstanceConstructorWithoutException() As Task
            Const test = "
Public Class TestClass
    Public Sub New()
        
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(test)

        End Function

        <Fact>
        Public Async Function WhenThrowIsRemovedFromStaticConstructor() As Task
            Const test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

            Const fix = "
Public Class TestClass
    Shared Sub New()
    End Sub
End Class"
            Await VerifyBasicFixAsync(test, fix, 0)

        End Function

        <Fact>
        Sub CanGetTypeSymbolForInferedString()
            Const code = "
    Class C
        Shared Sub Main()
            Dim b As String = """"
            Dim a = """"
        End Sub
    End Class"

            Dim tree = SyntaxFactory.ParseSyntaxTree(code)
            Dim compilation = VisualBasicCompilation.Create("test", {tree}, {MetadataReference.CreateFromFile(GetType(Object).Assembly.Location)})

            Dim result = compilation.Emit(New MemoryStream)

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim localNodes = tree.GetRoot().DescendantNodes.OfType(Of LocalDeclarationStatementSyntax)
            For Each node In localNodes
                Dim localSym = semanticModel.GetDeclaredSymbol(node.Declarators.Single.Names.Single)
                Trace.WriteLine(localSym.ToDisplayString())

                Dim symbol = semanticModel.GetTypeInfo(node) ' Is Nothing
                Dim variableType = node.Declarators.First.AsClause?.Type ' This is null for inferred types
                If variableType IsNot Nothing Then
                    Dim typeSymbol = semanticModel.GetTypeInfo(variableType).ConvertedType
                End If
            Next
        End Sub

    End Class
End Namespace