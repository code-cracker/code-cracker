Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Refactoring
    Public MustInherit Class BaseAllowMembersOrderingCodeFixProvider
        Inherits CodeFixProvider

        Protected ReadOnly Property CodeActionDescription As String

        Protected Sub New(codeActionDescription As String)
            Me.CodeActionDescription = codeActionDescription
        End Sub

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(AllowMembersOrderingAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = diagnostic.Location.SourceSpan

            Dim typeBlock = DirectCast(root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelfOfType(GetType(ClassBlockSyntax), GetType(StructureBlockSyntax), GetType(ModuleBlockSyntax)), TypeBlockSyntax)
            Dim newDocument = Await AllowMembersOrderingAsync(context.Document, typeBlock, context.CancellationToken)
            If newDocument IsNot Nothing Then
                context.RegisterCodeFix(CodeAction.Create(String.Format(CodeActionDescription, typeBlock), Function(ct)
                                                                                                               Return Task.FromResult(newDocument)
                                                                                                           End Function), diagnostic)
            End If
        End Function

        Private Async Function AllowMembersOrderingAsync(document As Document, typeBlock As TypeBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim membersDeclaration = typeBlock.Members.OfType(Of DeclarationStatementSyntax)
            Dim root = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken), CompilationUnitSyntax)

            Dim newTypeBlock As TypeBlockSyntax = Nothing
            Dim orderChanged = TryReplaceTypeMembers(typeBlock, membersDeclaration, membersDeclaration.OrderBy(Function(member) member, GetMemberDeclarationComparer(document, cancellationToken)), newTypeBlock)
            If Not orderChanged Then Return Nothing

            Dim newDocument = document.WithSyntaxRoot(root.
                                                      ReplaceNode(typeBlock, newTypeBlock).
                                                      WithAdditionalAnnotations(Formatter.Annotation))
            Return newDocument
        End Function

        Protected MustOverride Function GetMemberDeclarationComparer(document As Document, cancellationToken As CancellationToken) As IComparer(Of DeclarationStatementSyntax)

        Private Function TryReplaceTypeMembers(typeBlock As TypeBlockSyntax, membersDeclaration As IEnumerable(Of DeclarationStatementSyntax), sortedMembers As IEnumerable(Of DeclarationStatementSyntax), ByRef orderedType As TypeBlockSyntax) As Boolean
            Dim sortedMembersQueue = New Queue(Of DeclarationStatementSyntax)(sortedMembers)
            Dim orderChanged = False
            orderedType = typeBlock.ReplaceNodes(membersDeclaration,
                                                 Function(original, rewritten)
                                                     Dim newMember = sortedMembersQueue.Dequeue()
                                                     If Not orderChanged And Not original.Equals(newMember) Then
                                                         orderChanged = True
                                                     End If
                                                     Return newMember
                                                 End Function)
            Return orderChanged
        End Function
    End Class
End Namespace