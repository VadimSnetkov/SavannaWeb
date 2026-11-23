namespace Savanna.Simulation.Api.Models;

/// Summary:
/// Describes one animal without exposing mutable simulation objects.
public sealed record AnimalResponse(
    Guid Id, string Species, int Row, int Column, decimal Health,
    long Age, int OffspringCount, int Speed, int VisionRange);
