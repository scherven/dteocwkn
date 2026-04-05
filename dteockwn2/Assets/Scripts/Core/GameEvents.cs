using System;
using UnityEngine;

/// <summary>
/// Central event bus. Static events for call-site convenience.
/// Subscribers must unsubscribe in OnDisable/OnDestroy to avoid stale callbacks.
/// </summary>
public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static event Action<ResourceType, int> OnResourceAdded;
    public static event Action<ResourceType, int> OnResourceConsumed;
    public static event Action<BuildingDefinition, Vector3> OnBuildingPlaced;
    public static event Action<BuildingDefinition> OnBuildingCompleted;
    public static event Action<BuildingDefinition> OnBuildingUnlocked;
    public static event Action<CardData> OnCardPlayed;
    public static event Action<CardData> OnCardDrawn;
    public static event Action<CardData> OnCardAddedToDeck;
    public static event Action OnDeckReshuffled;
    public static event Action<int> OnVillagerCountChanged;

    public static void RaiseResourceAdded(ResourceType t, int amount)       => OnResourceAdded?.Invoke(t, amount);
    public static void RaiseResourceConsumed(ResourceType t, int amount)    => OnResourceConsumed?.Invoke(t, amount);
    public static void RaiseBuildingPlaced(BuildingDefinition def, Vector3 pos) => OnBuildingPlaced?.Invoke(def, pos);
    public static void RaiseBuildingCompleted(BuildingDefinition def)       => OnBuildingCompleted?.Invoke(def);
    public static void RaiseBuildingUnlocked(BuildingDefinition def)        => OnBuildingUnlocked?.Invoke(def);
    public static void RaiseCardPlayed(CardData card)                       => OnCardPlayed?.Invoke(card);
    public static void RaiseCardDrawn(CardData card)                        => OnCardDrawn?.Invoke(card);
    public static void RaiseCardAddedToDeck(CardData card)                  => OnCardAddedToDeck?.Invoke(card);
    public static void RaiseDeckReshuffled()                                => OnDeckReshuffled?.Invoke();
    public static void RaiseVillagerCountChanged(int count)                 => OnVillagerCountChanged?.Invoke(count);
}
