namespace Savanna.Simulation.Api.Models;

/// Summary:
/// Carries a consistent snapshot of a game and its available controls.
public sealed record GameResponse(
    Guid Id, int Width, int Height, long Round, bool IsRunning,
    int RoundDelayMilliseconds, AnimalResponse[] Animals,
    SpeciesResponse[] Species, string[] Warnings);
