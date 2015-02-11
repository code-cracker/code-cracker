using CodeCracker.CSharp.Refactoring;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.CSharp.Test.Refactoring
{
    public class ParameterRefectoryTests : CodeFixTest<ParameterRefactoryAnalyzer, ParameterRefactoryCodeFixProvider>
    {

        [Fact]
        public async Task WhenMethodDoesNotThreeParametersNotSuggestionANewClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string name,string age,string day)
            {
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task WhenMethodHasElementBodyAndHasMoreThreeParametersShouldNotSuggestionANewClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string name,string age,int day,int year)
            {
                if(true)
                {
                   day = 10;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task WhenMethodHasARefParameterShouldNotSuggestANewClass()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string a, string b, int year, ref string d)
        {

        }
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenMethodHasAnOutParameterShouldNotSuggestANewClass()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string a, string b, int year, out string d)
        {

        }
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenMethodHasAThisParameterShouldNotSuggestANewClass()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(this string a, string b, int year, string d)
        {

        }
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenMethodHasAParamsParameterShouldNotSuggestANewClass()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string a, string b, int year, params string[] d)
        {

        }
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenMethodIsMarkedAsExternShouldNotSuggestANewClass()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        [System.Runtime.InteropServices.DllImport(""dllName"")]
        public extern void Foo(string a, string b, int year, string d);
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ShouldUpdateParameterToClass()
        {
            const string oldTest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string a, string b, int year, string d)
        {

        }
    }

}";
            const string newTest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(NewClassFoo newClassFoo)
        {

        }
    }

    public class NewClassFoo
    {
        public string A { get; set; }
        public string B { get; set; }
        public int Year { get; set; }
        public string D { get; set; }
    }
}";
            await VerifyCSharpFixAsync(oldTest, newTest, 0);



        }

        [Fact]
        public async Task WhenHasNotNameSpaceShouldGenerateClassParameter()
        {
            const string oldTest = @"
using System;

class TypeName
{
    public void Foo(string a, string b, int year, string d)
    {

    }
}
";
            const string newTest = @"
using System;

class TypeName
{
    public void Foo(NewClassFoo newClassFoo)
    {

    }
}

public class NewClassFoo
{
    public string A { get; set; }
    public string B { get; set; }
    public int Year { get; set; }
    public string D { get; set; }
}";

            await VerifyCSharpFixAsync(oldTest, newTest, 0);



        }

        [Fact]
        public async void ShouldGenerateNewClassFoo2()
        {
            const string oldTest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(NewClassFoo newClassFoo)
        {

        }

        public void Foo2(string a, string b, int year, string d)
        {

        }
    }

    public class NewClassFoo
    {
        public string A { get; set; }
        public string B { get; set; }
        public int Year { get; set; }
        public string D { get; set; }
    }

}";
            const string newTest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(NewClassFoo newClassFoo)
        {

        }

        public void Foo2(NewClassFoo2 newClassFoo2)
        {

        }
    }

    public class NewClassFoo
    {
        public string A { get; set; }
        public string B { get; set; }
        public int Year { get; set; }
        public string D { get; set; }
    }

    public class NewClassFoo2
    {
        public string A { get; set; }
        public string B { get; set; }
        public int Year { get; set; }
        public string D { get; set; }
    }
}";
            await VerifyCSharpFixAsync(oldTest, newTest, 0);


        }

    }

}