using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Full-screen exploration inventory: 30-slot global grid on the left,
///     one <see cref="UnitTransferPanel" /> per player unit on the right.
///     Drag items from the left grid and drop them onto a unit slot.
/// </summary>
public sealed partial class ExplorationInventoryUI : CanvasLayer
{
    public const int InventoryLayer  = 10;

    // Tune in InventoryConstants — no magic numbers here.
    private bool _built;
    private GridContainer? _exploGrid;
    private HBoxContainer? _unitPanelRow;
    private readonly List<ExplorationSlotUI> _exploSlots = new();
    private readonly List<UnitTransferPanel> _unitPanels = new();

    public override void _Ready()
    {
        EnsureBuilt();
        BindGlobalInventory();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("open_inventory"))
            Toggle();
    }

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        Layer = InventoryLayer;
        Visible = false;

        // ── Full-screen dim backdrop ────────────────────────────────────
        var backdrop = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);

        // ── Centred main window ─────────────────────────────────────────
        var window = new Control { Name = "Window" };
        window.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        window.OffsetLeft = -520f;
        window.OffsetRight = 520f;
        window.OffsetTop = -320f;
        window.OffsetBottom = 320f;
        AddChild(window);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 8);
        outerVBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        window.AddChild(HudStyle.MakePanel(outerVBox));

        // ── Title row ───────────────────────────────────────────────────
        var titleRow = new HBoxContainer();
        outerVBox.AddChild(titleRow);

        var title = new Label
        {
            Text = "Inventory",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        HudStyle.StyleLabel(title);
        title.AddThemeFontSizeOverride("font_size", 18);
        titleRow.AddChild(title);

        var closeBtn = new Button { Text = "✕" };
        HudStyle.StyleButton(closeBtn);
        closeBtn.Pressed += Toggle;
        titleRow.AddChild(closeBtn);

        var sep = new HSeparator();
        sep.AddThemeColorOverride("color", HudStyle.PanelBorder);
        outerVBox.AddChild(sep);

        // ── Content row: global grid | unit panels ──────────────────────
        var contentRow = new HBoxContainer();
        contentRow.AddThemeConstantOverride("separation", 12);
        contentRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outerVBox.AddChild(contentRow);

        // Left: exploration grid
        var leftVBox = new VBoxContainer();
        leftVBox.AddThemeConstantOverride("separation", 6);
        leftVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        contentRow.AddChild(leftVBox);

        var gridHeader = new Label { Text = "Party Inventory" };
        HudStyle.StyleLabel(gridHeader);
        leftVBox.AddChild(gridHeader);

        _exploGrid = new GridContainer { Columns = InventoryConstants.ExplorationColumns };
        _exploGrid.AddThemeConstantOverride("h_separation", 4);
        _exploGrid.AddThemeConstantOverride("v_separation", 4);
        leftVBox.AddChild(_exploGrid);

        // Right: unit transfer panels
        var rightVBox = new VBoxContainer();
        rightVBox.AddThemeConstantOverride("separation", 6);
        contentRow.AddChild(rightVBox);

        var unitHeader = new Label { Text = "Unit Loadouts  ←  drag items here" };
        HudStyle.StyleLabel(unitHeader);
        unitHeader.AddThemeColorOverride("font_color", HudStyle.DimText);
        rightVBox.AddChild(unitHeader);

        _unitPanelRow = new HBoxContainer();
        _unitPanelRow.AddThemeConstantOverride("separation", 8);
        rightVBox.AddChild(_unitPanelRow);
    }

    // ── Public API ──────────────────────────────────────────────────────

    public void Toggle()
    {
        if (!Visible)
        {
            // Refresh unit panels every time the screen opens
            // so post-battle loadout changes are reflected
            if (PlayerInventoryManager.Instance is { } mgr)
                RefreshAll(mgr.Inventory);
        }
        Visible = !Visible;
    }

    /// <summary>
    ///     Call once at exploration start (or after a battle) to bind the
    ///     player units so their transfer panels reflect current loadouts.
    /// </summary>
    public void BindUnits(IReadOnlyList<IUnitSystem> playerUnits)
    {
        if (_unitPanelRow == null) return;

        foreach (Node child in _unitPanelRow.GetChildren()) child.QueueFree();
        _unitPanels.Clear();

        foreach (IUnitSystem unit in playerUnits)
        {
            var panel = new UnitTransferPanel();
            panel.EnsureBuilt();
            panel.Bind(unit);
            _unitPanelRow.AddChild(panel);
            _unitPanels.Add(panel);
        }
    }

    // ── Private ─────────────────────────────────────────────────────────

    private void BindGlobalInventory()
    {
        if (PlayerInventoryManager.Instance is not { } mgr)
        {
            GD.PrintErr("ExplorationInventoryUI: PlayerInventoryManager not found.");
            return;
        }

        mgr.Inventory.SlotChanged += OnGlobalSlotChanged;
        BuildExploSlots(mgr.Inventory);
        RefreshAll(mgr.Inventory);
    }

    private void BuildExploSlots(InventorySystem inventory)
    {
        if (_exploGrid == null) return;

        foreach (Node child in _exploGrid.GetChildren()) child.QueueFree();
        _exploSlots.Clear();

        // Always build ExplorationCapacity slots regardless of actual item count
        for (int i = 0; i < InventoryConstants.ExplorationCapacity; i++)
        {
            var slot = new ExplorationSlotUI();
            slot.EnsureBuilt();
            slot.Setup(i, inventory);
            _exploGrid.AddChild(slot);
            _exploSlots.Add(slot);
        }
    }

    private void RefreshAll(InventorySystem inventory)
    {
        for (int i = 0; i < _exploSlots.Count; i++)
            _exploSlots[i].Refresh(inventory.GetSlot(i));
    }

    private void OnGlobalSlotChanged(int index)
    {
        if (PlayerInventoryManager.Instance is not { } mgr) return;
        if (index < 0 || index >= _exploSlots.Count) return;
        _exploSlots[index].Refresh(mgr.Inventory.GetSlot(index));
    }
}
