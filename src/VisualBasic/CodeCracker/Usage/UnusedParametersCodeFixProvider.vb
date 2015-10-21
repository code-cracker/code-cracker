Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(UnusedParametersCodeFixProvider)), Composition.Shared>
    Public Class UnusedParametersCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim Diagnostic = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create(
                                    String.Format("Remove unused parameter: '{0}'", Diagnostic.Properties("identifier")),
                                    Function(c) RemoveParameterAsync(context.Document, Diagnostic, c),
                                    NameOf(UnusedParametersCodeFixProvider)),
                                    Diagnostic)
            Return Task.FromResult(0)
        End Function

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.UnusedParameters.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return UnusedParametersCodeFixAllProvider.Instance
        End Function
        Private Shared Async Function RemoveParameterAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Solution)
            Dim solution = document.Project.Solution
            Dim newSolution = solution
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim parameter = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of ParameterSyntax)
            Dim docs = Await RemoveParameterAsync(document, parameter, root, cancellationToken)
            For Each doc In docs
                newSolution = newSolution.WithDocumentSyntaxRoot(doc.DocumentId, doc.Root)
            Next
            Return newSolution
        End Function

        Public Shared Async Function RemoveParameterAsync(document As Document, parameter As ParameterSyntax, root As SyntaxNode, cancellationToken As CancellationToken) As Task(Of List(Of DocumentIdAndRoot))
            Dim solution = document.Project.Solution
            Dim parameterList = DirectCast(parameter.Parent, ParameterListSyntax)
            Dim parameterPosition = parameterList.Parameters.IndexOf(parameter)
            Dim newParameterList = parameterList.WithParameters(parameterList.Parameters.Remove(parameter))
            Dim foundDocument = False
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim method = parameter.FirstAncestorOfType(GetType(SubNewStatementSyntax), GetType(MethodBlockSyntax))
            Dim methodSymbol = semanticModel.GetDeclaredSymbol(method)
            Dim references = Await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(False)
            Dim documentGroups = references.SelectMany(Function(r) r.Locations).GroupBy(Function(loc) loc.Document)
            Dim docs = New List(Of DocumentIdAndRoot)
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
                    If parameter.Modifiers.Any(Function(m) m.IsKind(SyntaxKind.ParamArrayKeyword)) Then
                        Dim newArguments = arguments
                        While newArguments.Arguments.Count > parameterPosition
                            newArguments = newArguments.WithArguments(newArguments.Arguments.RemoveAt(parameterPosition))
                        End While
                        replacingArgs.Add(arguments, newArguments)
                    Else
                        Dim newArguments = arguments.WithArguments(arguments.Arguments.RemoveAt(parameterPosition))
                        replacingArgs.Add(arguments, newArguments)
                    End If
                Next
                Dim newLocRoot = locRoot.ReplaceNodes(replacingArgs.Keys, Function(original, rewritten) replacingArgs(original))
                docs.Add(New DocumentIdAndRoot With {.DocumentId = referencingDocument.Id, .Root = newLocRoot})
            Next
            If Not foundDocument Then
                Dim newRoot = root.ReplaceNode(parameterList, newParameterList)
                Dim newDocument = document.WithSyntaxRoot(newRoot)
                docs.Add(New DocumentIdAndRoot With {.DocumentId = document.Id, .Root = newRoot})
            End If
            Return docs
        End Function

        Public Structure DocumentIdAndRoot
            Friend DocumentId As DocumentId
            Friend Root As SyntaxNode
        End Structure
    End Class
End Namespace