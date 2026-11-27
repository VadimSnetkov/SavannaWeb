namespace Savanna.Simulation.Api.Configuration;

public sealed class GameSessionOptions
{
    public const string SectionName = "Sessions";
    public int IdleTimeoutMinutes { get; set; } = 30;
    public int MaximumGames { get; set; } = 20;
}
