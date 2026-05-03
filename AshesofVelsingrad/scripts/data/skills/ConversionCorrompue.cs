using System.Collections.Generic;
using AshesOfVelsingrad.Data.Corruption;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

/// <summary>
///     Conversion Corrompue — the hallmark Darkness-school spell.
/// </summary>
/// <remarks>
///     Twists an ally's mind: target ally fights as an enemy for 3 turns. Heaviest corruption
///     backlash chance in the catalogue (85% base). Belongs in the Mage Dark school but lives
///     in its own file because it depends on <see cref="CorruptionSystem" />.
/// </remarks>
public sealed class ConversionCorrompue : SkillSystem
{
    /// <summary>Base backlash probability before karma adjustment.</summary>
    public const float BaseBacklashChance = 0.85f;

    /// <summary>Number of turns the converted ally fights as an enemy.</summary>
    public const int ConversionTurns = 3;

    public ConversionCorrompue()
    {
        Name = "Conversion Corrompue";
        Description =
            $"Twist an ally's mind: target ally fights as an enemy for {ConversionTurns} turns. Heavy corruption backlash.";
        ManaCost = 24; TotalCooldown = 8; Cooldown = 0; Range = 5;
        MagicType = AovDataStructures.MagicType.Dark;
        EffectType = AovDataStructures.EffectType.Control;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    /// <inheritdoc />
    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map)
    {
        if (targets.Count == 0) return;

        IUnitSystem target = targets[0];
        // Convert allies only — the engine routes targeting through the SingleAlly TargetType
        // already, but we double-check in case a custom UI bypasses that.
        if (target.Faction == Faction.Enemy) return;

        CorruptionSystem.ApplyTemporaryConversion(target, Faction.Enemy, ConversionTurns);
        // Caster always rolls a backlash on this spell — it's the highest-cost dark spell in the
        // catalogue and the doc treats it as guaranteed-pressure.
        CorruptionSystem.RollBacklash(caster, BaseBacklashChance);
    }
}
