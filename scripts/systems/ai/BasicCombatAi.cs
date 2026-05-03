using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.corruption;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.systems.status_effects;
using AshesOfVelsingrad.systems.status_effects.effects;
using Godot;

namespace AshesOfVelsingrad.systems.ai;

/// <summary>
///     Reusable AI used for both <see cref="Faction.Ally" /> and <see cref="Faction.Enemy" /> units.
/// </summary>
/// <remarks>
///     <para>
///         The targeting logic is faction-agnostic: the AI always picks the closest
///         <c>enemies</c> entry, where "enemies" is whatever <see cref="TurnManager" />
///         passes in as the hostile list. This allows the same class to power
///         <see cref="AlliedAi" /> and <see cref="BasicEnemyAi" /> with no duplication.
///     </para>
///     <para>
///         Decision priorities (in order):
///         <list type="number">
///             <item>If stunned: pass turn.</item>
///             <item>If confused or corrupted-confused: attack a random unit (any faction).</item>
///             <item>If a usable skill is available: cast it on the closest hostile.</item>
///             <item>Otherwise: basic attack the closest hostile.</item>
///         </list>
///     </para>
/// </remarks>
public class BasicCombatAi : IUnitAi
{
    /// <summary>
    ///     Small artificial think-time so AI turns don't blast through instantly.
    ///     Ms.
    /// </summary>
    protected virtual int ThinkTimeMs => 600;

    /// <inheritdoc />
    public virtual async Task TakeTurnAsync(
        UnitSystem self,
        IReadOnlyList<UnitSystem> allies,
        IReadOnlyList<UnitSystem> enemies,
        MapSystem? map)
    {
        await Task.Delay(ThinkTimeMs);

        if (!self.IsAlive)
            return;

        // Stunned → skip.
        if (self.HasEffect<StunEffect>())
        {
            BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
                $"{self.UnitName} is stunned and cannot act.", LogSeverity.Info
            ));
            self.PassTurn();
            return;
        }

        // Confused → attack random alive unit (could be anyone).
        bool confused = self.HasEffect<ConfusionEffect>() || self.HasEffect<CorruptionLevel2Effect>() &&
            GD.Randf() < CorruptionLevel2Effect.MisdirectChance;
        IReadOnlyList<UnitSystem> targetingPool = confused
            ? allies.Concat(enemies).Where(u => u.IsAlive).ToArray()
            : enemies.Where(u => u.IsAlive).ToArray();

        if (targetingPool.Count == 0)
        {
            self.PassTurn();
            return;
        }

        UnitSystem chosen = ChooseTarget(self, targetingPool);

        // Try to use the highest-power available skill.
        DataDrivenSkill? skill = ChooseSkill(self);
        if (skill is not null)
        {
            using SkillExecutionScope.Frame _ = new(self);
            self.Play([chosen], map, skill);
            return;
        }

        // Fall back to a basic attack equivalent: deal raw BaseAtk damage.
        chosen.TakeDamage(self.BaseAtk);
        BattleEventBus.Instance?.Publish(new BattleEvents.LogMessage(
            $"{self.UnitName} attacks {chosen.UnitName} for {self.BaseAtk:F0} damage.",
            LogSeverity.Negative
        ));
        self.PassTurn();
    }

    /// <summary>
    ///     Default target picker: closest alive hostile.
    ///     Override to change priority (lowest HP, highest threat, etc.).
    /// </summary>
    /// <param name="self">The active unit.</param>
    /// <param name="pool">Eligible targets.</param>
    /// <returns>The chosen target.</returns>
    protected virtual UnitSystem ChooseTarget(UnitSystem self, IReadOnlyList<UnitSystem> pool)
    {
        // For simplicity at this stage: random alive unit. A future improvement is to use
        // the map's grid distance once <see cref="MapSystem.GetUnitPosition" /> is reliable.
        UnitSystem[] alive = pool.Where(u => u.IsAlive).ToArray();
        return alive[GD.RandRange(0, alive.Length - 1)];
    }

    /// <summary>
    ///     Pick a usable skill: in cooldown, with enough mana, prioritizing higher base power.
    /// </summary>
    /// <param name="self">The active unit.</param>
    /// <returns>The chosen skill or null if none usable.</returns>
    protected virtual DataDrivenSkill? ChooseSkill(UnitSystem self)
    {
        DataDrivenSkill? best = null;
        float bestPower = float.NegativeInfinity;
        foreach (SkillSystem candidate in self.ActiveSkills)
        {
            if (candidate is not DataDrivenSkill data) continue;
            if (data.Cooldown > 0) continue;
            if (data.ManaCost > self.ManaPoint) continue;

            float power = data.Definition.BasePower;
            if (power > bestPower)
            {
                best = data;
                bestPower = power;
            }
        }
        return best;
    }
}

/// <summary>
///     AI for hostile units. Identical to <see cref="BasicCombatAi" /> at this stage,
///     but exists as a named subclass so balance designers can later differentiate
///     "enemy" behaviour without affecting allies.
/// </summary>
public sealed class BasicEnemyAi : BasicCombatAi
{
}

/// <summary>
///     AI for friendly guest units. Same logic as <see cref="BasicCombatAi" />,
///     reserved as a separate type for tuning later (e.g. allies should never
///     friendly-fire when confused — override <c>ChooseTarget</c> here).
/// </summary>
public sealed class AlliedAi : BasicCombatAi
{
    /// <summary>Slightly faster than enemies so guest turns don't drag.</summary>
    protected override int ThinkTimeMs => 400;
}
