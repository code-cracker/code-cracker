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

        }

        [Fact]
        public void PropertyWithGetterAndPrivateSetterUsingInternalValueIsDetectedForUsingAutoProperty()
        {

        }

        [Fact]
        public void PropertyWithGetterUsingInternalValueWithoutSetterIsDetectedForUsingAutoProperty()
        {

        }

        [Fact]
        public void PropertyWithGetterReturningLiteralIsNotDetectedForUsingAutoProperty()
        {

        }

        [Fact]
        public void PropertyWithGetterReturningMethodResultIsNotDetectedForUsingAutoProperty()
        {

        }

        [Fact]
        public void PropertyWithSetterHavingInternalLogicIsNotDetectedForUsingAutoProperty()
        {

        }
    }
}