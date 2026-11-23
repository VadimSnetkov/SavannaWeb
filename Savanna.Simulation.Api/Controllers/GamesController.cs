using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Service;

namespace Savanna.Simulation.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly GameManager _games;
    private readonly IValidator<SpawnAnimalRequest> _validator;

    /// Summary:
    /// Connects HTTP commands to the simulation services and request validator.
    public GamesController(GameManager games, IValidator<SpawnAnimalRequest> validator)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(validator);
        _games = games;
        _validator = validator;
    }

    /// Summary:
    /// Creates a new paused game with an empty field.
    [HttpPost]
    public ActionResult<GameResponse> Create() => Respond(_games.Create);

    /// Summary:
    /// Returns the latest state of an existing game.
    [HttpGet("{id:guid}")]
    public ActionResult<GameResponse> Get(Guid id) => Respond(() => _games.Access(id));

    /// Summary:
    /// Validates a species and adds it to an empty cell.
    [HttpPost("{id:guid}/animals")]
    public ActionResult<GameResponse> Spawn(Guid id, SpawnAnimalRequest request)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }
        return Respond(() => _games.Access(id, game => game.Spawn(request.Species)));
    }

    /// Summary:
    /// Starts automatic rounds for this game.
    [HttpPost("{id:guid}/play")]
    public ActionResult<GameResponse> Play(Guid id) => Respond(() => _games.Access(id, game => game.Play()));

    /// Summary:
    /// Pauses automatic rounds for this game.
    [HttpPost("{id:guid}/pause")]
    public ActionResult<GameResponse> Pause(Guid id) => Respond(() => _games.Access(id, game => game.Pause()));

    /// Summary:
    /// Advances one round in a paused game.
    [HttpPost("{id:guid}/step")]
    public ActionResult<GameResponse> Step(Guid id) => Respond(() => _games.Access(id, game => game.Step()));

    /// Summary:
    /// Ends a game and discards its unsaved state.
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        _games.Delete(id);
        return NoContent();
    }

    /// Summary:
    /// Translates expected game errors into clear HTTP problem responses.
    private ActionResult<GameResponse> Respond(Func<GameResponse> action)
    {
        try { return Ok(action()); }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: exception.Message);
        }
    }
}
