namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Prevents the affected unit from acting on its turn.
/// </summary>
/// <remarks>
///     Non-stackable, purifiable. <c>UnitSystem</c> queries this effect at the
///     start of every turn; if present, the turn is auto-passed.
/// </remarks>
public sealed class StunEffect : StatusEffect
{
    /// <inheritdoc />
    public override bool IsStackable => false;

    /// <inheritdoc />
    public override bool IsPurifiable => true;

    /// <summary>
    ///     Build a new <see cref="StunEffect" />.
    /// </summary>
    /// <param name="duration">Number of turns the effect lasts.</param>
    public StunEffect(int duration = 1)
    {
        Name = "Stun";
        Description = "Cannot act on its turn.";
        Duration = duration;
    }
}
