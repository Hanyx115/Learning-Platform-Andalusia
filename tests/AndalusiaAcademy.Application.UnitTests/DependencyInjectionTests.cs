using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ReturnsTheSameServiceCollection()
    {
        IServiceCollection services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }
}
