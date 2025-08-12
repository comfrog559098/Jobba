using Xunit;

namespace Jobba.Tests;

public class SmokeTests
{
    [Fact]
    public void MathWorks()
    {
        Assert.Equal(4, 2 + 2);
    }
}
