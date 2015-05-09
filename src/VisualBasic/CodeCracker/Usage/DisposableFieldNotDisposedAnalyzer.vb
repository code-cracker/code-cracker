Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class DisposableFieldNotDisposedAnalyzer
        Inherits DiagnosticAnalyzer

        Friend Const Title = "Dispose Fields Properly"
        Friend Const MessageFormat = "Field {0} implements IDisposable and should be disposed."
        Private Const Description = "This class has a disposable field and is not disposing it."

        Friend Shared RuleForReturned As New DiagnosticDescriptor(
            DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.DisposableFieldNotDisposed_Returned))

        Friend Shared RuleForCreated As New DiagnosticDescriptor(
            DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(),
            Title,
            MessageFormat,
            SupportedCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLinkUri:=HelpLink.ForDiagnostic(DiagnosticId.DisposableFieldNotDisposed_Created))


        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(RuleForCreated, RuleForReturned)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSymbolAction(AddressOf AnalyzeField, SymbolKind.Field)
        End Sub

        Private Sub AnalyzeField(context As SymbolAnalysisContext)
            If (context.IsGenerated()) Then Return
            Dim fieldSymbol = DirectCast(context.Symbol, IFieldSymbol)
            If Not fieldSymbol.Type.AllInterfaces.Any(Function(i) i.ToString().EndsWith("IDisposable")) AndAlso Not fieldSymbol.Type.ToString().EndsWith("IDisposable") Then Exit Sub
            Dim fieldSyntaxRef = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault
            If fieldSyntaxRef Is Nothing Then Exit Sub
            Dim variableDeclarator = TryCast(fieldSyntaxRef.GetSyntax().Parent, VariableDeclaratorSyntax)
            If variableDeclarator Is Nothing Then Exit Sub
            If ContainingTypeImplementsIDisposableAndCallsItOnTheField(context, fieldSymbol, fieldSymbol.ContainingType) Then Exit Sub
            If variableDeclarator.AsClause.Kind = SyntaxKind.AsNewClause Then
                context.ReportDiagnostic(Diagnostic.Create(RuleForCreated, variableDeclarator.GetLocation(), fieldSymbol.Name))
            ElseIf TypeOf (variableDeclarator.Initializer?.Value) Is InvocationExpressionSyntax Then
                context.ReportDiagnostic(Diagnostic.Create(RuleForReturned, variableDeclarator.GetLocation(), fieldSymbol.Name))
            ElseIf TypeOf (variableDeclarator.Initializer?.Value) Is ObjectCreationExpressionSyntax Then
                context.ReportDiagnostic(Diagnostic.Create(RuleForCreated, variableDeclarator.GetLocation(), fieldSymbol.Name))
            End If
        End Sub

        Private Function ContainingTypeImplementsIDisposableAndCallsItOnTheField(context As SymbolAnalysisContext, fieldSymbol As IFieldSymbol, typeSymbol As INamedTypeSymbol) As Boolean
            If typeSymbol Is Nothing Then Return False
            Dim disposableInterface = typeSymbol.AllInterfaces.FirstOrDefault(Function(i) i.ToString().EndsWith("IDisposable"))
            If disposableInterface Is Nothing Then Return False

            Dim disposableMethod = disposableInterface.GetMembers("Dispose").OfType(Of IMethodSymbol).FirstOrDefault(Function(d) d.Arity = 0)
            Dim disposeMethodSymbol = DirectCast(typeSymbol.FindImplementationForInterfaceMember(disposableMethod), IMethodSymbol)
            If disposeMethodSymbol Is Nothing Then Return False

            Dim disposeMethod = TryCast(disposeMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().Parent, MethodBlockSyntax)
            If disposeMethod Is Nothing Then Return False
            If disposeMethod.SubOrFunctionStatement.Modifiers.Any(SyntaxKind.MustInheritKeyword) Then Return True
            Dim typeDeclaration = DirectCast(typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().Parent, TypeBlockSyntax)
            Dim semanticModel = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree)
            If CallsDisposeOnField(fieldSymbol, disposeMethod, semanticModel) Then Return True

            Return False
        End Function

        Private Shared Function CallsDisposeOnField(fieldSymbol As IFieldSymbol, disposeMethod As MethodBlockSyntax, semanticModel As SemanticModel) As Boolean
            Dim hasDisposeCall = disposeMethod.Statements.OfType(Of ExpressionStatementSyntax).
                Any(Function(exp)
                        Dim invocation = TryCast(exp.Expression, InvocationExpressionSyntax)
                        If Not If(invocation?.Expression?.IsKind(SyntaxKind.SimpleMemberAccessExpression), True) Then Return False
                        If invocation.ArgumentList.Arguments.Any() Then Return False  ' Calling the wrong dispose method
                        Dim memberAccess = DirectCast(invocation.Expression, MemberAccessExpressionSyntax)
                        If memberAccess.Name.Identifier.ToString() <> "Dispose" OrElse memberAccess.Name.Arity <> 0 Then Return False
                        Dim memberAccessIndentifier = TryCast(memberAccess.Expression, IdentifierNameSyntax)
                        If memberAccessIndentifier Is Nothing Then Return False
                        Return fieldSymbol.Equals(semanticModel.GetSymbolInfo(memberAccessIndentifier).Symbol)
                    End Function)
            Return hasDisposeCall
        End Function
    End Class
End Namespace