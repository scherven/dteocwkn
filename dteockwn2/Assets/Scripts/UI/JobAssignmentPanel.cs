using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IMGUI debug panel (press J to toggle) for assigning villagers to completed job buildings.
///
/// Workflow:
///   1. Click a villager name in "Unassigned" to select them (highlighted in yellow).
///   2. Click "Assign here" under any building with a free slot to assign the selected villager.
///   3. Click [X] next to an assigned villager to unassign them.
///   4. Clicking an already-selected villager deselects them.
/// </summary>
public class JobAssignmentPanel : MonoBehaviour
{
    bool           _visible;
    Vector2        _scroll;
    VillagerAgent  _selected;

    static readonly Rect PanelRect = new Rect(10, 530, 310, 420);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            _visible = !_visible;
    }

    void OnGUI()
    {
        if (!_visible) return;

        var bm = BuildingManager.Instance;
        var vm = VillagerManager.Instance;
        if (bm == null || vm == null) return;

        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.Label("Job Assignments  [J to close]");
        GUILayout.Space(2);

        _scroll = GUILayout.BeginScrollView(_scroll);

        DrawBuildingsSection(bm, vm);
        GUILayout.Space(8);
        DrawUnassignedSection(vm);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawBuildingsSection(BuildingManager bm, VillagerManager vm)
    {
        GUILayout.Label("── Buildings with job slots ──");

        bool anyJobBuilding = false;
        foreach (var (def, pos, _) in bm.AllBuildings)
        {
            if (def.jobSlots == 0) continue;
            anyJobBuilding = true;

            List<VillagerAgent> occupants = vm.GetOccupantsFor(def, pos);
            bool unlimited = def.jobSlots < 0;
            int  freeSlots = unlimited ? 1 : def.jobSlots - occupants.Count;
            string slotText = unlimited ? $"{occupants.Count}/∞" : $"{occupants.Count}/{def.jobSlots}";

            GUILayout.Label($"{def.buildingName}  ({slotText})");

            foreach (var occ in occupants)
            {
                var captured = occ;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  · {occ.name}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(28)))
                {
                    vm.UnassignJob(captured);
                    if (_selected == captured) _selected = null;
                }
                GUILayout.EndHorizontal();
            }

            if (freeSlots > 0 && _selected != null && vm.GetJobDef(_selected) == null)
            {
                if (GUILayout.Button($"  + Assign {_selected.name} here"))
                {
                    vm.AssignJob(_selected, def, pos);
                    _selected = null;
                }
            }
            else if (freeSlots > 0)
            {
                var old = GUI.color;
                GUI.color = Color.grey;
                GUILayout.Label("  (select a villager below to assign)");
                GUI.color = old;
            }

            GUILayout.Space(4);
        }

        if (!anyJobBuilding)
        {
            var old = GUI.color;
            GUI.color = Color.grey;
            GUILayout.Label("  No job buildings completed yet.");
            GUI.color = old;
        }
    }

    void DrawUnassignedSection(VillagerManager vm)
    {
        GUILayout.Label("── Unassigned Villagers ──");

        bool anyUnassigned = false;
        foreach (var v in vm.AllVillagers)
        {
            if (vm.GetJobDef(v) != null) continue;
            anyUnassigned = true;

            bool isSelected = _selected == v;
            var  captured   = v;

            var old = GUI.color;
            GUI.color = isSelected ? Color.yellow : Color.white;

            if (GUILayout.Button(isSelected ? $"» {v.name}  (selected)" : v.name))
                _selected = (_selected == captured) ? null : captured;

            GUI.color = old;
        }

        if (!anyUnassigned)
        {
            var old = GUI.color;
            GUI.color = Color.grey;
            GUILayout.Label("  All villagers have jobs.");
            GUI.color = old;
        }

        GUILayout.Space(4);
        GUILayout.Label("── Assigned Villagers ──");

        bool anyAssigned = false;
        foreach (var v in vm.AllVillagers)
        {
            var jobDef = vm.GetJobDef(v);
            if (jobDef == null) continue;
            anyAssigned = true;

            var captured = v;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {v.name}  →  {jobDef.buildingName}", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(28)))
                vm.UnassignJob(captured);
            GUILayout.EndHorizontal();
        }

        if (!anyAssigned)
        {
            var old = GUI.color;
            GUI.color = Color.grey;
            GUILayout.Label("  No villagers assigned.");
            GUI.color = old;
        }
    }
}
