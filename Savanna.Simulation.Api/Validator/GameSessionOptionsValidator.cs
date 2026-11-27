using FluentValidation;
using Savanna.Simulation.Api.Configuration;

namespace Savanna.Simulation.Api.Validator;

public sealed class GameSessionOptionsValidator : AbstractValidator<GameSessionOptions>
{
    /// Summary:
    /// Requires positive session limits and an idle timeout.
    public GameSessionOptionsValidator()
    {
        RuleFor(options => options.IdleTimeoutMinutes).GreaterThan(0);
        RuleFor(options => options.MaximumGames).GreaterThan(0);
    }
}
