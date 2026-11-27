using Savanna.Simulation.Api.Configuration;
using Savanna.Simulation.Api.Models;

namespace Savanna.Simulation.Api.Service;

public sealed class GameManager : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, IServiceScope> _games = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GameSessionOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<GameManager> _logger;

    /// Summary:
    /// Creates the in-memory game registry with configurable session limits.
    public GameManager(IServiceScopeFactory scopeFactory, GameSessionOptions options,
        TimeProvider clock, ILogger<GameManager> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    /// Summary:
    /// Creates a paused game with its own dependency scope and engine state.
    public GameResult Create()
    {
        lock (_gate)
        {
            RemoveExpired();
            if (_games.Count >= _options.MaximumGames)
                return new GameResult(GameResultStatus.Conflict, Message: "Too many active games. End an existing game or try again later.");
            var scope = _scopeFactory.CreateScope();
            try
            {
                var game = scope.ServiceProvider.GetRequiredService<GameSessionService>();
                game.LastAccess = _clock.GetUtcNow();
                var response = game.Snapshot();
                _games.Add(game.Id, scope);
                return new GameResult(GameResultStatus.Success, response);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }
    }

    /// Summary:
    /// Serializes reads, commands and ticking to keep every snapshot consistent.
    public GameResult Access(Guid id, GameCommand command = GameCommand.Read, string? species = null)
    {
        lock (_gate)
        {
            RemoveExpired();
            if (!_games.TryGetValue(id, out var scope))
                return new GameResult(GameResultStatus.NotFound, Message: "This game has ended or expired. Start a new game.");
            var game = scope.ServiceProvider.GetRequiredService<GameSessionService>();
            game.LastAccess = _clock.GetUtcNow();
            try
            {
                string? conflict = game.Execute(command, species);
                return conflict is null
                    ? new GameResult(GameResultStatus.Success, game.Snapshot())
                    : new GameResult(GameResultStatus.Conflict, Message: conflict);
            }
            catch
            {
                // Stop a partially completed turn, then let HTTP middleware log the cause.
                game.StopAfterError();
                throw;
            }
        }
    }

    /// Summary:
    /// Ends a game and releases its engine and plugin instances.
    public void Delete(Guid id)
    {
        lock (_gate)
        {
            if (_games.Remove(id, out var scope)) scope.Dispose();
        }
    }

    /// Summary:
    /// Advances all active games and removes abandoned sessions.
    public void Tick()
    {
        lock (_gate)
        {
            RemoveExpired();
            foreach (var scope in _games.Values)
            {
                var game = scope.ServiceProvider.GetRequiredService<GameSessionService>();
                try { game.Tick(); }
                catch (Exception exception)
                {
                    game.StopAfterError();
                    _logger.LogError(exception, "Game {GameId} stopped during a round.", game.Id);
                }
            }
        }
    }

    /// Summary:
    /// Removes games that have not received browser requests within the timeout.
    private void RemoveExpired()
    {
        var cutoff = _clock.GetUtcNow().AddMinutes(-_options.IdleTimeoutMinutes);
        var expired = _games.Where(entry => entry.Value.ServiceProvider
            .GetRequiredService<GameSessionService>().LastAccess <= cutoff)
            .Select(entry => entry.Key).ToArray();
        foreach (var id in expired) Delete(id);
    }

    /// Summary:
    /// Releases all remaining game scopes when the API stops.
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var scope in _games.Values) scope.Dispose();
            _games.Clear();
        }
    }
}
