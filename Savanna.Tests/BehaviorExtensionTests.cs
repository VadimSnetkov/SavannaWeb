using Savanna.Core.Behavior;
using Savanna.Core.Extensions;
using Savanna.Core.Service;

namespace Savanna.Tests;

public sealed class BehaviorExtensionTests
{
    /// Summary:
    /// Verifies the extension derives names from runtime classes rather than their Type properties.
    [Fact]
    public void GetSpeciesName_RemovesOnlyTheTrailingBehaviorSuffix()
    {
        var movement = new MovementService(new Random(42));
        IAnimalBehavior[] behaviors = [new AntelopeBehavior(movement), new LionBehavior(movement)];
        Assert.Equal(new[] { "Antelope", "Lion" }, behaviors.Select(behavior => behavior.GetSpeciesName()));
    }

    /// Summary:
    /// Verifies the extension's receiver cannot be null.
    [Fact]
    public void GetSpeciesName_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AnimalBehaviorExtensions.GetSpeciesName(null!));
    }
}
