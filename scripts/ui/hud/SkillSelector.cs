using System;
using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems;
using AshesOfVelsingrad.systems.battle;
using AshesOfVelsingrad.systems.skills;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Five-slot skill bar matching <see cref="systems.BattleInputSystem" />'s hot-keys.
/// </summary>
/// <remarks>
///     Bottom-left strip. Each slot shows the skill's name, mana cost, and current cooldown.
///     Disabled when cooldown &gt; 0, when the caster lacks mana, or when it's not a player turn.
/// </remarks>
public sealed partial class SkillSelector : Control
{
    /// <summary>Number of slots, fixed at 5.</summary>
    public const int SlotCount = 5;

    /// <summary>Fired when a slot is selected. Carries the slot index (0-4) and the resolved skill.</summary>
    public event Action<int, DataDrivenSkill>? OnSkillSelected;

    private readonly Button[] _buttons = new Button[SlotCount];

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();

        _ = HudBusHelper.WhenReadyAsync(this, bus =>
        {
            bus.Subscribe<BattleEvents.TurnStarted>(_ => RefreshFromCurrentUnit());
            bus.Subscribe<BattleEvents.SkillUsed>(_ => RefreshFromCurrentUnit());
            bus.Subscribe<BattleEvents.ManaChanged>(_ => RefreshFromCurrentUnit());
            RefreshFromCurrentUnit();
        });
    }

    private void BuildLayout()
    {
        // Bottom-centre strip placed just above the ActionMenu; wider than ActionMenu to
        // fit 5 skill slots comfortably and grow with viewport via the buttons' SizeFlags.
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        OffsetLeft = -360;
        OffsetRight = 360;
        OffsetTop = -134;
        OffsetBottom = -76;
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new();
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(HudStyle.MakePanel(panelContent));

        HBoxContainer row = new()
        {
            Name = "Slots",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", 4);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        for (int i = 0; i < SlotCount; i++)
        {
            int slot = i;
            Button b = new()
            {
                Text = $"{i + 1}. —",
                // Min size kept narrow so the row can shrink on small viewports;
                // SizeFlagsStretchRatio keeps the five slots equally wide.
                CustomMinimumSize = new Vector2(60, 48),
                Disabled = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f,
                ClipText = true,
                AutowrapMode = TextServer.AutowrapMode.Off,
            };
            HudStyle.StyleButton(b);
            b.Pressed += () => HandlePress(slot);
            _buttons[i] = b;
            row.AddChild(b);
        }
    }

    /// <summary>
    ///     Re-read the active player unit's loadout and update slot visuals.
    /// </summary>
    public void RefreshFromCurrentUnit()
    {
        UnitSystem? unit = TurnManager.Active?.CurrentUnit;
        if (unit is null || unit.Faction != Faction.Player)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _buttons[i].Text = $"{i + 1}. —";
                _buttons[i].Disabled = true;
                _buttons[i].TooltipText = "";
            }
            return;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            SkillSystem? skill = i < unit.ActiveSkills.Count ? unit.ActiveSkills[i] : null;
            if (skill is null)
            {
                _buttons[i].Text = $"{i + 1}. —";
                _buttons[i].Disabled = true;
                _buttons[i].TooltipText = "(empty slot)";
                continue;
            }

            // Single line, clipped — full info lives in the tooltip so the button never overflows.
            _buttons[i].Text = $"{i + 1}. {skill.Name}";
            _buttons[i].Disabled = skill.Cooldown > 0 || skill.ManaCost > unit.ManaPoint;
            string desc = string.IsNullOrEmpty(skill.Description) ? skill.Name : skill.Description;
            string cdText = skill.Cooldown > 0 ? $"  ⏱ on cooldown ({skill.Cooldown})" : $"  CD {skill.TotalCooldown}";
            _buttons[i].TooltipText = $"{skill.Name}\n{desc}\n\nMP {skill.ManaCost:F0}{cdText}  •  Range {skill.Range}";
        }
    }

    private void HandlePress(int slot)
    {
        UnitSystem? unit = TurnManager.Active?.CurrentUnit;
        if (unit is null || slot >= unit.ActiveSkills.Count) return;
        if (unit.ActiveSkills[slot] is DataDrivenSkill dds)
            OnSkillSelected?.Invoke(slot, dds);
    }
}
