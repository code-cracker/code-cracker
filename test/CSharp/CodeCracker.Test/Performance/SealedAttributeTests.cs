using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class SealedAttributeTests : CodeFixVerifier<SealedAttributeAnalyzer, SealedAttributeCodeFixProvider>
    {
        [Fact]
        public async Task ApplySealedWhenClassInheritsFromSystemAttributeClass()
        {
            const string test = @"
                public class MyAttribute : System.Attribute
                {

                }";

            var expected = new DiagnosticResult(DiagnosticId.SealedAttribute.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(2, 30)
                .WithMessage("Mark 'MyAttribute' as sealed.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task ApplySealedWhenClassInheritsIndirectlyFromSystemAttributeClass()
        {
            const string test = @"
                public abstract class MyAttribute : System.Attribute
                {

                }

                public class OtherAttribute : MyAttribute
                {

                }";

            var expected = new DiagnosticResult(DiagnosticId.SealedAttribute.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 30)
                .WithMessage("Mark 'OtherAttribute' as sealed.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsAbstract()
        {
            const string test = @"
                public abstract class MyAttribute : System.Attribute
                {

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsSealed()
        {
            const string test = @"
                public sealed class MyAttribute : System.Attribute
                {

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenIsStruct()
        {
            const string test = @"
                public struct MyStruct
                {

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenIsInterface()
        {
            const string test = @"
                public interface ITest
                {

                    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenSealedModifierIsAppliedOnClass()
        {
            const string source = @"
                public class MyAttribute : System.Attribute
                {
                }";

            const string fixtest = @"
                public sealed class MyAttribute : System.Attribute
                {
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenSealedModifierIsAppliedOnClassFixAll()
        {
            const string source1 = @"
                public class MyAttribute1 : System.Attribute
                {
                }
                public class MyAttribute3 : System.Attribute
                {
                }";
            const string fixtest1 = @"
                public sealed class MyAttribute1 : System.Attribute
                {
                }
                public sealed class MyAttribute3 : System.Attribute
                {
                }";

            const string source2 = @"
                public class MyAttribute2 : System.Attribute
                {
                }";
            const string fixtest2 = @"
                public sealed class MyAttribute2 : System.Attribute
                {
                }";

            await VerifyCSharpFixAllAsync(new string[] { source1, source2 }, new string[] { fixtest1, fixtest2 });
        }
    }
}