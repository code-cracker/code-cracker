using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerCallExtensionMethodAsExtensionCodeFixProvider", LanguageNames.CSharp)]
    public class CallExtensionMethodAsExtensionCodeFixProvider : CodeFixProvider
    {
        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var staticInvocationExpression = root
                .FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            context.RegisterFix(
                CodeAction.Create(
                    "Use extension method as an extension",
                    cancellationToken => CallAsExtensionAsync(context.Document, staticInvocationExpression, cancellationToken)),
                    diagnostic);
        }

        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CallExtensionMethodAsExtensionAnalyzer.DiagnosticId);
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Document> CallAsExtensionAsync(Document document, InvocationExpressionSyntax staticInvocationExpression, CancellationToken cancellationToken)
        {
            var childNodes = staticInvocationExpression.ChildNodes();

            var parametersExpression =
                childNodes
                    .OfType<ArgumentListSyntax>()
                    .SelectMany(s => s.Arguments)
                    .Select(s => s.Expression);

            var firstArgument = parametersExpression.FirstOrDefault();
            var callerMethod = childNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;

            root = ReplaceStaticCallWithExtionMethodCall(
                        root,
                        staticInvocationExpression,
                        firstArgument,
                        callerMethod.Name,
                        CreateArgumentListSyntaxFrom(parametersExpression.Skip(1))
                   ).WithAdditionalAnnotations(Formatter.Annotation);

            SemanticModel semanticModel;
            if (document.TryGetSemanticModel(out semanticModel))
                root = ImportNeededNamespace(root, semanticModel, callerMethod).WithAdditionalAnnotations(Formatter.Annotation);

            var newDocument = document.WithSyntaxRoot(root);

            return newDocument;
        }

        public ArgumentListSyntax CreateArgumentListSyntaxFrom(IEnumerable<ExpressionSyntax> expressions)
        {
            return SyntaxFactory
                    .ArgumentList()
                    .AddArguments(expressions.Select(s => SyntaxFactory.Argument(s)).ToArray());
        }

        private CompilationUnitSyntax ReplaceStaticCallWithExtionMethodCall(CompilationUnitSyntax root, InvocationExpressionSyntax staticInvocationExpression, ExpressionSyntax sourceExpression, SimpleNameSyntax methodName, ArgumentListSyntax argumentList)
        {
            var extensionInvocationExpression =
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        sourceExpression,
                        methodName),
                        argumentList
                    )
                    .WithLeadingTrivia(staticInvocationExpression.GetLeadingTrivia());

            return root.ReplaceNode(staticInvocationExpression, extensionInvocationExpression);
        }

        private CompilationUnitSyntax ImportNeededNamespace(CompilationUnitSyntax root, SemanticModel semanticModel, MemberAccessExpressionSyntax callerMethod)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(callerMethod.Name);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null) return root;

            var namespaceDisplayString = methodSymbol.ContainingNamespace.ToDisplayString();

            var hasNamespaceImported = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(s => s.DescendantNodes().OfType<IdentifierNameSyntax>())
                .Select(s => TransformIdentifierNameSyntaxIntoNamespace(s))
                .Any(p => p == namespaceDisplayString);

            if (!hasNamespaceImported)
            {
                var namespaceQualifiedName = GenerateNamespaceQualifiedName(namespaceDisplayString.Split('.'));
                root = root.AddUsings(SyntaxFactory.UsingDirective(namespaceQualifiedName));
            }
            return root;
        }

        private string TransformIdentifierNameSyntaxIntoNamespace(IEnumerable<IdentifierNameSyntax> usingIdentifierNames)
        {
            return string.Join(".", usingIdentifierNames.Select(s => s.Identifier.ValueText).ToArray());
        }

        private NameSyntax GenerateNamespaceQualifiedName(IEnumerable<string> names)
        {
            var total = names.Count();

            if (total == 1)
                return SyntaxFactory.IdentifierName(names.First());

            return SyntaxFactory.QualifiedName(
                GenerateNamespaceQualifiedName(names.Take(total - 1)),
                GenerateNamespaceQualifiedName(names.Skip(total - 1)) as IdentifierNameSyntax
            );
        }
    }
}