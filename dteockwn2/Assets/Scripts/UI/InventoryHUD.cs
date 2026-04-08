using TMPro;
using UnityEngine;

/// <summary>Top-right panel showing current resource counts and season.</summary>
public class InventoryHUD : MonoBehaviour
{
    TextMeshProUGUI _text;

    void Awake() => _text = GetComponentInChildren<TextMeshProUGUI>();

    void OnEnable()
    {
        GameEvents.OnResourceAdded    += OnResource;
        GameEvents.OnResourceConsumed += OnResource;
        GameEvents.OnHammersChanged   += OnHammers;
        GameEvents.OnSeasonChanged    += OnSeason;
        GameEvents.OnTurnPhaseChanged += OnPhase;
        Refresh();
    }

    void OnDisable()
    {
        GameEvents.OnResourceAdded    -= OnResource;
        GameEvents.OnResourceConsumed -= OnResource;
        GameEvents.OnHammersChanged   -= OnHammers;
        GameEvents.OnSeasonChanged    -= OnSeason;
        GameEvents.OnTurnPhaseChanged -= OnPhase;
    }

    void OnResource(ResourceType _, int __) => Refresh();
    void OnHammers(int _)                   => Refresh();
    void OnSeason(Season _)                 => Refresh();
    void OnPhase(TurnPhase _)               => Refresh(); // catches day-of-season tick

    void Refresh()
    {
        if (GameInventory.Instance == null || _text == null) return;

        var    sm      = SeasonManager.Instance;
        string season  = sm != null ? sm.DisplayString() : "—";
        int    hammers = HammerManager.Instance?.HammersAvailable ?? 0;

        _text.text =
            $"{season}\n" +
            $"Wood:    {GameInventory.Instance.GetCount(ResourceType.Wood)}\n" +
            $"Stone:   {GameInventory.Instance.GetCount(ResourceType.Stone)}\n" +
            $"Food:    {GameInventory.Instance.GetCount(ResourceType.Food)}\n" +
            $"\u26d3 {hammers}";
    }
}
