namespace Savanna.Core.Exceptions;

public sealed class AnimalBehaviorException : Exception
{
    public string Species { get; }

    /// Summary:
    /// Preserves a failed plugin's cause so the application boundary can log it.
    public AnimalBehaviorException(string species, Exception innerException)
        : base($"The {species} behavior failed.", innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(species);
        ArgumentNullException.ThrowIfNull(innerException);
        Species = species;
    }
}
