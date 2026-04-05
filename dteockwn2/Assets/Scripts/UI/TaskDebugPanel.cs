using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating debug window (toggle T) that shows the TaskDispatcher queue
/// and each villager's active task in real time.
/// </summary>
public class TaskDebugPanel : MonoBehaviour
{
    TextMeshProUGUI _text;
    bool _visible = true;

    void Awake() => _text = GetComponentInChildren<TextMeshProUGUI>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) Toggle();
        if (_visible) Refresh();
    }

    void Toggle()
    {
        _visible = !_visible;
        if (_text != null) _text.transform.parent.gameObject.SetActive(_visible);
    }

    void Refresh()
    {
        if (_text == null) return;

        var td = TaskDispatcher.Instance;
        var vm = VillagerManager.Instance;
        if (td == null || vm == null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"<b>Task Queue  [{td.QueueCount}]</b>");

        int shown = 0;
        foreach (var t in td.QueueSnapshot)
        {
            if (shown++ >= 8) { sb.AppendLine("  ..."); break; }
            sb.AppendLine($"  {t.Type}  →  {t.TargetPosition:F0}");
        }

        sb.AppendLine();
        sb.AppendLine("<b>Villagers</b>");
        foreach (var v in VillagerManager.Instance.AllVillagers)
        {
            var task = v.CurrentTask;
            string line = task == null
                ? $"  {v.name}  <color=grey>idle</color>"
                : $"  {v.name}  <color=yellow>{task.Type}</color>  →  {task.TargetPosition:F0}";
            sb.AppendLine(line);
        }

        _text.text = sb.ToString();
    }
}
