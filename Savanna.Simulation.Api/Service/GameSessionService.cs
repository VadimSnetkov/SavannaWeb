using Savanna.Core.Configuration;
using Savanna.Core.Models;
using Savanna.Core.Service;
using Savanna.Simulation.Api.Models;

namespace Savanna.Simulation.Api.Service;

public sealed class GameSessionService
{
    private readonly SimulationService _simulation;
    private readonly AnimalSpawnService _spawner;
    private readonly AnimalCatalog _catalog;
    private readonly SimulationOptions _options;
    private readonly PluginLoadResult _plugins;
    private string? _error;

    public Guid Id { get; } = Guid.NewGuid();
    public bool IsRunning { get; private set; }
    public DateTimeOffset LastAccess { get; set; }

    /// Summary:
    /// Holds the engine and behavior instances belonging to one browser game.
    public GameSessionService(SimulationService simulation, AnimalSpawnService spawner,
        AnimalCatalog catalog, SimulationOptions options, PluginLoadResult plugins)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(spawner);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(plugins);
        _simulation = simulation;
        _spawner = spawner;
        _catalog = catalog;
        _options = options;
        _plugins = plugins;
    }

    /// Summary:
    /// Copies the current state so responses cannot change during serialization.
    public GameResponse Snapshot()
    {
        var state = _simulation.State;
        var animals = state.Animals.Select(animal => new AnimalResponse(
            animal.Id, animal.Type, animal.Position.Row, animal.Position.Column,
            animal.Health, state.Round - animal.BirthRound, animal.OffspringCount,
            animal.Speed, animal.VisionRange)).ToArray();
        var species = _catalog.Animals.Select(entry => new SpeciesResponse(
            entry.Key, entry.Value.Symbol.ToString(), entry.Value.SpawnKey.ToString(),
            entry.Value.IsPredator, animals.Count(animal => animal.Species == entry.Key))).ToArray();
        var warnings = _plugins.Warnings.Concat(_catalog.Warnings).ToList();
        if (_catalog.LastBehaviorError is { } behaviorError) warnings.Add(behaviorError);
        if (_error is not null) warnings.Add(_error);
        return new GameResponse(Id, state.Width, state.Height, state.Round, IsRunning,
            _options.RoundDelayMilliseconds, animals, species, warnings.Distinct().ToArray());
    }

    /// Summary:
    /// Adds one animal or reports that the field has no empty cells.
    public void Spawn(string species)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(species);
        if (!_spawner.TryAddAnimal(_simulation.State, species))
            throw new InvalidOperationException("The field is full.");
    }

    /// Summary:
    /// Starts automatic rounds on the server.
    public void Play()
    {
        _error = null;
        IsRunning = true;
    }

    /// Summary:
    /// Stops automatic rounds while keeping the game available.
    public void Pause() => IsRunning = false;

    /// Summary:
    /// Advances exactly one round while the game is paused.
    public void Step()
    {
        if (IsRunning) throw new InvalidOperationException("Pause the game before stepping.");
        _simulation.AdvanceRound();
    }

    /// Summary:
    /// Gives a running game one round of simulation time.
    public void Tick()
    {
        if (IsRunning) _simulation.AdvanceRound();
    }

    /// Summary:
    /// Stops a failed game without stopping other sessions.
    public void StopAfterError()
    {
        IsRunning = false;
        _error = "The simulation stopped after an error. Start a new game to continue.";
    }
}
