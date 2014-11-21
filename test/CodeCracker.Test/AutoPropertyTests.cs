using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class AutoPropertyTests : CodeFixTest<AutoPropertyAnalyzer, AutoPropertyCodeFixProvider>
    {
        [Fact]
        public void PropertyWithGetterAndSetterUsingInternalValueIsDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return _value; }
                    set { _value = value; }
                }
            ";

            var expected = new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = "Use auto properties when possible.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 3, 17) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void PropertyWithGetterAndPrivateSetterUsingInternalValueIsDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return _value; }
                    private set { _value = value; }
                }
            ";

            var expected = new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = "Use auto properties when possible.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 3, 17) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void PropertyWithGetterUsingInternalValueWithoutSetterIsDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return _value; }
                }
            ";

            var expected = new DiagnosticResult
            {
                Id = AutoPropertyAnalyzer.DiagnosticId,
                Message = "Use auto properties when possible.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 3, 17) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void PropertyWithGetterReturningLiteralIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return 0; }
                    set { _value = value; }
                }
            ";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithGetterReturningMethodResultIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int a() { return 1; }
                public int Value
                {
                    get 
                    { 
    	                return a();
                    }
                }
            ";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithSetterHavingInternalLogicIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return _value; }
                    set
                    { 
                        _value = value++;
                    }
                }
            ";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithGetterHavingInternalLogicIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"
                private int _value;
                public int Value
                {
                    get { return _value++; }
                    set { _value = value; }
                }
            ";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithAutoGetterAndSetterIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"public int Value { get; set; }";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithAutoGetterAndPrivateSetterIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"public int Value { get; private set; }";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithAutoGetterIsNotDetectedForUsingAutoProperty()
        {
            const string source = @"public int Value { get; }";

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void PropertyWithGetterAndSetterUsingInternalValueIsRefactoredToUseAutoProperty()
        {
            const string sourceBefore = 
                @"public int Value
                {
                    get { return _value; }
                    set { _value = value; }
                }
            ";

            const string sourceAfter = @"public int Value { get; set; }";

            VerifyCSharpFix(sourceBefore, sourceAfter);
        }

        [Fact]
        public void PropertyWithGetterAndPrivateSetterUsingInternalValueIsRefactoredToUseAutoProperty()
        {
            const string sourceBefore =
                @"public int Value
                {
                    get { return _value; }
                    private set { _value = value; }
                }
            ";

            const string sourceAfter = @"public int Value { get; private set; }";

            VerifyCSharpFix(sourceBefore, sourceAfter);
        }

        [Fact]
        public void PropertyWithGetterAndNoSetterUsingInternalValueIsRefactoredToUseAutoProperty()
        {
            const string sourceBefore =
                @"public int Value
                {
                    get { return _value; }
                }
            ";

            const string sourceAfter = @"public int Value { get; }";

            VerifyCSharpFix(sourceBefore, sourceAfter);
        }
    }
}