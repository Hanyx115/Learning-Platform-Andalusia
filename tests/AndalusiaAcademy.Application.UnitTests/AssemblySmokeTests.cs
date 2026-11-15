namespace AndalusiaAcademy.Application.UnitTests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ApplicationAssembly_CanBeLoaded()
    {
        var assembly = System.Reflection.Assembly.Load("AndalusiaAcademy.Application");

        Assert.Equal("AndalusiaAcademy.Application", assembly.GetName().Name);
    }
}
