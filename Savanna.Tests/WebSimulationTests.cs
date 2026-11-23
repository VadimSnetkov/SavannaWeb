using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Savanna.Core.Configuration;
using Savanna.Simulation.Api.Configuration;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Service;
using Savanna.Tests.Helpers;

namespace Savanna.Tests;

public sealed class WebSimulationTests
{
    /// Summary:
    /// Verifies that browser games have separate animals, rounds and running states.
    [Fact]
    public void Games_DoNotShareSimulationState()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var first = manager.Create();
        var second = manager.Create();
        manager.Access(first.Id, game => game.Spawn("Antelope"));
        manager.Access(first.Id, game => game.Play());
        manager.Tick();

        Assert.Equal(1, manager.Access(first.Id).Round);
        Assert.Single(manager.Access(first.Id).Animals);
        var untouched = manager.Access(second.Id);
        Assert.Empty(untouched.Animals);
        Assert.Equal(0, untouched.Round);
        Assert.False(untouched.IsRunning);
    }

    /// Summary:
    /// Verifies that pause stops the clock and stepping advances exactly one round.
    [Fact]
    public void PauseAndStep_ControlTheServerClock()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create();
        manager.Tick();
        Assert.Equal(0, manager.Access(game.Id).Round);
        manager.Access(game.Id, session => session.Play());
        manager.Tick();
        Assert.Throws<InvalidOperationException>(() => manager.Access(game.Id, session => session.Step()));
        manager.Access(game.Id, session => session.Pause());
        manager.Tick();
        Assert.Equal(1, manager.Access(game.Id).Round);
        Assert.Equal(2, manager.Access(game.Id, session => session.Step()).Round);
    }

    /// Summary:
    /// Verifies that the separately loaded Zebra assembly works in a web game.
    [Fact]
    public void PluginAnimal_CanSpawnAndTakeItsTurn()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create();
        Assert.Contains(game.Species, species => species.Name == "Zebra");
        Assert.Empty(game.Warnings);
        var spawned = manager.Access(game.Id, session => session.Spawn("Zebra"));
        var original = Assert.Single(spawned.Animals);
        var advanced = manager.Access(game.Id, session => session.Step());
        var zebra = Assert.Single(advanced.Animals);
        Assert.Equal(original.Id, zebra.Id);
        Assert.Equal(1, zebra.Age);
        Assert.Equal(0, zebra.OffspringCount);
        Assert.Equal("Zebra", zebra.Species);
    }

    /// Summary:
    /// Verifies old responses are immutable copies and not live simulation references.
    [Fact]
    public void Snapshot_RemainsUnchangedAfterLaterCommands()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var empty = manager.Create();
        var populated = manager.Access(empty.Id, game => game.Spawn("Antelope"));
        var stepped = manager.Access(empty.Id, game => game.Step());
        Assert.Empty(empty.Animals);
        Assert.Equal(0, Assert.Single(populated.Animals).Age);
        Assert.Equal(1, Assert.Single(stepped.Animals).Age);
    }

    /// Summary:
    /// Verifies simultaneous commands cannot overlap animals or lose a spawn.
    [Fact]
    public void ConcurrentSpawns_CreateDistinctAnimalsAndPositions()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create();
        const int animalCount = 25;
        Parallel.For(0, animalCount, _ => manager.Access(game.Id, session => session.Spawn("Antelope")));
        var animals = manager.Access(game.Id).Animals;
        Assert.Equal(animalCount, animals.Length);
        Assert.Equal(animalCount, animals.Select(animal => animal.Id).Distinct().Count());
        Assert.Equal(animalCount, animals.Select(animal => (animal.Row, animal.Column)).Distinct().Count());
        Assert.All(animals, animal => Assert.Equal("Antelope", animal.Species));
    }

    /// Summary:
    /// Verifies field capacity rejects an extra animal without changing existing animals.
    [Fact]
    public void FullField_RejectsAnAdditionalAnimal()
    {
        using var provider = CreateProvider();
        var options = provider.GetRequiredService<SimulationOptions>();
        options.Width = 1;
        options.Height = 1;
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create();
        manager.Access(game.Id, session => session.Spawn("Antelope"));
        Assert.Throws<InvalidOperationException>(() => manager.Access(game.Id, session => session.Spawn("Lion")));
        Assert.Equal("Antelope", Assert.Single(manager.Access(game.Id).Animals).Species);
    }

    /// Summary:
    /// Verifies empty, missing and unknown species are rejected by input validation.
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void SpawnValidator_RejectsUnavailableSpecies(string? species)
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<SpawnAnimalRequest>>();
        Assert.False(validator.Validate(new SpawnAnimalRequest { Species = species! }).IsValid);
    }

    /// Summary:
    /// Verifies abandoned games expire while recently read games remain available.
    [Fact]
    public void Expiration_RemovesOnlyIdleGames()
    {
        var clock = new ManualTimeProvider();
        using var provider = CreateProvider(clock);
        var manager = provider.GetRequiredService<GameManager>();
        var idle = manager.Create();
        var active = manager.Create();
        clock.Current = clock.Current.AddMinutes(29);
        manager.Access(active.Id);
        clock.Current = clock.Current.AddMinutes(2);
        manager.Tick();
        Assert.Throws<KeyNotFoundException>(() => manager.Access(idle.Id));
        Assert.Equal(active.Id, manager.Access(active.Id).Id);
    }

    /// Summary:
    /// Verifies ending a game releases capacity and removes the previous state.
    [Fact]
    public void Delete_ReleasesTheSessionLimit()
    {
        using var provider = CreateProvider();
        provider.GetRequiredService<GameSessionOptions>().MaximumGames = 1;
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create();
        Assert.Throws<InvalidOperationException>(() => manager.Create());
        manager.Delete(game.Id);
        manager.Delete(game.Id);
        Assert.Throws<KeyNotFoundException>(() => manager.Access(game.Id));
        Assert.NotEqual(game.Id, manager.Create().Id);
    }

    /// Summary:
    /// Builds the production service registrations without starting a real timer.
    private static ServiceProvider CreateProvider(TimeProvider? clock = null)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("api-appsettings.json")
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Simulation:PluginDirectory"] = Path.Combine(AppContext.BaseDirectory, "PluginFixtures")
            }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSavannaSimulation(configuration);
        if (clock is not null) services.AddSingleton(clock);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
