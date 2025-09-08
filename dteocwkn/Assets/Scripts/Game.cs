using UnityEngine;
using System.Collections.Generic;

public class Game : MonoBehaviour
{
    public List<int> deckMaker; // count of each card type (count of wood, count of stone, etc)
    public List<CardType> deck;
    public List<Card> discardPile;
    public Hand hand;
    public int handSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hand = GetComponent<Hand>();

        handSize = 5;

        InitializeDeck();
        ShuffleDeck();
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
            Debug.Log("Deck is empty, cannot draw a card.");
            return;
        }

        CardType drawnCard = deck[0];
        deck.RemoveAt(0);
        hand.Add(drawnCard);
        Debug.Log("Drew a card: " + drawnCard);
    }

    public void Tick()
    {
        Debug.Log("ticking");
        DrawInitialHand();
        hand.MakeHand();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
