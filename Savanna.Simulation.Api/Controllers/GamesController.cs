using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Savanna.Simulation.Api.Models;
using Savanna.Simulation.Api.Service;

namespace Savanna.Simulation.Api.Controllers;

[ApiController]
[Route("api/games")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<GameResponse> Create() => ToActionResult(_games.Create());

    /// Summary:
    /// Returns the latest state of an existing game.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<GameResponse> Get(Guid id) => ToActionResult(_games.Access(id));

    /// Summary:
    /// Validates a species and adds it to an empty cell.
    [HttpPost("{id:guid}/animals")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<GameResponse> Spawn(Guid id, SpawnAnimalRequest request)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            var problem = new ValidationProblemDetails(validation.ToDictionary())
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "One or more inputs are invalid."
            };
            return UnprocessableEntity(problem);
        }
        return ToActionResult(_games.Access(id, GameCommand.AddAnimal, request.Species));
    }

    /// Summary:
    /// Starts automatic rounds for this game.
    [HttpPost("{id:guid}/play")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<GameResponse> Play(Guid id) => ToActionResult(_games.Access(id, GameCommand.Play));

    /// Summary:
    /// Pauses automatic rounds for this game.
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<GameResponse> Pause(Guid id) => ToActionResult(_games.Access(id, GameCommand.Pause));

    /// Summary:
    /// Advances one round in a paused game.
    [HttpPost("{id:guid}/step")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<GameResponse> Step(Guid id) => ToActionResult(_games.Access(id, GameCommand.Step));

    /// Summary:
    /// Ends a game and discards its unsaved state.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete(Guid id)
    {
        _games.Delete(id);
        return NoContent();
    }

    /// Summary:
    /// Maps ordinary service outcomes to explicit HTTP results without catching exceptions.
    private ActionResult<GameResponse> ToActionResult(GameResult result)
    {
        return result.Status switch
        {
            GameResultStatus.Success => Ok(result.Game),
            GameResultStatus.NotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound, Title = "Game not found.", Detail = result.Message
            }),
            GameResultStatus.Conflict => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "Game command cannot be completed.", Detail = result.Message
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
    }
}
