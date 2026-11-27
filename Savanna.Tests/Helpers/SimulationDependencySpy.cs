using Savanna.Core.Models;
using Savanna.Core.Service;

namespace Savanna.Tests.Helpers;

public sealed class SimulationDependencySpy : IAnimalMovementService, IHealthService, IBirthService
{
    public List<string> Calls { get; } = [];

    /// Summary:
    /// Records movement without executing the production movement implementation.
    public void MoveAnimal(SavannaState state, Animal animal) => Calls.Add("move");

    /// Summary:
    /// Records the engine's health check and keeps the test animal alive.
    public bool RemoveIfDead(SavannaState state, Animal animal)
    {
        Calls.Add("health");
        return false;
    }

    /// Summary:
    /// Records birth processing without executing the production birth implementation.
    public void ProcessRound(SavannaState state) => Calls.Add("birth");

    /// Summary:
    /// Records a movement health cost if requested.
    public void ApplyMovementCost(Animal animal) => Calls.Add("cost");

    /// Summary:
    /// Records a feeding operation if requested.
    public void FeedPredator(Animal predator) => Calls.Add("feed");
}
