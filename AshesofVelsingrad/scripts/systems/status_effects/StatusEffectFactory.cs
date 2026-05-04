using System;
using System.Collections.Generic;
using AshesOfVelsingrad.systems.status_effects.effects;
using Godot;

namespace AshesOfVelsingrad.systems.status_effects;

/// <summary>
///     Creates <see cref="StatusEffect" /> instances from string identifiers.
/// </summary>
/// <remarks>
///     <para>
///         Skill and item definitions reference status effects by string id (e.g. "poison",
///         "burn", "stun") so designers don't need to know the C# class name. The factory
///         is the single place that maps id → constructor.
///     </para>
///     <para>
///         To add a new status effect: write the <see cref="StatusEffect" /> subclass, then
///         register a constructor delegate here under a stable id. Skills can immediately
///         reference it via their <c>StatusEffectIdOnHit</c> property.
///     </para>
/// </remarks>
public static class StatusEffectFactory
{
    /// <summary>Map of id → factory delegate. Modify only via <see cref="Register" />.</summary>
    private static readonly Dictionary<string, Func<int, StatusEffect>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["poison"] = duration => new PoisonEffect(duration),
        ["burn"] = duration => new BurnEffect(duration),
        ["bleed"] = duration => new BleedEffect(duration),
        ["curse"] = _ => new CurseEffect(),
        ["confusion"] = duration => new ConfusionEffect(duration),
        ["stun"] = duration => new StunEffect(duration),
    };

    /// <summary>
    ///     Register a factory for a custom status effect.
    /// </summary>
    /// <param name="effectId">Stable string id (case-insensitive).</param>
    /// <param name="constructor">A delegate that takes a duration and produces an effect instance.</param>
    public static void Register(string effectId, Func<int, StatusEffect> constructor)
    {
        if (string.IsNullOrEmpty(effectId))
        {
            GD.PrintErr("StatusEffectFactory.Register: empty effectId.");
            return;
        }
        Factories[effectId] = constructor;
    }

    /// <summary>
    ///     Build a fresh status effect.
    /// </summary>
    /// <param name="effectId">The id (e.g. "poison").</param>
    /// <param name="duration">Duration in turns. -1 = permanent.</param>
    /// <returns>A new effect, or null if the id isn't registered.</returns>
    public static StatusEffect? Create(string effectId, int duration)
    {
        if (string.IsNullOrEmpty(effectId))
            return null;

        if (Factories.TryGetValue(effectId, out Func<int, StatusEffect>? factory))
            return factory(duration);

        GD.PrintErr($"StatusEffectFactory: unknown effectId '{effectId}'.");
        return null;
    }
}
