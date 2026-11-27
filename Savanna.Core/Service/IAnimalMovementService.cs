using Savanna.Core.Models;

namespace Savanna.Core.Service;

public interface IAnimalMovementService
{
    /// Summary:
    /// Performs one animal's movement, eating and movement health costs.
    void MoveAnimal(SavannaState state, Animal animal);
}
