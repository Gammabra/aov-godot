using System.Collections.Generic;
using System.Linq;
using LegacyEffectTarget = AshesOfVelsingrad.systems.status_effects.IEffectTarget;
using LegacyStatusEffect = AshesOfVelsingrad.systems.status_effects.StatusEffect;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     Compatibility partial that bridges <see cref="UnitSystem" /> to the
///     non-generic <see cref="LegacyStatusEffect" /> hierarchy in
///     <c>AshesOfVelsingrad.systems.status_effects</c>.
/// </summary>
/// <remarks>
///     <para>
///         The codebase is mid-migration: concrete effects (Bleed, Burn, Poison, Curse,
///         Stun, Confusion) still inherit from the legacy non-generic <c>StatusEffect</c>,
///         while the new <c>IUnitSystem</c> contract is built on the generic
///         <c>StatusEffect&lt;IUnitSystem&gt;</c>. Skill / item behaviours that target
///         units want to call <c>target.ApplyEffect(legacyEffect)</c>; without this
///         partial that call would be a type error.
///     </para>
///     <para>
///         Lifecycle here is intentionally minimal. Legacy effects' <c>OnApply</c> /
///         <c>OnRemove</c> hooks are invoked, and stacks are honoured, but per-turn
///         ticking is not driven from this partial — it's expected to be handled by
///         the legacy <c>StatusEffectSystem</c> if it gets wired in. Once the legacy
///         hierarchy is folded into <c>StatusEffect&lt;IUnitSystem&gt;</c> this whole
///         file should be deleted.
///     </para>
/// </remarks>
public abstract partial class UnitSystem : LegacyEffectTarget
{
    /// <summary>Effects stored via the legacy non-generic API.</summary>
    private readonly List<LegacyStatusEffect> _legacyStatusEffects = [];

    /// <summary>
    ///     Apply a legacy non-generic status effect to this unit. Stacks if already present
    ///     and stackable; otherwise stores it and fires <c>OnApply</c>.
    /// </summary>
    /// <param name="statusEffect">Legacy effect instance.</param>
    public void ApplyEffect(LegacyStatusEffect statusEffect)
    {
        if (statusEffect is null) return;

        LegacyStatusEffect? existing = _legacyStatusEffects
            .FirstOrDefault(e => e.GetType() == statusEffect.GetType());

        if (existing is not null && existing.IsStackable)
        {
            existing.AddStack();
            return;
        }

        if (existing is not null)
            return;

        _legacyStatusEffects.Add(statusEffect);
        statusEffect.OnApply(this);
    }

    /// <summary>
    ///     Remove a legacy non-generic status effect from this unit (no-op if absent).
    /// </summary>
    /// <param name="statusEffect">The effect to remove.</param>
    public void RemoveEffect(LegacyStatusEffect statusEffect)
    {
        if (statusEffect is null) return;

        if (_legacyStatusEffects.Remove(statusEffect))
            statusEffect.OnRemove(this);
    }

    // Explicit-interface implementations: these collide by signature with the
    // Core-typed overloads on the unit, so they're only callable through the
    // legacy interface (cast `unit` to `LegacyEffectTarget` first).

    bool LegacyEffectTarget.HasEffect<T>() => _legacyStatusEffects.Any(e => e is T);

    List<LegacyStatusEffect> LegacyEffectTarget.GetActiveEffects() => [.._legacyStatusEffects];
}
