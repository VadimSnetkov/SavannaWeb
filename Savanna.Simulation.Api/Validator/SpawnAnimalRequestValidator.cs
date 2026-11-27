using FluentValidation;
using Savanna.Core.Service;
using Savanna.Simulation.Api.Models;

namespace Savanna.Simulation.Api.Validator;

public sealed class SpawnAnimalRequestValidator : AbstractValidator<SpawnAnimalRequest>
{
    /// Summary:
    /// Rejects empty or unavailable species before the game is changed.
    public SpawnAnimalRequestValidator(AnimalCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        RuleFor(request => request.Species)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(catalog.Animals.ContainsKey)
            .WithMessage("Choose one of the available animal species.");
    }
}
