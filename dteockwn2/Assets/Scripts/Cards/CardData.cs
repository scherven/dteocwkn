using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Card Data", fileName = "NewCard")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;   // placeholder null is fine
    public CardType type;
    public CardEffect effect; // nullable

    /// <summary>
    /// Returns a runtime card instance bound to the given building position.
    /// The effect is given a chance to stamp in position-specific data.
    /// Stateless effects share the same effect instance; position-dependent
    /// effects (SpawnVillagerEffect) get a fresh copy with the position set.
    /// </summary>
    public CardData CreateBoundInstance(Vector3 buildingPos)
    {
        var card         = CreateInstance<CardData>();
        card.cardName    = cardName;
        card.description = description;
        card.artwork     = artwork;
        card.type        = type;
        card.effect      = effect != null ? effect.BindToBuilding(buildingPos) : null;
        return card;
    }
}
