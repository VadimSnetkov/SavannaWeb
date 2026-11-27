using FluentValidation;
using Savanna.Core.Configuration;
using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class SimulationService
{
    private readonly IAnimalMovementService _movementService;
    private readonly Random _random;
    private readonly IHealthService _healthService;
    private readonly IBirthService _birthService;

    public SavannaState State { get; }

    /// Summary:
    /// Validates the settings and creates an empty simulation.
    public SimulationService(
        SimulationOptions options,
        IValidator<SimulationOptions> validator,
        IAnimalMovementService movementService,
        Random random,
        IHealthService healthService,
        IBirthService birthService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(movementService);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(healthService);
        ArgumentNullException.ThrowIfNull(birthService);

        validator.ValidateAndThrow(options);

        _movementService = movementService;
        _random = random;
        _healthService = healthService;
        _birthService = birthService;

        State = new SavannaState(options.Width, options.Height);
    }

    /// Summary:
    /// Gives surviving animals their turns, then checks for births.
    public void AdvanceRound()
    {
        var animalsThisRound = State.Animals.ToArray();

        _random.Shuffle(animalsThisRound);

        foreach (var animal in animalsThisRound)
        {
            if (!State.MutableAnimals.Contains(animal) ||
                _healthService.RemoveIfDead(State, animal))
            {
                continue;
            }

            _movementService.MoveAnimal(State, animal);
        }

        State.Round++;
        _birthService.ProcessRound(State);
    }

}
