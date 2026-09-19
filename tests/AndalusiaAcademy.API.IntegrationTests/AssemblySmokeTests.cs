namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ApiAssembly_CanBeLoaded()
    {
        var assembly = System.Reflection.Assembly.Load("AndalusiaAcademy.API");

        Assert.Equal("AndalusiaAcademy.API", assembly.GetName().Name);
    }
}
