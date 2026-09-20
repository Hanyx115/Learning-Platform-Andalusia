using MediatR;
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

    [Theory]
    [InlineData(typeof(IMediator))]
    [InlineData(typeof(ISender))]
    [InlineData(typeof(IPublisher))]
    public void AddApplication_RegistersMediatRServices(Type serviceType)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == serviceType);
    }
}
