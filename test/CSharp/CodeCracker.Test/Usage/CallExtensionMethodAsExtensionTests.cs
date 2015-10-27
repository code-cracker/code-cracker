using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class CallExtensionMethodAsExtensionTests :
        CodeFixVerifier<CallExtensionMethodAsExtensionAnalyzer, CallExtensionMethodAsExtensionCodeFixProvider>
    {
        [Fact]
        public async Task WhenCallExtensionMethodAsExtensionHasNoDiagnostics()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any(x => x > 1);
                            }
                        }
                    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMenthodTriggerAFix()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(),
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 33) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithFullNamespaceAsStaticMenthodTriggerAFix()
        {
            const string source = @"
                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                System.Linq.Enumerable.Any(source);
                            }
                        }
                    }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(),
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 33) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMenthodShouldFix()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            const string expected = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any(x => x > 1);
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithFullNamespaceAndNotImportedShouldImport()
        {
            const string source = @"
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;

                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                System.Linq.Enumerable.Any(source);
                            }
                        }
                    }";

            const string expected = @"
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Linq;

                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Any();
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithChainCallsShouldNotBreakTheChain()
        {
            const string source = @"
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                Enumerable.Select(source, (p) => p.ToString()).FirstOrDefault();
                            }
                        }
                    }";

            const string expected = @"
                    using System.Linq;

                    namespace ConsoleApplication1
                    { 
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                source.Select((p) => p.ToString()).FirstOrDefault();
                            }
                        }
                    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithDynamicArgumentHasNoDiagnostics()
        {
            const string source = @"
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;

                    namespace ConsoleApplication1
                    {
                        class ExtensionTest
                        {
                            void method()
                            {
                                var list = new List<int>() { 5, 56, 2, 4, 63, 2 };
                                dynamic dList = list;
                                Console.WriteLine(Enumerable.First(dList));
                            }
                        }
                    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenTheSelectedMethodWouldChangeThenCreatesNoDiagnostics()
        {
            const string source = @"
using System.Linq;
using System.Collections.Generic;
public static class ExtensionsTestCase
{
    public static void Select(this IEnumerable<string> strings, System.Func<string, bool> mySelector)
    {
    }
    public static void UsesSelect()
    {
        Enumerable.Select(new[] { """" }, s => s == """");
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMethodInsideForDoesNotThrowAndCreatesDiagnostic()
        {
            const string source = @"
                    using System.Linq;
                    namespace ConsoleApplication1
                    {
                        public class Foo
                        {
                            public void Bar()
                            {
                                var source = new int[] { 1, 2, 3 };
                                for (int i = 0; i < 10; i++)
                                    Enumerable.Any(source, x => x > 1);
                            }
                        }
                    }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(),
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 37) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }


        [Fact]
        public async Task WhenCallExtensionMethodWithoutInvocationStatement()
        {
            const string source = @"
              using System.Linq;
              namespace ConsoleApplication1
              {
                  public class Foo
                  {
                      static int[] source = new int[] { 1, 2, 3 };
                      public static void Bar() => Enumerable.Any(source);
                  }
              }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(),
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 51) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}