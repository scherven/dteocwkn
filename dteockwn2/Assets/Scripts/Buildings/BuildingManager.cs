using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    // All completed buildings indexed by definition for stat queries.
    readonly List<(BuildingDefinition def, Vector3 pos, GameObject go)> _buildings = new();

    [Tooltip("Position of the storehouse. Set by Bootstrap at startup.")]
    public Vector3 StorehousePosition { get; private set; } = Vector3.zero;

    [Tooltip("Position of the granary. Defaults to StorehousePosition until a granary is placed.")]
    public Vector3 GranaryPosition { get; private set; } = Vector3.zero;

    // Farm seed state: keyed by rounded building position.
    readonly Dictionary<Vector3, bool>       _seededFarms = new();
    readonly Dictionary<Vector3, GameObject> _seedObjects  = new();

    // --- Coupled stat aggregates ---
    public int TotalHousingCapacity { get; private set; }
    public int TotalJobSlots        { get; private set; }

    // Future hooks (commented stubs):
    // public float TotalDrawRateModifier => _buildings.Sum(b => b.def.drawRateModifier);
    // public int   TotalHandSizeModifier => _buildings.Sum(b => b.def.handSizeModifier);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetStorehousePosition(Vector3 pos)
    {
        StorehousePosition = pos;
        // Default granary to storehouse until an actual granary is placed.
        GranaryPosition = pos;
    }

    public void SetGranaryPosition(Vector3 pos) => GranaryPosition = pos;

    public void RegisterBuilding(BuildingDefinition def, Vector3 pos, GameObject go)
    {
        _buildings.Add((def, pos, go));
        TotalHousingCapacity += def.housingCapacity;
        TotalJobSlots        += def.jobSlots;
        if (def.type == BuildingType.Granary)
            SetGranaryPosition(pos);
    }

    // ── Farm seed tracking ────────────────────────────────────────────────────

    static Vector3 RoundPos(Vector3 p) => new(Mathf.Round(p.x), Mathf.Round(p.y), Mathf.Round(p.z));

    public bool IsFarmSeeded(Vector3 pos) =>
        _seededFarms.TryGetValue(RoundPos(pos), out bool v) && v;

    public void SetFarmSeeded(Vector3 pos, bool seeded, GameObject seedObj = null)
    {
        var key = RoundPos(pos);
        _seededFarms[key] = seeded;
        if (seeded && seedObj != null)
            _seedObjects[key] = seedObj;
        else if (!seeded)
            _seedObjects.Remove(key);
    }

    /// <summary>
    /// Returns the visual GameObject of the building whose position is within
    /// <paramref name="tolerance"/> units of <paramref name="pos"/>, or null.
    /// </summary>
    public GameObject GetBuildingAt(Vector3 pos, float tolerance = 0.5f)
    {
        foreach (var b in _buildings)
            if (Vector3.Distance(b.pos, pos) < tolerance)
                return b.go;
        return null;
    }

    public IReadOnlyList<(BuildingDefinition def, Vector3 pos, GameObject go)> AllBuildings => _buildings;
}
