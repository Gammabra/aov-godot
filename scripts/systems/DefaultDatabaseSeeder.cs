using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.systems.items.behaviours;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.systems.skills.behaviours;
using Godot;

namespace AshesOfVelsingrad.systems;

/// <summary>
///     Programmatic catalogue of every skill and item shipped with the project.
/// </summary>
/// <remarks>
///     <para>
///         Acts as the runtime fallback database when no <c>.tres</c> resources are
///         loaded. Run once at boot, after <see cref="SkillRegistry" /> and
///         <see cref="ItemRegistry" /> have entered the tree, by adding this node
///         under the <c>Main</c> scene (or as an AutoLoad).
///     </para>
///     <para>
///         All numeric values are <b>placeholders</b>: the feature doc only fixes
///         the cooldown of <c>Coup Critique</c> (3 turns) and the "consumes all mana"
///         clause of <c>Explosion Pyromantique</c>. Everything else is calibrated for
///         playability — see <c>CHANGELOG.md → Balance Notes</c> for the full table.
///     </para>
/// </remarks>
public sealed partial class DefaultDatabaseSeeder : Node
{
    /// <inheritdoc />
    public override void _Ready()
    {
        SeedBehaviours();
        SeedSkills();
        SeedItems();
        GD.Print("DefaultDatabaseSeeder: loaded combat catalogue.");
    }

    #region Behaviour registration

    /// <summary>Register every concrete <see cref="ISkillBehaviour" /> / <see cref="IItemBehaviour" />.</summary>
    private static void SeedBehaviours()
    {
        SkillRegistry? skills = SkillRegistry.Instance;
        ItemRegistry? items = ItemRegistry.Instance;
        if (skills is null || items is null)
        {
            GD.PrintErr("DefaultDatabaseSeeder: registries missing, cannot seed behaviours.");
            return;
        }

        skills.RegisterBehaviour("damage", new DamageBehaviour());
        skills.RegisterBehaviour("heal", new HealBehaviour());
        skills.RegisterBehaviour("status_only", new StatusOnlyBehaviour());
        skills.RegisterBehaviour("dot", new DamageOverTimeBehaviour());
        skills.RegisterBehaviour("control", new ControlBehaviour());
        skills.RegisterBehaviour("resurrect", new ResurrectBehaviour());
        skills.RegisterBehaviour("cleanse", new CleanseBehaviour());
        skills.RegisterBehaviour("corrupted_conversion", new CorruptedConversionBehaviour());

        items.RegisterBehaviour("heal_item", new HealingItemBehaviour());
        items.RegisterBehaviour("mana_item", new ManaItemBehaviour());
        items.RegisterBehaviour("cleanse_item", new CleanseItemBehaviour());
        items.RegisterBehaviour("purifying_elixir", new PurifyingElixirBehaviour());
        items.RegisterBehaviour("damage_item", new DamageItemBehaviour());
        items.RegisterBehaviour("revive_item", new ReviveItemBehaviour());
    }

    #endregion

    #region Skill registration

    /// <summary>
    ///     Register the entire skill catalogue from the feature document.
    /// </summary>
    /// <remarks>
    ///     Skills are grouped by class. Each call to <see cref="MakeSkill" /> is a single
    ///     row-style declaration so this method reads as a balance table.
    /// </remarks>
    private static void SeedSkills()
    {
        SkillRegistry? r = SkillRegistry.Instance;
        if (r is null) return;

        // ── Combattant (Fighter) ────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("force_brute", "Force Brute", "Brute Strength: +10% melee damage.", "status_only", isPassive: true, statusId: "", basePower: 0));
        r.RegisterDefinition(MakeSkill("temerite", "Témérité", "Recklessness: +5% damage per 25% missing HP.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("endurance_guerriere", "Endurance Guerrière", "Warrior Endurance: -15% damage taken after a melee strike.", "status_only", isPassive: true));

        r.RegisterDefinition(MakeSkill("frappe_ecrasante", "Frappe Écrasante", "Crushing Strike: +50% damage; may stun.",
            "damage", manaCost: 8, cooldown: 2, range: 1, basePower: 18, scaling: ScalingStat.Attack,
            statusId: "stun", statusChance: 0.35f, statusDuration: 1, target: TargetTypes.SingleEnemy));
        r.RegisterDefinition(MakeSkill("cri_de_guerre", "Cri de Guerre", "War Cry: +15% Atk/Def to nearby allies.",
            "status_only", manaCost: 6, cooldown: 4, target: TargetTypes.AllAllies));
        r.RegisterDefinition(MakeSkill("charge", "Charge", "Combined movement and attack.",
            "damage", manaCost: 4, cooldown: 1, range: 4, basePower: 14, scaling: ScalingStat.Attack,
            target: TargetTypes.SingleEnemy));
        r.RegisterDefinition(MakeSkill("blocage", "Blocage", "Block: -50% damage on next attack.",
            "status_only", manaCost: 0, cooldown: 3, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("frappe_circulaire", "Frappe Circulaire", "Attacks all adjacent enemies.",
            "damage", manaCost: 10, cooldown: 3, range: 1, basePower: 12, scaling: ScalingStat.Attack,
            target: TargetTypes.AllEnemies));

        // ── Épéiste (Swordsman) ─────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("riposte", "Riposte", "30% chance to counter after a melee hit.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("danse_du_lame_passive", "Danse du Lame", "+10% crit if moved before attack.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("lame_spectrale", "Lame Spectrale", "Ignores 15% of enemy armor.", "status_only", isPassive: true));

        r.RegisterDefinition(MakeSkill("frappe_eclair", "Frappe Éclair", "Lightning Strike: instant rush + +20% damage.",
            "damage", manaCost: 10, cooldown: 2, range: 5, basePower: 16, scaling: ScalingStat.Attack));
        r.RegisterDefinition(MakeSkill("danse_des_lames", "Danse des Lames", "Strikes up to 3 adjacent enemies.",
            "damage", manaCost: 12, cooldown: 3, range: 1, basePower: 11, scaling: ScalingStat.Attack,
            target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("coup_de_bravoure", "Coup de Bravoure", "-50% damage taken + Riposte.",
            "status_only", manaCost: 8, cooldown: 4, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("frappe_fantome", "Frappe Fantôme", "Reach attack at 1 tile distance.",
            "damage", manaCost: 6, cooldown: 1, range: 2, basePower: 13, scaling: ScalingStat.Attack));
        r.RegisterDefinition(MakeSkill("lame_d_execution", "Lame d'Exécution", "+50% damage to targets <30% HP.",
            "damage", manaCost: 12, cooldown: 3, range: 1, basePower: 22, scaling: ScalingStat.Attack));

        // ── Assassin ────────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("frappe_sournoise", "Frappe Sournoise", "+30% damage if enemy hasn't attacked yet.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("ombre_furtive", "Ombre Furtive", "Pass through enemies without being blocked.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("toxines_mortelles", "Toxines Mortelles", "20% chance to poison on hit.", "status_only", isPassive: true));

        r.RegisterDefinition(MakeSkill("coup_critique", "Coup Critique", "Critical Strike: double damage.",
            "damage", manaCost: 8, cooldown: 3, range: 1, basePower: 30, scaling: ScalingStat.Attack));
        r.RegisterDefinition(MakeSkill("disparition", "Disparition", "Become invisible until end of next turn.",
            "status_only", manaCost: 6, cooldown: 4, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("coup_d_ombre", "Coup d'Ombre", "Attack without breaking invisibility.",
            "damage", manaCost: 8, cooldown: 2, range: 1, basePower: 16, scaling: ScalingStat.Attack));
        r.RegisterDefinition(MakeSkill("execution", "Exécution", "Instakill below 15% HP.",
            "damage", manaCost: 14, cooldown: 5, range: 1, basePower: 9999, scaling: ScalingStat.None));
        r.RegisterDefinition(MakeSkill("frappe_sanguine", "Frappe Sanguine", "Heals self for 20% of damage dealt.",
            "damage", manaCost: 7, cooldown: 2, range: 1, basePower: 14, scaling: ScalingStat.Attack));

        // ── Archer ──────────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("tir_precis", "Tir Précis", "+15% accuracy if didn't move.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("visee_fatale", "Visée Fatale", "+10% damage vs wounded.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("tir_en_mouvement", "Tir en Mouvement", "Shoot after moving with no penalty.", "status_only", isPassive: true));

        r.RegisterDefinition(MakeSkill("fleche_perforante", "Flèche Perforante", "Pierces up to 2 aligned enemies.",
            "damage", manaCost: 8, cooldown: 2, range: 6, basePower: 14, scaling: ScalingStat.Attack));
        r.RegisterDefinition(MakeSkill("pluie_de_fleches", "Pluie de Flèches", "AoE damage over a 3×3 zone.",
            "damage", manaCost: 14, cooldown: 4, range: 7, basePower: 10, scaling: ScalingStat.Attack,
            target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("fleche_empoisonnee", "Flèche Empoisonnée", "Poisons target for 3 turns.",
            "dot", manaCost: 7, cooldown: 2, range: 6, basePower: 8, scaling: ScalingStat.Attack,
            statusId: "poison", statusChance: 1.0f, statusDuration: 3));
        r.RegisterDefinition(MakeSkill("oeil_de_faucon", "Œil de Faucon", "+2 range for 1 turn.",
            "status_only", manaCost: 4, cooldown: 3, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("fleche_de_givre", "Flèche de Givre", "Freezes target for 1 turn.",
            "control", manaCost: 8, cooldown: 3, range: 6, basePower: 6, scaling: ScalingStat.Attack,
            statusId: "stun", statusChance: 1.0f, statusDuration: 1));

        // ── Mage – general passives ─────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("concentration_magique", "Concentration Magique", "+10% magic damage if didn't move.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("maitrise_elementaire", "Maîtrise Élémentaire", "Stronger status alteration on element hits.", "status_only", isPassive: true));
        r.RegisterDefinition(MakeSkill("absorption_de_mana", "Absorption de Mana", "+5% PM whenever a status is applied.", "status_only", isPassive: true));

        // ── Mage – Fire ─────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("boule_de_feu", "Boule de Feu", "Fireball: damage + burn.",
            "dot", manaCost: 10, cooldown: 1, range: 6, basePower: 18, scaling: ScalingStat.Intelligence,
            magic: MagicType.Fire, statusId: "burn", statusChance: 1.0f, statusDuration: 3));
        r.RegisterDefinition(MakeSkill("tempete_ardente", "Tempête Ardente", "Rain of flames over 3×3, ignites terrain.",
            "dot", manaCost: 18, cooldown: 4, range: 6, basePower: 14, scaling: ScalingStat.Intelligence,
            magic: MagicType.Fire, statusId: "burn", statusChance: 1.0f, statusDuration: 2,
            target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("souffle_du_dragon", "Souffle du Dragon", "Wide cone of fire.",
            "damage", manaCost: 16, cooldown: 3, range: 4, basePower: 22, scaling: ScalingStat.Intelligence,
            magic: MagicType.Fire, statusId: "burn", statusChance: 0.6f, statusDuration: 2,
            target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("mur_de_flammes", "Mur de Flammes", "Flaming barrier blocking passage.",
            "status_only", manaCost: 14, cooldown: 5, range: 5,
            magic: MagicType.Fire, target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("explosion_pyromantique", "Explosion Pyromantique", "Consumes ALL mana for devastating damage.",
            "damage", manaCost: 999, cooldown: 6, range: 5, basePower: 80, scaling: ScalingStat.Intelligence,
            magic: MagicType.Fire, target: TargetTypes.AllEnemies));

        // ── Mage – Water ────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("jet_d_eau", "Jet d'Eau", "Water Jet: moderate damage + slow.",
            "damage", manaCost: 8, cooldown: 1, range: 5, basePower: 12, scaling: ScalingStat.Intelligence,
            magic: MagicType.Water));
        r.RegisterDefinition(MakeSkill("pluie_guerisseuse", "Pluie Guérisseuse", "Heals all allies slightly.",
            "heal", manaCost: 16, cooldown: 4, basePower: 18, scaling: ScalingStat.Intelligence,
            magic: MagicType.Water, target: TargetTypes.AllAllies));
        r.RegisterDefinition(MakeSkill("vague_deferlante", "Vague Déferlante", "Knocks enemies back, extinguishes fires.",
            "damage", manaCost: 12, cooldown: 3, range: 4, basePower: 10, scaling: ScalingStat.Intelligence,
            magic: MagicType.Water, target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("bulle_de_protection", "Bulle de Protection", "Protect ally from damage for 1 turn.",
            "status_only", manaCost: 10, cooldown: 4,
            magic: MagicType.Water, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("glaciation_instantanee", "Glaciation Instantanée", "Slippery zone or frozen spikes.",
            "control", manaCost: 14, cooldown: 4, range: 4, basePower: 8, scaling: ScalingStat.Intelligence,
            magic: MagicType.Water, statusId: "stun", statusChance: 0.5f, statusDuration: 1));

        // ── Mage – Earth ────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("lance_de_roc", "Lance de Roc", "Pierces and shatters armor.",
            "damage", manaCost: 10, cooldown: 2, range: 5, basePower: 16, scaling: ScalingStat.Intelligence,
            magic: MagicType.Earth));
        r.RegisterDefinition(MakeSkill("onde_sismique", "Onde Sismique", "Knocks enemies down in a line.",
            "control", manaCost: 14, cooldown: 4, range: 5, basePower: 12, scaling: ScalingStat.Intelligence,
            magic: MagicType.Earth, statusId: "stun", statusChance: 0.7f, statusDuration: 1,
            target: TargetTypes.AllEnemies));
        r.RegisterDefinition(MakeSkill("forteresse_de_pierre", "Forteresse de Pierre", "Wall blocking movement.",
            "status_only", manaCost: 12, cooldown: 5, range: 4,
            magic: MagicType.Earth));
        r.RegisterDefinition(MakeSkill("peau_de_granit", "Peau de Granit", "+Defense to ally for 3 turns.",
            "status_only", manaCost: 8, cooldown: 4,
            magic: MagicType.Earth, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("avatar_du_titan", "Avatar du Titan", "Temporary stone-colossus form.",
            "status_only", manaCost: 24, cooldown: 6,
            magic: MagicType.Earth, target: TargetTypes.SingleAlly));

        // ── Mage – Light ────────────────────────────────────────────────────────
        r.RegisterDefinition(MakeSkill("rayon_sacre", "Rayon Sacré", "Damages enemies, heals allies in line.",
            "damage", manaCost: 12, cooldown: 2, range: 6, basePower: 14, scaling: ScalingStat.Intelligence,
            magic: MagicType.Light));
        r.RegisterDefinition(MakeSkill("eclat_purificateur", "Éclat Purificateur", "Removes all negative status from an ally.",
            "cleanse", manaCost: 12, cooldown: 4,
            magic: MagicType.Light, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("jugement_divin", "Jugement Divin", "Powerful lightning vs corrupted enemies.",
            "damage", manaCost: 18, cooldown: 4, range: 6, basePower: 26, scaling: ScalingStat.Intelligence,
            magic: MagicType.Light));
        r.RegisterDefinition(MakeSkill("resurrection", "Résurrection", "Revive a fallen ally with 50% HP.",
            "resurrect", manaCost: 30, cooldown: 8, basePower: 0.5f,
            magic: MagicType.Light, target: TargetTypes.SingleAlly));
        r.RegisterDefinition(MakeSkill("priere_divine", "Prière Divine", "Increase status resistance for 3 turns.",
            "status_only", manaCost: 10, cooldown: 4,
            magic: MagicType.Light, target: TargetTypes.AllAllies));

        // ── Mage – Darkness (corruption sources) ────────────────────────────────
        r.RegisterDefinition(MakeSkill("orbe_maudit", "Orbe Maudit", "Cursed Orb: weakens target.",
            "damage", manaCost: 10, cooldown: 2, range: 5, basePower: 12, scaling: ScalingStat.Intelligence,
            magic: MagicType.Dark, statusId: "curse", statusChance: 0.4f, statusDuration: -1,
            corruption: true, corruptionChance: 0.20f));
        r.RegisterDefinition(MakeSkill("lame_d_ombre", "Lame d'Ombre", "Ignores defense, drains life.",
            "damage", manaCost: 12, cooldown: 2, range: 1, basePower: 22, scaling: ScalingStat.Intelligence,
            magic: MagicType.Dark, corruption: true, corruptionChance: 0.25f));
        r.RegisterDefinition(MakeSkill("pacte_sombre", "Pacte Sombre", "Sacrifice HP to boost magic.",
            "status_only", manaCost: 0, cooldown: 5, target: TargetTypes.SingleAlly,
            magic: MagicType.Dark, corruption: true, corruptionChance: 0.50f));
        r.RegisterDefinition(MakeSkill("sang_corrompu", "Sang Corrompu", "Reduces target's regen and healing.",
            "status_only", manaCost: 8, cooldown: 3, range: 5, statusId: "bleed", statusChance: 1.0f, statusDuration: 4,
            magic: MagicType.Dark, corruption: true, corruptionChance: 0.30f));
        r.RegisterDefinition(MakeSkill("armee_des_morts", "Armée des Morts", "Summon corpses temporarily.",
            "status_only", manaCost: 22, cooldown: 6, target: TargetTypes.AllAllies,
            magic: MagicType.Dark, corruption: true, corruptionChance: 0.60f));

        // ── The hallmark "turn ally into enemy for 3 turns" spell ───────────────
        r.RegisterDefinition(MakeSkill("corrupted_conversion", "Conversion Corrompue",
            "Twists an ally's mind: target ally fights as an enemy for 3 turns. Heavy corruption backlash.",
            "corrupted_conversion", manaCost: 24, cooldown: 8, range: 5,
            magic: MagicType.Dark, target: TargetTypes.SingleAlly,
            statusDuration: 3, corruption: true, corruptionChance: 0.85f));
    }

    /// <summary>
    ///     Build a <see cref="SkillDefinition" /> with sensible defaults; used to keep
    ///     <see cref="SeedSkills" /> readable as a balance table.
    /// </summary>
    private static SkillDefinition MakeSkill(
        string skillId,
        string displayName,
        string description,
        string behaviour,
        bool isPassive = false,
        float manaCost = 0,
        int cooldown = 0,
        int range = 1,
        float basePower = 0,
        ScalingStat scaling = ScalingStat.Intelligence,
        float scalingFactor = 1.0f,
        MagicType magic = MagicType.None,
        TargetTypes target = TargetTypes.SingleEnemy,
        string statusId = "",
        float statusChance = 1.0f,
        int statusDuration = 3,
        bool corruption = false,
        float corruptionChance = 0.25f)
    {
        return new SkillDefinition
        {
            SkillId = skillId,
            DisplayName = displayName,
            Description = description,
            BehaviourId = behaviour,
            IsPassive = isPassive,
            ManaCost = manaCost,
            TotalCooldown = cooldown,
            Range = range,
            BasePower = basePower,
            Scaling = scaling,
            ScalingFactor = scalingFactor,
            MagicType = magic,
            EffectType = EffectTypeFor(behaviour),
            TargetType = target,
            StatusEffectIdOnHit = statusId,
            StatusEffectChance = statusChance,
            StatusEffectDuration = statusDuration,
            IsCorruptionSource = corruption,
            BaseCorruptionChance = corruptionChance,
        };
    }

    private static EffectType EffectTypeFor(string behaviour) => behaviour switch
    {
        "damage" => EffectType.Damage,
        "heal" => EffectType.Heal,
        "dot" => EffectType.Dot,
        "control" => EffectType.Control,
        "status_only" => EffectType.Buff,
        "cleanse" => EffectType.Buff,
        "resurrect" => EffectType.Heal,
        "corrupted_conversion" => EffectType.Control,
        _ => EffectType.Damage
    };

    #endregion

    #region Item registration

    /// <summary>Register the starter item set used by exploration loot drops and vendors.</summary>
    private static void SeedItems()
    {
        ItemRegistry? r = ItemRegistry.Instance;
        if (r is null) return;

        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "healing_potion",
            DisplayName = "Healing Potion",
            Description = "Restores 30 HP to one ally.",
            BehaviourId = "heal_item",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 30, Price = 25
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "greater_healing_potion",
            DisplayName = "Greater Healing Potion",
            Description = "Restores 70 HP to one ally.",
            BehaviourId = "heal_item",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 70, Price = 80
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "mana_potion",
            DisplayName = "Mana Potion",
            Description = "Restores 25 MP to one ally.",
            BehaviourId = "mana_item",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 25, Price = 35
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "antidote",
            DisplayName = "Antidote",
            Description = "Removes all purifiable status from one ally.",
            BehaviourId = "cleanse_item",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 0, Price = 40
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "purifying_elixir",
            DisplayName = "Purifying Elixir",
            Description = "Reduces an ally's corruption by one level. Rare.",
            BehaviourId = "purifying_elixir",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 0, Price = 200
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "alchemists_fire",
            DisplayName = "Alchemist's Fire",
            Description = "Throws a flask of liquid fire dealing 25 damage.",
            BehaviourId = "damage_item",
            TargetType = ItemTargetType.SingleEnemy,
            Magnitude = 25, Price = 50
        });
        r.RegisterDefinition(new ItemDefinition
        {
            ItemId = "phoenix_down",
            DisplayName = "Phoenix Down",
            Description = "Revives a fallen ally with 50% HP.",
            BehaviourId = "revive_item",
            TargetType = ItemTargetType.SingleAlly,
            Magnitude = 0.5f, Price = 250
        });
    }

    #endregion
}
