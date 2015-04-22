Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic


Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class DisposablesShouldCallSuppressFinalizeAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Disposables Should Call SuppressFinalize"
        Friend Const MessageFormat = "'{0}' should call GC.SuppressFinalize inside the Dispose method."
        Private Const Description = "Classes implementing IDisposable should call the GC.SuppressFinalize method  to avoid any finalizer from being called.
This rule should be followed even if the class doesn't have a finalizer in a derived class."

        Friend Shared Rule As New DiagnosticDescriptor(
        DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
        Title,
        MessageFormat,
        SupportedCategories.Naming,
        DiagnosticSeverity.Warning,
        isEnabledByDefault:=False,
        description:=Description,
        helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.DisposablesShouldCallSuppressFinalize))

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSymbolAction(AddressOf AnalyzeAsync, SymbolKind.NamedType)
        End Sub

        Public Async Sub AnalyzeAsync(context As SymbolAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim symbol = DirectCast(context.Symbol, INamedTypeSymbol)
            If symbol.TypeKind <> TypeKind.Class Then Exit Sub
            If Not symbol.Interfaces.Any(Function(i) i.SpecialType = SpecialType.System_IDisposable) Then Exit Sub

            Dim disposeMethod = FindDisposeMethod(symbol)
            If disposeMethod Is Nothing Then Exit Sub

            Dim syntaxTree = Await disposeMethod.DeclaringSyntaxReferences(0)?.GetSyntaxAsync(context.CancellationToken)
            Dim statements = TryCast(syntaxTree, MethodBlockSyntax)?.Statements.OfType(Of ExpressionStatementSyntax)

            If statements IsNot Nothing Then
                For Each statement In statements
                    Dim invocation = TryCast(statement.Expression, InvocationExpressionSyntax)
                    If invocation IsNot Nothing Then
                        Dim method = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
                        If method IsNot Nothing Then
                            If DirectCast(method.Expression, IdentifierNameSyntax).Identifier.ToString = "GC" AndAlso method.Name.ToString() = "SuppressFinalize" Then
                                Exit Sub
                            End If
                        End If
                    End If
                Next
            End If
            context.ReportDiagnostic(Diagnostic.Create(Rule, disposeMethod.Locations(0), symbol.Name))
        End Sub

        Private Shared Function FindDisposeMethod(symbol As INamedTypeSymbol) As ISymbol
            Dim methods = symbol.GetMembers().Where(Function(x) x.ToString().Contains("Dispose")).OfType(Of IMethodSymbol)

            Dim disposeMethod As ISymbol = Nothing
            For Each method In methods
                If method.Parameters.Any() Then
                    If method.Parameters(0).Type.SpecialType = SpecialType.System_Boolean Then
                        Return method
                    End If
                Else
                    disposeMethod = method ' Version without arguments, but keep checking in case there's one with the boolean dispose override
                End If
            Next
            Return disposeMethod
        End Function
    End Class
End Namespace