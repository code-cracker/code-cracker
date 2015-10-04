Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Namespace Usage

    Public NotInheritable Class UnusedParametersCodeFixAllProvider
        Inherits FixAllProvider

        Private Sub New()
            MyBase.New

        End Sub

        Public Shared Instance As UnusedParametersCodeFixAllProvider = New UnusedParametersCodeFixAllProvider
        Private Const message As String = "Remove unused parameter"

        Public Overrides Function GetFixAsync(ByVal fixAllContext As FixAllContext) As Task(Of CodeAction)
            Select Case (fixAllContext.Scope)
                Case FixAllScope.Document
                    Return Task.FromResult(CodeAction.Create(message, Async Function(ct) Await GetFixedSolutionAsync(fixAllContext, Await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Document))))
                Case FixAllScope.Project
                    Return Task.FromResult(CodeAction.Create(message, Async Function(ct) Await GetFixedSolutionAsync(fixAllContext, Await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Project))))
                Case FixAllScope.Solution
                    Return Task.FromResult(CodeAction.Create(message, Async Function(ct) Await GetFixedSolutionAsync(fixAllContext, Await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Solution))))
            End Select

            Return Nothing
        End Function

        Private Overloads Shared Async Function GetSolutionWithDocsAsync(ByVal fixAllContext As FixAllContext, ByVal solution As Solution) As Task(Of SolutionWithDocs)
            Dim docs = New List(Of DiagnosticsInDoc)
            Dim sol = New SolutionWithDocs() With {.Docs = docs, .Solution = solution}
            For Each pId In solution.Projects.Select(Function(p) p.Id)
                Dim project = sol.Solution.GetProject(pId)
                Dim newSol = Await GetSolutionWithDocsAsync(fixAllContext, project).ConfigureAwait(False)
                sol.Merge(newSol)
            Next
            Return sol
        End Function

        Private Overloads Shared Async Function GetSolutionWithDocsAsync(ByVal fixAllContext As FixAllContext, ByVal project As Project) As Task(Of SolutionWithDocs)
            Dim docs = New List(Of DiagnosticsInDoc)
            Dim newSolution = project.Solution
            For Each document In project.Documents
                Dim doc = Await GetDiagnosticsInDocAsync(fixAllContext, document)
                If doc.Equals(DiagnosticsInDoc.Empty) Then Continue For
                docs.Add(doc)
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, doc.TrackedRoot)
            Next
            Dim sol = New SolutionWithDocs() With {.Docs = docs, .Solution = newSolution}
            Return sol
        End Function

        Private Overloads Shared Async Function GetSolutionWithDocsAsync(ByVal fixAllContext As FixAllContext, ByVal document As Document) As Task(Of SolutionWithDocs)
            Dim docs = New List(Of DiagnosticsInDoc)
            Dim doc = Await GetDiagnosticsInDocAsync(fixAllContext, document)
            docs.Add(doc)
            Dim newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, doc.TrackedRoot)
            Dim sol = New SolutionWithDocs() With {.Docs = docs, .Solution = newSolution}
            Return sol
        End Function

        Private Shared Async Function GetDiagnosticsInDocAsync(ByVal fixAllContext As FixAllContext, ByVal document As Document) As Task(Of DiagnosticsInDoc)
            Dim diagnostics = Await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(False)
            If Not diagnostics.Any Then
                Return DiagnosticsInDoc.Empty
            End If
            Dim root = Await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(False)
            Dim doc = DiagnosticsInDoc.Create(document.Id, diagnostics, root)
            Return doc
        End Function

        Private Shared Async Function GetFixedSolutionAsync(ByVal fixAllContext As FixAllContext, ByVal sol As SolutionWithDocs) As Task(Of Solution)
            Dim newSolution = sol.Solution
            For Each doc In sol.Docs
                For Each node In doc.Nodes
                    Dim document = newSolution.GetDocument(doc.DocumentId)
                    Dim root = Await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(False)
                    Dim trackedNode = root.GetCurrentNode(node)
                    Dim parameter = trackedNode.AncestorsAndSelf().OfType(Of ParameterSyntax).First()
                    Dim docResults = Await UnusedParametersCodeFixProvider.RemoveParameterAsync(document, parameter, root, fixAllContext.CancellationToken)
                    For Each docResult In docResults
                        newSolution = newSolution.WithDocumentSyntaxRoot(docResult.DocumentId, docResult.Root)
                    Next
                Next
            Next
            Return newSolution
        End Function

        Private Structure DiagnosticsInDoc
            Public Shared Function Create(ByVal documentId As DocumentId, ByVal diagnostics As IList(Of Diagnostic), ByVal root As SyntaxNode) As DiagnosticsInDoc
                Dim nodes = diagnostics.Select(Function(d) root.FindNode(d.Location.SourceSpan)).Where(Function(n) Not n.IsMissing).ToList()
                Dim diagnosticsInDoc = New DiagnosticsInDoc() With {.DocumentId = documentId, .TrackedRoot = root.TrackNodes(nodes), .Nodes = nodes}
                Return diagnosticsInDoc
            End Function

            Public DocumentId As DocumentId

            Public Nodes As List(Of SyntaxNode)

            Public TrackedRoot As SyntaxNode

            Public Shared Property Empty As DiagnosticsInDoc = New DiagnosticsInDoc()
        End Structure

        Private Structure SolutionWithDocs

            Public Solution As Solution

            Public Docs As List(Of DiagnosticsInDoc)

            Public Sub Merge(ByVal sol As SolutionWithDocs)
                Solution = sol.Solution
                Docs.AddRange(sol.Docs)
            End Sub
        End Structure
    End Class
End Namespace