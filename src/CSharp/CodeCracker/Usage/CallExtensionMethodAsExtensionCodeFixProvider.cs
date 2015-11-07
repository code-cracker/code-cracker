using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallExtensionMethodAsExtensionCodeFixProvider)), Shared]
    public class CallExtensionMethodAsExtensionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                "Use extension method as an extension",
                cancellationToken => CallAsExtensionAsync(context.Document, diagnostic, cancellationToken), nameof(CallExtensionMethodAsExtensionCodeFixProvider)),
                diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> CallAsExtensionAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var staticInvocationExpression = root
                .FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            var childNodes = staticInvocationExpression.ChildNodes();
            var parameterExpressions = CallExtensionMethodAsExtensionAnalyzer.GetParameterExpressions(childNodes);

            var firstArgument = parameterExpressions.FirstOrDefault();
            var callerMethod = childNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            root = ReplaceStaticCallWithExtionMethodCall(
                        root,
                        staticInvocationExpression,
                        firstArgument,
                        callerMethod.Name,
                        CallExtensionMethodAsExtensionAnalyzer.CreateArgumentListSyntaxFrom(parameterExpressions.Skip(1))
                   ).WithAdditionalAnnotations(Formatter.Annotation);

            var semanticModel = await document.GetSemanticModelAsync();
            root = ImportNeededNamespace(root, semanticModel, callerMethod).WithAdditionalAnnotations(Formatter.Annotation);
            var newDocument = document.WithSyntaxRoot(root);

            return newDocument;
        }

        private static CompilationUnitSyntax ReplaceStaticCallWithExtionMethodCall(CompilationUnitSyntax root, InvocationExpressionSyntax staticInvocationExpression, ExpressionSyntax sourceExpression, SimpleNameSyntax methodName, ArgumentListSyntax argumentList)
        {
            var extensionInvocationExpression = CallExtensionMethodAsExtensionAnalyzer.CreateInvocationExpression(sourceExpression, methodName, argumentList)
                .WithLeadingTrivia(staticInvocationExpression.GetLeadingTrivia());
            return root.ReplaceNode(staticInvocationExpression, extensionInvocationExpression);
        }

        private static CompilationUnitSyntax ImportNeededNamespace(CompilationUnitSyntax root, SemanticModel semanticModel, MemberAccessExpressionSyntax callerMethod)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(callerMethod.Name);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (methodSymbol == null) return root;
            var namespaceDisplayString = methodSymbol.ContainingNamespace.ToDisplayString();
            var hasNamespaceImported = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(s => s.Name.ToString())
                .Any(n => n == namespaceDisplayString);
            if (!hasNamespaceImported)
            {
                var namespaceQualifiedName = methodSymbol.ContainingNamespace.ToNameSyntax();
                root = root.AddUsings(SyntaxFactory.UsingDirective(namespaceQualifiedName));
            }
            return root;
        }
    }
}