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
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        root.OffsetLeft = -300f;
        root.OffsetRight = 300f;
        root.OffsetTop = -180f;
        root.OffsetBottom = -80f;
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        root.AddChild(HudStyle.MakePanel(vbox));

        // Title row
        var titleRow = new HBoxContainer();
        vbox.AddChild(titleRow);

        var title = new Label { Text = "Unit Inventory", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        HudStyle.StyleLabel(title);
        title.AddThemeFontSizeOverride("font_size", 16);
        titleRow.AddChild(title);

        var closeBtn = new Button { Text = "✕" };
        HudStyle.StyleButton(closeBtn);
        closeBtn.AddThemeColorOverride("font_color", HudStyle.DimText);
        closeBtn.Pressed += Toggle;
        titleRow.AddChild(closeBtn);

        // Divider
        var sep = new HSeparator();
        sep.AddThemeColorOverride("color", HudStyle.PanelBorder);
        vbox.AddChild(sep);

        // Slot row — BattleCapacity slots in a single horizontal strip
        _slotRow = new HBoxContainer();
        _slotRow.AddThemeConstantOverride("separation", 6);
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
