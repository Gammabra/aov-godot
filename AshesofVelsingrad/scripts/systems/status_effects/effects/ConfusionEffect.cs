namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Causes the affected unit to attack a random unit (ally or enemy) on its turn.
/// </summary>
/// <remarks>
///     <para>
///         The actual targeting bypass is implemented by the AI controllers and the
///         player input layer: when a unit has this effect, both <c>BasicEnemyAi</c>
///         and <c>BattleHud</c> consult <see cref="UnitSystem.HasEffect{T}" /> and
///         pick a random valid target instead of the chosen one.
///     </para>
///     <para>
///         Non-stackable, purifiable. Per the doc, "Confusion: attacks at random
///         (enemy or ally) — non stackable, purifiable".
///     </para>
/// </remarks>
public sealed class ConfusionEffect : StatusEffect
{
    /// <inheritdoc />
    public override bool IsStackable => false;

    /// <inheritdoc />
    public override bool IsPurifiable => true;

    /// <summary>
    ///     Build a new <see cref="ConfusionEffect" /> lasting <paramref name="duration" /> turns.
    /// </summary>
    /// <param name="duration">Number of turns the effect lasts.</param>
    public ConfusionEffect(int duration = 2)
    {
        Name = "Confusion";
        Description = "Attacks a random target on its turn. Cannot be controlled normally.";
        Duration = duration;
    }
}
