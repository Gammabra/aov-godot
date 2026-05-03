using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Bundles the three world-space indicators (move tiles, skill targets, hover) under one
///     facade so callers don't have to juggle three <see cref="MoveIndicator" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Spawned by <c>GameManager</c> as a child of the active <see cref="MapSystem" />.
///         All three sub-indicators share the map and a single ray-cast helper for hover.
///     </para>
///     <para>
///         <see cref="ShowMoveTiles" /> highlights green move tiles. <see cref="ShowTargetTiles" />
///         highlights red enemy tiles. <see cref="ShowHover" /> draws a single yellow tile.
///         <see cref="HideAll" /> clears every overlay at once.
///     </para>
/// </remarks>
public sealed partial class IndicatorOverlay : Node3D
{
    private MoveIndicator? _move;
    private MoveIndicator? _target;
    private MoveIndicator? _hover;
    private MapSystem? _map;

    /// <summary>The map this overlay is bound to (set by <see cref="Initialize" />).</summary>
    public MapSystem? Map => _map;

    /// <summary>
    ///     Spawn the three child indicators and bind them to <paramref name="map" />.
    /// </summary>
    /// <param name="map">Active battle map.</param>
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
            Height = 0.08f, // sit slightly above the move/target tiles so it stays visible
        };
        AddChild(_hover);
        _hover.Bind(map);
    }

    /// <summary>Show green move-tile overlays. Hides the target overlay automatically.</summary>
    /// <param name="tiles">Cell coordinates the unit can reach.</param>
    public void ShowMoveTiles(IReadOnlyList<(int X, int Y, int Z)> tiles)
    {
        _move?.Show(tiles);
        _target?.Hide();
    }

    /// <summary>Show red overlays on every alive enemy unit. Hides the move overlay automatically.</summary>
    /// <param name="enemies">Hostile units to highlight.</param>
    public void ShowTargetTiles(IEnumerable<UnitSystem> enemies)
    {
        if (_target is null || _map is null) return;
        List<(int, int, int)> tiles = [];
        foreach (UnitSystem enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            (int, int, int)? pos;
            try
            {
                pos = _map.GetUnitPosition(enemy);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                continue;
            }
            if (pos is { } p) tiles.Add(p);
        }
        _target.Show(tiles);
        _move?.Hide();
    }

    /// <summary>Light a single yellow tile for hover feedback. Pass null to clear.</summary>
    /// <param name="cell">Cell to highlight; null hides the indicator.</param>
    public void ShowHover((int X, int Y, int Z)? cell)
    {
        if (_hover is null) return;
        if (cell is null) { _hover.Hide(); return; }
        _hover.Show([cell.Value]);
    }

    /// <summary>Hide every overlay (move, target, hover).</summary>
    public void HideAll()
    {
        _move?.Hide();
        _target?.Hide();
        _hover?.Hide();
    }

    /// <summary>
    ///     Cast a ray from the camera through <paramref name="screenPos" /> and return the cell hit.
    /// </summary>
    /// <param name="camera">Active 3D camera.</param>
    /// <param name="screenPos">Mouse position in viewport space.</param>
    /// <returns>The cell coords or null if the ray missed everything.</returns>
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
