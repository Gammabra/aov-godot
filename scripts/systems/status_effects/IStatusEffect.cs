namespace AshesOfVelsingrad.Systems;

/// <summary>
/// Empty interface to be able to contain any status effect as the actual <see cref="StatusEffect{TTarget}"/> abstract class cannot take object as the TTarget
/// </summary>
public interface IStatusEffect
{
    // TODO: Find a better use of this interface instead of just being an empty interface that can take anything
}
