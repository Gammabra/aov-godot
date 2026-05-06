namespace AshesOfVelsingrad.Data.Skills;

/// <summary>
///     Single source of truth for every skill's display name and description string.
/// </summary>
/// <remarks>
///     <para>
///         All skill <c>Name</c> / <c>Description</c> values now read from this class instead
///         of being hardcoded inside each skill constructor. Today every constant resolves to
///         English. To add a second language later, replace each constant body with a call
///         to <see cref="L10n.Tr" /> against a translation key (or use Godot's
///         <c>TranslationServer</c> + a <c>.po</c> / <c>.csv</c> resource):
///     </para>
///     <code>
///         public static string CrushingStrikeName => L10n.Tr("skill.crushing_strike.name");
///     </code>
///     <para>
///         Because every skill class references these constants by C# identifier, changing
///         the body of a single property propagates to every caller without touching the
///         skill catalogue files.
///     </para>
/// </remarks>
public static class SkillStrings
{
    // =========================================================================
    // Fighter (Combattant)
    // =========================================================================
    public const string CrushingStrikeName = "Crushing Strike";
    public const string CrushingStrikeDesc = "Heavy melee blow dealing 150% ATK. 35% chance to stun for 1 turn.";

    public const string WarCryName = "War Cry";
    public const string WarCryDesc = "Buff every ally's ATK and DEF by 15 (flat) for 3 turns.";

    public const string ChargeName = "Charge";
    public const string ChargeDesc = "Rush along a row or column up to 4 tiles, stop next to the target, strike for 120% ATK.";

    public const string BlockName = "Block";
    public const string BlockDesc = "Brace yourself: +60 DEF for 1 turn.";

    public const string CircularStrikeName = "Circular Strike";
    public const string CircularStrikeDesc = "Sweep every adjacent enemy for 100% ATK.";

    public const string BruteForceName = "Brute Force";
    public const string BruteForceDesc = "Passive — +10% damage on melee skills (Range 1).";

    public const string RecklessnessName = "Recklessness";
    public const string RecklessnessDesc = "Passive — +5% damage per 25% missing HP.";

    public const string WarriorEnduranceName = "Warrior Endurance";
    public const string WarriorEnduranceDesc = "Passive — -15% damage taken for the rest of the turn after landing a melee hit.";

    // =========================================================================
    // Swordsman (Épéiste)
    // =========================================================================
    public const string LightningStrikeName = "Lightning Strike";
    public const string LightningStrikeDesc = "Lightning rush: close 5 tiles and strike for 120% ATK.";

    public const string BladeDanceName = "Blade Dance";
    public const string BladeDanceDesc = "Spin and slash up to three adjacent enemies for 90% ATK each.";

    public const string GallantStrikeName = "Gallant Strike";
    public const string GallantStrikeDesc = "A bold thrust dealing 130% ATK; restores 10 MP on hit.";

    public const string PhantomStrikeName = "Phantom Strike";
    public const string PhantomStrikeDesc = "Strike and reposition: 110% ATK then swap places with the target.";

    public const string ExecutionerBladeName = "Executioner's Blade";
    public const string ExecutionerBladeDesc = "Slay an enemy below 25% HP outright; otherwise 110% ATK.";

    public const string RiposteName = "Riposte";
    public const string RiposteDesc = "Passive — counter-attack for 50% ATK whenever a melee strike misses you.";

    public const string BladeDancePassiveName = "Way of the Blade";
    public const string BladeDancePassiveDesc = "Passive — +10% crit chance with sword skills.";

    public const string SpectralBladePassiveName = "Spectral Blade";
    public const string SpectralBladePassiveDesc = "Passive — every third sword strike pierces armour (ignores DEF).";

    // =========================================================================
    // Assassin
    // =========================================================================
    public const string CriticalStrikeName = "Critical Strike";
    public const string CriticalStrikeDesc = "Backstab for 200% ATK if attacking from outside the target's vision arc.";

    public const string VanishName = "Vanish";
    public const string VanishDesc = "Slip into the shadows: untargetable for 1 turn (modelled as a +DEF burst).";

    public const string ShadowStrikeName = "Shadow Strike";
    public const string ShadowStrikeDesc = "Teleport behind a target up to 4 tiles away and strike for 130% ATK.";

    public const string ExecutionName = "Execution";
    public const string ExecutionDesc = "Finishing blow: 250% ATK against a target below 30% HP.";

    public const string BloodStrikeName = "Blood Strike";
    public const string BloodStrikeDesc = "Lifesteal — 100% ATK damage, heal yourself for 50% of the damage dealt.";

    // =========================================================================
    // Archer
    // =========================================================================
    public const string PiercingShotName = "Piercing Shot";
    public const string PiercingShotDesc = "Range 5, 130% ATK, ignores 50% of the target's DEF.";

    public const string MultiShotName = "Multi-Shot";
    public const string MultiShotDesc = "Fire three arrows at three different enemies for 80% ATK each.";

    public const string PoisonArrowName = "Poison Arrow";
    public const string PoisonArrowDesc = "Range 5, 90% ATK + Poison (10 dmg/turn for 3 turns).";

    public const string TrapShotName = "Trap Shot";
    public const string TrapShotDesc = "Place a damaging trap on a tile (90 dmg + 1-turn slow on trigger).";

    public const string HawkEyeName = "Hawk's Eye";
    public const string HawkEyeDesc = "Self-buff — +2 Range and +20 Atk for 3 turns.";

    // =========================================================================
    // Mage — General Passives
    // =========================================================================
    public const string ArcaneFocusName = "Arcane Focus";
    public const string ArcaneFocusDesc = "Passive — +10% spell damage but -10 DEF.";

    public const string ManaWellName = "Mana Well";
    public const string ManaWellDesc = "Passive — +5 MP at the start of every turn.";

    public const string ElementalAttunementName = "Elemental Attunement";
    public const string ElementalAttunementDesc = "Passive — +15% damage with the unit's primary element.";

    // =========================================================================
    // Mage — Fire
    // =========================================================================
    public const string FireballName = "Fireball";
    public const string FireballDesc = "Range 4. Hits a 3×3 area for 110% INT damage.";

    public const string IgniteName = "Ignite";
    public const string IgniteDesc = "Range 4. Apply Burning (12 dmg/turn) for 3 turns.";

    public const string FireWallName = "Wall of Flames";
    public const string FireWallDesc = "Range 3. Place a 3-tile fire wall (terrain effect) for 3 turns.";

    public const string FlamestormName = "Flamestorm";
    public const string FlamestormDesc = "Cross-shaped AOE around the caster — 90% INT damage and Burning.";

    public const string EmberShotName = "Ember Shot";
    public const string EmberShotDesc = "Range 5. 80% INT damage + 25% chance to apply Burning.";

    // =========================================================================
    // Mage — Water
    // =========================================================================
    public const string WaterJetName = "Water Jet";
    public const string WaterJetDesc = "Range 4. 100% INT damage and slow the target for 1 turn.";

    public const string FlashFreezeName = "Flash Freeze";
    public const string FlashFreezeDesc = "Range 3. 90% INT and slow the target for 2 turns.";

    public const string CrashingWaveName = "Crashing Wave";
    public const string CrashingWaveDesc = "Range 4 cone. 110% INT and knockback 1 tile.";

    public const string HealingTideName = "Healing Tide";
    public const string HealingTideDesc = "Range 0, all allies in a 3×3 around the caster heal 80 HP.";

    public const string MistArmorName = "Mist Armor";
    public const string MistArmorDesc = "Self-buff: +30 DEF for 2 turns.";

    // =========================================================================
    // Mage — Earth
    // =========================================================================
    public const string StoneSpikeName = "Stone Spike";
    public const string StoneSpikeDesc = "Range 4. 110% INT damage; can stun for 1 turn (20%).";

    public const string EarthquakeName = "Earthquake";
    public const string EarthquakeDesc = "Range 0. 90% INT to all enemies in a 3×3 around the caster.";

    public const string StoneFortressName = "Stone Fortress";
    public const string StoneFortressDesc = "Range 3. Place a blocking 1-tile rock for 3 turns.";

    public const string RootGraspName = "Root Grasp";
    public const string RootGraspDesc = "Range 4. Root the target in place for 2 turns.";

    public const string GeomanticArmorName = "Geomantic Armor";
    public const string GeomanticArmorDesc = "Self-buff: +50 DEF for 3 turns.";

    // =========================================================================
    // Mage — Light
    // =========================================================================
    public const string SacredRayName = "Sacred Ray";
    public const string SacredRayDesc = "Range 5. 110% INT damage to undead/cursed targets, 90% to living.";

    public const string PurifyingFlashName = "Purifying Flash";
    public const string PurifyingFlashDesc = "Range 0. Cleanse all debuffs from allies in a 3×3 area.";

    public const string DivineJudgmentName = "Divine Judgment";
    public const string DivineJudgmentDesc = "Range 6. 130% INT damage to a single target; massive vs. corrupted.";

    public const string ResurrectionName = "Resurrection";
    public const string ResurrectionDesc = "Range 2. Revive a fallen ally with 30% HP.";

    public const string DivinePrayerName = "Divine Prayer";
    public const string DivinePrayerDesc = "Range 0. Grant +30% status-effect resistance to allies in a 3×3 area for 3 turns.";

    // =========================================================================
    // Mage — Dark
    // =========================================================================
    public const string CursedOrbName = "Cursed Orb";
    public const string CursedOrbDesc = "Range 5. 120% INT damage; 25% chance of corruption backlash.";

    public const string ShadowChainsName = "Shadow Chains";
    public const string ShadowChainsDesc = "Range 4. Bind a target in place for 2 turns; 20% backlash chance.";

    public const string ArmyOfTheDeadName = "Army of the Dead";
    public const string ArmyOfTheDeadDesc = "Summon two skeleton minions; 30% backlash chance.";

    public const string CorruptedConversionName = "Corrupted Conversion";
    public const string CorruptedConversionDesc = "Range 4. Temporarily convert an enemy unit to your side; 35% backlash chance.";

    public const string LifestealName = "Lifesteal";
    public const string LifestealDesc = "Range 4. 110% INT damage; heal yourself for 50% of the damage dealt.";

    // =========================================================================
    // Misc / enemy-only / basic
    // =========================================================================
    public const string ArrowShotName = "Arrow Shot";
    public const string ArrowShotDesc = "Basic ranged attack.";

    public const string HeavySwingName = "Heavy Swing";
    public const string HeavySwingDesc = "Slam the target with a heavy blow for 100% ATK.";

    public const string IronBashName = "Iron Bash";
    public const string IronBashDesc = "90% ATK and stun for 1 turn.";
}

/// <summary>
///     Tiny localisation indirection so future i18n is a 1-line swap per call site.
/// </summary>
/// <remarks>
///     <para>
///         Today this just returns the input — every caller passes English strings already.
///         To add a real translation layer, change <see cref="Tr" />'s body to call
///         <c>TranslationServer.Translate(key)</c> (Godot built-in) or a custom dictionary
///         lookup, and pass <em>keys</em> (e.g. <c>"skill.charge.name"</c>) at the call sites
///         instead of the literal English strings stored in <see cref="SkillStrings" />.
///     </para>
/// </remarks>
public static class L10n
{
    /// <summary>Look up a translated string. Currently a passthrough.</summary>
    /// <param name="key">Either an English fallback or a translation key.</param>
    /// <returns>The translated string, or <paramref name="key" /> when no translation exists.</returns>
    public static string Tr(string key)
    {
        // Future: use Godot.TranslationServer.Translate(key).ToString();
        return key;
    }
}
