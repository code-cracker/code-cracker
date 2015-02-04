using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IntroduceFieldFromConstructorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Consider introduce field for constructor parameters.";
        internal const string MessageFormat = "Introduce a field for parameter: {0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Consider introduce field for constructor parameters.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId.TaskNameAsync));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructorMethod = (ConstructorDeclarationSyntax)context.Node;
            var methodMembers = (constructorMethod.Parent as ClassDeclarationSyntax).Members;
            var fieldMembers = methodMembers.OfType<FieldDeclarationSyntax>();
            var parameters = constructorMethod.ParameterList.Parameters;

            if (constructorMethod?.Body.Statements.Count == 0 && parameters.Count > 0)
            {
                foreach (var par in parameters)
                {
                    var errorMessage = par.Identifier.Text;
                    var diag = Diagnostic.Create(Rule, constructorMethod.GetLocation(), errorMessage);
                    context.ReportDiagnostic(diag);
                }
                return;
            }

            foreach (var par in parameters)
            {
                var foundAssignment = false;
                foreach (var statement in constructorMethod.Body.Statements)
                {
                    if (statement.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        var expression = statement as ExpressionStatementSyntax;
                        var assign = expression.Expression as AssignmentExpressionSyntax;
                        if ((assign?.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression) ?? false) && (assign?.Right.IsKind(SyntaxKind.IdentifierName) ?? false))
                        {
                            var right = assign.Right as IdentifierNameSyntax;
                            if (right.Identifier.Text == par.Identifier.Text)
                            {
                                foundAssignment = true;
                                break;
                            }
                        }
                    }
                }
                if (!foundAssignment)
                {
                    var errorMessage = par.Identifier.Text;
                    var diag = Diagnostic.Create(Rule, constructorMethod.GetLocation(), errorMessage);
                    context.ReportDiagnostic(diag);
                }
            }
        }
    }
}
