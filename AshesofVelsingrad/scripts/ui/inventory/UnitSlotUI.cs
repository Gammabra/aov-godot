using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using AshesOfVelsingrad.Managers;
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
    public event System.Action<int, int>? OnItemDropped; // (fromExploSlot, toUnitSlot)

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

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (Target == null) return false;
        if (data.VariantType != Variant.Type.Dictionary) return false;
        // Only accept if this unit slot is empty or has the same item (stackable)
        var slot = Target.GetSlot(SlotIndex);
        if (slot.IsEmpty) return true;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("item_id", out var idVar)) return false;
        return slot.ItemId == idVar.AsInt32(); // allow stacking same item
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (Target == null) return;
        var dict = data.AsGodotDictionary();
        if (!dict.TryGetValue("from_slot", out var fromVar)) return;
        if (!dict.TryGetValue("item_id", out var idVar)) return;

        int fromSlot = fromVar.AsInt32();
        int itemId = idVar.AsInt32();

        // Move one unit from exploration inventory → this unit slot
        bool removed = PlayerInventoryManager.Instance?.Inventory.RemoveItem(itemId, 1) ?? false;
        if (!removed) return;

        int leftover = Target.AddItem(itemId, 1);
        if (leftover > 0)
        {
            // Unit inventory full — give back to global
            PlayerInventoryManager.Instance?.Inventory.AddItem(itemId, leftover);
        }

        OnItemDropped?.Invoke(fromSlot, SlotIndex);
    }

    private void RefreshBorder(bool hover)
    {
        var style = HudStyle.MakePanelStyle();
        if (hover) style.BorderColor = HudStyle.PlayerColor;
        AddThemeStyleboxOverride("panel", style);
    }
}
