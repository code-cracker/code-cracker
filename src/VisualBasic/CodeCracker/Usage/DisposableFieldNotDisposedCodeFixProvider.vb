Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions

Namespace Usage
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(DisposableFieldNotDisposedCodeFixProvider)), Composition.Shared>
    Public Class DisposableFieldNotDisposedCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim span = diagnostic.Location.SourceSpan
            Dim variableDeclarator = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            context.RegisterCodeFix(CodeAction.Create("Dispose field '" & variableDeclarator.Names.First().ToString(),
                                              Function(c) DisposeField(context.Document, variableDeclarator, c)),
                            diagnostic)

        End Function

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Private Async Function DisposeField(document As Document, variableDeclarator As VariableDeclaratorSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
            Dim type = variableDeclarator.FirstAncestorOrSelf(Of ClassBlockSyntax)
            Dim typeSymbol = semanticModel.GetDeclaredSymbol(type)
            Dim newTypeImplementingIDisposable = AddIDisposableImplementationToType(type, typeSymbol)
            Dim newTypeWithDisposeMethod = AddDisposeDeclarationToDisposeMethod(variableDeclarator, newTypeImplementingIDisposable, typeSymbol)
            Dim root = Await document.GetSyntaxRootAsync()
            Dim newRoot = root.ReplaceNode(type, newTypeWithDisposeMethod)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function

        Private Shared Function AddIDisposableImplementationToType(type As ClassBlockSyntax, typeSymbol As INamedTypeSymbol) As ClassBlockSyntax
            Dim iDisposableInterface = typeSymbol.AllInterfaces.FirstOrDefault(Function(i) i.ToString.EndsWith("IDisposable"))
            If iDisposableInterface IsNot Nothing Then Return type
            Dim implementIdisposable = SyntaxFactory.ImplementsStatement(SyntaxFactory.ParseName("System.IDisposable")).
                WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed).
                WithAdditionalAnnotations(Simplifier.Annotation)

            Dim newImplementsList = If(Not type.Implements.Any(),
            type.Implements.Add(implementIdisposable),
            New SyntaxList(Of ImplementsStatementSyntax)().
                              Add(implementIdisposable))

            Dim newType = type.WithImplements(newImplementsList).
            WithAdditionalAnnotations(Formatter.Annotation)
            Return newType
        End Function

        Private Shared Function AddDisposeDeclarationToDisposeMethod(variableDeclarator As VariableDeclaratorSyntax, type As ClassBlockSyntax, typeSymbol As INamedTypeSymbol) As TypeBlockSyntax
            Dim disposableMethod = typeSymbol.GetMembers("Dispose").OfType(Of IMethodSymbol).FirstOrDefault(Function(d) d.Arity = 0)
            Dim disposeStatment = SyntaxFactory.ParseExecutableStatement(variableDeclarator.Names.First().ToString() & ".Dispose()").
                WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim newType As TypeBlockSyntax
            If disposableMethod Is Nothing Then
                Dim disposeMethod = SyntaxFactory.SubBlock(SyntaxFactory.SubStatement("Dispose").
                    WithParameterList(SyntaxFactory.ParameterList()).
                    WithImplementsClause(SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("IDisposable"), SyntaxFactory.IdentifierName("Dispose")))).
                    WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                    New SyntaxList(Of StatementSyntax)().Add(disposeStatment)).
                    WithAdditionalAnnotations(Formatter.Annotation)

                newType = type.AddMembers(disposeMethod)
            Else
                Dim existingDisposeMethod = TryCast(disposableMethod.DeclaringSyntaxReferences.FirstOrDefault?.GetSyntax().Parent, MethodBlockSyntax)
                If type.Members.Contains(existingDisposeMethod) Then
                    Dim newDisposeMethod = existingDisposeMethod.AddStatements(disposeStatment).
                    WithAdditionalAnnotations(Formatter.Annotation)

                    ' Ensure the dispose method includes the implements clause
                    Dim disposeStatement = newDisposeMethod.SubOrFunctionStatement
                    Dim disposeStatementTrailingTrivia = disposeStatement.GetTrailingTrivia()
                    If disposeStatement.ImplementsClause Is Nothing Then
                        disposeStatement = disposeStatement.
                        WithoutTrailingTrivia().
                        WithImplementsClause(SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("IDisposable"), SyntaxFactory.IdentifierName("Dispose")))).
                        NormalizeWhitespace(" ").
                        WithTrailingTrivia(disposeStatementTrailingTrivia).
                        WithAdditionalAnnotations(Formatter.Annotation)

                        newDisposeMethod = newDisposeMethod.ReplaceNode(newDisposeMethod.SubOrFunctionStatement, disposeStatement)
                    End If
                    newType = type.ReplaceNode(existingDisposeMethod, newDisposeMethod)
                Else
                    Dim fieldDeclaration = variableDeclarator.Parent
                    Dim newFieldDeclaration = fieldDeclaration.
                        WithTrailingTrivia(SyntaxFactory.CommentTrivia("' Add " & disposeStatment.ToString() & " to the Dispose method on the partial file." & vbCrLf))

                    newType = type.ReplaceNode(fieldDeclaration, newFieldDeclaration)
                End If
            End If
            Return newType
        End Function
    End Class
End Namespace