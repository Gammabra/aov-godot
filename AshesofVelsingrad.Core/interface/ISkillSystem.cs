using System.Collections.Generic;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public interface ISkillSystem
{
    string Name { get; }
    string Description { get; }
    float ManaCost { get; }
    int TotalCooldown { get; }
    int Cooldown { get; }
    int Range { get; }
    List<(int, int, int)> AreaEffect { get; }
    AovDataStructures.MagicType MagicType { get; }
    AovDataStructures.EffectType EffectType { get; }
    AovDataStructures.TargetTypes TargetType { get; }

    void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map);
    void SetCooldown();
    void ReduceCooldown();
}
