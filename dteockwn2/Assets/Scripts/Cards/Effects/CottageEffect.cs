using UnityEngine;

/// <summary>
/// Playing the Cottage card queues a new villager to spawn next Morning.
/// The cottage position is stamped in via BindToBuilding when the building completes.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Cottage", fileName = "CottageEffect")]
public class CottageEffect : CardEffect
{
    [HideInInspector] public Vector3 cottagePosition;
    [HideInInspector] public bool    hasCottagePosition;

    public override CardEffect BindToBuilding(Vector3 buildingPos)
    {
        var inst = CreateInstance<CottageEffect>();
        inst.cottagePosition    = buildingPos;
        inst.hasCottagePosition = true;
        return inst;
    }

    public override Vector3? GetBuildingHint() =>
        hasCottagePosition ? cottagePosition : (Vector3?)null;

    public override void Resolve(CardPlayContext ctx) =>
        ctx.deckManager.QueueVillagerSpawn(
            hasCottagePosition ? cottagePosition : Vector3.zero);
}
