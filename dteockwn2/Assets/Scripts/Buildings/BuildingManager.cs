using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    // All completed buildings indexed by definition for stat queries.
    readonly List<(BuildingDefinition def, Vector3 pos)> _buildings = new();

    [Tooltip("Position of the storehouse. Set by Bootstrap at startup.")]
    public Vector3 StorehousePosition { get; private set; } = Vector3.zero;

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

    public void SetStorehousePosition(Vector3 pos) => StorehousePosition = pos;

    public void RegisterBuilding(BuildingDefinition def, Vector3 pos)
    {
        _buildings.Add((def, pos));
        TotalHousingCapacity += def.housingCapacity;
        TotalJobSlots        += def.jobSlots;
    }

    public IReadOnlyList<(BuildingDefinition def, Vector3 pos)> AllBuildings => _buildings;
}
