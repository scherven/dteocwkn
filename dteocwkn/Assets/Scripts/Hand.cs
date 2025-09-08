using UnityEngine;
using System.Collections.Generic;

public class Hand : MonoBehaviour
{
    public RectTransform cardPrefab;
    public Canvas targetCanvas;
    public int tilt;
    public float xSpacing;
    public float ySpacing;
    public List<CardType> cards;

    public void MakeHand()
    {
        int nCards = cards.Count;
        int nLeft, nMiddle, nRight;
        if (nCards % 2 == 1)
        {
            nLeft = nRight = nCards / 2;
            nMiddle = 1;
        }
        else
        {
            nLeft = nRight = nCards / 2 - 1;
            nMiddle = 2;
        }

        Debug.Log($"Hand configuration: Left={nLeft}, Middle={nMiddle}, Right={nRight}");

        int cardIdx = 0;
        for (int i = -nLeft; i < 0; i++)
        {
            MakeCard(new Vector3(i * xSpacing, ySpacing * i, -2), new Vector3(0, 0, tilt * -i), cardIdx++);
        }

        for (int i = 0; i < nMiddle; i++)
        {
            MakeCard(new Vector3(0, 0, -2), new Vector3(0, 0, 0), cardIdx++); // Middle cards are upright
        }

        for (int i = 1; i < nRight + 1; i++)
        {
            MakeCard(new Vector3(i * xSpacing, ySpacing * -i, -2), new Vector3(0, 0, -tilt * i), cardIdx++);
        }
    }

    void MakeCard(Vector3 position, Vector3 rotation, int cardIdx)
    {
        RectTransform newCard = Instantiate(cardPrefab, position, Quaternion.Euler(rotation), targetCanvas.transform.Find("HandHolder"));
        newCard.name = "Card_" + cardIdx;
        newCard.anchoredPosition = position;

        Card cardComponent = newCard.GetComponent<Card>();
        cardComponent.type = cards[cardIdx];
        cardComponent.SetImage();
    }

    public void Add(CardType card)
    {
        cards.Add(card);
    }

    public void ClearHand()
    {
        cards.Clear();
        foreach (Transform child in targetCanvas.transform.Find("HandHolder"))
        {
            Destroy(child.gameObject);
        }
    }
}
