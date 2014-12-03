﻿using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ParameterRefectoryTest : CodeFixTest<ParameterRefactoryAnalyzer, ParameterRefactoryCodeFixProvider>
    {

        [Fact]
        public void WhenMethodDoesNotThreeParametersNotSuggestionANewClass()
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

            VerifyCSharpHasNoDiagnostics(test);

        }


        [Fact]
        public void ShouldUpdateParameterToClass()
        {
            const string oldTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string a,string b,int year,string d)
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
            VerifyCSharpFix(oldTest, newTest, 0);



        }

        [Fact]
        public void WhenHasNotNameSpaceShouldGenerateClassParameter()
        {
            const string oldTest = @"
    using System;

        class TypeName
        {
            public void Foo(string a,string b,int year,string d)
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

            VerifyCSharpFix(oldTest, newTest, 0);



        }

        [Fact]
        public void ShouldGenerateNewClassFoo2()
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

            public void Foo2(string a,string b,int year,string d)
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
            VerifyCSharpFix(oldTest, newTest, 0);


        }

    }

}
