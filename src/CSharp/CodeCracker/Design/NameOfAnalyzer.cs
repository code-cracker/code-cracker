namespace CodeCracker.CSharp.Design
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameOfAnalyzer : DiagnosticAnalyzer
    {
        private const char DotChar = '.';

        internal const string Title = "You should use nameof instead of program element name string";
        internal const string MessageFormat = "Use 'nameof({0})' instead of specifying the program element name.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 the nameof() operator should be used to specify the name of a program element instead of "
            + "a string literal as it produce code that is easier to refactor.";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NameOf.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
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

                if(!Found(programElementName))
                {
                    symbol = GetNamespaceSymbolThatEqualsStringLiteral(stringLiteral, semanticModel);

                    if(symbol != null)
                    {
                        programElementName = literalValueText;
                    }
                }
            }

            return programElementName;
        }

        private static ISymbol GetNamespaceSymbolThatEqualsStringLiteral(LiteralExpressionSyntax stringLiteral, SemanticModel semanticModel)
        {
            ISymbol result = null;
            var valueText = stringLiteral.Token.ValueText;
            if (valueText.IndexOf(DotChar) > 0)
            {
                var lastPartNamespaceStartIndex = valueText.LastIndexOf(DotChar);
                if (lastPartNamespaceStartIndex + 1 <= valueText.Length)
                {
                    var lastPartNamespace = valueText.Substring(lastPartNamespaceStartIndex + 1);

                    var namespaceSymbol = semanticModel.LookupNamespacesAndTypes(stringLiteral.Token.SpanStart, null, lastPartNamespace).FirstOrDefault();

                    if(namespaceSymbol != null && IsStringLiteralProperPartOfNamespaceName(valueText, namespaceSymbol.ToDisplayString()))
                    {
                        result = namespaceSymbol;
                    }
                }
            }

            return result;
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
                        {
                            var method = (BaseMethodDeclarationSyntax)ancestorThatMightHaveParameters;
                            parameters = method.ParameterList.Parameters;
                            break;
                        }
                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexer = (IndexerDeclarationSyntax)ancestorThatMightHaveParameters;
                            parameters = indexer.ParameterList.Parameters;
                            break;
                        }
                    case SyntaxKind.AttributeList:
                        {
                            break;
                        }
                }

                parameterName = GetParameterWithIdentifierEqualToStringLiteral(stringLiteral, parameters)?.Identifier.Text;
            }

            return parameterName;
        }

        private static bool IsStringLiteralProperPartOfNamespaceName(string stringLiteral, string namespaceName)
        {
            var result = false;

            var lastOccurence = namespaceName.LastIndexOf(stringLiteral, StringComparison.Ordinal);
            var areStringsSameLength = stringLiteral.Length == namespaceName.Length;

            if (!areStringsSameLength || lastOccurence >= 0)
            {
                var previousCharEqualsDot = lastOccurence > 0 ? namespaceName[lastOccurence - 1].Equals(DotChar) : true;

                var nextCharIndex = lastOccurence + stringLiteral.Length;
                var nextCharIndexExists = nextCharIndex < namespaceName.Length;
                var nextCharEqualsDot = nextCharIndexExists ? namespaceName[nextCharIndex].Equals(DotChar) : true;

                result = previousCharEqualsDot || nextCharEqualsDot;
            }
            else
            {
                result = lastOccurence >= 0;
            }

            return result;
        }

        private static bool Found(string programElement) => !string.IsNullOrEmpty(programElement);

        private static bool IncludeOnlyPartsThatAreName(SymbolDisplayPart displayPart) =>
            displayPart.IsAnyKind(SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.DelegateName, SymbolDisplayPartKind.EnumName, SymbolDisplayPartKind.EventName, SymbolDisplayPartKind.FieldName, SymbolDisplayPartKind.InterfaceName, SymbolDisplayPartKind.LocalName, SymbolDisplayPartKind.MethodName, SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.ParameterName, SymbolDisplayPartKind.PropertyName, SymbolDisplayPartKind.StructName);

        private static ParameterSyntax GetParameterWithIdentifierEqualToStringLiteral(LiteralExpressionSyntax stringLiteral, SeparatedSyntaxList<ParameterSyntax> parameters) =>
            parameters.FirstOrDefault(m => string.Equals(m.Identifier.ValueText, stringLiteral.Token.ValueText, StringComparison.Ordinal));
    }
}