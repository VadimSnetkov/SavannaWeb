using Savanna.Simulation.Api.Service;

namespace Savanna.Simulation.Api;

public partial class Program
{
    /// Summary:
    /// Starts the simulation API and its background game clock.
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddSavannaSimulation(builder.Configuration);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapControllers();
        app.Run();
    }
}
