using System;
using UnityEngine;

public enum TaskType { Idle, WalkTo, PickUpResource, DeliverResource, Build }

public class VillagerTask
{
    public TaskType Type;
    public Vector3 TargetPosition;
    public GameObject TargetObject; // optional

    /// <summary>
    /// When true on a DeliverResource task, the carried object is left at the
    /// destination (handed to TargetObject's BuildSite) instead of destroyed.
    /// </summary>
    public bool DepositAtDestination;

    /// <summary>Executed by the same villager immediately after this task completes.</summary>
    public VillagerTask FollowUp;

    /// <summary>Called once after this task (not the follow-up) finishes.</summary>
    public Action OnComplete;
}
