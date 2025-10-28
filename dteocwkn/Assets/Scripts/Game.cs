using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Game : MonoBehaviour
{
    public List<int> deckMaker; // count of each card type (count of wood, count of stone, etc)
    public List<CardType> deck;
    public List<CardType> discardPile;
    public Hand hand; // Assign this in the Inspector
    public int handSize;
    public Dictionary<ResourceType, int> resources;
    public TextMeshProUGUI resourceText;

    void Start()
    {
        if (hand == null)
        {
            hand = GetComponent<Hand>();
        }

        resources = new Dictionary<ResourceType, int>();
        foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[resource] = 0;
        }

        handSize = 5;
        discardPile = new List<CardType>();

        // Initialize deck with 7 wood and 3 stone
        deckMaker = new List<int> { 7, 3 }; // 7 Wood (index 0), 3 Stone (index 1)

        InitializeDeck();
        ShuffleDeck();
        DrawInitialHand();
        hand.MakeHand();
        UpdateResourceDisplay();
    }

    void InitializeDeck()
    {
        deck = new List<CardType>();
        for (int i = 0; i < deckMaker.Count; i++)
        {
            for (int j = 0; j < deckMaker[i]; j++)
            {
                deck.Add((CardType) i);
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardType temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void DrawInitialHand()
    {
        for (int i = 0; i < handSize; i++)
        {
            DrawCard();
        }
    }

    void DrawCard()
    {
        if (deck.Count == 0)
        {
            // Reshuffle discard pile into deck
            if (discardPile.Count > 0)
            {
                deck = new List<CardType>(discardPile);
                discardPile.Clear();
                ShuffleDeck();
                Debug.Log("Reshuffled discard pile into deck");
            }
            else
            {
                Debug.Log("Deck and discard pile are empty, cannot draw a card.");
                return;
            }
        }

        CardType drawnCard = deck[0];
        deck.RemoveAt(0);
        hand.Add(drawnCard);
        Debug.Log("Drew a card: " + drawnCard);
    }

    public void Tick()
    {
        // Discard current hand and draw new cards
        foreach (CardType card in hand.cards)
        {
            discardPile.Add(card);
        }
        
        hand.ClearHand();
        DrawInitialHand();
        hand.MakeHand();
        Debug.Log("Tick: Discarded hand and drew new cards");
    }

    public void Auto()
    {
        Dictionary<ResourceType, int> newResources = hand.ClearResourceCards(discardPile);
        foreach (var entry in newResources)
        {
            resources[entry.Key] += entry.Value;
        }
        
        hand.MakeHand();
        UpdateResourceDisplay();
        Debug.Log("Auto: Converted cards to resources");
    }

    void UpdateResourceDisplay()
    {
        if (resourceText != null)
        {
            string displayText = "";
            foreach (var entry in resources)
            {
                displayText += $"{entry.Key}: {entry.Value}\n";
            }
            resourceText.text = displayText.TrimEnd();
        }
    }

    void Update()
    {
        
    }
}