using System.Collections.Immutable;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyChangedEventArgsUnnecessaryAllocationCodeFixProvider)), Shared]
    public sealed class PropertyChangedEventArgsUnnecessaryAllocationCodeFixProvider : CodeFixProvider
    {
        public LocalizableString CodeActionTitle = new LocalizableResourceString(nameof(Resources.PropertyChangedEventArgsUnnecessaryAllocation_CodeActionTitle), Resources.ResourceManager, typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticId.PropertyChangedEventArgsUnnecessaryAllocation.ToDiagnosticId());

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(CodeActionTitle.ToString(),
                    token => ChangePropertyChangedEventArgsToStatic(context.Document, diagnostic.Location, diagnostic.Properties, token),
                    nameof(PropertyChangedEventArgsUnnecessaryAllocationCodeFixProvider)), diagnostic);

            return Task.FromResult(true);
        }

        private static async Task<Document> ChangePropertyChangedEventArgsToStatic(Document document, Location location,
            ImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var data = PropertyChangedEventArgsAnalyzerData.FromDiagnosticProperties(properties);

            var newSyntaxRoot = new PropertyChangedUnnecessaryAllocationRewriter(data, location.SourceSpan).Visit(syntaxRoot);

            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        private class PropertyChangedUnnecessaryAllocationRewriter : CSharpSyntaxRewriter
        {
            private readonly PropertyChangedEventArgsAnalyzerData ContextData;
            private readonly TextSpan DiagnosticLocation;

            private bool DiagnosticLocationFound = false;
            private IEnumerable<string> nameHints;

            public PropertyChangedUnnecessaryAllocationRewriter(PropertyChangedEventArgsAnalyzerData contextData, TextSpan diagnosticLocation)
            {
                this.ContextData = contextData;
                this.DiagnosticLocation = diagnosticLocation;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                nameHints = node.Members.OfType<FieldDeclarationSyntax>()
                                .SelectMany(fd => fd.Declaration.Variables.Select(vds => vds.Identifier.ValueText));

                var traverseResult = base.VisitClassDeclaration(node) as ClassDeclarationSyntax;
                var result = DiagnosticLocationFound ? AddPropertyChangedEventArgsStaticField(traverseResult, nameHints ?? Enumerable.Empty<string>()) : traverseResult;
                DiagnosticLocationFound = false;
                return result;
            }

            public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if(node.Span == DiagnosticLocation)
                {
                    DiagnosticLocationFound = true;

                    return ParseExpression(ContextData.StaticFieldIdentifierName(nameHints ?? Enumerable.Empty<string>()))
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia());
                }
                return base.VisitObjectCreationExpression(node);
            }

            private ClassDeclarationSyntax AddPropertyChangedEventArgsStaticField(ClassDeclarationSyntax declaration, IEnumerable<string> nameHints) => declaration
                .WithMembers(declaration.Members.Insert(0, ContextData.PropertyChangedEventArgsStaticField(nameHints).WithAdditionalAnnotations(Formatter.Annotation)));
        }
    }
}
