namespace AndalusiaAcademy.Domain.UnitTests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void DomainAssembly_CanBeLoaded()
    {
        var assembly = System.Reflection.Assembly.Load("AndalusiaAcademy.Domain");

        Assert.Equal("AndalusiaAcademy.Domain", assembly.GetName().Name);
    }
}
