using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     One of the <see cref="InventoryConstants.BattleCapacity" /> slots shown per unit
///     in the exploration transfer screen. Accepts drops from <see cref="ExplorationSlotUI" />.
/// </summary>
public sealed partial class UnitSlotUI : PanelContainer
{
    private Label? _nameLabel;
    private Label? _qtyLabel;
    private bool _built;

    public int SlotIndex { get; private set; }
    public InventorySystem? Target { get; private set; }

    /// <summary>Raised after a successful drop so the parent panel can react.</summary>

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        CustomMinimumSize = new Vector2(InventoryConstants.SlotSize, InventoryConstants.SlotSize);
        MouseFilter = MouseFilterEnum.Stop;
        RefreshBorder(false);

        MouseEntered += () => RefreshBorder(true);
        MouseExited += () => RefreshBorder(false);

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

    public void Setup(int index, InventorySystem target)
    {
        EnsureBuilt();
        SlotIndex = index;
        Target = target;
    }

    public void Refresh(IInventorySlot slot)
    {
        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_qtyLabel))
            return;

        EnsureBuilt();
        bool empty = slot.IsEmpty;
        if (_nameLabel != null)
        {
            _nameLabel.Visible = !empty;
            if (!empty) _nameLabel.Text = ItemCatalog.Get(slot.ItemId).Name ?? string.Empty;
        }
        if (_qtyLabel != null)
        {
            _qtyLabel.Visible = !empty && slot.Quantity > 1;
            if (!empty) _qtyLabel.Text = $"x{slot.Quantity}";
        }
    }

    // ── Godot drop ─────────────────────────────────────────────────────

    // Add to UnitSlotUI — drag OUT of a unit slot:
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Target == null) return default;
        var slot = Target.GetSlot(SlotIndex);
        if (slot.IsEmpty) return default;

        var ghost = new Label { Text = ItemCatalog.Get(slot.ItemId).Name ?? "?" };
        HudStyle.StyleLabel(ghost);
        SetDragPreview(ghost);

        // Tag the source as a unit loadout slot so _DropData can distinguish it
        return new Godot.Collections.Dictionary
        {
            ["source"] = "unit",
            ["from_slot"] = SlotIndex,
            ["item_id"] = slot.ItemId,
            ["quantity"] = slot.Quantity,
            ["from_inv_id"] = GetInstanceId(), // used to identify which UnitSlotUI dragged
        };
    }

    // Replace _CanDropData and _DropData:
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (Target == null) return false;
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar)) return false;

        var slot = Target.GetSlot(SlotIndex);
        if (slot.IsEmpty) return true;
        return slot.ItemId == idVar.AsInt32(); // allow stacking
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (Target == null) return;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar)) return;
        if (!dict.TryGetValue("from_slot", out var fromSlotVar)) return;

        int itemId = idVar.AsInt32();
        int fromSlot = fromSlotVar.AsInt32();
        bool fromUnit = dict.TryGetValue("source", out var srcVar) && srcVar.AsString() == "unit";

        if (fromUnit)
        {
            // Drag from another unit slot — find the source inventory via instance id
            if (!dict.TryGetValue("from_inv_id", out var idInst)) return;
            ulong instId = idInst.AsUInt64();

            // Walk all party loadouts to find which one owns that UnitSlotUI instance
            if (PlayerInventoryManager.Instance is not { } mgr) return;
            InventorySystem? sourceInv = null;
            foreach (var loadout in mgr.PartyLoadouts)
            {
                // Check if this is the same inventory reference the dragged slot uses
                if (ReferenceEquals(loadout, Target)) continue; // skip self
                // We stored the UnitSlotUI's instance id — find the slot UI and get its Target
                // Simpler: just remove from the loadout that has the item in that slot
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
            if (leftover > 0) sourceInv.AddItem(itemId, leftover); // put back if full
        }
        else
        {
            // Drag from global exploration inventory
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
