using System;
using AshesOfVelsingrad.UI.Hud;
using AshesOfVelsingrad.systems.items;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Modal panel listing the shared <see cref="PartyInventory" />.
/// </summary>
/// <remarks>
///     Centred, hidden by default. Toggled via <see cref="Toggle" /> from the
///     <see cref="ActionMenu" />'s "Item" button. Selecting a row raises <see cref="OnItemChosen" />.
///     Width / height scale with the user's UI scale slider.
/// </remarks>
public sealed partial class InventoryPanel : Control, IHudWidget
{
    /// <summary>Fired when the player selects an item to use. Carries the item id.</summary>
    public event Action<string>? OnItemChosen;

    private VBoxContainer? _list;
    private bool _built;

    /// <inheritdoc />
    public override void _Ready()
    {
        EnsureBuilt();
        Visible = false;

        if (PartyInventory.Instance is { } inv)
            inv.InventoryChanged += Refresh;
    }

    /// <summary>Idempotent build — safe to call before <c>_Ready</c> fires.</summary>
    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        BuildLayout();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (PartyInventory.Instance is { } inv)
            inv.InventoryChanged -= Refresh;
    }

    /// <inheritdoc />
    public void Relayout() => ApplyAnchorOffsets();

    private void ApplyAnchorOffsets()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        int halfW = HudStyle.ScaledPx(420) / 2;
        int halfH = HudStyle.ScaledPx(540) / 2;
        OffsetLeft = -halfW;
        OffsetRight = halfW;
        OffsetTop = -halfH;
        OffsetBottom = halfH;
    }

    private void BuildLayout()
    {
        ApplyAnchorOffsets();
        MouseFilter = MouseFilterEnum.Stop;

        VBoxContainer outer = new() { Name = "Outer" };
        outer.AddThemeConstantOverride("separation", HudStyle.PadSm);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer, HudStyle.PanelTier.Heavy));

        HBoxContainer titleRow = new();
        titleRow.AddThemeConstantOverride("separation", HudStyle.PadSm);
        outer.AddChild(titleRow);

        TextureRect icon = new()
        {
            Texture = HudStyle.LoadIcon("inventory"),
            CustomMinimumSize = new Vector2(
                HudStyle.ScaledPx(HudStyle.FontSizeHeader),
                HudStyle.ScaledPx(HudStyle.FontSizeHeader)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleRow.AddChild(icon);

        Label header = new()
        {
            Text = "INVENTORY",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        HudStyle.StyleHeader(header, HudStyle.FontSizeHeader);
        titleRow.AddChild(header);

        ColorRect rule = new()
        {
            Color = HudStyle.BronzeDim,
            CustomMinimumSize = new Vector2(0, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        outer.AddChild(rule);

        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        outer.AddChild(scroll);

        _list = new VBoxContainer { Name = "ItemList", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", HudStyle.PadXs);
        scroll.AddChild(_list);

        Button close = new()
        {
            Text = "Close",
            CustomMinimumSize = new Vector2(0, HudStyle.ScaledPx(HudStyle.ButtonHeight - 8)),
        };
        HudStyle.StyleButton(close, HudStyle.FontSizeSub);
        close.Pressed += () => Visible = false;
        outer.AddChild(close);
    }

    /// <summary>Open or close the panel. Refreshes contents on open.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) Refresh();
    }

    /// <summary>Re-read the inventory and rebuild the list.</summary>
    public void Refresh()
    {
        if (_list is null) return;
        foreach (Node child in _list.GetChildren())
            child.QueueFree();

        PartyInventory? inv = PartyInventory.Instance;
        ItemRegistry? reg = ItemRegistry.Instance;
        if (inv is null || reg is null) return;

        if (inv.Items.Count == 0)
        {
            Label empty = new() { Text = "(no items)", HorizontalAlignment = HorizontalAlignment.Center };
            HudStyle.StyleLabel(empty);
            empty.AddThemeColorOverride("font_color", HudStyle.ParchmentDim);
            _list.AddChild(empty);
            return;
        }

        foreach ((string itemId, int count) in inv.Items)
        {
            ItemDefinition? def = reg.GetDefinition(itemId);
            string label = def is null ? itemId : def.DisplayName;

            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", HudStyle.PadMd);
            _list.AddChild(row);

            Label name = new() { Text = $"{label} ×{count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            HudStyle.StyleLabel(name, HudStyle.FontSizeBody);
            row.AddChild(name);

            Button use = new()
            {
                Text = "Use",
                CustomMinimumSize = new Vector2(HudStyle.ScaledPx(80), 0),
            };
            HudStyle.StyleButton(use, HudStyle.FontSizeSmall);
            string id = itemId;
            use.Pressed += () =>
            {
                OnItemChosen?.Invoke(id);
                Visible = false;
            };
            row.AddChild(use);
        }
    }
}
