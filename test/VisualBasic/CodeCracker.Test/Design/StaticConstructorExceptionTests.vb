Imports System.IO
Imports CodeCracker.Test.TestHelper
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports System.Linq
Imports Xunit
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Class StaticConstructorExceptionTests
    Inherits CodeFixTest(Of StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider)

    <Fact>
    Public Async Function WarningIfExceptionIsThrownInsideStaticConstructor() As Task
        Dim test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Dim expected = New DiagnosticResult With {
            .Id = DesignDiagnostics.StaticConstructorExceptionAnalyzer,
            .Message = "Don't throw exceptions inside static constructors.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 4, 9)}
        }
        Await VerifyBasicDiagnosticsAsync(test, expected)
    End Function

    <Fact>
    Public Async Function NotWarningWhenNoExceptionIsThrownInsideStaticConstructor() As Task
        Dim test = "
Public Class TestClass
    Public Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function StaticConstructorWithoutException() As Task
        Dim test = "
Public Class TestClass
    Shared Sub New()
        
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)

    End Function

    <Fact>
    Public Async Function InstanceConstructorWithoutException() As Task
        Dim test = "
Public Class TestClass
    Public Sub New()
        
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)

    End Function

    <Fact>
    Public Sub WhenThrowIsRemovedFromStaticConstructor()
        Dim test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Dim fix = "
Public Class TestClass
    Shared Sub New()
    End Sub
End Class"
        VerifyBasicFix(test, fix, 0)

    End Sub

    <Fact>
    Sub CanGetTypeSymbolForInferedString()
        Dim code = "
    Class C
        Shared Sub Main()
            Dim b As String = """"
            Dim a = """"
        End Sub
    End Class"

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Dim compilation = VisualBasicCompilation.Create("test", {tree}, {MetadataReference.CreateFromAssembly(GetType(Object).Assembly)})

        Dim result = compilation.Emit(New MemoryStream)

        Dim semanticModel = compilation.GetSemanticModel(tree)

        Dim localNodes = tree.GetRoot().DescendantNodes.OfType(Of LocalDeclarationStatementSyntax)
        For Each node In localNodes
            Dim localSym = semanticModel.GetDeclaredSymbol(node.Declarators.Single.Names.Single)
            Trace.WriteLine(localSym.ToDisplayString())

            ' TODO: Figure how to get the typeinfo from inferred type
            Dim symbol = semanticModel.GetTypeInfo(node) ' Is Nothing
            Dim variableType = node.Declarators.First.AsClause?.Type ' This is null for inferred types
            If variableType IsNot Nothing Then
                Dim typeSymbol = SemanticModel.GetTypeInfo(variableType).ConvertedType
                If typeSymbol.IsReferenceType AndAlso typeSymbol.SpecialType <> SpecialType.System_String Then
                    '
                    'Then
                    'If node.Declarators.First.AsClause() IsNot Nothing Then
                    'Assert.True(node.Declarators.First.AsClause.Type.r)
                    'Assert.Equal(node.Declarators.First.AsClause.VBKind, vbString)
                End If
            End If


        Next
    End Sub

    Public Class TestClass
        Shared Sub New()
            Throw New System.Exception()
        End Sub
    End Class

End Class

