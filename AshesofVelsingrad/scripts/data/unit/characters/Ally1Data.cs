using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Data.Skills;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
///     Ally1 — friendly AI-controlled guest unit, Light-magic supporter.
/// </summary>
/// <remarks>
///     <see cref="UnitSystem.Faction" /> is set to <see cref="Systems.Faction.Ally" /> by
///     <c>GameManager.LoadUnits</c> based on which container this node is found under
///     (<c>AlliedUnits</c>). The unit is AI-driven — attach an <see cref="EnemyAIBehavior" />
///     child node in the scene to give it tactical decisions.
/// </remarks>
public sealed partial class Ally1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Poussacha";
        Description = "Friendly support — heals allies, damages enemies with Light magic";
        MaxHp = 600;
        Hp = MaxHp;
        BaseAtk = 80;
        BaseDef = 0;
        BaseSpeed = 140;
        Intelligence = 280;
        ManaMax = 250;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
        Personality = AIPersonality.Defensive;

        ActiveSkills.Add(new SacredRay());
        ActiveSkills.Add(new PurifyingFlash());
        ActiveSkills.Add(new DivineJudgment());
        ActiveSkills.Add(new Resurrection());
        ActiveSkills.Add(new DivinePrayer());

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = "Poussacha",
            ClassName = "Mage Lumière",
            Level = 1,
            PortraitPath = "res://assets/portraits/poussacha.png",
        });
    }
}
