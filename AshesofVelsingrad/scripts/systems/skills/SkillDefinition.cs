using AshesOfVelsingrad.Utilities;
using Godot;
using Godot.Collections;
using static AshesOfVelsingrad.Utilities.AovDataStructures;

namespace AshesOfVelsingrad.systems.skills;

/// <summary>
///     The stat used to scale a skill's base power.
///     The runtime behaviour reads <see cref="UnitSystem.BaseAtk" /> or
///     <see cref="UnitSystem.Intelligence" /> accordingly.
/// </summary>
public enum ScalingStat
{
    /// <summary>No stat-based scaling — use raw <see cref="SkillDefinition.BasePower" />.</summary>
    None,

    /// <summary>Scales with the caster's physical attack.</summary>
    Attack,

    /// <summary>Scales with the caster's intelligence (magic power).</summary>
    Intelligence
}

/// <summary>
///     Authored data for a skill (active or passive).
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="SkillDefinition" /> is the "what" of a skill: name, costs,
///         range, target type, scaling, status side-effects. The "how" lives in
///         an <see cref="ISkillBehaviour" /> referenced by <see cref="BehaviourId" />.
///         This split lets designers create new skills as <c>.tres</c> files without
///         writing code, while letting programmers add new behaviours without
///         touching the data.
///     </para>
///     <para>
///         All numerical fields are placeholders calibrated to feel reasonable.
///         See <c>CHANGELOG.md → Balance Notes</c> for the full balance table.
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class SkillDefinition : Resource
{
    /// <summary>Stable identifier (e.g. "fireball", "shadow_blade"). Used by the registry.</summary>
    [Export] public string SkillId { get; set; } = string.Empty;

    /// <summary>Display name shown to the player.</summary>
    [Export] public string DisplayName { get; set; } = string.Empty;

    /// <summary>Long-form description shown in tooltips.</summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Mana spent on cast.</summary>
    [Export] public float ManaCost { get; set; }

    /// <summary>Cooldown between casts, in turns.</summary>
    [Export] public int TotalCooldown { get; set; }

    /// <summary>Maximum cast range in grid tiles.</summary>
    [Export] public int Range { get; set; } = 1;

    /// <summary>Magic element (Fire, Water, ..., or None).</summary>
    [Export] public MagicType MagicType { get; set; } = MagicType.None;

    /// <summary>Effect category. Determines which behaviours are appropriate.</summary>
    [Export] public EffectType EffectType { get; set; } = EffectType.Damage;

    /// <summary>Targeting pattern.</summary>
    [Export] public TargetTypes TargetType { get; set; } = TargetTypes.SingleEnemy;

    /// <summary>
    ///     The behaviour identifier (e.g. "damage", "heal", "corrupted_conversion").
    ///     Must match an entry registered in <see cref="SkillRegistry" />.
    /// </summary>
    [Export] public string BehaviourId { get; set; } = "damage";

    /// <summary>Base raw power before stat scaling (damage, heal amount, etc.).</summary>
    [Export] public float BasePower { get; set; }

    /// <summary>How <see cref="BasePower" /> scales with caster stats.</summary>
    [Export] public ScalingStat Scaling { get; set; } = ScalingStat.Intelligence;

    /// <summary>Multiplier applied to the scaling stat (e.g. 1.0 = full stat, 0.5 = half).</summary>
    [Export] public float ScalingFactor { get; set; } = 1.0f;

    /// <summary>If non-empty, applies this status effect to each affected target.</summary>
    [Export] public string StatusEffectIdOnHit { get; set; } = string.Empty;

    /// <summary>Probability in [0, 1] of applying the status effect.</summary>
    [Export] public float StatusEffectChance { get; set; } = 1.0f;

    /// <summary>Duration of the applied status effect, in turns. -1 = permanent.</summary>
    [Export] public int StatusEffectDuration { get; set; } = 3;

    /// <summary>
    ///     <c>true</c> if this skill is a Darkness / cursed skill that can trigger
    ///     a corruption backlash on the caster.
    /// </summary>
    [Export] public bool IsCorruptionSource { get; set; }

    /// <summary>
    ///     Base probability in <c>[0, 1]</c> that casting this skill produces
    ///     a corruption backlash, before karma modulation.
    ///     Ignored when <see cref="IsCorruptionSource" /> is false.
    /// </summary>
    [Export] public float BaseCorruptionChance { get; set; } = 0.25f;

    /// <summary>
    ///     <c>true</c> for passive skills that should never be put into a loadout slot.
    /// </summary>
    [Export] public bool IsPassive { get; set; }

    /// <summary>
    ///     Cells affected relative to the target position
    ///     (use [(0,0,0)] for single-target, [(0,0,0),(1,0,0),(-1,0,0),(0,0,1),(0,0,-1)] for cross, ...).
    /// </summary>
    [Export]
    public Array<Vector3I> AreaOfEffect { get; set; } = [Vector3I.Zero];
}
