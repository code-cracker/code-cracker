using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using CodeCracker.CSharp.Refactoring;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class AddPropertyToConstructorTest : CodeFixVerifier<AddPropertyToConstructorAnalyzer, AddPropertyToConstructorFixProvider>
    {
        [Fact]
        public async Task WhenNotHasPropertyShouldNotGenerateDiagnostic()
        {
            const string test = @"
public class Foo
{
    public Foo(){}
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenHavePropertyThatIsNotValidNotGenerateDiagnostic()
        {
            const string test = @"
public class Foo
{
    public readonly int PReadOnly;
    public const string PConst = "";
    public static string PStatic { get; private set; }
    private string Pprivate { get; set; }
    public int Pint { get; set; } = int.MinValue;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenNotHaveConstructorShouldGenerateDiagnostic()
        {
            const string test = @"
public class Foo
{
	public string Name {get; private set;}
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.AddPropertyToConstructor.ToDiagnosticId(),
                Message = "Add property Name to constructor.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 2) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenHasPropertyInAnyConstructorShouldNotGenerateDiagnostic()
        {
            const string test = @"
public class Foo
{
	public string Name {get; private set;}
    public string Year {get; private set;}
    public Foo(){}
    public Foo(int year = 0){}
	public Foo(string name, int year)
	{
        this.Name = name;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.AddPropertyToConstructor.ToDiagnosticId(),
                Message = "Add property Year to constructor.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 5) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

        }

        [Fact]
        public async Task WhenHasPropertyInAnyConstructorShouldNotRaiseDiagnostic()
        {
            const string test = @"
public class Foo
{
	public string Name {get; private set;}
    public Foo(){}
    public Foo(int year = 0){}
	public Foo(string name)
    {
        Name = name;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public Task WhenNotHasDefaultConstructorShouldGenerateFixWithConstructorDefault()
        {
            const string source = @"
public class Foo
{
	public string Name { get; private set;}
}";

            const string expected = @"
public class Foo
{
    public string Name { get; private set; }

    public Foo()
    {
    }

    public Foo(string name)
    {
        Name = name;
    }
}";
            return VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public Task WhenHasConstructorDefaultShouldGenerateNewConstructor()
        {
            const string source = @"
public class Foo
{
    public System.DateTime DateUpdate { get; private set; }
    
    public Foo()
    {
    }
}";

            const string expected = @"
public class Foo
{
    public System.DateTime DateUpdate { get; private set; }

    public Foo()
    {
    }

    public Foo(System.DateTime dateUpdate):this()
    {
        DateUpdate = dateUpdate;
    }
}";
            return VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public Task WhenHasConstructorDefaultWithParameterShoulAddNewParameterWithDefaultValue()
        {
            const string source = @"
public class Foo
{
    public string Name { get; private set; }

    public string LastName {get; private set;}
    
    public int Year { get; private set;}  
 
    public Foo(string Name)
    {
        Name = name;
    }
}";

            const string expected = @"
public class Foo
{
    public string Name { get; private set; }

    public string LastName { get; private set;}
    
    public int Year { get; private set;}  
   
    public Foo(string Name, string lastName = """", int year = int.MinValue)
    {
       Name = name;
       LastName = lastName;
       Year = year;
    }
}";
            return VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public Task WhenHasConstructorWithSameAmountParameterShouldAddNewParamterInLastConstructor()
        {
            const string source = @"
public class Foo
{
    public int Day { get; private set; }
    
    public int Month { get; private set;}

    public sting Year { get; private set;}

    public string Name { get; private set;}
    
    public string LastName { get; private set;}

    public Foo()
    {
    }

    public Foo(int day, int month)
    {
        Day = day;
        Month = month;
    }
    
    public Foo(string year,string name)
    {
       Year = year;
       Name = name;
    }
}";

            const string expected = @"
public class Foo
{
    public int Day { get; private set; }

    public int Month { get; private set; }

    public sting Year { get; private set; }

    public string Name { get; private set; }

    public string LastName { get; private set; }

    public Foo()
    {
    }

    public Foo(int day, int month)
    {
        Day = day;
        Month = month;
    }

    public Foo(string year, string name, string lastName = """")
    {
         Year = year;
         Name = name;
         LastName = lastName;
    }
}";
            return VerifyCSharpFixAsync(source, expected);

        }

        [Fact]
        public Task ShouldGenerateParameterWithAllDefaultTypesPredefined()
        {
            const string source = @"
public class Foo
{
    public  System.Boolean? Pboolean { get; private set; }
    public bool Pbool { get; private set; }
    public byte Pbyte { get; private set; }
    public sbyte Psbyte { get; private set; }
    public char Pchar { get; private set; }
    public decimal Pdecimal { get; private set; }
    public double Pdouble { get; private set; }
    public float Pfloat { get; private set; }
    public int Pint { get; private set; }
    public System.Int16? Pint16 { get; private set; }
    public System.Int32 Pint32 { get; private set; }
    public System.Int64 Pint64 { get; private set; }
    public uint Puint { get; private set; }
    public System.UInt16 Puint16 { get; private set; }
    public System.UInt32 Puint32 { get; private set; }
    public System.UInt64 Puint64 { get; private set; }
    public long Plong { get; private set; }
    public ulong Pulong { get; private set; }
    public short Pshort { get; private set; }
    public ushort Pushort { get; private set; }
}";
            const string expected = @"
public class Foo
{
    public System.Boolean? Pboolean { get; private set; }
    public bool Pbool { get; private set; }
    public byte Pbyte { get; private set; }
    public sbyte Psbyte { get; private set; }
    public char Pchar { get; private set; }
    public decimal Pdecimal { get; private set; }
    public double Pdouble { get; private set; }
    public float Pfloat { get; private set; }
    public int Pint { get; private set; }
    public System.Int16? Pint16 { get; private set; }
    public System.Int32 Pint32 { get; private set; }
    public System.Int64 Pint64 { get; private set; }
    public uint Puint { get; private set; }
    public System.UInt16 Puint16 { get; private set; }
    public System.UInt32 Puint32 { get; private set; }
    public System.UInt64 Puint64 { get; private set; }
    public long Plong { get; private set; }
    public ulong Pulong { get; private set; }
    public short Pshort { get; private set; }
    public ushort Pushort { get; private set; }

    public Foo()
    {
    }

    public Foo(System.Boolean? pboolean, bool pbool = false, byte pbyte = byte.MinValue, sbyte psbyte = sbyte.MinValue, char pchar = char.MinValue, decimal pdecimal = decimal.MinValue, double pdouble = double.MinValue, float pfloat = float.MinValue, int pint = int.MinValue, System.Int16? pint16 = System.Int16.MinValue, System.Int32 pint32 = System.Int32.MinValue, System.Int64 pint64 = System.Int64.MinValue, uint puint = uint.MinValue, System.UInt16 puint16 = System.UInt16.MinValue, System.UInt32 puint32 = System.UInt32.MinValue, System.UInt64 puint64 = System.UInt64.MinValue, long plong = long.MinValue, ulong pulong = ulong.MinValue, short pshort = short.MinValue, ushort pushort = ushort.MinValue)
    {
        Pboolean = pboolean;
        Pbool = pbool;
        Pbyte = pbyte;
        Psbyte = psbyte;
        Pchar = pchar;
        Pdecimal = pdecimal;
        Pdouble = pdouble;
        Pfloat = pfloat;
        Pint = pint;
        Pint16 = pint16;
        Pint32 = pint32;
        Pint64 = pint64;
        Puint = puint;
        Puint16 = puint16;
        Puint32 = puint32;
        Puint64 = puint64;
        Plong = plong;
        Pulong = pulong;
        Pshort = pshort;
        Pushort = pushort;
    }
}";
            return VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public Task ShouldGenerateParameterWithDefaultType()
        {
            const string source = @"
using System;
using System.Collections.Generic;

namespace ClassLibrary
{
    public class Foo
    {
        public string LastName { get; private set; }
        
        public DateTime? DateUpdate { get; private set; }

        public List<Foo> LFoot { get; private set; }

        public Foo()
        {
        }
    }
}";
            const string expected = @"
using System;
using System.Collections.Generic;

namespace ClassLibrary
{
    public class Foo
    {
        public string LastName { get; private set; }
        
        public DateTime? DateUpdate { get; private set; }

        public List<Foo> LFoot { get; private set; }

        public Foo()
        {
        }

        public Foo(string lastName, DateTime? dateUpdate = default(DateTime?), List<Foo> lFoot = default(List<Foo>)):this()
        {
            LastName = lastName;
            DateUpdate = dateUpdate;
            LFoot = lFoot;
        }
    }
}";

            return VerifyCSharpFixAsync(source, expected);
        }
    }
}