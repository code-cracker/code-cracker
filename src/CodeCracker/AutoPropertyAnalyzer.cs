using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AutoPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.AutoPropertyAnalyzer";
        internal const string Title = "Use auto properties when possible";
        internal const string MessageFormat = "Use auto properties when possible.";
        internal const string Category = "Structure";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;

            if (PropertyHasGetterWithMultipleStatements(property))
            { return; }
            if (PropertyHasGetterWithComplexReturnStatement(property))
            { return; }
            if (PropertyHasSetterWithMultipleStatements(property))
            { return; }
            if (PropertyHasSetterWithComplexAssigmentStatement(property))
            { return; }

            context.ReportDiagnostic(Diagnostic.Create(Rule, property.GetLocation()));

        }

        private bool PropertyHasGetterWithMultipleStatements(PropertyDeclarationSyntax property)
        {
            var getter = property.AccessorList.Accessors.First(a => a.Keyword.Text == "get");
            return getter.Body.Statements.Count > 1;
        }


        private bool PropertyHasGetterWithComplexReturnStatement(PropertyDeclarationSyntax property)
        {
            var getter = property.AccessorList.Accessors.First(a => a.Keyword.Text == "get");
            var statement = getter.Body.Statements.Single() as ReturnStatementSyntax;
            return !statement.Expression.IsKind(SyntaxKind.IdentifierName);
        }

        private bool PropertyHasSetterWithMultipleStatements(PropertyDeclarationSyntax property)
        {
            if (property.AccessorList.Accessors.Any(a => a.Keyword.Text == "set"))
            {
                var setter = property.AccessorList.Accessors.Single(a => a.Keyword.Text == "set");

                return setter.Body.Statements.Count > 1;
            }

            return false;
        }

        private bool PropertyHasSetterWithComplexAssigmentStatement(PropertyDeclarationSyntax property)
        {
            if (property.AccessorList.Accessors.Any(a => a.Keyword.Text == "set"))
            {
                var setter = property.AccessorList.Accessors.Single(a => a.Keyword.Text == "set");
                var assignment = (setter.Body.Statements.Single() as ExpressionStatementSyntax)
                    .Expression as AssignmentExpressionSyntax;

                return !assignment.Left.IsKind(SyntaxKind.IdentifierName) 
                    || !assignment.Right.IsKind(SyntaxKind.IdentifierName);
            }

            return false;
        }
    }
}