using Savanna.Core.Configuration;

namespace Savanna.Simulation.Api.Service;

public sealed class SimulationWorker : BackgroundService
{
    private readonly GameManager _games;
    private readonly SimulationOptions _options;
    private readonly ILogger<SimulationWorker> _logger;

    /// Summary:
    /// Connects the background clock to the game registry.
    public SimulationWorker(GameManager games, SimulationOptions options, ILogger<SimulationWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _games = games;
        _options = options;
        _logger = logger;
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
            // StopAsync records normal shutdown, even when cancellation happens before the first tick.
        }
    }

    /// Summary:
    /// Waits for the game clock to stop and records normal application shutdown.
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Simulation worker stopped because application shutdown was requested.");
    }
}
