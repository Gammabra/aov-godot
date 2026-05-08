using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Battle-phase inventory overlay. Shows the active unit's
///     <see cref="InventoryConstants.BattleCapacity" /> slots.
///     Spawned by <c>GameManager</c>, follows the BattleHud pattern.
/// </summary>
public sealed partial class BattleInventoryUI : CanvasLayer
{
    public const int InventoryLayer = 101;

    private bool _built;
    private HBoxContainer? _slotRow;
    private BattleInputSystem? _battleInputSystem;
    private InventorySystem? _inventory;
    private readonly List<BattleInventorySlotUI> _slots = new();

    public override void _Ready() => EnsureBuilt();

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        Layer = InventoryLayer;
        Visible = false;

        // ── Outer anchor: centre-bottom, above ActionMenu ──────────────
        var root = new Control { Name = "Root" };
        root.AnchorLeft   = 0.5f;
        root.AnchorRight  = 0.5f;
        root.AnchorTop    = 1.0f;
        root.AnchorBottom = 1.0f;
        root.OffsetLeft   = -200f;
        root.OffsetRight  =  200f;
        root.OffsetTop    = -134f;
        root.OffsetBottom =  -76f;
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(HudStyle.MakePanel(vbox));

        // Small label so the player knows what they're looking at
        var header = new Label { Text = "Items" };
        HudStyle.StyleLabel(header);
        header.AddThemeColorOverride("font_color", HudStyle.DimText);
        header.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(header);

        _slotRow = new HBoxContainer();
        _slotRow.AddThemeConstantOverride("separation", 4);
        _slotRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
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

    // ── Public API ─────────────────────────────────────────────────────

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

    // ── Private ────────────────────────────────────────────────────────

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
