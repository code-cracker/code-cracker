using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameOfAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NameOfAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.NameOfAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NameOfAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Design;
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NameOf.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NameOf));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyze, SyntaxKind.StringLiteralExpression);

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var stringLiteral = context.Node as LiteralExpressionSyntax;
            if (string.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText)) return;

            var programElementName = GetProgramElementNameThatMatchStringLiteral(stringLiteral, context.SemanticModel);

            if (Found(programElementName))
            {
                var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), programElementName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string GetProgramElementNameThatMatchStringLiteral(LiteralExpressionSyntax stringLiteral, SemanticModel semanticModel)
        {
            var programElementName = GetParameterNameThatMatchStringLiteral(stringLiteral);

            if (!Found(programElementName))
            {
                var literalValueText = stringLiteral.Token.ValueText;
                var symbol = semanticModel.LookupSymbols(stringLiteral.Token.SpanStart, null, literalValueText).FirstOrDefault();

                programElementName = symbol?.ToDisplayParts().LastOrDefault(IncludeOnlyPartsThatAreName).ToString();
            }

            return programElementName;
        }

        private static string GetParameterNameThatMatchStringLiteral(LiteralExpressionSyntax stringLiteral)
        {
            var ancestorThatMightHaveParameters = stringLiteral.FirstAncestorOfType(typeof(AttributeListSyntax), typeof(MethodDeclarationSyntax), typeof(ConstructorDeclarationSyntax), typeof(IndexerDeclarationSyntax));
            var parameterName = string.Empty;
            if (ancestorThatMightHaveParameters != null)
            {
                var parameters = new SeparatedSyntaxList<ParameterSyntax>();
                switch (ancestorThatMightHaveParameters.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                        var method = (BaseMethodDeclarationSyntax)ancestorThatMightHaveParameters;
                        parameters = method.ParameterList.Parameters;
                        break;
                    case SyntaxKind.IndexerDeclaration:
                        var indexer = (IndexerDeclarationSyntax)ancestorThatMightHaveParameters;
                        parameters = indexer.ParameterList.Parameters;
                        break;
                    case SyntaxKind.AttributeList:
                        break;
                }
                parameterName = GetParameterWithIdentifierEqualToStringLiteral(stringLiteral, parameters)?.Identifier.Text;
            }
            return parameterName;
        }

        private static bool Found(string programElement) => !string.IsNullOrEmpty(programElement);

        public static bool IncludeOnlyPartsThatAreName(SymbolDisplayPart displayPart) =>
            displayPart.IsAnyKind(SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.DelegateName, SymbolDisplayPartKind.EnumName, SymbolDisplayPartKind.EventName, SymbolDisplayPartKind.FieldName, SymbolDisplayPartKind.InterfaceName, SymbolDisplayPartKind.LocalName, SymbolDisplayPartKind.MethodName, SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.ParameterName, SymbolDisplayPartKind.PropertyName, SymbolDisplayPartKind.StructName);

        private static ParameterSyntax GetParameterWithIdentifierEqualToStringLiteral(LiteralExpressionSyntax stringLiteral, SeparatedSyntaxList<ParameterSyntax> parameters) =>
            parameters.FirstOrDefault(m => string.Equals(m.Identifier.ValueText, stringLiteral.Token.ValueText, StringComparison.Ordinal));
    }
}