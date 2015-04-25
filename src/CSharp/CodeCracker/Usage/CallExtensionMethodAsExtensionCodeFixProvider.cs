using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider("CodeCrackerCallExtensionMethodAsExtensionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class CallExtensionMethodAsExtensionCodeFixProvider : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var staticInvocationExpression = root
                .FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use extension method as an extension",
                    cancellationToken => CallAsExtensionAsync(context.Document, staticInvocationExpression, cancellationToken)),
                    diagnostic);
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> CallAsExtensionAsync(Document document, InvocationExpressionSyntax staticInvocationExpression, CancellationToken cancellationToken)
        {
            var childNodes = staticInvocationExpression.ChildNodes();
            var parameterExpressions = GetParameterExpressions(childNodes);

            var firstArgument = parameterExpressions.FirstOrDefault();
            var callerMethod = childNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;

            root = ReplaceStaticCallWithExtionMethodCall(
                        root,
                        staticInvocationExpression,
                        firstArgument,
                        callerMethod.Name,
                        CreateArgumentListSyntaxFrom(parameterExpressions.Skip(1))
                   ).WithAdditionalAnnotations(Formatter.Annotation);

            SemanticModel semanticModel;
            if (document.TryGetSemanticModel(out semanticModel))
                root = ImportNeededNamespace(root, semanticModel, callerMethod).WithAdditionalAnnotations(Formatter.Annotation);

            var newDocument = document.WithSyntaxRoot(root);

            return newDocument;
        }

        public static IEnumerable<ExpressionSyntax> GetParameterExpressions(IEnumerable<SyntaxNode> childNodes) =>
            childNodes.OfType<ArgumentListSyntax>().SelectMany(s => s.Arguments).Select(s => s.Expression);

        public static ArgumentListSyntax CreateArgumentListSyntaxFrom(IEnumerable<ExpressionSyntax> expressions) =>
            SyntaxFactory.ArgumentList().AddArguments(expressions.Select(s => SyntaxFactory.Argument(s)).ToArray());

        private CompilationUnitSyntax ReplaceStaticCallWithExtionMethodCall(CompilationUnitSyntax root, InvocationExpressionSyntax staticInvocationExpression, ExpressionSyntax sourceExpression, SimpleNameSyntax methodName, ArgumentListSyntax argumentList)
        {
            var extensionInvocationExpression = CreateInvocationExpression(sourceExpression, methodName, argumentList)
                .WithLeadingTrivia(staticInvocationExpression.GetLeadingTrivia());
            return root.ReplaceNode(staticInvocationExpression, extensionInvocationExpression);
        }

        public static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax sourceExpression, SimpleNameSyntax methodName, ArgumentListSyntax argumentList) =>
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                sourceExpression,
                methodName),
                argumentList);

        private CompilationUnitSyntax ImportNeededNamespace(CompilationUnitSyntax root, SemanticModel semanticModel, MemberAccessExpressionSyntax callerMethod)
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