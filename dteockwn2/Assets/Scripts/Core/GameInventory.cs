using System;
using System.Collections.Generic;
using UnityEngine;

public class GameInventory : MonoBehaviour
{
    public static GameInventory Instance { get; private set; }

    readonly Dictionary<ResourceType, int> _counts = new();

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Add(ResourceType type, int amount)
    {
        _counts[type] = GetCount(type) + amount;
        GameEvents.RaiseResourceAdded(type, amount);
        OnInventoryChanged?.Invoke();
    }

    public bool Consume(ResourceType type, int amount)
    {
        if (GetCount(type) < amount) return false;
        _counts[type] -= amount;
        GameEvents.RaiseResourceConsumed(type, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool CanAfford(IEnumerable<ResourceCost> costs)
    {
        foreach (var cost in costs)
            if (GetCount(cost.type) < cost.amount) return false;
        return true;
    }

    public bool ConsumeAll(IEnumerable<ResourceCost> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var cost in costs)
            Consume(cost.type, cost.amount);
        return true;
    }

    public int GetCount(ResourceType type) => _counts.TryGetValue(type, out int v) ? v : 0;
}
