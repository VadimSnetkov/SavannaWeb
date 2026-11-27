namespace Savanna.Simulation.Api.Models;

/// Summary:
/// Describes an available species, including species loaded from plugins.
public sealed record SpeciesResponse(
    string Name, string Symbol, string SpawnKey, bool IsPredator, int Count);
