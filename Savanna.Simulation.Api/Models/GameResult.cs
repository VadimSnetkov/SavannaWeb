namespace Savanna.Simulation.Api.Models;

/// Summary:
/// Represents an expected game outcome without throwing an exception.
public sealed record GameResult(GameResultStatus Status, GameResponse? Game = null, string? Message = null);
