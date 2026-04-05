using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [SerializeField] float baseDrawInterval = 2f;
    [SerializeField] int baseMaxHandSize = 7;

    public float DrawInterval => Mathf.Max(0.5f, baseDrawInterval);
    public int MaxHandSize => baseMaxHandSize;

    public IReadOnlyList<CardData> Hand => _hand;
    public int DrawPileCount => _drawPile.Count;
    public int DiscardCount => _discard.Count;

    readonly List<CardData> _drawPile = new();
    readonly List<CardData> _hand = new();
    readonly List<CardData> _discard = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => StartCoroutine(DrawLoop());

    IEnumerator DrawLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(DrawInterval);
            DrawCard();
        }
    }

    // Set initial deck without firing OnCardAddedToDeck for each card.
    public void SetStartingDeck(List<CardData> cards)
    {
        _drawPile.Clear();
        _hand.Clear();
        _discard.Clear();
        _drawPile.AddRange(cards);
        Shuffle(_drawPile);
    }

    public void AddCardToDeck(CardData card)
    {
        int insertAt = Random.Range(0, _drawPile.Count + 1);
        _drawPile.Insert(insertAt, card);
        GameEvents.RaiseCardAddedToDeck(card);
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (!_drawPile.Remove(card))
            _discard.Remove(card);
    }

    public void DrawCard()
    {
        if (_hand.Count >= MaxHandSize) return;

        if (_drawPile.Count == 0)
        {
            if (_discard.Count == 0) return;
            _drawPile.AddRange(_discard);
            _discard.Clear();
            Shuffle(_drawPile);
            GameEvents.RaiseDeckReshuffled();
        }

        var card = _drawPile[^1];
        _drawPile.RemoveAt(_drawPile.Count - 1);
        _hand.Add(card);
        GameEvents.RaiseCardDrawn(card);
    }

    public void PlayCard(CardData card)
    {
        if (!_hand.Remove(card)) return;
        _discard.Add(card);

        if (card.effect != null)
        {
            var ctx = new CardPlayContext
            {
                inventory = GameInventory.Instance,
                buildingManager = BuildingManager.Instance,
                taskDispatcher = TaskDispatcher.Instance,
                deckManager = this,
                sourceCard = card
            };
            card.effect.Resolve(ctx);
        }

        GameEvents.RaiseCardPlayed(card);
    }

    static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
