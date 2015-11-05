using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.Simplification;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InconsistentParameterDefaultValueCodeFixProvider)), Shared]
    public class InconsistentParameterDefaultValueCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(InconsistentParameterDefaultValueAnalyzer.Id);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            string baseDefaultValue;
            if (diagnostic.Properties.TryGetValue(nameof(baseDefaultValue), out baseDefaultValue))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        Resources.InconsistentParameterDefaultValueCodeFix_UseValueFromBaseDefinition,
                        ct => UseValueFromBaseDefinitionAsync(context.Document, diagnostic, baseDefaultValue, ct),
                        nameof(InconsistentParameterDefaultValueCodeFixProvider) + "_UseValueFromBaseDefinition"),
                    diagnostic);
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.InconsistentParameterDefaultValueCodeFix_RemoveDefaultValue,
                    ct => RemoveDefaultValueAsync(context.Document, diagnostic, ct),
                    nameof(InconsistentParameterDefaultValueCodeFixProvider) + "_RemoveDefaultValue"),
                diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> RemoveDefaultValueAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var param = (ParameterSyntax) node;
            var newParam = param.WithDefault(null).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(param, newParam);
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> UseValueFromBaseDefinitionAsync(Document document, Diagnostic diagnostic, string baseDefaultValue, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var param = (ParameterSyntax)node;
            var defaultExpr = SyntaxFactory.ParseExpression(baseDefaultValue).WithAdditionalAnnotations(Simplifier.Annotation);
            var newParam = param.WithDefault(SyntaxFactory.EqualsValueClause(defaultExpr)).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(param, newParam);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}