using UnityEngine;

/// <summary>
/// Directly adds a fixed quantity of resources to the inventory.
/// Used as the job-card effect for assigned villagers (e.g., Woodcutter, Farmer).
/// Stateless — safe to share across multiple card instances.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/GiveResource", fileName = "GiveResourceEffect")]
public class GiveResourceEffect : CardEffect
{
    public ResourceType resourceType = ResourceType.Wood;
    public int          amount       = 1;

    public override void Resolve(CardPlayContext ctx) =>
        ctx.inventory.Add(resourceType, amount);
}
