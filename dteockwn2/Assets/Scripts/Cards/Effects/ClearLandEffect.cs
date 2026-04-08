using UnityEngine;

/// <summary>
/// Clear Land — player clicks a 2×2 area on the terrain to prepare it for building.
/// Only prepared plots accept building placement.
/// </summary>
[CreateAssetMenu(menuName = "Cardstone/Effects/Clear Land", fileName = "ClearLandEffect")]
public class ClearLandEffect : CardEffect
{
    public override void Resolve(CardPlayContext ctx) =>
        GridManager.Instance.BeginPreparePlotSelection();
}
