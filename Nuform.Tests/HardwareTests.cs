using Nuform.Core.Services;
using Xunit;

namespace Nuform.Tests;

public class HardwareTests
{
    [Fact]
    public void ScrewPackageCalculationUsesDivisors()
    {
        Assert.Equal(1, BomService.CalcScrewPackages(100, 0, 2.0));
        Assert.Equal(2, BomService.CalcScrewPackages(800, 0, 1.5));
        Assert.Equal(2, BomService.CalcScrewPackages(1001, 0, 2.0));
    }
}
