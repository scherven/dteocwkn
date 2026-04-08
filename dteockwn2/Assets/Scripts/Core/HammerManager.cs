using UnityEngine;

/// <summary>
/// Tracks the Hammer pool for the current day.
/// Hammers are produced by playing Villager cards and consumed each Evening
/// to advance construction projects.
/// </summary>
public class HammerManager : MonoBehaviour
{
    public static HammerManager Instance { get; private set; }

    public int HammersAvailable { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddHammer(int amount)
    {
        HammersAvailable += amount;
        GameEvents.RaiseHammersChanged(HammersAvailable);
    }

    /// <summary>Spend <paramref name="amount"/> hammers. Returns false and does nothing if insufficient.</summary>
    public bool TrySpend(int amount)
    {
        if (HammersAvailable < amount) return false;
        HammersAvailable -= amount;
        GameEvents.RaiseHammersChanged(HammersAvailable);
        return true;
    }

    /// <summary>Returns the total hammers accumulated and resets the pool to zero.</summary>
    public int ConsumeAll()
    {
        int total = HammersAvailable;
        HammersAvailable = 0;
        GameEvents.RaiseHammersChanged(0);
        return total;
    }
}
