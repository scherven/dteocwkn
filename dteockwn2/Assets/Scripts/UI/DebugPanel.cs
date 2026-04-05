using TMPro;
using UnityEngine;

/// <summary>Top-left panel showing runtime debug stats.</summary>
public class DebugPanel : MonoBehaviour
{
    TextMeshProUGUI _text;

    void Awake() => _text = GetComponentInChildren<TextMeshProUGUI>();

    void Update()
    {
        if (_text == null) return;

        var deck = DeckManager.Instance;
        var inv  = GameInventory.Instance;
        var vm   = VillagerManager.Instance;

        if (deck == null || inv == null) return;

        _text.text =
            $"Draw pile:   {deck.DrawPileCount}\n" +
            $"Hand:        {deck.Hand.Count} / {deck.MaxHandSize}\n" +
            $"Discard:     {deck.DiscardCount}\n" +
            $"Draw every:  {deck.DrawInterval:F1}s\n" +
            $"Villagers:   {vm?.TotalVillagerCount ?? 0}\n" +
            $"Wood:        {inv.GetCount(ResourceType.Wood)}\n" +
            $"Stone:       {inv.GetCount(ResourceType.Stone)}";
    }
}
