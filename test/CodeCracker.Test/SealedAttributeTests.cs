using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class SealedAttributeTests : CodeFixTest<SealedAttributeAnalyzer, SealedAttributeCodeFixProvider>
    {
        [Fact]
        public async Task ApplySealedWhenClassInheritsFromSystemAttributeClass()
        {
            var test = @"
                public class MyAttribute : System.Attribute 
                { 

                }";

            var expected = new DiagnosticResult
            {
                Id = SealedAttributeAnalyzer.DiagnosticId,
                Message = "Mark 'MyAttribute' as sealed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 30) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task ApplySealedWhenClassInheritsIndirectlyFromSystemAttributeClass()
        {
            var test = @"
                public abstract class MyAttribute : System.Attribute 
                { 

                }

                public class OtherAttribute : MyAttribute 
                { 

                }";

            var expected = new DiagnosticResult
            {
                Id = SealedAttributeAnalyzer.DiagnosticId,
                Message = "Mark 'OtherAttribute' as sealed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 30) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsAbstract()
        {
            var test = @"
                public abstract class MyAttribute : System.Attribute 
                { 

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsSealed()
        {
            var test = @"
                public sealed class MyAttribute : System.Attribute 
                { 

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenIsStruct()
        {
            var test = @"
                public struct MyStruct 
                { 

                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotApplySealedWhenIsInterface()
        {
            var test = @"
                public interface ITest 
                { 

                    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenSealedModifierIsAppliedOnClass()
        {
            var source = @"
                public class MyAttribute : System.Attribute 
                { 
                }";

            var fixtest = @"
                public sealed class MyAttribute : System.Attribute 
                { 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}