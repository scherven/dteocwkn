using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bottom-center panel showing the player's hand.
/// Each card is a button; clicking it plays the card.
/// </summary>
public class HandUI : MonoBehaviour
{
    [SerializeField] float cardWidth  = 100f;
    [SerializeField] float cardHeight = 140f;
    [SerializeField] float cardSpacing = 8f;

    readonly List<(CardData data, GameObject obj)> _cards = new();

    GameObject _hoveredBuilding;
    readonly Dictionary<Renderer, Color> _originalColors = new();

    RectTransform _rect;

    void Awake() => _rect = GetComponent<RectTransform>();

    void OnEnable()
    {
        GameEvents.OnCardDrawn     += OnCardDrawn;
        GameEvents.OnCardPlayed    += OnCardPlayed;
        GameEvents.OnHandDiscarded += OnHandDiscarded;
    }

    void OnDisable()
    {
        GameEvents.OnCardDrawn     -= OnCardDrawn;
        GameEvents.OnCardPlayed    -= OnCardPlayed;
        GameEvents.OnHandDiscarded -= OnHandDiscarded;
    }

    void OnCardDrawn(CardData card)
    {
        var btn = CreateCardButton(card);
        _cards.Add((card, btn));
        LayoutCards();
    }

    void OnCardPlayed(CardData card)
    {
        int idx = _cards.FindIndex(c => c.data == card);
        if (idx < 0) return;
        Destroy(_cards[idx].obj);
        _cards.RemoveAt(idx);
        LayoutCards();
    }

    void OnHandDiscarded()
    {
        foreach (var (_, obj) in _cards)
            Destroy(obj);
        _cards.Clear();
        OnCardHoverExit();
    }

    void LayoutCards()
    {
        float totalWidth = _cards.Count * (cardWidth + cardSpacing) - cardSpacing;
        float startX = -totalWidth / 2f + cardWidth / 2f;

        for (int i = 0; i < _cards.Count; i++)
        {
            var rt = _cards[i].obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0f);
        }
    }

    GameObject CreateCardButton(CardData card)
    {
        // Container
        var go  = new GameObject(card.cardName, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cardWidth, cardHeight);

        // Background
        var img = go.AddComponent<Image>();
        img.color = CardColor(card);

        // Button
        var btn = go.AddComponent<Button>();
        var captured = card;
        btn.onClick.AddListener(() => DeckManager.Instance?.PlayCard(captured));

        // Hover → highlight associated building
        var trigger = go.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => OnCardHoverEnter(captured));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => OnCardHoverExit());
        trigger.triggers.Add(exitEntry);

        // Label — omitted for resource cards (colour communicates type)
        if (card.type != CardType.Resource)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(4, 4);
            labelRt.offsetMax = new Vector2(-4, -4);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = $"<b>{card.cardName}</b>\n<size=70%>{card.description}</size>";
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.fontSize  = 12f;
            tmp.color     = Color.white;
        }

        return go;
    }

    void OnCardHoverEnter(CardData card)
    {
        OnCardHoverExit(); // clear any previous highlight

        var hint = card.effect?.GetBuildingHint();
        if (hint == null) return;

        var building = BuildingManager.Instance?.GetBuildingAt(hint.Value);
        if (building == null) return;

        _hoveredBuilding = building;
        foreach (var r in building.GetComponentsInChildren<Renderer>())
        {
            _originalColors[r] = r.material.color;
            r.material.color   = Color.Lerp(r.material.color, Color.yellow, 0.45f);
        }
    }

    void OnCardHoverExit()
    {
        if (_hoveredBuilding == null) return;
        foreach (var r in _hoveredBuilding.GetComponentsInChildren<Renderer>())
            if (_originalColors.TryGetValue(r, out var c))
                r.material.color = c;
        _originalColors.Clear();
        _hoveredBuilding = null;
    }

    static Color CardColor(CardData card) =>
        card.type switch
        {
            CardType.Resource => new Color(0.25f, 0.55f, 0.25f), // green (forage)
            CardType.Building => new Color(0.45f, 0.35f, 0.20f), // tan
            CardType.Villager => new Color(0.25f, 0.35f, 0.65f), // blue
            CardType.Event    => new Color(0.65f, 0.25f, 0.25f), // red
            _                 => Color.gray
        };
}
