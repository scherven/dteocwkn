using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ordered list of BuildSites awaiting Hammer investment.
/// Each Evening, hammers flow into the front project first; overflow continues
/// to subsequent projects. Any remaining hammers are wasted.
/// </summary>
public class ConstructionQueue : MonoBehaviour
{
    public static ConstructionQueue Instance { get; private set; }

    readonly List<BuildSite> _queue = new();

    public IReadOnlyList<BuildSite> Queue => _queue;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Enqueue(BuildSite site) => _queue.Add(site);

    /// <summary>
    /// Distribute hammers sequentially: fill the front project first, then overflow.
    /// Completed projects are removed automatically.
    /// Returns any hammers left over after all projects are filled.
    /// </summary>
    public int ApplyHammers(int hammers)
    {
        // Clean up stale entries first
        _queue.RemoveAll(s => s == null || s.IsComplete);

        int remaining = hammers;
        for (int i = 0; i < _queue.Count && remaining > 0; i++)
            remaining = _queue[i].ApplyHammers(remaining);

        // Remove any that just completed
        _queue.RemoveAll(s => s == null || s.IsComplete);

        return remaining;
    }

    /// <summary>Short summary string for the debug panel.</summary>
    public string QueueSummary()
    {
        if (_queue.Count == 0) return "empty";
        var first = _queue[0];
        if (first == null) return "empty";
        return $"{first.Definition.buildingName} [{first.HammersApplied}/{first.HammerCost}]" +
               (_queue.Count > 1 ? $" +{_queue.Count - 1} more" : "");
    }
}
