using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RedundantFieldAssignmentTests : CodeFixVerifier<RedundantFieldAssignmentAnalyzer, RedundantFieldAssignmentCodeFixProvider>
    {
        [Fact]
        public async Task FieldWithoutAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToOneIntDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i = 1;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToOneDecimalDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private decimal i = 1.1m;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToNearZeroDecimalDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private decimal i = 0.1m;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToDoubleMaxLiteralDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private double i = 1.7976931348623157E+308;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToIntMaxLiteralDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i = 2147483647;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToUintMaxLiteralDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private uint i = 4294967295;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StringFieldWithAssignmentToAStringDoesNotCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private string s = ""a"";
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstStringFieldWithAssignmentToNullDoesNotCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private const string s = null;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntPtrWithAssignmentToNewSystemIntPtrDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.IntPtr i = new System.IntPtr(1);
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UIntPtrWithAssignmentToNewSystemUIntPtrDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.UIntPtr i = new System.UIntPtr(1);
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DateTimeWithAssignmentToNewDateTimeDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.DateTime d = new System.DateTime(1);
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task EnumWithAssignmentToSomeEnumValueDoesNotCreateDiagnostic()
        {
            const string source = @"
enum E { A = 1, B = 2 }
class TypeName
{
    private E e = E.A;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task BoolWithAssignmentToTrueDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private bool b = true;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToZeroOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i = 0;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 17)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", 0));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToDefaultCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i = default(int);
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 17)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "default(int)"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task StringFieldWithAssignmentToNullCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private string s = null;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 20)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "s", "null"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task LongFieldWithAssignmentTo0LCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private long i = 0L;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 18)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "0L"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task LongFieldWithAssignmentToZeroCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private long i = 0;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 18)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "0"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IntPtrWithAssignmentToSystemIntPtrZeroCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.IntPtr i = System.IntPtr.Zero;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 27)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "System.IntPtr.Zero"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IntPtrWithAssignmentToIntPtrZeroCreatesDiagnostic()
        {
            const string source = @"
using System;
class TypeName
{
    private IntPtr i = IntPtr.Zero;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(5, 20)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "IntPtr.Zero"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task UIntPtrWithAssignmentToSystemUIntPtrZeroCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.UIntPtr i = System.UIntPtr.Zero;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 28)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "i", "System.UIntPtr.Zero"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DateTimeWithAssignmentToDateTimeMinValueCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private System.DateTime d = System.DateTime.MinValue;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 29)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "d", "System.DateTime.MinValue"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task EnumWithAssignmentToZeroCreatesDiagnostic()
        {
            const string source = @"
enum E { A = 1, B = 2 }
class TypeName
{
    private E e = 0;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(5, 15)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "e", "0"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task EnumWithAssignmentToZeroDoubleCreatesDiagnostic()
        {
            const string source = @"
enum E { A = 1, B = 2 }
class TypeName
{
    private E e = 0.0;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(5, 15)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "e", "0.0"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task BoolWithAssignmentToFalseCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private bool b = false;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 18)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "b", "false"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IntFieldWithAssignmentToZeroWithMultipleVariableDeclarationsOnTheSameFieldOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i, j, k = 0;
}";
            var expected = new DiagnosticResult(DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(4, 23)
                .WithMessage(string.Format(RedundantFieldAssignmentAnalyzer.MessageFormat, "k", 0));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task RemoveInitializerFromSimpleIntField()
        {
            const string source = @"
class TypeName
{
    //comment 1
    private int i = 0;//comment 2
}";
            const string fixtest = @"
class TypeName
{
    //comment 1
    private int i;//comment 2
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task RemoveInitializerFromMultipleVariables()
        {
            const string source = @"
class TypeName
{
    //comment 1
    private int i, j, k = 0;//comment 2
}";
            const string fixtest = @"
class TypeName
{
    //comment 1
    private int i, j, k;//comment 2
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAllRemoveInitializerFromTwoSimpleIntFields()
        {
            const string source = @"
class TypeName
{
    //comment 1
    private int i = 0;//comment 2
    private int j = 0;//comment 3
    class TypeName2
    {
        //comment 4
        private int k = 0;//comment 5
        private int l = 0;//comment 6
    }
    private int m = 0;//comment 7
}";
            const string fixtest = @"
class TypeName
{
    //comment 1
    private int i;//comment 2
    private int j;//comment 3
    class TypeName2
    {
        //comment 4
        private int k;//comment 5
        private int l;//comment 6
    }
    private int m;//comment 7
}";
            await VerifyCSharpFixAllAsync(source, fixtest);
        }
    }
}