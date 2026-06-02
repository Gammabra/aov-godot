using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

public sealed partial class UnitSlotUI : PanelContainer
{
    [Export] private Label _nameLabel = null!;
    [Export] private Label _qtyLabel = null!;

    public int SlotIndex { get; private set; }
    public InventorySystem? Target { get; private set; }

    public override void _Ready()
    {
        RefreshBorder(false);
        MouseEntered += () => RefreshBorder(true);
        MouseExited += () => RefreshBorder(false);
    }

    public void Setup(int index, InventorySystem target)
    {
        SlotIndex = index;
        Target = target;
    }

    public void Refresh(IInventorySlot slot)
    {
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_qtyLabel) || !GodotObject.IsInstanceValid(_nameLabel))
            return;

        bool empty = slot.IsEmpty;
        _nameLabel.Visible = !empty;
        if (!empty) 
            _nameLabel.Text = ItemCatalog.Get(slot.ItemId).Name ?? string.Empty;

        _qtyLabel.Visible = !empty && slot.Quantity > 1;
        if (!empty) 
            _qtyLabel.Text = $"x{slot.Quantity}";
    }

    // ── Drag & Drop ─────────────────────────────────────────────────────

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Target == null) return default;
        var slot = Target.GetSlot(SlotIndex);
        if (slot.IsEmpty) return default;

        var ghost = new Label { Text = ItemCatalog.Get(slot.ItemId).Name ?? "?" };
        HudStyle.StyleLabel(ghost);
        SetDragPreview(ghost);

        return new Godot.Collections.Dictionary
        {
            ["source"] = "unit",
            ["from_slot"] = SlotIndex,
            ["item_id"] = slot.ItemId,
            ["quantity"] = slot.Quantity,
            ["from_inv_id"] = GetInstanceId(),
        };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (Target == null || data.VariantType != Variant.Type.Dictionary) return false;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar)) return false;

        var slot = Target.GetSlot(SlotIndex);
        return slot.IsEmpty || slot.ItemId == idVar.AsInt32();
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (Target == null) return;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar) || !dict.TryGetValue("from_slot", out var fromSlotVar)) return;

        int itemId = idVar.AsInt32();
        int fromSlot = fromSlotVar.AsInt32();
        bool fromUnit = dict.TryGetValue("source", out var srcVar) && srcVar.AsString() == "unit";

        if (fromUnit)
        {
            if (PlayerInventoryManager.Instance is not { } mgr) return;
            InventorySystem? sourceInv = null;
            
            foreach (var loadout in mgr.PartyLoadouts)
            {
                if (ReferenceEquals(loadout, Target)) continue;
                if (loadout.GetSlot(fromSlot).ItemId == itemId)
                {
                    sourceInv = loadout;
                    break;
                }
            }

            if (sourceInv == null || ReferenceEquals(sourceInv, Target)) return;
            
            bool removed = sourceInv.RemoveItem(itemId, 1);
            if (!removed) return;
            
            int leftover = Target.AddItem(itemId, 1);
            if (leftover > 0) sourceInv.AddItem(itemId, leftover);
        }
        else
        {
            bool removed = PlayerInventoryManager.Instance?.GlobalInventory.RemoveItem(itemId, 1) ?? false;
            if (!removed) return;
            
            int leftover = Target.AddItem(itemId, 1);
            if (leftover > 0)
                PlayerInventoryManager.Instance?.GlobalInventory.AddItem(itemId, leftover);
        }
    }

    private void RefreshBorder(bool hover)
    {
        var style = HudStyle.MakePanelStyle();
        if (hover) style.BorderColor = HudStyle.PlayerColor;
        AddThemeStyleboxOverride("panel", style);
    }
}
