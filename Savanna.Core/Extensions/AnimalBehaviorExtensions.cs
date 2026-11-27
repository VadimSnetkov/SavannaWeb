using Savanna.Core.Behavior;

namespace Savanna.Core.Extensions;

public static class AnimalBehaviorExtensions
{
    private const string BehaviorSuffix = "Behavior";

    /// Summary:
    /// Derives a species name by removing the trailing Behavior suffix from its class name.
    public static string GetSpeciesName(this IAnimalBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        string name = behavior.GetType().Name;
        return name.EndsWith(BehaviorSuffix, StringComparison.Ordinal) && name.Length > BehaviorSuffix.Length
            ? name[..^BehaviorSuffix.Length]
            : name;
    }
}
