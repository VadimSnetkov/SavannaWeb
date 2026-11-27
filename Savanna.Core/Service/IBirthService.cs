using Savanna.Core.Models;

namespace Savanna.Core.Service;

public interface IBirthService
{
    /// Summary:
    /// Checks surviving animal pairs for births after a round.
    void ProcessRound(SavannaState state);
}
