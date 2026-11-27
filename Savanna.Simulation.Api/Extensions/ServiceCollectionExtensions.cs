using FluentValidation;
using Savanna.Simulation.Api.Service;
using Serilog;
using Savanna.Core.Behavior;
using Savanna.Core.Configuration;
using Savanna.Core.Service;
using Savanna.Core.Validator;
using Savanna.Simulation.Api.Configuration;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Validator;

namespace Savanna.Simulation.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// Summary:
    /// Registers API controllers, documentation, exception responses and configured Serilog logging.
    public static IServiceCollection AddSavannaApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        // Well-formed bodies reach FluentValidation even when a required string is null.
        // JSON syntax and binding errors still receive the framework's 400 response.
        services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSerilog((provider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(provider));
        return services;
    }

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
        services.AddScoped<IMovementService, MovementService>();
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
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IBirthService, BirthService>();
        services.AddScoped<IAnimalMovementService, AnimalMovementService>();
        services.AddScoped<SimulationService>();
        services.AddScoped<GameSessionService>();
        services.AddScoped<IValidator<SpawnAnimalRequest>, SpawnAnimalRequestValidator>();
        services.AddSingleton<GameManager>();
        services.AddHostedService<SimulationWorker>();
        return services;
    }
}
