using System;
using AshesOfVelsingrad.Systems;
using Godot;

namespace AshesOfVelsingrad.UI.Hud;

/// <summary>
///     Five-slot skill bar matching <see cref="BattleInputSystem" />'s hot-keys
///     (<c>battle_select_skill1</c> .. <c>battle_select_skill5</c>).
/// </summary>
/// <remarks>
///     <para>
///         Reads from the active player unit's <see cref="IUnitSystem.ActiveSkills" /> list.
///         Each slot's button is disabled when the skill is on cooldown or the caster has
///         insufficient mana. Hover tooltips carry the full description / MP / CD / Range.
///     </para>
///     <para>
///         The widget is purely presentational — it raises <see cref="OnSkillSelected" /> with
///         the slot index and the resolved skill, and the consumer (<c>GameManager</c>) decides
///         what to do (enter targeting, etc.).
///     </para>
/// </remarks>
public sealed partial class SkillSelector : Control
{
    /// <summary>Number of slots, fixed at 5.</summary>
    public const int SlotCount = 5;

    /// <summary>Fired when a slot is clicked. Carries the slot index (0-4) and the skill.</summary>
    public event Action<int, ISkillSystem>? OnSkillSelected;

    private readonly Button[] _buttons = new Button[SlotCount];
    private IUnitSystem? _bound;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready()
    {
        EnsureBuilt();
    }

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        OffsetLeft = -360;
        OffsetRight = 360;
        OffsetTop = -134;
        OffsetBottom = -76;
        MouseFilter = MouseFilterEnum.Ignore;

        Control panelContent = new() { MouseFilter = MouseFilterEnum.Ignore };
        panelContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
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
                CustomMinimumSize = new Vector2(60, 48),
                Disabled = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
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
    ///     Bind the selector to the unit whose loadout to display. Refreshes immediately.
    ///     Also hides the widget entirely on non-player turns — the player can only act on
    ///     <see cref="Faction.Player" /> units, so showing AI-driven skills would be misleading.
    /// </summary>
    /// <param name="unit">Unit to track, or null to clear.</param>
    public void Bind(IUnitSystem? unit)
    {
        _bound = unit;
        // Skill bar is only meaningful when a player-controlled unit is acting.
        Visible = unit is not null && unit.Faction == Faction.Player;
        Refresh();
    }

    /// <summary>Re-read the bound unit's skills and refresh slot text + disabled state.</summary>
    public void Refresh()
    {
        // Guard: BuildLayout populates _buttons[]; if Bind() is called before _Ready fires
        // (e.g. on the very first turn while still inside InitializeGameManager) the array
        // entries are still null. Skip — the deferred RefreshHudOnReady re-runs Bind once
        // the widget has built itself.
        if (_buttons[0] is null) return;

        if (_bound is null)
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
            ISkillSystem? skill = i < _bound.ActiveSkills.Count ? _bound.ActiveSkills[i] : null;
            if (skill is null)
            {
                _buttons[i].Text = $"{i + 1}. —";
                _buttons[i].Disabled = true;
                _buttons[i].TooltipText = "(empty slot)";
                continue;
            }

            _buttons[i].Text = $"{i + 1}. {skill.Name}";
            _buttons[i].Disabled = skill.Cooldown > 0 || skill.ManaCost > _bound.Mana;

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
