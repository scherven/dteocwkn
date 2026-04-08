using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Effects/Give Hammer", fileName = "GiveHammerEffect")]
public class GiveHammerEffect : CardEffect
{
    [HideInInspector] public int amount = 1;

    /// <summary>Add hammers to the bank; they distribute automatically at end of day.</summary>
    public override void Resolve(CardPlayContext ctx) => ctx.hammerManager.AddHammer(amount);
}
