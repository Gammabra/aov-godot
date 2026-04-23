using Godot;
using AshesOfVelsingrad.Systems;

public partial class InventorySlotUI : PanelContainer
{
    [Export] private Label? _label;
    [Export] private Button? _useButton;

    private int _slotIndex;
    private InventoryUI? _inventoryUI;

    public void Setup(int slotIndex, InventoryUI inventoryUI)
    {
        _slotIndex = slotIndex;
        _inventoryUI = inventoryUI;

        if (_useButton != null)
            _useButton.Pressed += OnUsePressed;
    }

    public void Refresh(InventorySlot slot)
    {
        if (_label == null) return;

        bool isEmpty = slot.IsEmpty;

        if (isEmpty)
            _label.Text = $"[{_slotIndex}] Empty";
        else
        {
            var item = ItemCatalog.Get(slot.ItemId);
            _label.Text = $"[{_slotIndex}] {item.Name} x{slot.Quantity}";
        }

        if (_useButton != null)
            _useButton.Visible = !isEmpty;
    }

    private void OnUsePressed()
    {
        _inventoryUI?.NotifyUseItemPressed(_slotIndex);
    }
}
