using UnityEngine;
using System.Collections.Generic;
 using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public CardType type;
    public List<Sprite> images;

    public Card(CardType type)
    {
        this.type = type;
    }

    public void SetImage()
    {
        Image image = GetComponent<Image>();
        image.sprite = images[(int)type];
    }
}