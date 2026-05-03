using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems.ai;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.systems.progression;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.systems.status_effects;
using Godot;

namespace AshesOfVelsingrad.systems;

/// <summary>
///     Represents the different unit archetypes in the game.
/// </summary>
public enum UnitType
{
    /// <summary>Default player-controlled unit.</summary>
    Player,

    /// <summary>Close-range melee fighter unit.</summary>
    Fighter,

    /// <summary>Heavy melee swordsman unit.</summary>
    Swordsman,

    /// <summary>High-mobility stealth unit.</summary>
    Assassin,

    /// <summary>Ranged physical attacker.</summary>
    Archer,

    /// <summary>Magic-based ranged attacker.</summary>
    Mage
}

/// <summary>
///     Base class for all units in the tactical grid-based system.
///     Handles stats, movement, effects, and Godot integration.
/// </summary>
/// <remarks>
///     <para>
///         This class provides fundamental behaviour for all combat units, including:
///         <list type="bullet">
///             <item>Base stats (HP, attack, defense, speed, intelligence, mana).</item>
///             <item>Turn logic (<see cref="HasPlayed" />, <see cref="WaitForActionAsync" />).</item>
///             <item>Movement logic (BFS pathfinding in 3D).</item>
///             <item>Faction membership for turn ownership and target validation.</item>
///             <item>Integration with <see cref="MapSystem" /> and <see cref="StatusEffect" />.</item>
///             <item>Bridges to <see cref="CharacterProfile" /> for level / XP / equipped skills.</item>
///         </list>
///     </para>
///     <para>
///         All HP / mana / stat changes go through public methods (<see cref="Heal" />,
///         <see cref="SpendMana" />, <see cref="AdjustDefense" />, ...) so the
///         <see cref="BattleEventBus" /> sees consistent events. Direct field access is
///         intentionally not exposed.
///     </para>
/// </remarks>
public abstract partial class UnitSystem : CharacterBody3D, IEffectTarget
{
    #region Private fields

    private readonly EffectTarget _effectTarget = new();
    private TaskCompletionSource? _actionTcs;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    #endregion

    #region Godot signals

    /// <summary>Emitted when the unit's portrait texture changes.</summary>
    /// <param name="texture">The new portrait texture.</param>
    [Signal]
    public delegate void PortraitChangedEventHandler(Texture2D texture);

    /// <summary>Emitted when the unit's health changes.</summary>
    /// <param name="currentHp">The unit's current HP value.</param>
    /// <param name="maxHp">The unit's maximum HP value.</param>
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHp, float maxHp);

    /// <summary>Emitted when the unit's mana changes.</summary>
    /// <param name="currentMana">Current mana value.</param>
    /// <param name="maxMana">Maximum mana value.</param>
    [Signal]
    public delegate void ManaChangedEventHandler(float currentMana, float maxMana);

    /// <summary>Emitted when the unit dies.</summary>
    [Signal]
    public delegate void DiedEventHandler();

    #endregion

    #region Visual properties

    /// <summary>The portrait texture displayed for this unit in the UI.</summary>
    public Texture2D? PortraitTexture { get; protected set; }

    /// <summary>The 3D Sprite of the unit displayed in the UI.</summary>
    public Sprite3D? CharacterSprite { get; protected set; }

    #endregion

    #region Stat properties

    /// <summary>The name of the unit.</summary>
    public string UnitName { get; protected set; } = string.Empty;

    /// <summary>Descriptive text about the unit.</summary>
    public string Description { get; protected set; } = string.Empty;

    /// <summary>The current health points of the unit.</summary>
    public float Hp { get; protected set; }

    /// <summary>The maximum health points of the unit.</summary>
    public float MaxHp { get; protected set; }

    /// <summary>The base physical attack value of the unit.</summary>
    public float BaseAtk { get; protected set; }

    /// <summary>The base physical defense value of the unit.</summary>
    public float BaseDef { get; protected set; }

    /// <summary>The unit's base speed (used for turn order or initiative).</summary>
    public float BaseSpeed { get; protected set; }

    /// <summary>The unit's intelligence stat, affecting magical power or effects.</summary>
    public float Intelligence { get; protected set; }

    /// <summary>The unit's available mana points for casting skills.</summary>
    public float ManaPoint { get; protected set; }

    /// <summary>The unit's maximum mana points.</summary>
    public float MaxMana { get; protected set; }

    /// <summary>The type or archetype of the unit.</summary>
    public UnitType Type { get; protected set; }

    /// <summary>Indicates whether the unit is alive.</summary>
    public bool IsAlive { get; protected set; } = true;

    /// <summary>Indicates whether the unit has already acted this turn.</summary>
    public bool HasPlayed { get; protected set; }

    /// <summary>The maximum number of tiles the unit can move per turn.</summary>
    public int PossibleMovesRange { get; protected set; }

    /// <summary>List of active (usable) skills available to this unit.</summary>
    public List<SkillSystem> ActiveSkills { get; protected set; } = [];

    /// <summary>List of passive (always-on) skills applied to this unit.</summary>
    public List<SkillSystem> PassiveSkills { get; protected set; } = [];

    /// <summary>The unit's curse value (used for status mechanics or debuffs).</summary>
    public float Curse { get; protected set; }

    #endregion

    #region Combat-layer properties

    /// <summary>The faction this unit belongs to. Defaults to <see cref="Faction.Player" />.</summary>
    /// <remarks>
    ///     Use <see cref="AssignFaction" /> to mutate. Direct setter is private to ensure
    ///     <see cref="BattleEvents.FactionChanged" /> is always raised on change.
    /// </remarks>
    public Faction Faction { get; private set; } = Faction.Player;

    /// <summary>
    ///     Optional persistent profile (level, XP, equipped skills, karma, corruption).
    ///     Player-faction units typically have a profile; transient enemies usually don't.
    /// </summary>
    public CharacterProfile? Profile { get; set; }

    /// <summary>
    ///     Optional per-unit AI override (boss patterns, scripted minibosses, etc.).
    ///     If null, <see cref="AiRegistry.ResolveFor" /> picks the default for the faction.
    /// </summary>
    public IUnitAi? OverrideAi { get; set; }

    #endregion

    #region Class Initialization

    /// <inheritdoc />
    public override void _Ready()
    {
        Initialize();
    }

    /// <summary>
    ///     Initialize the unit instance.
    /// </summary>
    /// <remarks>
    ///     Called once from <see cref="_Ready" />. Override to set stats, portrait, and
    ///     skills. Always call <c>base.Initialize()</c> for sprite discovery.
    /// </remarks>
    protected virtual void Initialize()
    {
        foreach (Node child in GetChildren())
            if (child is Sprite3D sprite)
            {
                CharacterSprite = sprite;
                break;
            }

        // Default mana initialization if subclass forgot to set MaxMana.
        if (MaxMana <= 0 && ManaPoint > 0)
            MaxMana = ManaPoint;
    }

    #endregion

    #region Faction / identity

    /// <summary>
    ///     Set the unit's faction. Publishes <see cref="BattleEvents.FactionChanged" />
    ///     when the value actually changes.
    /// </summary>
    /// <param name="faction">New faction.</param>
    public void AssignFaction(Faction faction)
    {
        if (faction == Faction)
            return;
        Faction old = Faction;
        Faction = faction;
        BattleEventBus.Instance?.Publish(new BattleEvents.FactionChanged(this, old, faction, -1));
    }

    /// <summary>
    ///     Replace the unit's display name (used by <see cref="BattleLauncher" /> when
    ///     <c>UnitSpawn.DisplayNameOverride</c> is set).
    /// </summary>
    /// <param name="newName">New name.</param>
    public void OverrideDisplayName(string newName)
    {
        UnitName = newName;
    }

    #endregion

    #region Turn mechanics

    /// <summary>
    ///     Lock the system and wait for the player to choose an action.
    /// </summary>
    /// <returns>A task that completes when the player finishes their action.</returns>
    public Task WaitForActionAsync()
    {
        _actionTcs = new TaskCompletionSource();
        return _actionTcs.Task;
    }

    /// <summary>
    ///     Mark the start of a new turn for this unit, resetting <see cref="HasPlayed" /> and
    ///     ticking down skill cooldowns by one.
    /// </summary>
    /// <remarks>
    ///     Called by <see cref="TurnManager" /> at the start of every turn.
    /// </remarks>
    public virtual void BeginTurn()
    {
        HasPlayed = false;
        foreach (SkillSystem skill in ActiveSkills)
            skill.ReduceCooldown();
    }

    /// <summary>
    ///     Marks the unit as having completed its turn.
    /// </summary>
    public void PassTurn()
    {
        HasPlayed = true;
        ReportSystemUnitHasPlayed();
    }

    /// <summary>
    ///     Called by every action method to unlock the turn loop.
    /// </summary>
    protected void ReportSystemUnitHasPlayed()
    {
        GD.Print($"{Name} has played");
        _actionTcs?.TrySetResult();
        _actionTcs = null;
    }

    #endregion

    #region Combat actions

    /// <summary>
    ///     Performs an attack on the specified targets using the given skill.
    /// </summary>
    /// <param name="targets">List of target units to attack.</param>
    /// <param name="map">Reference to the map system for positional logic.</param>
    /// <param name="skill">The active skill to use.</param>
    /// <remarks>
    ///     Validates that <paramref name="skill" /> is in <see cref="ActiveSkills" />, that
    ///     mana cost can be paid, and that the cooldown is over. The skill itself is
    ///     responsible for spending mana and starting cooldown via <see cref="DataDrivenSkill" />.
    /// </remarks>
    public virtual void Play(List<UnitSystem> targets, MapSystem? map, SkillSystem skill)
    {
        if (!ActiveSkills.Contains(skill))
        {
            GD.PrintErr($"The unit does not have the skill '{skill.Name}'");
            return;
        }

        if (skill.Cooldown > 0)
        {
            GD.PrintErr($"Skill '{skill.Name}' is on cooldown ({skill.Cooldown} turns left).");
            return;
        }

        if (skill.ManaCost > ManaPoint)
        {
            GD.PrintErr($"Not enough mana to cast '{skill.Name}'.");
            return;
        }

        using SkillExecutionScope.Frame _ = new(this);
        skill.Use(targets, map);
        ReportSystemUnitHasPlayed();
    }

    /// <summary>
    ///     Use an item from the shared <see cref="PartyInventory" />.
    /// </summary>
    /// <param name="itemId">Item id.</param>
    /// <param name="targets">Target list.</param>
    public virtual void UseItem(string itemId, IReadOnlyList<UnitSystem> targets)
    {
        if (PartyInventory.Instance is null)
        {
            GD.PrintErr("UnitSystem.UseItem: PartyInventory not initialised.");
            return;
        }
        if (!PartyInventory.Instance.Use(this, itemId, targets))
            return;
        ReportSystemUnitHasPlayed();
    }

    /// <summary>
    ///     Apply incoming damage and update HP. Publishes
    ///     <see cref="BattleEvents.HpChanged" /> and, on death,
    ///     <see cref="BattleEvents.UnitDied" />.
    /// </summary>
    /// <param name="damage">The amount of damage received.</param>
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        float realDamage = damage - BaseDef;
        if (realDamage < 0)
            realDamage = 0;

        float oldHp = Hp;
        Hp = Mathf.Max(0, Hp - realDamage);
        EmitSignal(SignalName.HealthChanged, Hp, MaxHp);
        BattleEventBus.Instance?.Publish(new BattleEvents.HpChanged(this, Hp - oldHp, Hp, MaxHp));

        if (Hp <= 0)
            Die();
    }

    /// <summary>
    ///     Heal the unit. Publishes <see cref="BattleEvents.HpChanged" /> on success.
    /// </summary>
    /// <param name="amount">HP to restore (must be &gt; 0).</param>
    /// <remarks>
    ///     If the unit has a <see cref="status_effects.effects.BleedEffect" />, healing
    ///     is suppressed (per the feature doc). Corruption level 1 reduces the healing
    ///     by 10% (see <see cref="corruption.CorruptionLevel1Effect.HealingMultiplier" />).
    /// </remarks>
    public virtual void Heal(float amount)
    {
        if (!IsAlive || amount <= 0) return;

        if (HasEffect<status_effects.effects.BleedEffect>())
            return;

        if (HasEffect<corruption.CorruptionLevel1Effect>())
            amount *= corruption.CorruptionLevel1Effect.HealingMultiplier;

        float oldHp = Hp;
        Hp = Mathf.Min(MaxHp, Hp + amount);
        EmitSignal(SignalName.HealthChanged, Hp, MaxHp);
        BattleEventBus.Instance?.Publish(new BattleEvents.HpChanged(this, Hp - oldHp, Hp, MaxHp));
    }

    /// <summary>
    ///     Revive a dead unit with a fixed HP value.
    /// </summary>
    /// <param name="hp">HP to restore (clamped to MaxHp).</param>
    public virtual void Revive(float hp)
    {
        if (IsAlive) return;
        IsAlive = true;
        float clamped = Mathf.Clamp(hp, 1, MaxHp);
        Hp = clamped;
        EmitSignal(SignalName.HealthChanged, Hp, MaxHp);
        BattleEventBus.Instance?.Publish(new BattleEvents.HpChanged(this, clamped, Hp, MaxHp));
    }

    /// <summary>
    ///     Mark the unit as dead. Publishes <see cref="BattleEvents.UnitDied" />.
    /// </summary>
    /// <param name="killer">The unit that dealt the killing blow, if known.</param>
    protected virtual void Die(UnitSystem? killer = null)
    {
        IsAlive = false;
        EmitSignal(SignalName.Died);
        BattleEventBus.Instance?.Publish(new BattleEvents.UnitDied(this));
    }

    #endregion

    #region Mana management

    /// <summary>
    ///     Spend mana. Publishes <see cref="BattleEvents.ManaChanged" />.
    /// </summary>
    /// <param name="amount">Amount to spend (clamped to 0).</param>
    public virtual void SpendMana(float amount)
    {
        if (amount <= 0) return;
        float old = ManaPoint;
        ManaPoint = Mathf.Max(0, ManaPoint - amount);
        EmitSignal(SignalName.ManaChanged, ManaPoint, MaxMana);
        BattleEventBus.Instance?.Publish(new BattleEvents.ManaChanged(this, ManaPoint - old, ManaPoint));
    }

    /// <summary>
    ///     Restore mana. Publishes <see cref="BattleEvents.ManaChanged" />.
    /// </summary>
    /// <param name="amount">Amount to restore.</param>
    public virtual void RestoreMana(float amount)
    {
        if (amount <= 0) return;
        float old = ManaPoint;
        ManaPoint = Mathf.Min(MaxMana, ManaPoint + amount);
        EmitSignal(SignalName.ManaChanged, ManaPoint, MaxMana);
        BattleEventBus.Instance?.Publish(new BattleEvents.ManaChanged(this, ManaPoint - old, ManaPoint));
    }

    #endregion

    #region Stat adjustments

    /// <summary>
    ///     Apply a permanent (until cleared) modifier to attack.
    /// </summary>
    /// <param name="delta">Signed delta.</param>
    public void AdjustAttack(float delta) => BaseAtk = Mathf.Max(0, BaseAtk + delta);

    /// <summary>Apply a permanent modifier to defense.</summary>
    /// <param name="delta">Signed delta.</param>
    public void AdjustDefense(float delta) => BaseDef = Mathf.Max(0, BaseDef + delta);

    /// <summary>Apply a permanent modifier to speed.</summary>
    /// <param name="delta">Signed delta.</param>
    public void AdjustSpeed(float delta) => BaseSpeed = Mathf.Max(0, BaseSpeed + delta);

    /// <summary>Apply a permanent modifier to intelligence.</summary>
    /// <param name="delta">Signed delta.</param>
    public void AdjustIntelligence(float delta) => Intelligence = Mathf.Max(0, Intelligence + delta);

    #endregion

    #region Movement

    /// <summary>Handles the physics of the unit in 3D space.</summary>
    /// <param name="delta">Frame delta time in seconds.</param>
    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= _gravity * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
    }

    /// <summary>
    ///     Move the unit to the specified grid coordinates in the <see cref="MapSystem" />.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate (height).</param>
    /// <param name="z">The Z grid coordinate.</param>
    /// <param name="map">The map on which the unit exists.</param>
    public virtual void SetGridPosition(int x, int y, int z, MapSystem map)
    {
        try
        {
            (int, int, int)? from = map.GetUnitPosition(this);
            map.MoveUnit(this, x, y, z);
            BattleEventBus.Instance?.Publish(
                new BattleEvents.LogMessage($"{UnitName} moves.", LogSeverity.Info)
            );
            _ = from; // (kept for future event payload extension)
        }
        catch (ArgumentOutOfRangeException e)
        {
            GD.Print(e.Message);
        }
    }

    /// <summary>Check whether the unit can move to a given position.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate (height).</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="map">The map to check against.</param>
    /// <returns><c>true</c> if the position is walkable.</returns>
    public virtual bool CanMoveTo(int x, int y, int z, MapSystem map)
    {
        return map.IsWalkable(x, y, z);
    }

    /// <summary>
    ///     Move the unit to the specified coordinates if possible.
    /// </summary>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate (height).</param>
    /// <param name="z">Target Z coordinate.</param>
    /// <param name="map">The map to interact with.</param>
    /// <returns><c>true</c> if the move was successful.</returns>
    /// <remarks>
    ///     Per <c>Feature Document § 2.1.1</c>, movement does <b>not</b> end the turn,
    ///     so this method does not call <see cref="ReportSystemUnitHasPlayed" />. The
    ///     player must still pick a main action (skill, item, basic attack, or pass).
    /// </remarks>
    public virtual bool MoveTo(int x, int y, int z, MapSystem map)
    {
        if (!CanMoveTo(x, y, z, map))
            return false;
        SetGridPosition(x, y, z, map);
        return true;
    }

    /// <summary>
    ///     Calculate all reachable tiles for this unit.
    /// </summary>
    /// <param name="map">The map to evaluate movement on.</param>
    /// <returns>A list of reachable coordinates (x, y, z).</returns>
    /// <remarks>
    ///     Uses a BFS that respects walkability and vertical traversal.
    /// </remarks>
    public virtual List<(int, int, int)> GetPossibleMoves(MapSystem map)
    {
        List<(int, int, int)> possibleMoves = [];
        Queue<((int, int, int) pos, int dist)> toExplore = new();
        (int, int, int)? unitPosition = map.GetUnitPosition(this);
        List<(int, int, int)> visitedCells = [];
        (int, int, int)[] directions =
        [
            (-1, 0, 0), // Left
            (1, 0, 0),  // Right
            (0, 0, 1),  // Forward
            (0, 0, -1)  // Backward
        ];

        if (unitPosition == null)
            return possibleMoves;

        visitedCells.Add(unitPosition.Value);
        toExplore.Enqueue((unitPosition.Value, 0));
        while (toExplore.Count > 0)
        {
            ((int, int, int), int) currentPos = toExplore.Dequeue();
            if (currentPos.Item2 > PossibleMovesRange)
                continue;
            if (currentPos.Item1 != unitPosition.Value && !visitedCells.Contains(currentPos.Item1))
            {
                possibleMoves.Add(currentPos.Item1);
                visitedCells.Add(currentPos.Item1);
            }

            foreach ((int, int, int) dir in directions)
            {
                (int, int, int) pos = currentPos.Item1;
                pos.Item1 += dir.Item1;
                pos.Item3 += dir.Item3;
                try
                {
                    if (map.IsWalkable(pos.Item1, pos.Item2, pos.Item3))
                        toExplore.Enqueue((pos, currentPos.Item2 + 1));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // outside map bounds → skip
                }
            }
        }

        return possibleMoves;
    }

    #endregion

    #region IEffectTarget

    /// <inheritdoc />
    public void ApplyEffect(StatusEffect statusEffect)
    {
        _effectTarget.ApplyEffect(statusEffect);
        BattleEventBus.Instance?.Publish(
            new BattleEvents.StatusEffectApplied(this, statusEffect.Name, statusEffect.Duration)
        );
    }

    /// <inheritdoc />
    public void RemoveEffect(StatusEffect statusEffect)
    {
        _effectTarget.RemoveEffect(statusEffect);
        BattleEventBus.Instance?.Publish(
            new BattleEvents.StatusEffectRemoved(this, statusEffect.Name)
        );
    }

    /// <inheritdoc />
    public bool HasEffect<T>() where T : StatusEffect => _effectTarget.HasEffect<T>();

    /// <inheritdoc />
    public List<StatusEffect> GetActiveEffects() => _effectTarget.GetActiveEffects();

    #endregion

    #region Class Destroyer

    /// <inheritdoc />
    public override void _ExitTree()
    {
        Cleanup();
    }

    /// <summary>
    ///     Cleanup hook called from <see cref="_ExitTree" />.
    /// </summary>
    /// <remarks>Override in subclasses to release resources / disconnect signals.</remarks>
    protected virtual void Cleanup()
    {
    }

    #endregion
}
