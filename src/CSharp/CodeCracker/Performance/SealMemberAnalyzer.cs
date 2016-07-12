using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SealMemberAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.SealMemberAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString ClassMessageFormat = new LocalizableResourceString(nameof(Resources.SealMemberAnalyzer_ClassMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MemberMessageFormat = new LocalizableResourceString(nameof(Resources.SealMemberAnalyzer_MemberMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.SealMemberAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Design;
        private const DiagnosticSeverity severity = DiagnosticSeverity.Hidden;
        private const bool isEnabledByDefault = true;
        private static readonly string helpLinkUri = HelpLink.ForDiagnostic(DiagnosticId.SealMember);
        private static readonly string diagnosticId = DiagnosticId.SealMember.ToDiagnosticId();
        internal static DiagnosticDescriptor ClassRule = new DiagnosticDescriptor(
            diagnosticId,
            Title,
            ClassMessageFormat,
            Category,
            severity,
            isEnabledByDefault: isEnabledByDefault,
            description: Description,
            helpLinkUri: helpLinkUri);
        internal static DiagnosticDescriptor MemberRule = new DiagnosticDescriptor(
            diagnosticId,
            Title,
            MemberMessageFormat,
            Category,
            severity,
            isEnabledByDefault: isEnabledByDefault,
            description: Description,
            helpLinkUri: helpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ClassRule, MemberRule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (!classDeclaration.BaseList?.Types.Any() ?? true) return;
            if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword))) return;
            var overridingMembers = classDeclaration.Members.Where(member =>
            {
                if (!member.IsAnyKind(SyntaxKind.PropertyDeclaration, SyntaxKind.MethodDeclaration,
                    SyntaxKind.EventDeclaration, SyntaxKind.EventFieldDeclaration))
                    return false;
                if (member.IsKind(SyntaxKind.EventFieldDeclaration)
                    && ((EventFieldDeclarationSyntax)member).Declaration?.Variables.Count != 1)
                    return false;
                var mods = member.GetModifiers();
                return mods.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)) &&
                !mods.Any(mod => mod.IsKind(SyntaxKind.SealedKeyword));
            });
            if (!overridingMembers.Any()) return;
            var properties = new Dictionary<string, string> {["kind"] = "class" }.ToImmutableDictionary();
            var classDiagnostic = Diagnostic.Create(ClassRule, classDeclaration.Identifier.GetLocation(), properties, classDeclaration.Identifier.ValueText);
            context.ReportDiagnostic(classDiagnostic);
            foreach (var member in overridingMembers)
            {
                var propertiesMember = new Dictionary<string, string> {["kind"] = "amember" }.ToImmutableDictionary();
                var identifierValue = GetIdentifierValue(member);
                if (identifierValue == null) continue;
                var memberDiagnostic = Diagnostic.Create(MemberRule, member.GetLocation(), propertiesMember, identifierValue);
                context.ReportDiagnostic(memberDiagnostic);
            }
        }

        public static string GetIdentifierValue(MemberDeclarationSyntax member)
        {
            if (member.IsKind(SyntaxKind.MethodDeclaration))
                return ((MethodDeclarationSyntax)member).Identifier.ValueText;
            if (member.IsKind(SyntaxKind.PropertyDeclaration))
                return ((PropertyDeclarationSyntax)member).Identifier.ValueText;
            if (member.IsKind(SyntaxKind.EventDeclaration))
                return ((EventDeclarationSyntax)member).Identifier.ValueText;
            if (member.IsKind(SyntaxKind.EventFieldDeclaration))
                return ((EventFieldDeclarationSyntax)member).Declaration.Variables[0].Identifier.ValueText;
            return null;
        }
    }
}