Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Style
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(InterfaceNameCodeFixProvider)), Composition.Shared>
    Public Class InterfaceNameCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.InterfaceName.ToDiagnosticId())

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create("Consider start Interface name with letter 'I'.",
                                              Function(c) ChangeInterfaceNameAsync(context.Document, diagnostic, c), NameOf(InterfaceNameCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function ChangeInterfaceNameAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Solution)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelfOfType(GetType(InterfaceStatementSyntax))
            Dim interfaceStatement = DirectCast(declaration, InterfaceStatementSyntax)

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim newName = "I" & interfaceStatement.Identifier.Text

            Dim solution = document.Project.Solution
            Dim symbol = semanticModel.GetDeclaredSymbol(interfaceStatement, cancellationToken)
            Dim options = solution.Workspace.Options
            Dim newSolution = Await Renamer.RenameSymbolAsync(solution, symbol, newName, options, cancellationToken).ConfigureAwait(False)
            Return newSolution
        End Function
    End Class
End Namespace