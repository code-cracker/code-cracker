Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports CodeCracker.Extensions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Refactoring
    <ExportCodeFixProvider("ParameterRefactoryCodeFixProvider", LanguageNames.VisualBasic)>
    <Shared()>
    Public Class ParameterRefactoryCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim diagnosticClass = context.Diagnostics.First()
            Dim diagnosticSpanClass = diagnosticClass.Location.SourceSpan
            Dim declarationClass = root.FindToken(diagnosticSpanClass.Start).Parent.FirstAncestorOfType(Of ClassStatementSyntax)

            Dim diagnosticNamespace = context.Diagnostics.First()
            Dim diagnosticNamespaceSpan = diagnosticNamespace.Location.SourceSpan
            Dim declarationNamespace = root.FindToken(diagnosticNamespaceSpan.Start).Parent.FirstAncestorOfType(Of NamespaceStatementSyntax)

            Dim diagnosticMethod = context.Diagnostics.First()
            Dim diagnosticMethodSpan = diagnosticMethod.Location.SourceSpan
            Dim declarationMethod = root.FindToken(diagnosticMethodSpan.Start).Parent.FirstAncestorOfType(Of MethodBaseSyntax)

            context.RegisterFix(CodeAction.Create("Change to new Class", Function(c) NewClassAsync(context.Document, declarationNamespace, declarationClass, declarationMethod, c)), diagnosticClass)
        End Function

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.ParameterRefactory.ToDiagnosticId())
        End Function
        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function NewClassAsync(document As Document, oldNamespace As NamespaceStatementSyntax, oldClass As ClassStatementSyntax, oldMethod As MethodBaseSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRootParameter As SyntaxNode
            If oldNamespace Is Nothing Then
                Dim newCompilation = NewCompilationFactory(DirectCast(oldClass.Parent, CompilationUnitSyntax), oldClass, oldMethod)
                newRootParameter = root.ReplaceNode(oldClass.Parent, newCompilation)
                Return document.WithSyntaxRoot(newRootParameter)
            End If
            Dim newNamespace = NewNamespaceFactory(oldNamespace, oldClass, oldMethod)
            newRootParameter = root.ReplaceNode(oldNamespace, newNamespace)
            Return document.WithSyntaxRoot(newRootParameter)
        End Function

        Public Property foo As String

        Private Shared Function NewPropertyClassFactory(methodOld As MethodStatementSyntax) As List(Of PropertyStatementSyntax)
            Dim properties = New List(Of PropertyStatementSyntax)
            For Each param In methodOld.ParameterList.Parameters
                Dim newProperty = SyntaxFactory.PropertyStatement(attributeLists:=New SyntaxList(Of AttributeListSyntax),
                                                               modifiers:=SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                                               identifier:=SyntaxFactory.Identifier(param.Identifier.GetText().ToString()),
                                                               parameterList:=SyntaxFactory.ParameterList(),
                                                               asClause:=param.AsClause,
                                                               initializer:=Nothing,
                                                               implementsClause:=Nothing)
                properties.Add(newProperty)
            Next
            Return properties
        End Function

        Private Shared Function NewClassParameterFactory(newClassName As String, properties As List(Of PropertyStatementSyntax)) As ClassBlockSyntax
            Dim declaration = SyntaxFactory.ClassStatement(newClassName).
                WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))

            Return SyntaxFactory.ClassBlock(declaration).
                WithMembers(SyntaxFactory.List(Of StatementSyntax)(properties)).
                WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Private Shared Function NewNamespaceFactory(oldNamespace As NamespaceBlockSyntax, oldClass As ClassStatementSyntax, oldMethod As MethodStatementSyntax) As NamespaceBlockSyntax
            Dim newNamespace = oldNamespace
            Dim className = "NewClass" & oldMethod.Keyword.Text
            Dim memberNamespaceOld = oldNamespace.Members.FirstOrDefault(Function(member) member.Equals(oldClass))
            newNamespace = oldNamespace.ReplaceNode(memberNamespaceOld, NewClassFactory(className, oldClass, oldMethod))
            Dim newParameterClass = NewClassParameterFactory(className, NewPropertyClassFactory(oldMethod))
            newNamespace = newNamespace.
                WithMembers(newNamespace.Members.Add(newParameterClass)).
                WithAdditionalAnnotations(Formatter.Annotation)
            Return newNamespace
        End Function

        Private Shared Function NewClassFactory(className As String, oldClass As ClassBlockSyntax, oldMethod As MethodStatementSyntax) As ClassBlockSyntax




            Dim parameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(Of ParameterSyntax).Add())
        End Function

    End Class
End Namespace