using AshesOfVelsingrad.Systems;
using System.Collections.Generic;
using System.Reflection;
using AshesOfVelsingrad.Utilities;
using Godot;

namespace UnitTests;

public partial class TestConcreteSkillSystem : SkillSystem
{
	public bool WasUsed { get; private set; }
	public List<UnitSystem> LastTargets { get; private set; } = new();
	public MapSystem? LastMap { get; private set; }

	// Add parameterless constructor for basic usage
	public TestConcreteSkillSystem()
	{
		Name = "TestSkill";
		Description = "Test skill description";
		EffectType = AovDataStructures.EffectType.Damage;
		TargetType = AovDataStructures.TargetTypes.SingleEnemy;
		Range = 1;
		ManaCost = 10;
		Cooldown = 0;
	}

	// Add constructor with parameters for customization
	public TestConcreteSkillSystem(
		string name = "TestSkill",
        string description = "Test skill description",
        float manaCost = 10,
        int cooldown = 0,
        int range = 1,
        AovDataStructures.MagicType magic = AovDataStructures.MagicType.None,
        AovDataStructures.EffectType effect = AovDataStructures.EffectType.Damage,
        AovDataStructures.TargetTypes target = AovDataStructures.TargetTypes.SingleEnemy
    )
    {
        Name = name;
        Description = description;
		EffectType = effect;
		TargetType = target;
		Range = range;
		ManaCost = manaCost;
        TotalCooldown = cooldown;
        Cooldown = 0;
        MagicType = magic;

        AreaEffect = new List<Vector3I>();
	}

	// Add public setters for properties that need to be modified in tests
	public new string Name
	{
		get => base.Name;
		set
		{
			var field = typeof(SkillSystem).GetProperty("Name");
			field?.SetValue(this, value);
		}
	}

	public new AovDataStructures.EffectType EffectType
	{
		get => base.EffectType;
		set
		{
			var field = typeof(SkillSystem).GetProperty("EffectType");
			field?.SetValue(this, value);
		}
	}

	public new AovDataStructures.TargetTypes TargetType
	{
		get => base.TargetType;
		set
		{
			var field = typeof(SkillSystem).GetProperty("TargetType");
			field?.SetValue(this, value);
		}
	}

	public new int Range
	{
		get => base.Range;
		set
		{
			var field = typeof(SkillSystem).GetProperty("Range");
			field?.SetValue(this, value);
		}
	}

	public new float ManaCost
	{
		get => base.ManaCost;
		set
		{
			var field = typeof(SkillSystem).GetProperty("ManaCost");
			field?.SetValue(this, value);
		}
	}

	public new int Cooldown
	{
		get => base.Cooldown;
		set
		{
			var field = typeof(SkillSystem).GetProperty("Cooldown");
			field?.SetValue(this, value);
		}
	}

    public override void Use(UnitSystem caster, List<UnitSystem> targets, MapSystem? map)
    {
        WasUsed = true;
        LastTargets = targets;
        LastMap = map;
        GD.Print($"[TEST] Skill {Name} used");

        // Call base implementation if needed (currently abstract, so no logic to execute)
    }
}