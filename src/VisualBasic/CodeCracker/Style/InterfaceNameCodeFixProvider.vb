Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Style
    <ExportCodeFixProvider("CodeCrackerInterfaceNameCodeFixProvider", LanguageNames.VisualBasic)>
    Public Class InterfaceNameCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.InterfaceName.ToDiagnosticId())
        End Function

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            Dim declaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelfOfType(GetType(InterfaceStatementSyntax))
            context.RegisterFix(CodeAction.Create("Consider start Interface name with letter 'I'.",
                                              Function(c) ChangeInterfaceNameAsync(context.Document, DirectCast(declaration, InterfaceStatementSyntax), c)), diagnostic)

        End Function

        Private Async Function ChangeInterfaceNameAsync(document As Document, interfaceStatement As InterfaceStatementSyntax, cancellationToken As CancellationToken) As Task(Of Solution)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim newName = "I" & interfaceStatement.Identifier.Text

            Dim solution = document.Project.Solution
            If solution Is Nothing Then Return Nothing
            Dim symbol = semanticModel.GetDeclaredSymbol(interfaceStatement, cancellationToken)
            If symbol Is Nothing Then Return Nothing
            Dim options = solution.Workspace.Options
            Dim newSolution = Await Renamer.RenameSymbolAsync(solution, symbol, newName, options, cancellationToken).ConfigureAwait(False)
            Return newSolution
        End Function
    End Class
End Namespace