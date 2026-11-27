using Savanna.Core.Models;

namespace Savanna.Core.Service;

public interface IMovementService
{
    /// Summary:
    /// Finds the nearest visible animal matching a behavior's rule.
    Animal? FindNearestVisibleAnimal(Animal observer, IReadOnlyList<Animal> animals, Func<Animal, bool> matches);

    /// Summary:
    /// Selects one of the available positions.
    Position ChooseRandomPosition(IReadOnlyList<Position> positions);

    /// Summary:
    /// Chooses a neighbouring cell or stays when no movement is possible.
    Position ChooseWanderingPosition(Animal animal, IReadOnlyList<Position> positions);
}
