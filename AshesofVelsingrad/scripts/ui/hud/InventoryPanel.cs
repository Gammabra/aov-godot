using System;
using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.ui.hud;

/// <summary>
///     Modal panel listing the shared <see cref="PartyInventory" />.
/// </summary>
/// <remarks>
///     Centred 380×500 panel, hidden by default. Toggled via <see cref="Toggle" /> from the
///     <see cref="ActionMenu" />'s "Item" button. Selecting a row raises <see cref="OnItemChosen" />.
/// </remarks>
public sealed partial class InventoryPanel : Control
{
    /// <summary>Fired when the player selects an item to use. Carries the item id.</summary>
    public event Action<string>? OnItemChosen;

    private VBoxContainer? _list;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildLayout();
        Visible = false;

        if (PartyInventory.Instance is { } inv)
            inv.InventoryChanged += Refresh;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (PartyInventory.Instance is { } inv)
            inv.InventoryChanged -= Refresh;
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        OffsetLeft = -190;
        OffsetRight = 190;
        OffsetTop = -250;
        OffsetBottom = 250;
        // Modal: when visible, it should catch input. When hidden, Visible=false stops events.
        MouseFilter = MouseFilterEnum.Stop;

        VBoxContainer outer = new() { Name = "Outer" };
        outer.AddThemeConstantOverride("separation", 6);
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(outer));

        Label header = new() { Text = "Inventory", HorizontalAlignment = HorizontalAlignment.Center };
        HudStyle.StyleLabel(header);
        outer.AddChild(header);

        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        outer.AddChild(scroll);

        _list = new VBoxContainer { Name = "ItemList", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_list);

        Button close = new() { Text = "Close" };
        HudStyle.StyleButton(close);
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
            empty.AddThemeColorOverride("font_color", HudStyle.DimText);
            _list.AddChild(empty);
            return;
        }

        foreach ((string itemId, int count) in inv.Items)
        {
            ItemDefinition? def = reg.GetDefinition(itemId);
            string label = def is null ? itemId : def.DisplayName;

            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 8);
            _list.AddChild(row);

            Label name = new() { Text = $"{label} ×{count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            HudStyle.StyleLabel(name);
            row.AddChild(name);

            Button use = new() { Text = "Use" };
            HudStyle.StyleButton(use);
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
