using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

public sealed partial class BattleSlotUI : PanelContainer
{
    [Export] private Label _nameLabel = null!;
    [Export] private Label _qtyLabel = null!;
    [Export] private Button _useButton = null!;

    private int _slotIndex;
    private BattleInventoryUI? _owner;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", HudStyle.MakePanelStyle());
        
        if (_useButton != null)
            _useButton.Pressed += OnUsePressed;
    }

    public void Setup(int slotIndex, BattleInventoryUI owner)
    {
        _slotIndex = slotIndex;
        _owner = owner;
    }

    public void Refresh(IInventorySlot slot)
    {
        bool empty = slot.IsEmpty;

        _nameLabel.Visible = !empty;
        if (!empty)
        {
            if (!ItemCatalog.TryGet(slot.ItemId, out var item))
            {
                GD.PrintErr($"Unknown item id {slot.ItemId}");
                return;
            }
            _nameLabel.Text = item.Name ?? string.Empty;
            _nameLabel.AddThemeColorOverride("font_color", HudStyle.TextColor);
        }

        _qtyLabel.Visible = !empty && slot.Quantity > 1;
        if (!empty) 
            _qtyLabel.Text = $"x{slot.Quantity}";

        _useButton.Visible = !empty;
    }

    private void OnUsePressed() => _owner?.NotifyUseItemPressed(_slotIndex);
}
