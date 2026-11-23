using FluentValidation;
using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Savanna.Simulation.Api.Configuration;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Validator;

namespace Savanna.Simulation.Api.Service;

public static class ServiceCollectionExtensions
{
    /// Summary:
    /// Registers isolated game engines and loads the configured animal plugins.
    public static IServiceCollection AddSavannaSimulation(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var simulation = configuration.GetRequiredSection(SimulationOptions.SectionName)
            .Get<SimulationOptions>() ?? throw new InvalidOperationException("Missing simulation settings.");
        var sessions = configuration.GetRequiredSection(GameSessionOptions.SectionName)
            .Get<GameSessionOptions>() ?? throw new InvalidOperationException("Missing session settings.");
        new SimulationOptionsValidator().ValidateAndThrow(simulation);
        new GameSessionOptionsValidator().ValidateAndThrow(sessions);

        var plugins = new PluginLoader().Load(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, simulation.PluginDirectory)));

        services.AddSingleton(simulation);
        services.AddSingleton(sessions);
        services.AddSingleton(plugins);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IValidator<SimulationOptions>, SimulationOptionsValidator>();
        services.AddScoped<Random>(_ => new Random());
        services.AddScoped<MovementService>();
        services.AddScoped<IAnimalBehavior, AntelopeBehavior>();
        services.AddScoped<IAnimalBehavior, LionBehavior>();
        foreach (var behavior in plugins.Behaviors)
        {
            var behaviorType = behavior.GetType();
            // A plugin instance belongs to one game, even if the plugin keeps state.
            services.AddScoped(typeof(IAnimalBehavior), behaviorType);
        }
        services.AddScoped<AnimalCatalog>();
        services.AddScoped<AnimalSpawnService>();
        services.AddScoped<HealthService>();
        services.AddScoped<BirthService>();
        services.AddScoped<SimulationService>();
        services.AddScoped<GameSessionService>();
        services.AddScoped<IValidator<SpawnAnimalRequest>, SpawnAnimalRequestValidator>();
        services.AddSingleton<GameManager>();
        services.AddHostedService<SimulationWorker>();
        return services;
    }
}
