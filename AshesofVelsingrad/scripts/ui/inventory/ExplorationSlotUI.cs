using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

public sealed partial class ExplorationSlotUI : PanelContainer
{
    [Export] private Label _nameLabel = null!;
    [Export] private Label _qtyLabel = null!;

    public int SlotIndex { get; private set; }
    public InventorySystem? Source { get; private set; }

    public override void _Ready()
    {
        // Dynamic procedural styling can stay here if not using a theme file yet
        AddThemeStyleboxOverride("panel", HudStyle.MakePanelStyle());
    }

    public void Setup(int index, InventorySystem source)
    {
        SlotIndex = index;
        Source = source;
    }

    public void Refresh(IInventorySlot slot)
    {
        if (slot.IsEmpty)
        {
            _nameLabel.Text = string.Empty;
            _nameLabel.Visible = false;
            _qtyLabel.Visible = false;
            return;
        }

        if (!ItemCatalog.TryGet(slot.ItemId, out var item))
        {
            GD.PrintErr($"ExplorationSlotUI: unknown item id {slot.ItemId} in slot {SlotIndex}");
            _nameLabel.Text = "?";
            _nameLabel.Visible = true;
            _qtyLabel.Visible = false;
            return;
        }

        _nameLabel.Text = item.Name ?? string.Empty;
        _nameLabel.Visible = true;

        _qtyLabel.Text = $"x{slot.Quantity}";
        _qtyLabel.Visible = slot.Quantity > 1;
    }

    // ── Godot Drag & Drop Layout ────────────────────────────────────────

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Source == null) return default;
        var slot = Source.GetSlot(SlotIndex);
        if (slot.IsEmpty) return default;

        var ghost = new Label { Text = ItemCatalog.Get(slot.ItemId).Name ?? "?" };
        HudStyle.StyleLabel(ghost);
        SetDragPreview(ghost);

        return new Godot.Collections.Dictionary
        {
            ["from_slot"] = SlotIndex,
            ["item_id"] = slot.ItemId,
            ["quantity"] = slot.Quantity,
        };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dict = data.AsGodotDictionary();
        return dict.TryGetValue("source", out var src) && src.AsString() == "unit";
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (PlayerInventoryManager.Instance is not { } mgr) return;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar) || !dict.TryGetValue("from_slot", out var fromVar)) return;

        int itemId = idVar.AsInt32();
        int fromSlot = fromVar.AsInt32();

        foreach (var loadout in mgr.PartyLoadouts)
        {
            if (loadout.GetSlot(fromSlot).ItemId != itemId) continue;
            bool removed = loadout.RemoveItem(itemId, 1);
            if (removed) mgr.GlobalInventory.AddItem(itemId, 1);
            break;
        }
    }
}
