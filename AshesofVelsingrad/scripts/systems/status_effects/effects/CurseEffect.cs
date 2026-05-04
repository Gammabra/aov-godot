using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.systems.battle;
using Godot;

namespace AshesOfVelsingrad.systems.status_effects.effects;

/// <summary>
///     Permanently reduces one of the target's stats by a random amount.
/// </summary>
/// <remarks>
///     Per <c>Feature Document § 5</c>: curse is non-stackable and <b>NOT purifiable</b> —
///     once applied it persists until the unit dies or a special "Lift Curse" mechanic is used
///     (no such mechanic is currently implemented; the doc treats curses as permanent).
/// </remarks>
public sealed class CurseEffect : StatusEffect
{
    /// <summary>The stat that was randomly chosen to reduce.</summary>
    public CursedStat Stat { get; private set; }

    /// <summary>Magnitude of the reduction, decided at apply time.</summary>
    public float Reduction { get; private set; }

    /// <inheritdoc />
    public override bool IsStackable => false;

    /// <inheritdoc />
    public override bool IsPurifiable => false;

    /// <summary>
    ///     Build a new curse. The specific stat and magnitude are rolled in <see cref="OnApply" />.
    /// </summary>
    public CurseEffect()
    {
        Name = "Curse";
        Description = "Permanently weakens one stat. Cannot be cleansed.";
        Duration = -1; // permanent
    }

    /// <inheritdoc />
    public override void OnApply(IEffectTarget target)
    {
        if (target is not UnitSystem unit) return;

        // Pick a random stat to weaken.
        Stat = (CursedStat)GD.RandRange(0, 2);
        Reduction = (float)GD.RandRange(3.0, 8.0);

        switch (Stat)
        {
            case CursedStat.Attack:
                unit.AdjustAttack(-Reduction);
                break;
            case CursedStat.Defense:
                unit.AdjustDefense(-Reduction);
                break;
            case CursedStat.Speed:
                unit.AdjustSpeed(-Reduction);
                break;
        }

        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"{unit.UnitName} is cursed: {Stat} -{Reduction:F0}.", LogSeverity.Critical
        ));
    }
}

/// <summary>
///     The stat a curse can target.
/// </summary>
public enum CursedStat
{
    /// <summary>Reduces physical attack.</summary>
    Attack,

    /// <summary>Reduces physical defense.</summary>
    Defense,

    /// <summary>Reduces base speed (initiative).</summary>
    Speed
}
