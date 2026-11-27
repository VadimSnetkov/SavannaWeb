using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Savanna.Core.Service;
using Savanna.Simulation.Api.Extensions;
using Savanna.Tests.Helpers;

namespace Savanna.Tests;

public sealed class SimulationDependencyTests
{
    /// Summary:
    /// Proves DI can replace movement, health and birth implementations without changing the engine.
    [Fact]
    public void AdvanceRound_UsesTheRegisteredServiceContracts()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("api-appsettings.json").Build();
        var spy = new SimulationDependencySpy();
        var services = new ServiceCollection();
        services.AddSavannaSimulation(configuration);
        services.AddScoped<IAnimalMovementService>(_ => spy);
        services.AddScoped<IHealthService>(_ => spy);
        services.AddScoped<IBirthService>(_ => spy);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var simulation = scope.ServiceProvider.GetRequiredService<SimulationService>();
        var spawner = scope.ServiceProvider.GetRequiredService<AnimalSpawnService>();
        Assert.True(spawner.TryAddAnimal(simulation.State, "Antelope"));

        simulation.AdvanceRound();

        Assert.Equal(new[] { "health", "move", "birth" }, spy.Calls);
        Assert.Equal(1, simulation.State.Round);
        Assert.Single(simulation.State.Animals);
    }
}
