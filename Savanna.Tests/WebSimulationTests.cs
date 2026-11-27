using FluentValidation;
using Savanna.Simulation.Api.Extensions;
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
        var first = manager.Create().Game!;
        var second = manager.Create().Game!;
        manager.Access(first.Id, GameCommand.AddAnimal, "Antelope");
        manager.Access(first.Id, GameCommand.Play);
        manager.Tick();

        Assert.Equal(1, manager.Access(first.Id).Game!.Round);
        Assert.Single(manager.Access(first.Id).Game!.Animals);
        var untouched = manager.Access(second.Id).Game!;
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
        var game = manager.Create().Game!;
        manager.Tick();
        Assert.Equal(0, manager.Access(game.Id).Game!.Round);
        manager.Access(game.Id, GameCommand.Play);
        manager.Tick();
        Assert.Equal(GameResultStatus.Conflict, manager.Access(game.Id, GameCommand.Step).Status);
        manager.Access(game.Id, GameCommand.Pause);
        manager.Tick();
        Assert.Equal(1, manager.Access(game.Id).Game!.Round);
        Assert.Equal(2, manager.Access(game.Id, GameCommand.Step).Game!.Round);
    }

    /// Summary:
    /// Verifies that the separately loaded Zebra assembly works in a web game.
    [Fact]
    public void PluginAnimal_CanSpawnAndTakeItsTurn()
    {
        using var provider = CreateProvider();
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create().Game!;
        Assert.Contains(game.Species, species => species.Name == "Zebra");
        Assert.Empty(game.Warnings);
        var spawned = manager.Access(game.Id, GameCommand.AddAnimal, "Zebra").Game!;
        var original = Assert.Single(spawned.Animals);
        var advanced = manager.Access(game.Id, GameCommand.Step).Game!;
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
        var empty = manager.Create().Game!;
        var populated = manager.Access(empty.Id, GameCommand.AddAnimal, "Antelope").Game!;
        var stepped = manager.Access(empty.Id, GameCommand.Step).Game!;
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
        var game = manager.Create().Game!;
        const int animalCount = 25;
        Parallel.For(0, animalCount, _ => manager.Access(game.Id, GameCommand.AddAnimal, "Antelope"));
        var animals = manager.Access(game.Id).Game!.Animals;
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
        var game = manager.Create().Game!;
        manager.Access(game.Id, GameCommand.AddAnimal, "Antelope");
        Assert.Equal(GameResultStatus.Conflict, manager.Access(game.Id, GameCommand.AddAnimal, "Lion").Status);
        Assert.Equal("Antelope", Assert.Single(manager.Access(game.Id).Game!.Animals).Species);
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
        var idle = manager.Create().Game!;
        var active = manager.Create().Game!;
        clock.Current = clock.Current.AddMinutes(29);
        manager.Access(active.Id);
        clock.Current = clock.Current.AddMinutes(2);
        manager.Tick();
        Assert.Equal(GameResultStatus.NotFound, manager.Access(idle.Id).Status);
        Assert.Equal(active.Id, manager.Access(active.Id).Game!.Id);
    }

    /// Summary:
    /// Verifies ending a game releases capacity and removes the previous state.
    [Fact]
    public void Delete_ReleasesTheSessionLimit()
    {
        using var provider = CreateProvider();
        provider.GetRequiredService<GameSessionOptions>().MaximumGames = 1;
        var manager = provider.GetRequiredService<GameManager>();
        var game = manager.Create().Game!;
        Assert.Equal(GameResultStatus.Conflict, manager.Create().Status);
        manager.Delete(game.Id);
        manager.Delete(game.Id);
        Assert.Equal(GameResultStatus.NotFound, manager.Access(game.Id).Status);
        Assert.NotEqual(game.Id, manager.Create().Game!.Id);
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
