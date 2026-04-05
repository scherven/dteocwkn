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

    int _spawnCount;

    /// <summary>Creates a new villager capsule on the NavMesh at the given position.</summary>
    public VillagerAgent SpawnVillager(Vector3 pos)
    {
        var parent = GameObject.Find("Villagers") ?? new GameObject("Villagers");

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Villager_{++_spawnCount}";
        go.transform.SetParent(parent.transform);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(1f, 0.9f, 1f);

        go.GetComponent<Renderer>().material =
            ResourceVisuals.CreateUnlit(Color.white);

        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius           = 0.4f;
        agent.height           = 1.8f;
        agent.speed            = 3.5f;
        agent.angularSpeed     = 120f;
        agent.acceleration     = 8f;
        agent.stoppingDistance = 0.3f;

        go.AddComponent<VillagerNeeds>();
        go.AddComponent<VillagerAnimator>();
        return go.AddComponent<VillagerAgent>(); // VillagerAgent.Start registers with us
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
