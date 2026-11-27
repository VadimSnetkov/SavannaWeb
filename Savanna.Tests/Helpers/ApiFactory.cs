using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Savanna.Core.Behavior;
using Savanna.Core.Models;
using Savanna.Simulation.Api;
using Savanna.Simulation.Api.Service;
using Serilog.Core;

namespace Savanna.Tests.Helpers;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _failBehavior;
    public TestLogSink Logs { get; } = new();

    /// Summary:
    /// Optionally installs a failing behavior to test the real exception pipeline.
    public ApiFactory(bool failBehavior = false) => _failBehavior = failBehavior;

    /// Summary:
    /// Keeps tests deterministic while retaining production controllers, middleware and Serilog.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            var worker = services.Single(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(SimulationWorker));
            services.Remove(worker);
            services.AddSingleton<ILogEventSink>(Logs);
            if (_failBehavior)
            {
                var antelope = services.Single(descriptor => descriptor.ServiceType == typeof(IAnimalBehavior)
                    && descriptor.ImplementationType == typeof(AntelopeBehavior));
                services.Remove(antelope);
                services.AddScoped<IAnimalBehavior>(_ => new TestAnimalBehavior(AnimalType.Antelope)
                {
                    ChoosePosition = (_, _) => throw new InvalidOperationException("Private plugin failure details.")
                });
            }
        });
    }
}
