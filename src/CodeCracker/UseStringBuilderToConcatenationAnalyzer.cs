using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStringBuilderToConcatenationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0019";
        internal const string Title = "Use StringBuilder To Concatenations";
        internal const string MessageFormat = "Use 'StringBuilder' instead of concatenation.";
        internal const string Category = "Syntax";
        internal const int NumberMaxConcatenations = 2;
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }


        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

            if (localDeclaration == null) return;
            if (!IsString(context, localDeclaration)) return;

            var variableDeclaration = localDeclaration.ChildNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();

            var variableDeclarator = variableDeclaration.ChildNodes()
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault();

            var equalsValueClause = variableDeclarator.ChildNodes()
                .OfType<EqualsValueClauseSyntax>()
                .FirstOrDefault();

            if(NumberOfConcatenations(equalsValueClause.ChildNodes()) > NumberMaxConcatenations)
            {
                var diagnostic = Diagnostic.Create(Rule, variableDeclaration.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsString(SyntaxNodeAnalysisContext context, LocalDeclarationStatementSyntax localDeclaration)
        {
            var semanticModel = context.SemanticModel;
            var variableTypeName = localDeclaration.Declaration.Type;
            var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;
            return variableType.SpecialType == SpecialType.System_String;
        }

        private int NumberOfConcatenations(IEnumerable<SyntaxNode> nodes)
        {
            const int concatenationCurrent = 1;

            var addExpression = nodes
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault();

            return addExpression?.ChildNodes() != null ?
                concatenationCurrent + NumberOfConcatenations(addExpression.ChildNodes()) : 
                concatenationCurrent;
        }
    }
}
