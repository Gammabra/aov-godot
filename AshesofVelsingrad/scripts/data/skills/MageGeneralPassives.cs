using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Data.Skills;

// =============================================================================
// Mage general passives — shared by every magic school per the feature doc.
// =============================================================================

public sealed class ConcentrationMagique : SkillSystem
{
    public ConcentrationMagique()
    {
        Name = SkillStrings.ArcaneFocusName;
        Description = "Passive — +10% magic damage when the unit didn't move this turn.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class MaitriseElementaire : SkillSystem
{
    public MaitriseElementaire()
    {
        Name = SkillStrings.ElementalAttunementName;
        Description = "Passive — elemental skills apply a stronger status alteration.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}

public sealed class AbsorptionDeMana : SkillSystem
{
    public AbsorptionDeMana()
    {
        Name = SkillStrings.ManaWellName;
        Description = "Passive — recover 5% MP whenever you apply a status alteration.";
        ManaCost = 0; TotalCooldown = 0; Cooldown = 0; Range = 0;
        MagicType = AovDataStructures.MagicType.None;
        EffectType = AovDataStructures.EffectType.Buff;
        TargetType = AovDataStructures.TargetTypes.SingleAlly;
    }

    public override void Use(IUnitSystem caster, List<IUnitSystem> targets, IMapSystem? map) { /* passive */ }
}
