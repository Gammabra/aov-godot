using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.UI.Hud;
using AshesOfVelsingrad.Managers;
using Godot;

namespace AshesOfVelsingrad.UI.Inventory;

/// <summary>
///     Exploration inventory panel: 30-slot global grid on the left,
///     4 unit loadout rows on the right. Sized as a fraction of the viewport
///     so it scales cleanly at any resolution.
/// </summary>
public sealed partial class ExplorationInventoryUI : CanvasLayer
{
    public const int InventoryLayer = 10;

    private bool _built;
    private GridContainer? _exploGrid;
    private VBoxContainer? _unitPanelColumn;
    private readonly List<ExplorationSlotUI> _exploSlots = new();
    private readonly List<UnitTransferPanel> _unitPanels = new();

    public override void _Ready()
    {
        EnsureBuilt();
        BindGlobalInventory();
        RefreshUnitPanels();
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

        // ── Main window: 80% wide, 70% tall, centred ───────────────────
        // Using anchor-based sizing means the panel scales with the viewport
        // instead of being glued to fixed pixel dimensions.
        var window = new Control { Name = "Window" };
        // Anchor to a centred 80×70% rectangle
        window.AnchorLeft   = 0.10f;
        window.AnchorRight  = 0.90f;
        window.AnchorTop    = 0.15f;
        window.AnchorBottom = 0.85f;
        // Zero offsets — the anchors do all the sizing
        window.OffsetLeft   = 0f;
        window.OffsetRight  = 0f;
        window.OffsetTop    = 0f;
        window.OffsetBottom = 0f;
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

        // ── Content row ─────────────────────────────────────────────────
        var contentRow = new HBoxContainer();
        contentRow.AddThemeConstantOverride("separation", 12);
        contentRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outerVBox.AddChild(contentRow);

        // ── LEFT: exploration grid ──────────────────────────────────────
        // No ExpandFill — let it take only as much space as the grid needs.
        var leftVBox = new VBoxContainer();
        leftVBox.AddThemeConstantOverride("separation", 6);
        // ShrinkBegin so it hugs the grid instead of stretching to fill
        leftVBox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        contentRow.AddChild(leftVBox);

        var gridHeader = new Label { Text = "Party Inventory" };
        HudStyle.StyleLabel(gridHeader);
        leftVBox.AddChild(gridHeader);

        _exploGrid = new GridContainer { Columns = InventoryConstants.ExplorationColumns };
        _exploGrid.AddThemeConstantOverride("h_separation", 4);
        _exploGrid.AddThemeConstantOverride("v_separation", 4);
        leftVBox.AddChild(_exploGrid);

        // ── Vertical divider ────────────────────────────────────────────
        var vDivider = new VSeparator();
        vDivider.AddThemeColorOverride("color", HudStyle.PanelBorder);
        contentRow.AddChild(vDivider);

        // ── RIGHT: unit loadout rows ────────────────────────────────────
        // ExpandFill here so the unit side takes the remaining space.
        var rightVBox = new VBoxContainer();
        rightVBox.AddThemeConstantOverride("separation", 6);
        rightVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        contentRow.AddChild(rightVBox);

        var unitHeader = new Label { Text = "Unit Loadouts  ←  drag items here" };
        HudStyle.StyleLabel(unitHeader);
        unitHeader.AddThemeColorOverride("font_color", HudStyle.DimText);
        rightVBox.AddChild(unitHeader);

        var unitSep = new HSeparator();
        unitSep.AddThemeColorOverride("color", HudStyle.PanelBorder);
        rightVBox.AddChild(unitSep);

        _unitPanelColumn = new VBoxContainer();
        _unitPanelColumn.AddThemeConstantOverride("separation", 6);
        _unitPanelColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _unitPanelColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rightVBox.AddChild(_unitPanelColumn);
    }

    // ── Public API ──────────────────────────────────────────────────────

    public void Toggle()
    {
        if (!Visible)
        {
            if (PlayerInventoryManager.Instance is { } mgr)
                RefreshAll(mgr.GlobalInventory);
            RefreshUnitPanels();
        }
        Visible = !Visible;
    }

    /// <summary>
    ///     Rebuild unit rows from PlayerInventoryManager's persistent loadouts.
    ///     Safe to call multiple times (e.g. after a battle updates party names).
    /// </summary>
    public void RefreshUnitPanels()
    {
        if (_unitPanelColumn == null) return;
        if (PlayerInventoryManager.Instance is not { } mgr) return;

        foreach (Node child in _unitPanelColumn.GetChildren()) child.QueueFree();
        _unitPanels.Clear();

        for (int i = 0; i < PlayerInventoryManager.MaxPartySize; i++)
        {
            var panel = new UnitTransferPanel();
            panel.EnsureBuilt();
            // ExpandFill width so rows stretch across the right column,
            // fixed minimum height so each row is always tall enough for the slots.
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;
            panel.CustomMinimumSize   = new Vector2(0f, InventoryConstants.SlotSize + 20f);
            panel.Bind(mgr.PartyNames[i], mgr.PartyLoadouts[i]);
            _unitPanelColumn.AddChild(panel);
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

        mgr.GlobalInventory.SlotChanged += OnGlobalSlotChanged;
        BuildExploSlots(mgr.GlobalInventory);
        RefreshAll(mgr.GlobalInventory);
    }

    private void BuildExploSlots(InventorySystem inventory)
    {
        if (_exploGrid == null) return;

        foreach (Node child in _exploGrid.GetChildren()) child.QueueFree();
        _exploSlots.Clear();

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
        _exploSlots[index].Refresh(mgr.GlobalInventory.GetSlot(index));
    }
}
