// scripts/ui/hud/InventorySlotUI.cs
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

/// <summary>
///     Single inventory slot — label + Use button. Built entirely in code,
///     no .tscn required, matching the BattleHud widget pattern.
/// </summary>
public sealed partial class InventorySlotUI : PanelContainer
{
    private Label? _label;
    private Button? _useButton;
    private int _slotIndex;
    private InventoryUI? _inventoryUI;

    public override void _Ready()
    {
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_label != null) return;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _label = new Label { Text = string.Empty, Visible = false };
        vbox.AddChild(_label);

        _useButton = new Button { Text = "Use", Visible = false };
        _useButton.Pressed += OnUsePressed;
        vbox.AddChild(_useButton);
    }

    public void Setup(int slotIndex, InventoryUI inventoryUI)
    {
        EnsureBuilt(); // may be called before _Ready in the deferred-AddChild path
        _slotIndex = slotIndex;
        _inventoryUI = inventoryUI;
    }

    public void Refresh(IInventorySlot slot)
    {
        EnsureBuilt();
        if (_label == null) return;

        if (slot.IsEmpty)
        {
            _label.Text = string.Empty;
            _label.Visible = false;
            if (_useButton != null) _useButton.Visible = false;
            return;
        }

        _label.Visible = true;
        var item = ItemCatalog.Get(slot.ItemId);
        _label.Text = $"{item.Name} x{slot.Quantity}";
        if (_useButton != null) _useButton.Visible = true;
    }

    private void OnUsePressed()
    {
        _inventoryUI?.NotifyUseItemPressed(_slotIndex);
    }
}
