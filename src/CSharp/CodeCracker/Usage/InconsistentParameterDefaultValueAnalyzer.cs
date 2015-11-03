using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InconsistentParameterDefaultValueAnalyzer : DiagnosticAnalyzer
    {
        public static readonly string Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId();
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.InconsistentParameterDefaultValueAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.InconsistentParameterDefaultValueAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Usage;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.InconsistentParameterDefaultValue));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Parameter);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var paramNode = (ParameterSyntax) context.Node;
            var param = context.SemanticModel.GetDeclaredSymbol(paramNode, context.CancellationToken);
            IParameterSymbol baseParam;
            if (HasInconsistentDefaultValue(param, out baseParam))
            {
                var formattedBaseDefaultValue = FormatDefaultValue(baseParam);

                var properties = ImmutableDictionary<string, string>.Empty;
                if (baseParam.HasExplicitDefaultValue && formattedBaseDefaultValue != null)
                {
                    properties = properties.Add("baseDefaultValue", formattedBaseDefaultValue);
                }

                var diagnostic = Diagnostic.Create(
                    Rule,
                    paramNode.GetLocation(),
                    properties,
                    FormatDefaultValue(param),
                    param.Name,
                    formattedBaseDefaultValue,
                    baseParam.ContainingSymbol);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string FormatDefaultValue(IParameterSymbol param)
        {
            if (!param.HasExplicitDefaultValue)
                return $"({Resources.DefaultValue_None})";
            return FormatDefaultValue(param.Type, param.ExplicitDefaultValue);
        }

        private static string FormatDefaultValue(ITypeSymbol type, object value)
        {
            if (value == null) return "null";

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.LiteralExpression(
                        (bool)value
                            ? SyntaxKind.TrueLiteralExpression
                            : SyntaxKind.FalseLiteralExpression).ToString();
                case SpecialType.System_Char:
                    return SyntaxFactory.Literal((char)value).ToString();
                case SpecialType.System_SByte:
                    return SyntaxFactory.Literal((sbyte)value).ToString();
                case SpecialType.System_Byte:
                    return SyntaxFactory.Literal((byte)value).ToString();
                case SpecialType.System_Int16:
                    return SyntaxFactory.Literal((short)value).ToString();
                case SpecialType.System_UInt16:
                    return SyntaxFactory.Literal((ushort)value).ToString();
                case SpecialType.System_Int32:
                    return SyntaxFactory.Literal((int)value).ToString();
                case SpecialType.System_UInt32:
                    return SyntaxFactory.Literal((uint)value).ToString();
                case SpecialType.System_Int64:
                    return SyntaxFactory.Literal((long)value).ToString();
                case SpecialType.System_UInt64:
                    return SyntaxFactory.Literal((ulong)value).ToString();
                case SpecialType.System_Decimal:
                    return SyntaxFactory.Literal((decimal)value).ToString();
                case SpecialType.System_Single:
                    return SyntaxFactory.Literal((float)value).ToString();
                case SpecialType.System_Double:
                    return SyntaxFactory.Literal((double)value).ToString();
                case SpecialType.System_String:
                    return SyntaxFactory.Literal((string)value).ToString();
                default:
                    if (type.BaseType.SpecialType == SpecialType.System_Enum)
                    {
                        return FormatEnumValue(type, value);
                    }
                    ITypeSymbol underlyingType;
                    if (IsNullable(type, out underlyingType))
                    {
                        return FormatDefaultValue(underlyingType, value);
                    }
                    return null;
            }
        }

        private static bool IsNullable(ITypeSymbol type, out ITypeSymbol underlyingType)
        {
            underlyingType = null;
            var namedType = type as INamedTypeSymbol;
            if (namedType == null)
                return false;
            if (!namedType.IsGenericType)
                return false;
            if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                underlyingType = namedType.TypeArguments.First();
                return true;
            }
            return false;
        }

        private static string FormatEnumValue(ITypeSymbol type, object value)
        {
            var member = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => value.Equals(f.ConstantValue));
            return member?.ToString();
        }

        private static bool HasInconsistentDefaultValue(IParameterSymbol param, out IParameterSymbol baseParam)
        {
            baseParam = null;

            // No default value specified
            if (!param.HasExplicitDefaultValue)
                return false;

            var method = (IMethodSymbol) param.ContainingSymbol;
            var baseMethod = method.IsOverride ? method.OverriddenMethod : GetImplementedInterfaceMethods(method).FirstOrDefault();

            // Not an override or interface implementation
            if (baseMethod == null)
                return false;

            baseParam = baseMethod.Parameters[param.Ordinal];

            // Base definition doesn't specify a default value; specifying one is inconsistent
            if (!baseParam.HasExplicitDefaultValue)
                return true;

            // Both this method and the base definition specify a default value, but it's not the same
            return !Equals(baseParam.ExplicitDefaultValue, param.ExplicitDefaultValue);
        }

        private static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(IMethodSymbol method)
        {
            if (method.ExplicitInterfaceImplementations.Any())
                return method.ExplicitInterfaceImplementations;

            return from i in method.ContainingType.AllInterfaces from m in i.GetMembers(method.Name).OfType<IMethodSymbol>() where method.Equals(method.ContainingType.FindImplementationForInterfaceMember(m)) select m;
        }
    }
}
