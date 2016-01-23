Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting

Namespace Style
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(PreferAnyToCountGreaterThanZeroCodeFixProvider)), Composition.Shared>
    Public Class PreferAnyToCountGreaterThanZeroCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId())
        Private Shared ReadOnly AnyName As SimpleNameSyntax = TryCast(SyntaxFactory.ParseName(NameOf(Any)), SimpleNameSyntax)

        Public NotOverridable Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create(Properties.Resources.PreferAnyToCountGreaterThanZeroAnalyzer_Title,
                                              Function(c) ConvertToAnyAsync(context.Document, diagnostic, c), NameOf(PreferAnyToCountGreaterThanZeroCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function ConvertToAnyAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = TryCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
            Dim node = root.FindNode(diagnostic.Location.SourceSpan)
            Dim greaterThanExpression = TryCast(node, BinaryExpressionSyntax)
            Dim leftExpression = greaterThanExpression.Left
            Dim memberExpression = leftExpression.DescendantNodesAndSelf().OfType(Of MemberAccessExpressionSyntax)().FirstOrDefault()
            Dim invocationExpression = TryCast(leftExpression, InvocationExpressionSyntax)
            Dim anyExpression = If(invocationExpression Is Nothing,
                SyntaxFactory.InvocationExpression(memberExpression.WithName(AnyName), SyntaxFactory.ArgumentList()),
                invocationExpression.WithExpression(memberExpression.WithName(AnyName))) _
                .WithLeadingTrivia(greaterThanExpression.GetLeadingTrivia()) _
                .WithTrailingTrivia(greaterThanExpression.GetTrailingTrivia())
            Dim newRoot = root.ReplaceNode(greaterThanExpression, anyExpression)
            newRoot = AddImportsSystemLinq(root, newRoot).WithAdditionalAnnotations(Formatter.Annotation)
            Return document.WithSyntaxRoot(newRoot)
        End Function

        Private Shared Function AddImportsSystemLinq(root As CompilationUnitSyntax, newRoot As CompilationUnitSyntax) As CompilationUnitSyntax
            Dim isUsingSystemLinq = root.Imports.Any(Function(u) u.ImportsClauses.ToString() = "System.Linq")
            If Not isUsingSystemLinq Then
                Dim clause As ImportsClauseSyntax = SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Linq").NormalizeWhitespace(elasticTrivia:=True))
                Dim clauseList = SyntaxFactory.SeparatedList({clause})
                Dim statement = SyntaxFactory.ImportsStatement(clauseList)
                newRoot = newRoot.AddImports(statement)
            End If

            Return newRoot
        End Function
    End Class
End Namespace