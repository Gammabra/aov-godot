using System.Collections.Generic;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.systems.skills;

/// <summary>
///     Concrete <see cref="SkillSystem" /> implementation that defers all logic
///     to a <see cref="SkillDefinition" /> + <see cref="ISkillBehaviour" /> pair.
/// </summary>
/// <remarks>
///     <para>
///         This is the bridge between the existing inheritance-based
///         <see cref="SkillSystem" /> contract (which <see cref="UnitSystem" />
///         consumes via <c>UnitSystem.ActiveSkills</c>) and the new data-driven
///         architecture. New skills should never subclass <see cref="SkillSystem" />
///         directly — instead, create a <see cref="SkillDefinition" /> and use
///         <see cref="From" /> to wrap it.
///     </para>
///     <para>
///         The wrapper also handles common cross-cutting concerns: mana cost
///         deduction, cooldown start, missed-cast logging, and the
///         <see cref="BattleEvents.SkillUsed" /> event publication. Behaviours
///         only have to worry about applying the actual effect.
///     </para>
/// </remarks>
public sealed class DataDrivenSkill : SkillSystem
{
    private readonly SkillDefinition _definition;
    private readonly ISkillBehaviour _behaviour;

    /// <summary>Authored data backing this skill.</summary>
    public SkillDefinition Definition => _definition;

    /// <summary>The skill id (alias for <see cref="SkillDefinition.SkillId" />).</summary>
    public string SkillId => _definition.SkillId;

    /// <summary>
    ///     Build a <see cref="DataDrivenSkill" /> from a definition.
    /// </summary>
    /// <param name="definition">Authored data.</param>
    /// <param name="behaviour">The runtime behaviour to execute on cast.</param>
    private DataDrivenSkill(SkillDefinition definition, ISkillBehaviour behaviour)
    {
        _definition = definition;
        _behaviour = behaviour;

        // Mirror the data into the SkillSystem fields so existing code that
        // reads them (UnitSystem.Play, HUD tooltips, ...) keeps working.
        Name = definition.DisplayName;
        Description = definition.Description;
        ManaCost = definition.ManaCost;
        TotalCooldown = definition.TotalCooldown;
        Range = definition.Range;
        MagicType = definition.MagicType;
        EffectType = definition.EffectType;
        TargetType = definition.TargetType;

        AreaEffect = [];
        foreach (Vector3I cell in definition.AreaOfEffect)
            AreaEffect.Add((cell.X, cell.Y, cell.Z));
    }

    /// <summary>
    ///     Factory: resolve the behaviour from <see cref="SkillRegistry" /> and
    ///     return a runnable skill.
    /// </summary>
    /// <param name="definition">Authored data.</param>
    /// <returns>The wrapped skill, or null if the behaviour can't be resolved.</returns>
    public static DataDrivenSkill? From(SkillDefinition definition)
    {
        ISkillBehaviour? behaviour = SkillRegistry.Instance?.GetBehaviour(definition.BehaviourId);
        if (behaviour is null)
        {
            GD.PrintErr(
                $"DataDrivenSkill.From: cannot build '{definition.SkillId}' — " +
                $"behaviour '{definition.BehaviourId}' not registered."
            );
            return null;
        }

        return new DataDrivenSkill(definition, behaviour);
    }

    /// <inheritdoc />
    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        // Validate target list. Behaviours can assume non-null targets.
        targets ??= [];

        // Project the interface lists/refs onto the concrete UnitSystem / MapSystem
        // types that SkillExecutionContext (and the behaviours) work with. Targets
        // that aren't UnitSystem instances are silently skipped — they have no
        // meaningful in-battle representation for behaviours to act on.
        List<UnitSystem> concreteTargets = new(targets.Count);
        foreach (IUnitSystem t in targets)
        {
            if (t is UnitSystem cu)
                concreteTargets.Add(cu);
        }

        UnitSystem? concreteCaster = caster as UnitSystem
                                     ?? SkillExecutionScope.CurrentCaster
                                     ?? (concreteTargets.Count > 0 ? concreteTargets[0] : null);
        if (concreteCaster is null)
            return;

        MapSystem? concreteMap = map as MapSystem;

        SkillExecutionContext ctx = new(
            concreteCaster,
            concreteTargets,
            concreteMap,
            _definition
        );

        _behaviour.Execute(ctx);

        // Mana + cooldown bookkeeping.
        ctx.Caster.SpendMana(_definition.ManaCost);
        StartCooldown();

        // Tell the world a skill was used.
        BattleEventBus.Instance?.Publish(
            new BattleEvents.SkillUsed(ctx.Caster, _definition.SkillId, concreteTargets)
        );
    }

    /// <summary>
    ///     Begin the skill's cooldown. Called automatically after a successful cast.
    /// </summary>
    public void StartCooldown()
    {
        Cooldown = TotalCooldown;
    }
}

/// <summary>
///     Thread-static helper used by <see cref="UnitSystem" /> to pass the caster
///     into <see cref="SkillSystem.Use" /> without breaking the existing signature.
/// </summary>
/// <remarks>
///     A scope is opened around the call to <c>UnitSystem.PlaySkill</c> so the
///     <see cref="DataDrivenSkill" /> wrapper can read the caster reliably.
///     This avoids a breaking change to the public SkillSystem API.
/// </remarks>
internal static class SkillExecutionScope
{
    [System.ThreadStatic] private static UnitSystem? _currentCaster;

    /// <summary>The caster currently executing a skill on this thread, or null.</summary>
    public static UnitSystem? CurrentCaster => _currentCaster;

    /// <summary>RAII helper to scope the caster.</summary>
    public readonly struct Frame : System.IDisposable
    {
        private readonly UnitSystem? _previous;

        /// <summary>Open the scope.</summary>
        /// <param name="caster">Caster to expose.</param>
        public Frame(UnitSystem caster)
        {
            _previous = _currentCaster;
            _currentCaster = caster;
        }

        /// <summary>Close the scope, restoring the previous caster.</summary>
        public void Dispose()
        {
            _currentCaster = _previous;
        }
    }
}
