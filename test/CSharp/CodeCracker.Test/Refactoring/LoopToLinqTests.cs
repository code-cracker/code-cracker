using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class LoopToLinqTests : CodeFixVerifier<LoopToLinqAnalyzer, LoopToLinqCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreEmptyLoop()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
foreach (var x in xs)
{
}
".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreLoopWithoutAdd()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
foreach (var x in xs)
{
    Console.WriteLine(x);
}
".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenTargetCollectionIsAField()
        {
            var source = @"
System.Collections.Generic.List<int> ys;
void Foo()
{
    var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
    foreach (var x in xs)
    {
        ys.Add(x);
    }
}
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenTargetCollectionIsAParam()
        {
            var source = @"
void Foo(System.Collections.Generic.List<int> ys)
{
    var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
    foreach (var x in xs)
    {
        ys.Add(x);
    }
}
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenAddIsEmpty()
        {
            var source = @"
    var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
    var ys = new System.Collections.Generic.List<int>();
    foreach (var x in xs)
    {
        ys.Add();
    }
".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenCollectionIsAnExpression()
        {
            var source = @"
    var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
    var ys = new System.Collections.Generic.List<int>();
    foreach (var x in xs)
    {
        ys.ToList().Add(x);
    }
".WrapInCSharpMethod(usings: "\r\nusing System.Linq;");
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenVariableHasMoreThanOneDeclarator()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    int w,z = x + 1;
    ys.Add(z);
}".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenCollectionHasInitializer()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int> { 4 };
foreach (var x in xs)
{
    ys.Add(x);
}".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithSimpleAddShouldReportDiagnostic()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod();
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(13, 24));
        }

        [Fact]
        public async Task WithFilterAndAddShouldReportDiagnostic()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    if (x > 1)
        ys.Add(x);
}
".WrapInCSharpMethod();
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(13, 24));
        }

        [Fact]
        public async Task IgnoreWhenTargetCollectionIsAltered()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
ys.Add(1);
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenTargetCollectionIsNotNewedUp()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
System.Collections.Generic.List<int> ys = GetList();
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TurnSimpleLoopIntoLinq()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         select x;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task CreateLinqWhenThereAreMultipleVariablesInDeclaration()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
//comment 3
System.Collections.Generic.IEnumerable<int> zs, ys = new System.Collections.Generic.List<int>();//comment 4
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
//comment 3
System.Collections.Generic.IEnumerable<int> zs;//comment 4
var ys = from x in xs
         select x;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task CreateLinqAndAddUsingSystemLinq()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    ys.Add(x);
}
".WrapInCSharpMethod();
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         select x;
".WrapInCSharpMethod(usings: "\r\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task TurnAnotherSimpleLoopIntoLinq()
        {
            var source = @"
var coll = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var c in coll)
{
    ys.Add(c);
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var coll = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from c in coll
         select c;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task TurnALoopWithACollectionOfObjectsAndMemberAccessorWithDifferentTypeIntoLinq()
        {
            const string source = @"
using System.Linq;
class C
{
    public int I { get; set; }
    void Foo()
    {
        var foos = new System.Collections.Generic.List<C>();
        var other = new System.Collections.Generic.List<int>();
        //comment 2
        foreach (var f in foos)
        {
            other.Add(f.I);
        }//comment 1
    }
}
";
            const string fix = @"
using System.Linq;
class C
{
    public int I { get; set; }
    void Foo()
    {
        var foos = new System.Collections.Generic.List<C>();
        //comment 2
        var other = from f in foos
                    select f.I;//comment 1
    }
}
";
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task TurnLoopWithConditionIntoLinq()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    if (x > 1)
        ys.Add(x);
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         where x > 1
         select x;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task WithVariableShouldReportDiagnostic()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    var z = x + 1;
    ys.Add(z);
}".WrapInCSharpMethod();
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(13, 24));
        }

        [Fact]
        public async Task TurnLoopWithLetIntoLinq()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    var z = x + 1;
    ys.Add(z);
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         let z = x + 1
         select z;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task TurnLoopWithLetAndIfIntoLinq()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    var z = x + 1;
    if (z > 1)
    {
        ys.Add(z);
    }
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         let z = x + 1
         where z > 1
         select z;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(source, fix);
        }

        [Fact]
        public async Task FixAllInSameMethodWorks()
        {
            var source = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    var z = x + 1;
    if (z > 1)
    {
        ys.Add(z);
    }
}
var ws = new System.Collections.Generic.List<int>();
foreach (var x in xs)
{
    var z = x + 1;
    if (z > 1)
    {
        ws.Add(z);
    }
}
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
var ys = from x in xs
         let z = x + 1
         where z > 1
         select z;
var ws = from x in xs
         let z = x + 1
         where z > 1
         select z;
".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAllAsync(source, fix);
        }

        private static DiagnosticResult CreateDiagnostic(int diagnosticLine, int diagnosticColumn)
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.LoopToLinq.ToDiagnosticId(),
                Message = LoopToLinqAnalyzer.MessageFormat.ToString(),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", diagnosticLine, diagnosticColumn) }
            };
        }
    }
}