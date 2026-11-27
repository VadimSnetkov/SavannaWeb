using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Savanna.Core.Configuration;
using Savanna.Simulation.Api.Configuration;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Service;
using Savanna.Tests.Helpers;
using Serilog.Events;

namespace Savanna.Tests;

public sealed class ApiTests
{
    /// Summary:
    /// Verifies missing games are ordinary 404 results and not logged as exceptions.
    [Fact]
    public async Task MissingGame_ReturnsNotFoundWithoutErrorLog()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.DoesNotContain(factory.Logs.Events, entry => entry.Level == LogEventLevel.Error);
    }

    /// Summary:
    /// Verifies well-formed but invalid species produce the mentor's requested 422 response.
    [Theory]
    [InlineData("{\"species\":\"Unknown\"}")]
    [InlineData("{\"species\":\"\"}")]
    [InlineData("{\"species\":null}")]
    [InlineData("{}")]
    public async Task InvalidSpecies_Returns422WithValidationErrors(string json)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var game = await CreateGame(client);
        var response = await client.PostAsync($"/api/games/{game.Id}/animals",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").GetProperty("Species").GetArrayLength() > 0);
        Assert.Empty((await client.GetFromJsonAsync<GameResponse>($"/api/games/{game.Id}"))!.Animals);
    }

    /// Summary:
    /// Keeps malformed JSON as a binding error, distinct from semantic validation.
    [Fact]
    public async Task MalformedJson_Returns400()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var game = await CreateGame(client);
        var response = await client.PostAsync($"/api/games/{game.Id}/animals",
            new StringContent("{broken", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// Summary:
    /// Verifies full fields, running steps and session limits return conflicts without exceptions.
    [Fact]
    public async Task ExpectedConflicts_Return409WithoutErrorLogs()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<SimulationOptions>();
        options.Width = 1;
        options.Height = 1;
        factory.Services.GetRequiredService<GameSessionOptions>().MaximumGames = 1;
        var game = await CreateGame(client);
        (await client.PostAsJsonAsync($"/api/games/{game.Id}/animals", new { species = "Antelope" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/games/{game.Id}/animals", new { species = "Lion" })).StatusCode);
        await client.PostAsync($"/api/games/{game.Id}/play", null);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/games/{game.Id}/step", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/games", null)).StatusCode);
        Assert.DoesNotContain(factory.Logs.Events, entry => entry.Level == LogEventLevel.Error);
    }

    /// Summary:
    /// Ensures an InvalidOperationException from a plugin becomes a logged safe 500, never a 409.
    [Fact]
    public async Task PluginFailure_ReachesMiddlewareAndLogsOriginalExceptionOnce()
    {
        using var factory = new ApiFactory(failBehavior: true);
        using var client = factory.CreateClient();
        var game = await CreateGame(client);
        await client.PostAsJsonAsync($"/api/games/{game.Id}/animals", new { species = "Antelope" });
        var response = await client.PostAsync($"/api/games/{game.Id}/step", null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Private plugin", body);
        Assert.DoesNotContain("stackTrace", body);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        var loggedException = Assert.Single(factory.Logs.Events, entry => entry.Exception is not null);
        Assert.Equal("Private plugin failure details.", loggedException.Exception!.GetBaseException().Message);
        Assert.Contains("ExceptionHandlingMiddleware", loggedException.Properties["SourceContext"].ToString());
        var state = await client.GetFromJsonAsync<GameResponse>($"/api/games/{game.Id}");
        Assert.False(state!.IsRunning);
        Assert.Single(state.Warnings);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/games/{game.Id}/play", null)).StatusCode);
    }

    /// Summary:
    /// Verifies a failed background game is logged and does not stop another running game.
    [Fact]
    public async Task BackgroundFailure_StopsOnlyFailedGame()
    {
        using var factory = new ApiFactory(failBehavior: true);
        using var client = factory.CreateClient();
        var failed = await CreateGame(client);
        var healthy = await CreateGame(client);
        await client.PostAsJsonAsync($"/api/games/{failed.Id}/animals", new { species = "Antelope" });
        await client.PostAsJsonAsync($"/api/games/{healthy.Id}/animals", new { species = "Zebra" });
        await client.PostAsync($"/api/games/{failed.Id}/play", null);
        await client.PostAsync($"/api/games/{healthy.Id}/play", null);
        factory.Services.GetRequiredService<GameManager>().Tick();
        var failedState = await client.GetFromJsonAsync<GameResponse>($"/api/games/{failed.Id}");
        var healthyState = await client.GetFromJsonAsync<GameResponse>($"/api/games/{healthy.Id}");
        Assert.False(failedState!.IsRunning);
        Assert.True(healthyState!.IsRunning);
        Assert.Equal(1, healthyState.Round);
        Assert.Single(factory.Logs.Events, entry => entry.Exception is not null);
    }

    /// Summary:
    /// Checks that normal background-worker shutdown emits an information event.
    [Fact]
    public async Task WorkerShutdown_IsLoggedAtInformation()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var worker = new SimulationWorker(
            factory.Services.GetRequiredService<GameManager>(),
            factory.Services.GetRequiredService<SimulationOptions>(),
            factory.Services.GetRequiredService<ILogger<SimulationWorker>>());
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        Assert.Contains(factory.Logs.Events, entry => entry.Level == LogEventLevel.Information
            && entry.RenderMessage().Contains("shutdown was requested"));
    }

    /// Summary:
    /// Verifies Swagger is accessible and documents validation and expected failure codes.
    [Fact]
    public async Task Swagger_DescribesGameEndpointsAndStatusCodes()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var responses = document.GetProperty("paths").GetProperty("/api/games/{id}/animals")
            .GetProperty("post").GetProperty("responses");
        Assert.All(new[] { "200", "400", "404", "409", "422", "500" }, code => Assert.True(responses.TryGetProperty(code, out _)));
    }

    /// Summary:
    /// Creates a game through the real HTTP endpoint for each isolated test host.
    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/api/games", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameResponse>())!;
    }
}
