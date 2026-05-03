using System.Collections.Generic;

namespace AshesOfVelsingrad.systems.skills;

/// <summary>
///     Context passed to a skill behaviour at execution time.
/// </summary>
/// <param name="Caster">The unit casting the skill.</param>
/// <param name="Targets">Resolved target list.</param>
/// <param name="Map">The active map.</param>
/// <param name="Definition">The skill's authored data.</param>
public readonly record struct SkillExecutionContext(
    UnitSystem Caster,
    IReadOnlyList<UnitSystem> Targets,
    MapSystem? Map,
    SkillDefinition Definition
);

/// <summary>
///     Strategy interface for runtime skill behaviour.
/// </summary>
/// <remarks>
///     A behaviour reads the <see cref="SkillDefinition" /> for stats and
///     applies the appropriate effect to the targets. Behaviours are
///     registered in <see cref="SkillRegistry" /> and looked up by
///     <see cref="SkillDefinition.BehaviourId" />, so a single behaviour
///     class typically powers many skills (e.g. one
///     <c>DamageBehaviour</c> serves Fireball, Boule de Feu, Lance de Roc, etc.).
/// </remarks>
public interface ISkillBehaviour
{
    /// <summary>
    ///     Apply the skill's effect.
    /// </summary>
    /// <param name="context">All inputs required to resolve the skill.</param>
    void Execute(in SkillExecutionContext context);
}
