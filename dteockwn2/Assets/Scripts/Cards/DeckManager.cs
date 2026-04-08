using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [SerializeField] int baseMaxHandSize = 7;

    public int MaxHandSize  => baseMaxHandSize;
    public int CurrentTurn  { get; private set; } = 0;
    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Morning;

    const int DrawsPerTurn = 5;

    public IReadOnlyList<CardData> Hand => _hand;
    public int DrawPileCount => _drawPile.Count;
    public int DiscardCount  => _discard.Count;

    /// <summary>Returns true if the card is currently in the player's hand.</summary>
    public bool IsCardInHand(CardData card) => _hand.Contains(card);

    readonly List<CardData> _drawPile = new();
    readonly List<CardData> _hand     = new();
    readonly List<CardData> _discard  = new();

    // Villager spawns queued by Cottage cards — resolved each Morning.
    readonly List<Vector3> _pendingVillagerSpawns = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => StartCoroutine(BeginMorning());

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by the "End Day" button.</summary>
    public void EndDay()
    {
        if (CurrentPhase != TurnPhase.Day) return;
        StartCoroutine(RunEveningThroughMorning());
    }

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
        if (CurrentPhase != TurnPhase.Day) return;
        if (!_hand.Remove(card)) return;
        _discard.Add(card);

        if (card.effect != null)
        {
            var ctx = new CardPlayContext
            {
                inventory       = GameInventory.Instance,
                buildingManager = BuildingManager.Instance,
                taskDispatcher  = TaskDispatcher.Instance,
                deckManager     = this,
                hammerManager   = HammerManager.Instance,
                sourceCard      = card
            };
            card.effect.Resolve(ctx);
        }

        GameEvents.RaiseCardPlayed(card);
    }

    /// <summary>Queue a villager to spawn at the given position next Morning.</summary>
    public void QueueVillagerSpawn(Vector3 position) =>
        _pendingVillagerSpawns.Add(position);

    // ── Phase coroutine ───────────────────────────────────────────────────────

    IEnumerator BeginMorning()
    {
        SetPhase(TurnPhase.Morning);
        CurrentTurn++;
        SpawnQueuedVillagers();

        for (int i = 0; i < DrawsPerTurn; i++)
            DrawCard();

        yield return null; // let the frame render with the new hand

        SetPhase(TurnPhase.Day);
    }

    IEnumerator RunEveningThroughMorning()
    {
        // Evening — distribute all banked hammers: buildings first, then planned roads.
        SetPhase(TurnPhase.Evening);
        int hammers   = HammerManager.Instance.ConsumeAll();
        int leftover  = ConstructionQueue.Instance.ApplyHammers(hammers);
        RoadManager.Instance?.ApplyHammers(leftover);

        yield return new WaitForSeconds(0.3f);

        // Night — discard hand
        SetPhase(TurnPhase.Night);
        DiscardHand();

        yield return new WaitForSeconds(0.3f);

        // Morning of next day
        yield return StartCoroutine(BeginMorning());
    }

    void DiscardHand()
    {
        foreach (var card in _hand)
            _discard.Add(card);
        _hand.Clear();
        GameEvents.RaiseHandDiscarded();
    }

    void SpawnQueuedVillagers()
    {
        foreach (var pos in _pendingVillagerSpawns)
            VillagerManager.Instance?.SpawnVillager(pos);
        _pendingVillagerSpawns.Clear();
    }

    void SetPhase(TurnPhase phase)
    {
        CurrentPhase = phase;
        GameEvents.RaiseTurnPhaseChanged(phase);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
