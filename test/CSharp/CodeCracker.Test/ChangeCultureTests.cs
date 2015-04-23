#pragma warning disable CC0022 //todo: remove this pragma as soon as CC0022 is fixed (issue #319)
using FluentAssertions;
using Xunit;

namespace CodeCracker.Test.CSharp
{
    public class ChangeCultureTests
    {
        [Fact]
        public void ChangesCulture()
        {
            using (new ChangeCulture("en-US"))
                2.5.ToString().Should().Be("2.5");
            using (new ChangeCulture("pt-BR"))
                2.5.ToString().Should().Be("2,5");
            using (new ChangeCulture(""))
                2.5.ToString().Should().Be("2.5");
        }
    }
}
#pragma warning restore CC0022