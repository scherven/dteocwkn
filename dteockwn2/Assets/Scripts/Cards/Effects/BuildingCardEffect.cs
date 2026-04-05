using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Effects/Building Card", fileName = "BuildingCardEffect")]
public class BuildingCardEffect : CardEffect
{
    public BuildingDefinition building;

    public override void Resolve(CardPlayContext ctx)
    {
        // TODO: passive effect or town bonus on play.
        // Building cards enter the deck as a record of what has been built.
        // Their drawn/played effect is intentionally undefined for now.
    }
}
