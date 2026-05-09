using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Battle-phase inventory panel. Lives inside <see cref="BattleHud" />
///     as a plain Control so it shares the same coordinate space as
///     <see cref="SkillSelector" /> and <see cref="ActionMenu" />.
/// </summary>
public sealed partial class BattleInventoryUI : Control
{
    private bool _built;
    private HBoxContainer? _slotRow;
    private BattleInputSystem? _battleInputSystem;
    private InventorySystem? _inventory;
    private readonly List<BattleInventorySlotUI> _slots = new();

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built)
            return;

        _built = true;

        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        OffsetLeft   = -180f;  // match ActionMenu width exactly
        OffsetRight  =  180f;
        OffsetTop    = -134f;  // same as SkillSelector top
        OffsetBottom =  -72f;  // just touching the top of ActionMenu (-64 + some margin)
        MouseFilter  = MouseFilterEnum.Ignore;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(HudStyle.MakePanel(vbox));

        _slotRow = new HBoxContainer();
        _slotRow.AddThemeConstantOverride("separation", 4);
        _slotRow.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(_slotRow);

        for (int i = 0; i < InventoryConstants.BattleCapacity; i++)
        {
            var slot = new BattleInventorySlotUI();
            slot.EnsureBuilt();
            slot.Setup(i, this);
            _slotRow.AddChild(slot);
            _slots.Add(slot);
        }
    }

    public void SetBattleInputSystem(BattleInputSystem bis) => _battleInputSystem = bis;

    public void BindInventory(InventorySystem inventory)
    {
        if (_inventory != null) _inventory.SlotChanged -= OnSlotChanged;
        _inventory = inventory;
        _inventory.SlotChanged += OnSlotChanged;
        RefreshAll();
    }

    public void NotifyUseItemPressed(int slotIndex)
    {
        _battleInputSystem?.EmitSignal(BattleInputSystem.SignalName.OnUseItemPressed, slotIndex);
        Toggle();
    }

    public void Toggle() => Visible = !Visible;

    private void RefreshAll()
    {
        if (_inventory == null) return;
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Refresh(_inventory.GetSlot(i));
    }

    private void OnSlotChanged(int index)
    {
        if (_inventory == null || index < 0 || index >= _slots.Count) return;
        _slots[index].Refresh(_inventory.GetSlot(index));
    }
}
