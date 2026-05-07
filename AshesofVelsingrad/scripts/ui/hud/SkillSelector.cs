using System;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Five-slot skill bar matching <see cref="BattleInputSystem"/>'s hot-keys
///     (<c>battle_select_skill1</c> .. <c>battle_select_skill5</c>).
/// </summary>
/// <remarks>
///     Souls-like slot frames in a row directly above the action bar. Each slot shows the
///     hotkey number, the skill icon (default rune until skill resources expose icon paths)
///     and the remaining cooldown overlaid in the centre. Slots disable when the skill is
///     on cooldown or the caster lacks mana.
/// </remarks>
public sealed partial class SkillSelector : Control, IHudWidget
{
    /// <summary>Number of slots, fixed at 5.</summary>
    public const int SlotCount = 5;

    /// <summary>Fired when a slot is clicked.</summary>
    public event Action<int, ISkillSystem>? OnSkillSelected;

    private readonly Button[] _buttons = new Button[SlotCount];
    private readonly Label[] _cdLabels = new Label[SlotCount];
    private IUnitSystem? _bound;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready() => EnsureBuilt();

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        int halfW = HudStyle.ScaledPx(HudStyle.SkillBarWidth) / 2;
        int height = HudStyle.ScaledPx(HudStyle.SkillBarHeight);
        int actionH = HudStyle.ScaledPx(HudStyle.ActionBarHeight);
        OffsetLeft = -halfW;
        OffsetRight = halfW;
        OffsetTop = -actionH - height - HudStyle.PadLg - HudStyle.PadSm;
        OffsetBottom = -actionH - HudStyle.PadLg - HudStyle.PadSm;
        CustomMinimumSize = new Vector2(2 * halfW, height);
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { Name = "Content", MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(panelContent, HudStyle.PanelTier.Heavy));

        HBoxContainer row = new()
        {
            Name = "Slots",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", HudStyle.PadXs);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelContent.AddChild(row);

        for (int i = 0; i < SlotCount; i++)
        {
            int slot = i;
            row.AddChild(BuildSlot(slot));
        }
    }

    /// <summary>
    ///     A slot is a single Button. The hotkey number is rendered as the button's text and
    ///     the skill icon is set on the button's Icon field, so all input handling is on the
    ///     button itself — no overlapping Label children that could intercept clicks.
    /// </summary>
    private Control BuildSlot(int slot)
    {
        // Outer wrapper exists only to host the cooldown label overlay. Marked Ignore so
        // every click in the slot's rect falls through to the button beneath.
        Control wrapper = new()
        {
            Name = $"Slot{slot}",
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.SkillSlotSize),
                HudStyle.ScaledPx(HudStyle.SkillSlotSize)),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        Button b = new()
        {
            Text = (slot + 1).ToString(),
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.SkillSlotSize),
                HudStyle.ScaledPx(HudStyle.SkillSlotSize)),
            Disabled = true,
            ClipText = true,
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Center,
            ExpandIcon = false,
        };
        HudStyle.StyleSlotButton(b, HudStyle.FontSizeSmall);
        HudStyle.SetButtonIcon(b, "skill_default", HudStyle.SlotIconSize);
        b.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        b.Pressed += () => HandlePress(slot);
        wrapper.AddChild(b);
        _buttons[slot] = b;

        // Cooldown overlay — only visible while CD > 0. MouseFilter.Ignore so it never
        // blocks the button beneath it.
        Label cd = new()
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        HudStyle.StyleLabel(cd, HudStyle.FontSizeHeader);
        cd.AddThemeColorOverride("font_color", new Color(1f, 0.78f, 0.42f, 1f));
        cd.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        wrapper.AddChild(cd);
        _cdLabels[slot] = cd;

        return wrapper;
    }

    /// <summary>Bind the selector to the unit whose loadout to display.</summary>
    public void Bind(IUnitSystem? unit)
    {
        _bound = unit;
        Visible = unit is not null && unit.Faction == Faction.Player;
        Refresh();
    }

    /// <summary>Re-read the bound unit's skills and refresh slot visuals.</summary>
    public void Refresh()
    {
        if (_buttons[0] is null) return;

        if (_bound is null)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _buttons[i].Text = (i + 1).ToString();
                _buttons[i].Disabled = true;
                _buttons[i].TooltipText = "";
                HudStyle.SetButtonIcon(_buttons[i], "skill_default", HudStyle.SlotIconSize);
                if (_cdLabels[i] is not null) _cdLabels[i].Text = "";
            }
            return;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            ISkillSystem? skill = i < _bound.ActiveSkills.Count ? _bound.ActiveSkills[i] : null;
            if (skill is null)
            {
                _buttons[i].Text = (i + 1).ToString();
                _buttons[i].Disabled = true;
                _buttons[i].TooltipText = "(empty slot)";
                HudStyle.SetButtonIcon(_buttons[i], "skill_default", HudStyle.SlotIconSize);
                _cdLabels[i].Text = "";
                continue;
            }

            // Show "1" "2" "3" "4" "5" as the button text — easy hotkey recognition.
            _buttons[i].Text = (i + 1).ToString();
            _buttons[i].Disabled = skill.Cooldown > 0 || skill.ManaCost > _bound.Mana;
            HudStyle.SetButtonIcon(_buttons[i], "skill_default", HudStyle.SlotIconSize);

            _cdLabels[i].Text = skill.Cooldown > 0 ? skill.Cooldown.ToString() : "";

            string desc = string.IsNullOrEmpty(skill.Description) ? skill.Name : skill.Description;
            string cdText = skill.Cooldown > 0
                ? $"  ⏱ on cooldown ({skill.Cooldown})"
                : $"  CD {skill.TotalCooldown}";
            _buttons[i].TooltipText =
                $"{skill.Name}\n{desc}\n\nMP {skill.ManaCost:F0}{cdText}  •  Range {skill.Range}";
        }
    }

    private void HandlePress(int slot)
    {
        if (_bound is null || slot >= _bound.ActiveSkills.Count) return;
        OnSkillSelected?.Invoke(slot, _bound.ActiveSkills[slot]);
    }
}
