using System.Collections.Generic;
using UnityEngine;

public class VillagerManager : MonoBehaviour
{
    public static VillagerManager Instance { get; private set; }

    readonly List<VillagerAgent> _villagers = new();

    // Job assignments: villager → build site they work at
    readonly Dictionary<VillagerAgent, BuildSite> _jobAssignments = new();

    public int TotalVillagerCount => _villagers.Count;
    public IReadOnlyList<VillagerAgent> AllVillagers => _villagers;

    // Stub: always returns 1 until VillagerNeeds is wired up.
    public float AverageHappiness => 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterVillager(VillagerAgent v)
    {
        if (_villagers.Contains(v)) return;
        _villagers.Add(v);
        GameEvents.RaiseVillagerCountChanged(TotalVillagerCount);
    }

    public void UnregisterVillager(VillagerAgent v)
    {
        _villagers.Remove(v);
        _jobAssignments.Remove(v);
        GameEvents.RaiseVillagerCountChanged(TotalVillagerCount);
    }

    public VillagerAgent GetIdleVillager()
    {
        foreach (var v in _villagers)
            if (v.IsIdle) return v;
        return null;
    }

    // --- Job stubs ---
    // BuildingDefinition.jobSlots defines capacity; VillagerManager tracks assignments.
    // Future: job assignment UI, influence on TaskDispatcher priority, passive building output.

    public void AssignJob(VillagerAgent v, BuildSite site)
    {
        _jobAssignments[v] = site;
    }

    public void UnassignJob(VillagerAgent v)
    {
        _jobAssignments.Remove(v);
    }

    public BuildSite GetJob(VillagerAgent v) =>
        _jobAssignments.TryGetValue(v, out var site) ? site : null;
}
