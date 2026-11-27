using Savanna.Core.Models;

namespace Savanna.Core.Service;

public sealed class AnimalMovementService : IAnimalMovementService
{
    private const int NeighbourStep = 1;
    private readonly AnimalCatalog _catalog;
    private readonly IHealthService _healthService;

    /// Summary:
    /// Creates movement operations using the behavior catalog and health contract.
    public AnimalMovementService(AnimalCatalog catalog, IHealthService healthService)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(healthService);
        _catalog = catalog;
        _healthService = healthService;
    }

    /// Summary:
    /// Moves an animal up to its speed, applying eating and health rules.
    public void MoveAnimal(SavannaState state, Animal animal)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animal);
        for (int step = 0; step < animal.Speed; step++)
        {
            var allowedPositions = GetAllowedPositions(state, animal);

            var nextPosition = _catalog.GetNextPosition(
                animal,
                state.Animals,
                allowedPositions);

            if (nextPosition == animal.Position)
            {
                break;
            }

            var prey = state.Animals.FirstOrDefault(other =>
                other != animal &&
                other.Position == nextPosition);

            if (prey is not null)
            {
                state.MutableAnimals.Remove(prey);
                _healthService.FeedPredator(animal);
            }

            animal.Position = nextPosition;
            _healthService.ApplyMovementCost(animal);

            if (_healthService.RemoveIfDead(state, animal) || prey is not null)
            {
                break;
            }
        }
    }

    /// Summary:
    /// Finds neighbouring cells the animal may enter, including its own.
    private static List<Position> GetAllowedPositions(SavannaState state, Animal animal)
    {
        var positions = new List<Position>();

        for (int rowOffset = -NeighbourStep;
             rowOffset <= NeighbourStep;
             rowOffset++)
        {
            for (int columnOffset = -NeighbourStep;
                 columnOffset <= NeighbourStep;
                 columnOffset++)
            {
                int row = animal.Position.Row + rowOffset;
                int column = animal.Position.Column + columnOffset;

                if (row < 0 || row >= state.Height ||
                    column < 0 || column >= state.Width)
                {
                    continue;
                }

                var position = new Position(row, column);

                var occupant = state.Animals.FirstOrDefault(other =>
                    other.Position == position);

                bool canEnter =
                    occupant is null ||
                    occupant == animal ||
                    (animal.IsPredator && !occupant.IsPredator);

                if (canEnter)
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }
}
