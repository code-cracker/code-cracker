Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider("CodeCrackerUnusedParametersCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class UnusedParametersCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim parameter = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of ParameterSyntax)
            context.RegisterCodeFix(CodeAction.Create(String.Format("Remove unused parameter: '{0}'", parameter.Identifier.GetText()), Function(c) RemovePArameterAsync(root, context.Document, parameter, c)), diagnostic)
        End Function

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.UnusedParameters.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Shared Async Function RemovePArameterAsync(root As SyntaxNode, document As Document, parameter As ParameterSyntax, cancellationToken As CancellationToken) As Task(Of Solution)
            Dim solution = document.Project.Solution
            Dim parameterList = DirectCast(parameter.Parent, ParameterListSyntax)
            Dim parameterPosition = parameterList.Parameters.IndexOf(parameter)
            Dim newParameterList = parameterList.WithParameters(parameterList.Parameters.Remove(parameter))
            Dim newSolution = solution
            Dim foundDocument = False
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim method = parameter.FirstAncestorOfType(GetType(SubNewStatementSyntax), GetType(MethodBlockSyntax))
            Dim methodSymbol = semanticModel.GetDeclaredSymbol(method)
            Dim references = Await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(False)
            Dim documentGroups = references.SelectMany(Function(r) r.Locations).GroupBy(Function(loc) loc.Document)
            For Each documentGroup In documentGroups
                Dim referencingDocument = documentGroup.Key
                Dim locRoot As SyntaxNode
                Dim locSemanticModel As SemanticModel
                Dim replacingArgs = New Dictionary(Of SyntaxNode, SyntaxNode)
                If referencingDocument.Equals(document) Then
                    locSemanticModel = semanticModel
                    locRoot = root
                    replacingArgs.Add(parameterList, newParameterList)
                    foundDocument = True
                Else
                    locSemanticModel = Await referencingDocument.GetSemanticModelAsync(cancellationToken)
                    locRoot = Await locSemanticModel.SyntaxTree.GetRootAsync(cancellationToken)
                End If
                For Each loc In documentGroup
                    Dim methodIdentifier = locRoot.FindNode(loc.Location.SourceSpan)
                    Dim objectCreation = TryCast(methodIdentifier.Parent, ObjectCreationExpressionSyntax)
                    Dim arguments = If(objectCreation IsNot Nothing,
                        objectCreation.ArgumentList,
                        methodIdentifier.FirstAncestorOfType(Of InvocationExpressionSyntax).ArgumentList)
                    Dim newArguments = arguments.WithArguments(arguments.Arguments.RemoveAt(parameterPosition))
                    replacingArgs.Add(arguments, newArguments)
                Next
                Dim newLocRoot = locRoot.ReplaceNodes(replacingArgs.Keys, Function(original, rewritten) replacingArgs(original))
                newSolution = newSolution.WithDocumentSyntaxRoot(referencingDocument.Id, newLocRoot)
            Next
            If Not foundDocument Then
                Dim newRoot = root.ReplaceNode(parameterList, newParameterList)
                Dim newDocument = document.WithSyntaxRoot(newRoot)
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newRoot)
            End If
            Return newSolution
        End Function
    End Class
End Namespace