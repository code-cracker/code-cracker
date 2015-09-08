﻿using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class StaticConstructorExceptionTests : CodeFixVerifier<StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider>
    {
        [Fact]
        public async Task WarningIfExceptionIsThrowInsideStaticConstructor()
        {
            const string test = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StaticConstructorException.ToDiagnosticId(),
                Message = "Don't throw exception inside static constructors.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task NotWarningWhenNoExceptionIsThrowInsideStaticConstructor()
        {
            const string test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task StaticConstructorWithoutException()
        {
            const string test = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 

                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task InstanceConstructorWithoutException()
        {
            const string test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                    
                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenThrowIsRemovedFromStaticConstructor()
        {
            const string source = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            const string fixtest = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                    } 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowIsRemovedFromAllStaticConstructors()
        {
            const string source1 = @"
                public class MyClass1 
                { 
                    static MyClass1() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";
            const string fixtest1 = @"
                public class MyClass1 
                { 
                    static MyClass1() 
                    { 
                    } 
                }";

            const string source2 = @"
                public class MyClass2 
                { 
                    static MyClass2() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";
            const string fixtest2 = @"
                public class MyClass2 
                { 
                    static MyClass2() 
                    { 
                    } 
                }";

            await VerifyCSharpFixAllAsync(new string[] { source1, source2 }, new string[] { fixtest1, fixtest2 });
        }
    }
}