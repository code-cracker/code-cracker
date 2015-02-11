using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.CSharp.Test.Performance
{
    public class SealedAttributeTests : CodeFixTest<SealedAttributeAnalyzer, SealedAttributeCodeFixProvider>
    {
        [Fact]
        public async Task ApplySealedWhenClassInheritsFromSystemAttributeClass()
        {
            const string test = @"
                public class MyAttribute : System.Attribute 
                { 

                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.SealedAttribute.ToDiagnosticId(),
                Message = "Mark 'MyAttribute' as sealed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 30) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.SealedAttribute.ToDiagnosticId(),
                Message = "Mark 'OtherAttribute' as sealed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 30) }
            };

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
    }
}