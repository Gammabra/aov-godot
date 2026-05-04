using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Data.Skills;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad;

/// <summary>
///     Player1 — the test scene's player-controlled fighter.
/// </summary>
/// <remarks>
///     Active skill slots map to the Fighter feature-doc catalogue
///     (<see cref="FrappeEcrasante" />, <see cref="CriDeGuerre" />, <see cref="Charge" />,
///     <see cref="Blocage" />, <see cref="FrappeCirculaire" />). Passives are wired into
///     <c>PassiveSkills</c> for the relevant systems to read; their <c>Use</c> methods are
///     intentional no-ops since they're handled in damage-formula / status-effect code paths.
/// </remarks>
public sealed partial class Player1Data : UnitSystem
{
    protected override void Initialize()
    {
        UnitName = "Player1";
        Description = "Test player unit — Fighter loadout";
        MaxHp = 1000;
        Hp = MaxHp;
        BaseAtk = 200;
        BaseDef = 0;
        BaseSpeed = 200;
        Intelligence = 200;
        ManaMax = 200;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;

        // Five active slots (matches BattleInputSystem's 1-5 hot-keys and the
        // SkillSelector's 5-button bar).
        ActiveSkills.Add(new FrappeEcrasante());
        ActiveSkills.Add(new CriDeGuerre());
        ActiveSkills.Add(new Charge());
        ActiveSkills.Add(new Blocage());
        ActiveSkills.Add(new FrappeCirculaire());

        // Passives — present so other systems (damage formula, end-of-turn hooks)
        // can detect them. Their Use() is a no-op.
        PassiveSkills.Add(new ForceBrute());
        PassiveSkills.Add(new Temerite());
        PassiveSkills.Add(new EnduranceGuerriere());

        base.Initialize();

        // Status-effect system needed for the Stun / AtkBuffer effects the actives apply.
        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = "Pikachu",
            ClassName = "Combattant",
            Level = 1,
            Portrait = ResourceLoader.Load<Texture2D>("res://assets/portraits/Pikachu.png"),
        });
    }

    // No Play() override — we want the base UnitSystem.Play implementation, which calls
    // skill.Use(this, targets, map) (applies damage / effects), decrements mana, and
    // reports the turn played. The previous override only called ReportSystemUnitHasPlayed
    // and silently skipped skill.Use, which is why enemies took no damage.
}
