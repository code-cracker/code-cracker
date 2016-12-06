using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System.Collections.Immutable;
using System;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeMethodStaticAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use static method";
        internal const string MessageFormat = "Make '{0}' method static.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "If the method is not referencing any instance variable and if you are " +
            "not creating a virtual, abstract, new or partial method, and if it is not a method override, " +
            "your instance method may be changed to a static method.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.MakeMethodStatic.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.MakeMethodStatic));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, AnalyzeMethod, SyntaxKind.MethodDeclaration);

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;

            var method = (MethodDeclarationSyntax)context.Node;
            if (method.Modifiers.Any(
                SyntaxKind.StaticKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.VirtualKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.AbstractKeyword,
                SyntaxKind.OverrideKeyword))
                return;

            if (method.ExplicitInterfaceSpecifier != null)
                return;

            var semanticModel = context.SemanticModel;
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null) return;
            if (methodSymbol.IsImplementingInterface()) return;

            if (method.Body == null)
            {
                if (method.ExpressionBody?.Expression == null) return;
                var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(method.ExpressionBody.Expression);
                if (!dataFlowAnalysis.Succeeded) return;
                if (dataFlowAnalysis.DataFlowsIn.Any(inSymbol => inSymbol.Name == "this")) return;
            }
            else if (method.Body.Statements.Any())
            {
                var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(method.Body);
                if (!dataFlowAnalysis.Succeeded) return;
                if (dataFlowAnalysis.DataFlowsIn.Any(inSymbol => inSymbol.Name == "this")
                    || dataFlowAnalysis.WrittenInside.Any(inSymbol => inSymbol.Name == "this")) return;
            }

            if (IsTestMethod(method, methodSymbol)) return;
            if (IsWebFormsMethod(methodSymbol)) return;
            if (IsGetEnumerator(methodSymbol)) return;
            if (HasRoutedEventArgs(methodSymbol)) return;

            var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasRoutedEventArgs(IMethodSymbol methodSymbol)
        {
            var routedEventArgsParameters = methodSymbol.Parameters.Where(p => p.Type.ToString() == "System.Windows.RoutedEventArgs");
            return routedEventArgsParameters.Any();
        }

        private static readonly string[] webFormsMethods = {
            "Application_AuthenticateRequest", "Application_BeginRequest",
            "Application_End", "Application_EndRequest",
            "Application_Error", "Application_Start",
            "Session_End", "Session_Start" };
        private static bool IsWebFormsMethod(IMethodSymbol methodSymbol)
        {
            if (!webFormsMethods.Contains(methodSymbol.Name)) return false;
            return (methodSymbol.ContainingType.AllBaseTypes().Any(t => t.ToString() == "System.Web.HttpApplication"));
        }

        private static bool IsGetEnumerator(IMethodSymbol methodSymbol)
        {
            return methodSymbol.Name == "GetEnumerator" && methodSymbol.ReturnType.Name == "IEnumerator";
        }

        private static bool IsTestMethod(MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
        {
            var result = false;

            // Test if the method has any known test framework's attribute.
            result = method.AttributeLists.HasAnyAttribute(AllTestFrameworksMethodAttributes.Value);

            if (!result && methodSymbol.Name.IndexOf("Test", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Test if the containing class has any NUnit class attribute
                result = methodSymbol.ContainingType.GetAttributes().Any(attribute => attribute.AttributeClass.Name == NUnitTestClassAttribute);

                if (!result)
                {
                    // Test if any other method in the containing class has an NUnit method attribute.
                    result = method.Parent.DescendantNodes().Any(descendant => descendant.IsKind(SyntaxKind.MethodDeclaration) && ((MethodDeclarationSyntax)descendant).AttributeLists.HasAnyAttribute(NUnitTestMethodAttributes));
                }
            }

            return result;
        }

        internal const string NUnitTestClassAttribute = "TestFixtureAttribute";
        internal static readonly string[] MicrosoftTestMethodAttributes = new string[] { "TestMethod", "ClassInitialize", "ClassCleanup", "TestInitialize", "TestCleanup", "AssemblyInitialize", "AssemblyCleanup" };
        internal static readonly string[] XUnitTestMethodAttributes = new string[] { "Fact", "Theory" };
        internal static readonly string[] NUnitTestMethodAttributes = new string[] { "Test", "TestCase", "TestCaseSource", "TestFixtureSetup", "TestFixtureTeardown", "SetUp", "TearDown", "OneTimeSetUp", "OneTimeTearDown" };
        internal static readonly System.Lazy<string[]> AllTestFrameworksMethodAttributes = new System.Lazy<string[]>(() => { return XUnitTestMethodAttributes.Concat(MicrosoftTestMethodAttributes).Concat(NUnitTestMethodAttributes).ToArray(); });
    }
}