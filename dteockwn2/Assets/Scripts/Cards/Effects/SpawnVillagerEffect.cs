using UnityEngine;

[CreateAssetMenu(menuName = "Cardstone/Effects/Spawn Villager", fileName = "SpawnVillagerEffect")]
public class SpawnVillagerEffect : CardEffect
{
    public override void Resolve(CardPlayContext ctx)
    {
        // Stub — instantiate a villager capsule at a map edge and register it.
        // Full implementation deferred until VillagerNeeds and job system are wired.
        Debug.Log("[SpawnVillagerEffect] Stub: would spawn a villager here.");
    }
}
