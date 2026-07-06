using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using Godot;
using Catalog = AshesOfVelsingrad.Data.Skills;

namespace AshesOfVelsingrad;

/// <summary>
///     Generic configurable enemy soldier — every stat, name, and portrait can be
///     overridden from the Inspector so a designer can drop an instance into any
///     battle scene and shape it without touching the script.
/// </summary>
/// <remarks>
///     <para>
///         Default loadout is the Fighter catalogue's basic actives so each soldier
///         already feels like a competent threat in the test scene; if a designer wants
///         a softer or weirder enemy they can subclass and override <see cref="Initialize" />.
///     </para>
///     <para>
///         Stats use the same shape as <see cref="KaelenVoss" /> at level 1 but slightly
///         weaker so a single soldier is a fair fight for one player. Scale up for
///         later encounters by exporting <see cref="SoldierLevel" /> and a stat
///         multiplier later.
///     </para>
/// </remarks>
public sealed partial class ThiefSwordman : UnitSystem
{
    /// <summary>Display name shown in the HUD. Overridable per-instance.</summary>
    [Export]
    public string ThiefName { get; set; } = "Thief Swordman";

    /// <summary>Level shown next to the name in the turn-queue chip / status panel.</summary>
    [Export]
    public int ThiefLevel { get; set; } = 1;

    /// <summary>Portrait <c>res://</c> path. Falls back to a coloured square if empty.</summary>
    [Export(PropertyHint.File, "*.png,*.jpg,*.svg")]
    public string ThiefPortraitPath { get; set; } = "res://assets/Krita/icone_bandit.png";

    /// <summary>Max HP. Adjust per-instance for tougher / weaker variants.</summary>
    [Export]
    public float ThiefMaxHp { get; set; } = 800f;

    /// <summary>Base attack. Default sized to be a fair fight for a level-1 fighter.</summary>
    [Export]
    public float ThiefBaseAtk { get; set; } = 140f;

    /// <summary>Base defence.</summary>
    [Export]
    public float ThiefBaseDef { get; set; } = 25f;

    /// <summary>Base speed — drives turn order. Lower than Kaelen's 180 so the player acts first.</summary>
    [Export]
    public float ThiefBaseSpeed { get; set; } = 120f;

    /// <summary>Tactical-AI personality: Aggressive / Defensive / Opportunistic / Balanced.</summary>
    [Export]
    public AIPersonality ThiefPersonality { get; set; } = AIPersonality.Aggressive;

    /// <inheritdoc />
    protected override void Initialize()
    {
        UnitName = ThiefName;
        Description = "Velsingrad regular. Doesn't ask questions.";
        MaxHp = ThiefMaxHp;
        Hp = MaxHp;
        BaseAtk = ThiefBaseAtk;
        BaseDef = ThiefBaseDef;
        BaseSpeed = ThiefBaseSpeed;
        Intelligence = 40;
        ManaMax = 80;
        Mana = ManaMax;
        IsAlive = true;
        PossibleMovesRange = 2;
        Curse = 0;
        Personality = ThiefPersonality;
        Type = AovDataStructures.UnitType.Fighter;

        ActiveSkills.Add(new Catalog.CrushingStrike());
        ActiveSkills.Add(new Catalog.Charge());
        ActiveSkills.Add(new Catalog.Block());

        PassiveSkills.Add(new Catalog.BruteForce());

        base.Initialize();

        var statusEffectSystem = new StatusEffectSystem();
        InjectDependencies(statusEffectSystem);

        SetEntityProfile(new EntityProfile
        {
            DisplayName = ThiefName,
            ClassName = "ThiefSwordman",
            Level = ThiefLevel,
            PortraitPath = ThiefPortraitPath,
            Bio = "One of the many thieves that infest the streets of Velsingrad.",
        });
    }
}
