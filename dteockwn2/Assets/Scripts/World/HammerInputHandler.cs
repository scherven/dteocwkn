using UnityEngine;

/// <summary>
/// Detects left-clicks on unfinished BuildSites and spends one hammer from the bank.
/// Suppressed while PlacementMode or RoadTool are active.
/// Uses RaycastAll with trigger detection so the BuildSite's trigger collider
/// is found even if the terrain plane is the primary hit.
/// </summary>
public class HammerInputHandler : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (PlacementMode.Instance  != null && PlacementMode.Instance.IsActive)  return;
        if (RoadTool.Instance       != null && RoadTool.Instance.IsActive)       return;
        if (ClearLandTool.Instance  != null && ClearLandTool.Instance.IsActive)  return;

        Ray ray  = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            var site = hit.collider.GetComponentInParent<BuildSite>();
            if (site == null || site.IsComplete) continue;
            site.TryApplyOneHammer();
            return;
        }
    }
}
