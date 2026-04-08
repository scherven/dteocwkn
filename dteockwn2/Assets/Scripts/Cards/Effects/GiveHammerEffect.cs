using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Effects/Give Hammer", fileName = "GiveHammerEffect")]
public class GiveHammerEffect : CardEffect
{
    /// <summary>Add one hammer to the bank; it distributes automatically at end of day.</summary>
    public override void Resolve(CardPlayContext ctx) => ctx.hammerManager.AddHammer(1);
}
