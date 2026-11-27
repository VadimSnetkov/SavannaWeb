using Savanna.Simulation.Api.Extensions;
using Serilog;

namespace Savanna.Simulation.Api;

public partial class Program
{
    /// Summary:
    /// Starts the simulation API and its background game clock.
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSavannaApi(builder.Configuration);
        builder.Services.AddSavannaSimulation(builder.Configuration);

        var app = builder.Build();
        app.UseSerilogRequestLogging();
        app.UseSavannaExceptionHandling();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.MapControllers();
        app.Run();
    }
}
