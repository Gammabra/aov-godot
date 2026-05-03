using AshesOfVelsingrad.systems;

namespace AshesOfVelsingrad;

/// <summary>
///     Test ally unit. Shares the player's stat profile but is AI-controlled and lives in
///     the <see cref="Faction.Ally" /> faction so the turn manager treats it as a guest.
/// </summary>
/// <remarks>
///     Drop a <c>CharacterBody3D</c> in your battle scene under a sibling node named
///     <c>AlliedUnits</c> (next to <c>PlayerUnits</c> and <c>EnemyUnits</c>), then attach this
///     script. Then on the <c>GameManager</c> node set <c>AlliedUnitsPath</c> to point at the
///     <c>AlliedUnits</c> container. The unit's faction is auto-tagged by
///     <c>GameManager.LoadUnits</c>.
/// </remarks>
public sealed partial class Ally1Data : UnitSystem
{
    /// <inheritdoc />
    protected override void Initialize()
    {
        base.Initialize();
        UnitName = "Ally1";
        Description = "Test ally — recruited mercenary, AI-controlled.";
        MaxHp = 1500;
        Hp = MaxHp;
        BaseAtk = 150;
        BaseDef = 150;
        BaseSpeed = 150;
        Intelligence = 150;
        ManaPoint = 150;
        MaxMana = 150;
        IsAlive = true;
        HasPlayed = false;
        PossibleMovesRange = 2;
        Curse = 0;
        // ActiveSkills is left empty here on purpose — GameManager.AutoEquipDefaultSkills
        // populates it from the seeded database. To author a bespoke kit, fill it explicitly:
        //   ActiveSkills.Add(DataDrivenSkill.From(SkillRegistry.Instance!.GetDefinition("rayon_sacre")!)!);
    }
}
