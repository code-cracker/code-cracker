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
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NameOf.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NameOf));
        internal static readonly DiagnosticDescriptor RuleExternal = new DiagnosticDescriptor(
            DiagnosticId.NameOf_External.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NameOf_External));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleExternal);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyze, SyntaxKind.StringLiteralExpression);

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var stringLiteral = context.Node as LiteralExpressionSyntax;
            if (string.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText)) return;

            bool externalSymbol;
            var programElementName = GetProgramElementNameThatMatchStringLiteral(stringLiteral, context.SemanticModel, out externalSymbol);

            if (Found(programElementName) && OutSideOfDeclarationSideWithSameName(stringLiteral))
            {
                var diagnostic = Diagnostic.Create(externalSymbol ? RuleExternal : Rule, stringLiteral.GetLocation(), programElementName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool OutSideOfDeclarationSideWithSameName(LiteralExpressionSyntax stringLiteral)
        {
            var variableDeclaration = stringLiteral.FirstAncestorOfType<VariableDeclaratorSyntax>();
            if (variableDeclaration != null)
            {
                return !string.Equals(variableDeclaration.Identifier.ValueText, stringLiteral.Token.ValueText, StringComparison.Ordinal);
            }

            var propertyDeclaration = stringLiteral.FirstAncestorOfType<PropertyDeclarationSyntax>();
            var outSideOfAccessors = null == stringLiteral.FirstAncestorOfType<AccessorListSyntax>();
            if (!outSideOfAccessors) return true;
            return !string.Equals(propertyDeclaration?.Identifier.ValueText, stringLiteral.Token.ValueText, StringComparison.Ordinal);
        }

        private static string GetProgramElementNameThatMatchStringLiteral(LiteralExpressionSyntax stringLiteral, SemanticModel semanticModel, out bool externalSymbol)
        {
            externalSymbol = false;
            var programElementName = GetParameterNameThatMatchStringLiteral(stringLiteral);
            if (!Found(programElementName))
            {
                var literalValueText = stringLiteral.Token.ValueText;
                var symbol = semanticModel.LookupSymbols(stringLiteral.Token.SpanStart, null, literalValueText).FirstOrDefault();
                if (symbol == null) return null;
                externalSymbol = !symbol.Locations.Any(l => l.IsInSource);

                if (symbol.Kind == SymbolKind.Local)
                {
                    var symbolSpan = symbol.Locations.Min(i => i.SourceSpan);
                    if (symbolSpan.CompareTo(stringLiteral.Token.Span) > 0)
                        return null;
                }

                programElementName = symbol.ToDisplayParts(NameOfSymbolDisplayFormat).LastOrDefault(AnalyzerExtensions.IsName).ToString();
            }

            return programElementName;
        }

        private static string GetParameterNameThatMatchStringLiteral(LiteralExpressionSyntax stringLiteral)
        {
            var ancestorThatMightHaveParameters = stringLiteral.FirstAncestorOfType(typeof(AttributeListSyntax), typeof(MethodDeclarationSyntax), typeof(ConstructorDeclarationSyntax), typeof(IndexerDeclarationSyntax));
            string parameterName = null;
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
                    default:
                        break;
                }
                parameterName = GetParameterWithIdentifierEqualToStringLiteral(stringLiteral, parameters)?.Identifier.Text;
            }
            return parameterName;
        }

        private static bool Found(string programElement) => !string.IsNullOrEmpty(programElement);

        private static ParameterSyntax GetParameterWithIdentifierEqualToStringLiteral(LiteralExpressionSyntax stringLiteral, SeparatedSyntaxList<ParameterSyntax> parameters) =>
            parameters.FirstOrDefault(m => string.Equals(m.Identifier.ValueText, stringLiteral.Token.ValueText, StringComparison.Ordinal));

        private static readonly SymbolDisplayFormat NameOfSymbolDisplayFormat = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.None, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    }
}