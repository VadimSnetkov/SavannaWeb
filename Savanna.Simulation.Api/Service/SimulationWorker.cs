using Savanna.Core.Configuration;

namespace Savanna.Simulation.Api.Service;

public sealed class SimulationWorker : BackgroundService
{
    private readonly GameManager _games;
    private readonly SimulationOptions _options;

    /// Summary:
    /// Connects the background clock to the game registry.
    public SimulationWorker(GameManager games, SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(options);
        _games = games;
        _options = options;
    }

    /// Summary:
    /// Advances games at the configured interval until the application stops.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(_options.RoundDelayMilliseconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken)) _games.Tick();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}
