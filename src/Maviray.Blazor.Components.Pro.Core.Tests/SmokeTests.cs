using Maviray.Blazor.Components.Pro.Core.Constants;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void TestProject_ReferencesCoreAssembly()
    {
        Assert.Equal("Id", QueryConstants.ID);
    }
}
