using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using Godot;
// Disambiguation alias — root AshesOfVelsingrad namespace has legacy `CrushingStrike` /
// `WarCry` / `CircularStrike` from `FighterData.cs` that would shadow the catalog version.
// Using the alias lets us pick the catalog versions explicitly.
using Catalog = AshesOfVelsingrad.Data.Skills;

namespace AshesOfVelsingrad;

/// <summary>
///     Kaelen Voss — level 1 fighter, named protagonist of the prison-break encounter.
/// </summary>
/// <remarks>
///     Player-controlled by default (faction set to <see cref="Faction.Player" /> by
///     <c>GameManager.LoadUnits</c> based on which container the unit lives in).
///     Active loadout uses the Fighter catalogue: <c>CrushingStrike</c>, <c>WarCry</c>,
///     <c>Charge</c>, <c>Block</c>, <c>CircularStrike</c>. Passives are wired into
///     <c>PassiveSkills</c> for future damage-formula hooks.
/// </remarks>
public sealed partial class KaelenVoss : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Kaelen Voss";
        Description = "A disgraced soldier turned reluctant hero. Solid blade, thicker armor.";
        MaxHp = 1100;
        Hp = MaxHp;
        BaseAtk = 190;
        BaseDef = 40;
        BaseSpeed = 180;
        Intelligence = 80;
        ManaMax = 180;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;

        ActiveSkills.Add(new Catalog.CrushingStrike());
        ActiveSkills.Add(new Catalog.WarCry());
        ActiveSkills.Add(new Catalog.Charge());
        ActiveSkills.Add(new Catalog.Block());
        ActiveSkills.Add(new Catalog.CircularStrike());

        PassiveSkills.Add(new Catalog.BruteForce());
        PassiveSkills.Add(new Catalog.Recklessness());
        PassiveSkills.Add(new Catalog.WarriorEndurance());

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = "Kaelen Voss",
            ClassName = "Fighter",
            Level = 1,
            PortraitPath = "res://assets/portraits/Pikachu.png",
            Bio = "Once a soldier of Velsingrad, now a fugitive. Carries the weight of a verdict he doesn't believe.",
        });
    }
}
