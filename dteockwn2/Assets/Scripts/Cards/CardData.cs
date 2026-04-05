using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Card Data", fileName = "NewCard")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;   // placeholder null is fine
    public CardType type;
    public CardEffect effect; // nullable
}
