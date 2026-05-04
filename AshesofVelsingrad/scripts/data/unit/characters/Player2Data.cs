using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Data.Skills;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
///     Player2 — second player-controlled unit, Swordsman loadout.
/// </summary>
/// <remarks>
///     Used to verify multi-character control. Both Player1 and Player2 are part of the
///     player party and act on Player turns. The Swordsman skill catalogue (Phase B)
///     drives the active slots; passives are wired but inert until the damage formula
///     reads them.
/// </remarks>
public sealed partial class Player2Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Meowth";
        Description = "Test player unit — Swordsman loadout";
        MaxHp = 850;
        Hp = MaxHp;
        BaseAtk = 220;
        BaseDef = 5;
        BaseSpeed = 240;
        Intelligence = 100;
        ManaMax = 150;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 3;
        Curse = 0;

        ActiveSkills.Add(new FrappeEclair());
        ActiveSkills.Add(new DanseDesLames());
        ActiveSkills.Add(new CoupDeBravoure());
        ActiveSkills.Add(new FrappeFantome());
        ActiveSkills.Add(new LameDExecution());

        PassiveSkills.Add(new Riposte());
        PassiveSkills.Add(new DanseDuLame());
        PassiveSkills.Add(new LameSpectrale());

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = "Meowth",
            ClassName = "Épéiste",
            Level = 1,
            PortraitPath = "res://assets/portraits/meowth.png",
        });
    }
}
