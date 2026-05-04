using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.Systems.Battle;

/// <summary>
///     World-space overlay that draws a translucent quad above each cell in a list.
/// </summary>
/// <remarks>
///     <para>
///         Used for move-tile, skill-target and hover overlays. A pool of mesh instances is
///         maintained internally and reused between calls so the indicator can be flipped on
///         and off many times per turn without alloc churn.
///     </para>
///     <para>
///         Bind once with <see cref="Bind" /> after the map exists, then call <see cref="Show" />
///         with the cells to highlight or <see cref="Hide" /> to clear.
///     </para>
/// </remarks>
public partial class MoveIndicator : Node3D
{
    /// <summary>Translucent fill colour. Defaults to a friendly green.</summary>
    [Export]
    public Color TileColor { get; set; } = new(0.30f, 0.95f, 0.55f, 0.50f);

    /// <summary>Vertical offset above the cell so the quad isn't z-fighting with the floor.</summary>
    [Export]
    public float Height { get; set; } = 0.05f;

    private readonly List<MeshInstance3D> _pool = [];
    private MapSystem? _map;
    private StandardMaterial3D? _material;

    /// <summary>
    ///     Attach the indicator to a map. Must be called before <see cref="Show" />.
    /// </summary>
    /// <param name="map">The active <see cref="MapSystem" />.</param>
    public void Bind(MapSystem map)
    {
        _map = map;
    }

    /// <summary>
    ///     Display indicators on every tile in <paramref name="tiles" />, hiding extras.
    /// </summary>
    /// <param name="tiles">Cell coordinates to highlight.</param>
    public void Show(IReadOnlyList<(int X, int Y, int Z)> tiles)
    {
        if (_map is null)
        {
            GD.PrintErr($"{nameof(MoveIndicator)}: Bind(map) must be called first.");
            return;
        }

        EnsurePool(tiles.Count);

        for (int i = 0; i < _pool.Count; i++)
        {
            MeshInstance3D mesh = _pool[i];
            if (i < tiles.Count)
            {
                (int x, int y, int z) = tiles[i];
                Vector3 world = _map.MapToLocal(new Vector3I(x, y, z));
                world.Y += _map.CellSize.Y * 0.5f + Height;
                mesh.Position = world;
                mesh.Visible = true;
            }
            else
            {
                mesh.Visible = false;
            }
        }
    }

    /// <summary>Hide every indicator.</summary>
    public new void Hide()
    {
        foreach (MeshInstance3D mesh in _pool)
            mesh.Visible = false;
    }

    private void EnsurePool(int count)
    {
        while (_pool.Count < count)
        {
            MeshInstance3D mesh = BuildIndicator();
            _pool.Add(mesh);
            AddChild(mesh);
        }
    }

    private MeshInstance3D BuildIndicator()
    {
        Vector3 cell = _map?.CellSize ?? Vector3.One;
        PlaneMesh plane = new()
        {
            Size = new Vector2(cell.X * 0.92f, cell.Z * 0.92f),
        };

        _material ??= new StandardMaterial3D
        {
            AlbedoColor = TileColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            DisableReceiveShadows = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true,
            Emission = new Color(TileColor.R, TileColor.G, TileColor.B, 1f) * 0.4f,
        };
        plane.SurfaceSetMaterial(0, _material);

        return new MeshInstance3D
        {
            Mesh = plane,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }
}
