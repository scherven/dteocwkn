using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks planned and built road cells.
/// Planned roads are marked by the player via the Road Tool.
/// Unspent hammers at end of day convert planned → built, one hammer per cell.
/// Villagers move 1.75× faster on built road cells.
/// </summary>
public class RoadManager : MonoBehaviour
{
    public static RoadManager Instance { get; private set; }

    readonly HashSet<Vector2Int>                _planned        = new();
    readonly HashSet<Vector2Int>                _built          = new();
    readonly Dictionary<Vector2Int, GameObject> _plannedVisuals = new();
    readonly Dictionary<Vector2Int, GameObject> _builtVisuals   = new();

    GameObject _visualRoot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _visualRoot = new GameObject("RoadVisuals");
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public bool IsBuiltRoad(Vector2Int cell)               => _built.Contains(cell);
    public bool IsPlannedRoad(Vector2Int cell)             => _planned.Contains(cell);
    /// <summary>Exposes the built road set so GridManager can bake it into the NavMesh.</summary>
    public IReadOnlyCollection<Vector2Int> BuiltRoads      => _built;

    // ── Road tool interaction ─────────────────────────────────────────────────

    /// <summary>Toggle a cell between planned/unplanned. Built cells are left unchanged.</summary>
    public void TogglePlanned(Vector2Int cell)
    {
        if (_built.Contains(cell)) return;

        if (_planned.Contains(cell))
        {
            _planned.Remove(cell);
            DestroyVisual(_plannedVisuals, cell);
        }
        else
        {
            _planned.Add(cell);
            _plannedVisuals[cell] = SpawnQuad(cell, 0.022f,
                new Color(1f, 0.85f, 0.2f, 0.55f), "PlannedRoad");
        }
    }

    // ── End-of-day distribution ───────────────────────────────────────────────

    /// <summary>
    /// Convert as many planned road cells as hammers allow (one hammer per cell).
    /// Rebakes the NavMesh if any roads were built so agents immediately route through them.
    /// Returns unused hammers.
    /// </summary>
    public int ApplyHammers(int hammers)
    {
        if (hammers <= 0) return 0;

        var snapshot  = new List<Vector2Int>(_planned);
        int remaining = hammers;
        int built     = 0;

        foreach (var cell in snapshot)
        {
            if (remaining <= 0) break;
            BuildRoad(cell);
            remaining--;
            built++;
        }

        if (built > 0)
            GridManager.Instance?.RebakeNavMesh();

        return remaining;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    void BuildRoad(Vector2Int cell)
    {
        _planned.Remove(cell);
        DestroyVisual(_plannedVisuals, cell);

        _built.Add(cell);
        _builtVisuals[cell] = SpawnQuad(cell, 0.022f,
            new Color(0.38f, 0.33f, 0.26f, 1f), "Road");
    }

    GameObject SpawnQuad(Vector2Int cell, float y, Color color, string goName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = goName;
        go.transform.SetParent(_visualRoot.transform);
        go.transform.position   = new Vector3(cell.x, y, cell.y);
        go.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * 0.93f;
        go.GetComponent<Renderer>().material = ResourceVisuals.CreateUnlit(color);
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    void DestroyVisual(Dictionary<Vector2Int, GameObject> dict, Vector2Int cell)
    {
        if (dict.TryGetValue(cell, out var go)) { Destroy(go); dict.Remove(cell); }
    }
}
