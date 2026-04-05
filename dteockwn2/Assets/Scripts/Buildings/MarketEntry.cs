using UnityEngine;

[System.Serializable]
public class MarketEntry
{
    public BuildingDefinition building;
    public bool unlocked;

    // Stub: no implementations yet.
    // Future: data-driven unlock rules (e.g. reach 5 villagers, complete a prior building, etc.)
    public UnlockCondition unlockCondition;
}
