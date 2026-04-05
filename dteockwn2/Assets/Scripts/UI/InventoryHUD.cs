using TMPro;
using UnityEngine;

/// <summary>Top-right panel showing current resource counts.</summary>
public class InventoryHUD : MonoBehaviour
{
    TextMeshProUGUI _text;

    void Awake() => _text = GetComponentInChildren<TextMeshProUGUI>();

    void OnEnable()
    {
        GameEvents.OnResourceAdded    += Refresh;
        GameEvents.OnResourceConsumed += Refresh;
        Refresh(default, 0);
    }

    void OnDisable()
    {
        GameEvents.OnResourceAdded    -= Refresh;
        GameEvents.OnResourceConsumed -= Refresh;
    }

    void Refresh(ResourceType _, int __)
    {
        if (GameInventory.Instance == null || _text == null) return;
        _text.text =
            $"Wood:  {GameInventory.Instance.GetCount(ResourceType.Wood)}\n" +
            $"Stone: {GameInventory.Instance.GetCount(ResourceType.Stone)}";
    }
}
