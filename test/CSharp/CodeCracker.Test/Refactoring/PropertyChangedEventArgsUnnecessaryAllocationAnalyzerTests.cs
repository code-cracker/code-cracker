using System.Collections.Generic;
using System.Threading.Tasks;
using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class PropertyChangedEventArgsUnnecessaryAllocationAnalyzerTests : CodeFixVerifier
    {
        public static IEnumerable<object[]> SharedData
        {
            get
            {
                yield return new[] {"\"Name\""};
                yield return new[] {"nameof(Name)"};
                yield return new[] {"null"};
            }
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticWithEmptySourceCodeAsync()
        {
            var source = @"";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [MemberData(nameof(SharedData))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreation(string ctorArg)
        {
            var source = $"var args = new PropertyChangedEventArgs({ctorArg})";

           await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(0, 12));
        }

        [Theory]
        [MemberData(nameof(SharedData))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInMethodInvocation(string ctorArg)
        {
            var source = $@"
public class Test 
{{
    public void TestMethod()
    {{
        PropertyChanged(new PropertyChangedEventArgs({ctorArg}))
    }}
}}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(6, 25));
        }

        [Theory]
        [MemberData(nameof(SharedData))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInObjectInitializer(string ctorArg)
        {
            var source = $"object args = new {{ Name = new PropertyChangedEventArgs({ctorArg}) }}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(0, 28));
        }

        [Theory]
        [MemberData(nameof(SharedData))]
        public async Task DoesNotTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInFieldAssignmentWhenFieldIsStatic(string ctorArg)
        {
            var source = $@"
public class Test 
{{
    private static PropertyChangedEventArgs field = new PropertyChangedEventArgs({ctorArg});
}}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInStaticConstructor()
        {
            var source = @"
public class Test 
{
    private static PropertyChangedEventArgs field;
    
    static Test()
    {
        field = new PropertyChangedEventArgs(""Name"");
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticAtObjectInstanceCreation()
        {
            var source = @"
public class Test 
{
    private object field = new object();
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesTriggerDiagnosticAtObjectInstanceCreationUsingQualifiedName()
        {
            var source = @"
public class Test 
{
    private object field = new System.ComponentModel.PropertyChangedEventArgs(null);
}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(4,28));
        }

        [Theory]
        [MemberData(nameof(SharedData))]
        public async Task DoesTriggerDiagnosticInLambdaExpression(string ctorArg)
        {
            var source = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private PropertyChangedEventArgs field;

    public Test()
    {{
        Action action = () => field = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(10,39));
        }

        [Fact]
        public async Task DoesNotTriggerWhenArgumentIsNotLiteral()
        {
            var source = $@"
public class Test 
{{
    public void TestMethod(string propertyName)
    {{
        PropertyChanged(new PropertyChangedEventArgs(propertyName))
    }}
}}";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new PropertyChangedEventArgsUnnecessaryAllocationAnalyzer();
        }

        public static DiagnosticResult PropertyChangedUnnecessaryAllocationDiagnostic(int line, int column)
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.PropertyChangedEventArgsUnnecessaryAllocation.ToDiagnosticId(),
                Message = "Create PropertyChangedEventArgs static instance and reuse it to avoid unecessary memory allocation.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line, column),
                }
            };
        }
    }
}
