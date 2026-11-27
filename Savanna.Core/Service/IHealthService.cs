using Savanna.Core.Models;

namespace Savanna.Core.Service;

public interface IHealthService
{
    /// Summary:
    /// Deducts health for one completed movement.
    void ApplyMovementCost(Animal animal);

    /// Summary:
    /// Restores health after a predator eats.
    void FeedPredator(Animal predator);

    /// Summary:
    /// Removes an animal when its health is exhausted.
    bool RemoveIfDead(SavannaState state, Animal animal);
}
