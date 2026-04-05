using System.Collections.Generic;
using UnityEngine;

public class TaskDispatcher : MonoBehaviour
{
    public static TaskDispatcher Instance { get; private set; }

    readonly Queue<VillagerTask> _queue = new();

    public int QueueCount => _queue.Count;
    public IEnumerable<VillagerTask> QueueSnapshot => _queue;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update() => TryAssignNext();

    /// <summary>Enqueue a task. An idle villager will be assigned on the next poll.</summary>
    public void EnqueueTask(VillagerTask task) => _queue.Enqueue(task);

    /// <summary>Assign a specific task to a specific villager, bypassing the queue.</summary>
    public void AssignTask(VillagerTask task, VillagerAgent villager) => villager.AssignTask(task);

    /// <summary>Called by VillagerAgent when it completes a task. Triggers immediate queue poll.</summary>
    public void OnTaskCompleted(VillagerAgent villager) => TryAssignNext();

    void TryAssignNext()
    {
        while (_queue.Count > 0)
        {
            var villager = VillagerManager.Instance?.GetIdleVillager();
            if (villager == null) break;
            villager.AssignTask(_queue.Dequeue());
        }
    }
}
