using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;
namespace CodeCracker.Test.Usage
{
    public class CallExtensionMethodAsExtensionTests :
        CodeFixTest<CallExtensionMethodAsExtensionAnalyzer, CallExtensionMethodAsExtensionCodeFixProvider>
    {
        [Fact]
        public async Task WhenCallExtensionMethodAsExtensionHasNoDiagnostics()
        {
            var source = @"
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
            var source = @"
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
                Id = CallExtensionMethodAsExtensionAnalyzer.DiagnosticId,
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 33) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodWithFullNamespaceAsStaticMenthodTriggerAFix()
        {
            var source = @"
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
                Id = CallExtensionMethodAsExtensionAnalyzer.DiagnosticId,
                Message = "Do not call 'Any' method of class 'Enumerable' as a static method",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 33) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCallExtensionMethodAsStaticMenthodShouldFix()
        {
            var source = @"
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

            var expected = @"
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
            var source = @"
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

            var expected = @"
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
            var source = @"
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

            var expected = @"
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
    }
}