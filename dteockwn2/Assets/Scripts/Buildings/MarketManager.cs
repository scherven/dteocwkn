using System.Collections.Generic;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [SerializeField] List<MarketEntry> _entries = new();

    public IReadOnlyList<MarketEntry> Entries => _entries;

    // Set by Bootstrap
    BuildingDefinition _pendingPurchase;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddEntry(MarketEntry entry) => _entries.Add(entry);

    public void UnlockBuilding(BuildingDefinition def)
    {
        foreach (var e in _entries)
        {
            if (e.building == def && !e.unlocked)
            {
                e.unlocked = true;
                GameEvents.RaiseBuildingUnlocked(def);
                return;
            }
        }
        // Building not in list yet — add it as unlocked
        _entries.Add(new MarketEntry { building = def, unlocked = true });
        GameEvents.RaiseBuildingUnlocked(def);
    }

    /// <summary>
    /// Called by MarketUI when the player clicks Build.
    /// Returns true and enters placement mode if the player can afford it.
    /// </summary>
    public bool TryPurchase(BuildingDefinition def)
    {
        if (!GameInventory.Instance.CanAfford(def.materialCost)) return false;
        if (!GameInventory.Instance.ConsumeAll(def.materialCost)) return false;

        PlacementMode.Instance.Begin(def);
        return true;
    }
}
