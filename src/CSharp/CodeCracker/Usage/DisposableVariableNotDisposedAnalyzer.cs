using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableVariableNotDisposedAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Should dispose object";
        internal const string MessageFormat = "{0} should be disposed.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "When a disposable object is created it should be disposed as soon as possible.\n" +
            "This warning will appear if you create a disposable object and don't store, return or dispose it.";
        public const string cantFix = "cantFix";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposableVariableNotDisposed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var objectCreation = context.Node as ObjectCreationExpressionSyntax;
            if (objectCreation == null) return;
            if (objectCreation.Parent == null) return;
            if (objectCreation.Parent.IsAnyKind(SyntaxKind.ReturnStatement, SyntaxKind.UsingStatement))
                return;
            if (objectCreation.Ancestors().Any(i => i.IsAnyKind(
                SyntaxKind.ThisConstructorInitializer,
                SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ObjectCreationExpression)))
                return;

            var semanticModel = context.SemanticModel;
            var type = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol as INamedTypeSymbol;
            if (type == null) return;
            if (!type.AllInterfaces.Any(i => i.ToString() == "System.IDisposable")) return;
            ISymbol identitySymbol = null;
            StatementSyntax statement = null;
            if (objectCreation.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var assignmentExpression = (AssignmentExpressionSyntax)objectCreation.Parent;
                identitySymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
                if (identitySymbol?.Kind != SymbolKind.Local) return;
                if (assignmentExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>() == null) return;
                var usingStatement = assignmentExpression.Parent as UsingStatementSyntax;
                if (usingStatement != null) return;
                statement = assignmentExpression.Parent as ExpressionStatementSyntax;
            }
            else if (objectCreation.Parent.IsKind(SyntaxKind.EqualsValueClause) && objectCreation.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
            {
                var variableDeclarator = (VariableDeclaratorSyntax)objectCreation.Parent.Parent;
                var variableDeclaration = variableDeclarator?.Parent as VariableDeclarationSyntax;
                identitySymbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
                if (identitySymbol == null) return;
                var usingStatement = variableDeclaration?.Parent as UsingStatementSyntax;
                if (usingStatement != null) return;
                statement = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
                if ((statement?.FirstAncestorOrSelf<MethodDeclarationSyntax>()) == null) return;
            }
            else if (objectCreation.Parent.IsAnyKind(SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression))
            {
                var anonymousFunction = objectCreation.Parent as AnonymousFunctionExpressionSyntax;
                var methodSymbol = semanticModel.GetSymbolInfo(anonymousFunction).Symbol as IMethodSymbol;
                if (!methodSymbol.ReturnsVoid) return;
                var props = new Dictionary<string, string> { { "typeName", type.Name }, { cantFix, "" } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation(), props, type.Name.ToString()));
            }
            else
            {
                var props = new Dictionary<string, string> { { "typeName", type.Name } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation(), props, type.Name.ToString()));
                return;
            }
            if (statement != null && identitySymbol != null)
            {
                var isDisposeOrAssigned = IsDisposedOrAssigned(semanticModel, statement, (ILocalSymbol)identitySymbol);
                if (isDisposeOrAssigned) return;
                var props = new Dictionary<string, string> { { "typeName", type.Name } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation(), props, type.Name.ToString()));
            }
        }

        private static bool IsDisposedOrAssigned(SemanticModel semanticModel, StatementSyntax statement, ILocalSymbol identitySymbol)
        {
            var method = statement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null) return false;
            if (IsReturned(method, statement, semanticModel, identitySymbol)) return true;
            foreach (var childStatements in method.Body.DescendantNodes().OfType<StatementSyntax>())
            {
                if (childStatements.SpanStart > statement.SpanStart
                && (IsCorrectDispose(childStatements as ExpressionStatementSyntax, semanticModel, identitySymbol)
                || IsAssignedToField(childStatements as ExpressionStatementSyntax, semanticModel, identitySymbol)))
                    return true;
            }
            return false;
        }

        private static bool IsReturned(MethodDeclarationSyntax method, StatementSyntax statement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            var anonymousFunction = statement.FirstAncestorOfKind(SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression) as AnonymousFunctionExpressionSyntax;
            IMethodSymbol methodSymbol;
            BlockSyntax body;
            if (anonymousFunction != null)
            {
                methodSymbol = semanticModel.GetSymbolInfo(anonymousFunction).Symbol as IMethodSymbol;
                body = anonymousFunction.Body as BlockSyntax;
            }
            else
            {
                methodSymbol = semanticModel.GetDeclaredSymbol(method);
                body = method.Body;
            }
            if (body == null) return true;
            var returnExpressions = body.DescendantNodes().OfType<ReturnStatementSyntax>().Select(r => r.Expression);
            var returnTypeSymbol = methodSymbol?.ReturnType;
            if (returnTypeSymbol == null) return false;
            if (returnTypeSymbol.SpecialType == SpecialType.System_Void) return false;
            var isReturning = returnExpressions.Any(returnExpression =>
            {
                var returnSymbol = semanticModel.GetSymbolInfo(returnExpression).Symbol;
                if (returnSymbol == null) return false;
                return returnSymbol.Equals(identitySymbol);
            });
            return isReturning;
        }

        private static bool IsAssignedToField(ExpressionStatementSyntax expressionStatement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            if (expressionStatement == null) return false;
            if (!expressionStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression)) return false;
            var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var assignmentTarget = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (assignmentTarget?.Kind != SymbolKind.Field) return false;
            var assignmentSource = semanticModel.GetSymbolInfo(assignment.Right).Symbol;
            return (identitySymbol.Equals(assignmentSource));
        }

        private static bool IsCorrectDispose(ExpressionStatementSyntax expressionStatement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            if (expressionStatement == null) return false;
            var invocation = expressionStatement.Expression as InvocationExpressionSyntax;
            if (invocation?.ArgumentList.Arguments.Any() ?? true) return false;
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null) return false;
            ISymbol memberSymbol;
            if (memberAccess.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                memberSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            }
            else if (memberAccess.Expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                var parenthesizedExpression = (ParenthesizedExpressionSyntax)memberAccess.Expression;
                var cast = parenthesizedExpression.Expression as CastExpressionSyntax;
                if (cast == null) return false;
                var catTypeSymbol = semanticModel.GetTypeInfo(cast.Type).Type;
                if (catTypeSymbol.SpecialType != SpecialType.System_IDisposable) return false;
                memberSymbol = semanticModel.GetSymbolInfo(cast.Expression).Symbol;
            }
            else return false;
            if (memberSymbol == null || !memberSymbol.Equals(identitySymbol)) return false;
            var memberAccessed = memberAccess.Name as IdentifierNameSyntax;
            if (memberAccessed == null) return false;
            if (memberAccessed.Identifier.Text != "Dispose" || memberAccessed.Arity != 0) return false;
            var methodSymbol = semanticModel.GetSymbolInfo(memberAccessed).Symbol as IMethodSymbol;
            if (methodSymbol == null) return false;
            if (methodSymbol.ToString() == "System.IDisposable.Dispose()") return true;
            var disposeMethod = (IMethodSymbol)semanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers("Dispose").Single();
            var isDispose = methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(disposeMethod));
            return isDispose;
        }
    }
}