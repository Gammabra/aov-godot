using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     Bundles three world-space tile overlays — move (green), skill target (red),
///     hover (yellow) — under a single facade.
/// </summary>
/// <remarks>
///     <para>
///         Spawn one as a child of the <see cref="MapSystem" /> and call
///         <see cref="Initialize" /> once. From then on, callers use the high-level methods
///         (<see cref="ShowMoveTiles" />, <see cref="ShowTargetTiles" />, <see cref="ShowHover" />,
///         <see cref="HideAll" />) without juggling three mesh pools.
///     </para>
///     <para>
///         <see cref="RaycastCell" /> is colocated here because every caller that asks for a
///         hover update needs both the camera ray and the indicator update.
///     </para>
/// </remarks>
public sealed partial class IndicatorOverlay : Node3D
{
    private MoveIndicator? _move;
    private MoveIndicator? _target;
    private MoveIndicator? _hover;
    private MapSystem? _map;

    /// <summary>The map this overlay was bound to.</summary>
    public MapSystem? Map => _map;

    /// <summary>
    ///     Spawn the three child indicators and bind them to <paramref name="map" />.
    /// </summary>
    /// <param name="map">The active battle map.</param>
    public void Initialize(MapSystem map)
    {
        _map = map;

        _move = new MoveIndicator { Name = "MoveIndicator" };
        AddChild(_move);
        _move.Bind(map);

        _target = new MoveIndicator
        {
            Name = "TargetIndicator",
            TileColor = new Color(0.95f, 0.30f, 0.30f, 0.55f),
        };
        AddChild(_target);
        _target.Bind(map);

        _hover = new MoveIndicator
        {
            Name = "HoverIndicator",
            TileColor = new Color(0.95f, 0.85f, 0.30f, 0.65f),
            Height = 0.08f, // sit above the move/target so it's always visible
        };
        AddChild(_hover);
        _hover.Bind(map);
    }

    /// <summary>Show green tiles for valid move destinations. Hides the target overlay.</summary>
    /// <param name="tiles">Cells to highlight.</param>
    public void ShowMoveTiles(IReadOnlyList<(int X, int Y, int Z)> tiles)
    {
        _move?.Show(tiles);
        _target?.Hide();
    }

    /// <summary>Show red tiles for valid skill targets. Hides the move overlay.</summary>
    /// <param name="tiles">Cells to highlight.</param>
    public void ShowTargetTiles(IReadOnlyList<(int X, int Y, int Z)> tiles)
    {
        _target?.Show(tiles);
        _move?.Hide();
    }

    /// <summary>Light a single yellow tile for hover feedback. Pass <c>null</c> to clear.</summary>
    /// <param name="cell">Cell to highlight, or null to hide.</param>
    public void ShowHover((int X, int Y, int Z)? cell)
    {
        if (_hover is null) return;
        if (cell is null) { _hover.Hide(); return; }
        _hover.Show([cell.Value]);
    }

    /// <summary>Hide all three overlays at once.</summary>
    public void HideAll()
    {
        _move?.Hide();
        _target?.Hide();
        _hover?.Hide();
    }

    /// <summary>
    ///     Cast a ray from <paramref name="camera" /> through <paramref name="screenPos" /> and
    ///     return the cell it hits.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    /// <param name="screenPos">Mouse position in viewport coordinates.</param>
    /// <returns>The cell or null when the ray missed any collider.</returns>
    public Vector3I? RaycastCell(Camera3D camera, Vector2 screenPos)
    {
        if (_map is null) return null;

        Vector3 from = camera.ProjectRayOrigin(screenPos);
        Vector3 dir = camera.ProjectRayNormal(screenPos);
        Vector3 to = from + dir * 2000f;

        PhysicsDirectSpaceState3D space = camera.GetWorld3D().DirectSpaceState;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);
        Godot.Collections.Dictionary? result = space.IntersectRay(query);
        if (result is null || result.Count == 0 || !result.TryGetValue("position", out Variant posV))
            return null;

        return _map.LocalToMap((Vector3)posV);
    }
}
