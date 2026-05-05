using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     One slot in the 30-slot exploration inventory.
///     Supports Godot drag-and-drop — drag starts here, drop lands on
///     <see cref="UnitSlotUI" />.
/// </summary>
public sealed partial class ExplorationSlotUI : PanelContainer
{
    private Label? _nameLabel;
    private Label? _qtyLabel;
    private bool _built;

    public int SlotIndex { get; private set; }
    public InventorySystem? Source { get; private set; }

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        CustomMinimumSize = new Vector2(InventoryConstants.SlotSize, InventoryConstants.SlotSize);
        AddThemeStyleboxOverride("panel", HudStyle.MakePanelStyle());
        MouseFilter = MouseFilterEnum.Stop;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(vbox);

        _nameLabel = new Label
        {
            Text = string.Empty,
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        HudStyle.StyleLabel(_nameLabel);
        _nameLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_nameLabel);

        _qtyLabel = new Label { Visible = false };
        HudStyle.StyleLabel(_qtyLabel);
        _qtyLabel.AddThemeColorOverride("font_color", HudStyle.DimText);
        _qtyLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_qtyLabel);
    }

    public void Setup(int index, InventorySystem source)
    {
        EnsureBuilt();
        SlotIndex = index;
        Source = source;
    }

    public void Refresh(IInventorySlot slot)
    {
        EnsureBuilt(); // guarantee labels exist even if _Ready hasn't fired yet

        if (slot.IsEmpty)
        {
            if (_nameLabel != null) { _nameLabel.Text = string.Empty; _nameLabel.Visible = false; }
            if (_qtyLabel != null) { _qtyLabel.Visible = false; }
            return;
        }

        if (!ItemCatalog.TryGet(slot.ItemId, out var item))
        {
            // Item id present but not registered — treat as empty rather than crash
            GD.PrintErr($"ExplorationSlotUI: unknown item id {slot.ItemId} in slot {SlotIndex}");
            if (_nameLabel != null) { _nameLabel.Text = "?"; _nameLabel.Visible = true; }
            return;
        }

        if (_nameLabel != null) { _nameLabel.Text = item.Name ?? string.Empty; _nameLabel.Visible = true; }
        if (_qtyLabel != null) { _qtyLabel.Text = $"x{slot.Quantity}"; _qtyLabel.Visible = slot.Quantity > 1; }
    }

    // ── Godot drag ─────────────────────────────────────────────────────

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Source == null) return default;
        var slot = Source.GetSlot(SlotIndex);
        if (slot.IsEmpty) return default;

        // Visual ghost shown under the cursor while dragging
        var ghost = new Label { Text = ItemCatalog.Get(slot.ItemId).Name ?? "?" };
        HudStyle.StyleLabel(ghost);
        SetDragPreview(ghost);

        // Payload: a simple Godot Dictionary so it crosses the C#/GDScript boundary safely
        var payload = new Godot.Collections.Dictionary
        {
            ["from_slot"] = SlotIndex,
            ["item_id"]   = slot.ItemId,
            ["quantity"]  = slot.Quantity,
        };
        return payload;
    }
}
