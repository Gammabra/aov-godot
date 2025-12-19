using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

/// <summary>
/// Concrete implementation of SkillSystem used only for unit testing.
/// </summary>
public class TestConcreteSkillSystem : SkillSystem
{
    public bool WasUsed { get; private set; }
    public List<UnitSystem> LastTargets { get; private set; } = [];
    public MapSystem? LastMap { get; private set; }

    public TestConcreteSkillSystem(
        string name = "TestSkill",
        string description = "Test skill description",
        float manaCost = 5,
        int cooldown = 0,
        int range = 1,
        MagicType magic = MagicType.None,
        EffectType effect = EffectType.Damage,
        TargetTypes target = TargetTypes.SingleEnemy
    )
    {
        Name = name;
        Description = description;
        ManaCost = manaCost;
        TotalCooldown = cooldown;
        Cooldown = 0;
        Range = range;
        MagicType = magic;
        EffectType = effect;
        TargetType = target;

        AreaEffect = new List<(int, int, int)>();

        GD.Print("[TEST] TestConcreteSkillSystem constructor called");
    }

    public override void Use(List<UnitSystem> targets, MapSystem? map)
    {
        WasUsed = true;

        LastTargets = new List<UnitSystem>(targets);
        LastMap = map;
        GD.Print($"[TEST] Skill {Name} used");
    }
}
