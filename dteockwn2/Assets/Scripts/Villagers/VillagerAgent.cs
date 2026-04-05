using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(VillagerNeeds))]
[RequireComponent(typeof(VillagerAnimator))]
public class VillagerAgent : MonoBehaviour
{
    const float ArriveDist   = 0.4f;
    const float PickupPause  = 0.4f; // seconds to "pick up" a resource
    const float CarryHeight  = 2.2f; // local Y offset above villager while carrying

    NavMeshAgent   _nav;
    VillagerAnimator _anim;
    VillagerTask   _currentTask;
    GameObject     _carried;

    public bool IsIdle => _currentTask == null;
    public VillagerTask CurrentTask => _currentTask;

    void Awake()
    {
        _nav  = GetComponent<NavMeshAgent>();
        _anim = GetComponent<VillagerAnimator>();
    }

    void Start()
    {
        VillagerManager.Instance.RegisterVillager(this);
        _anim.OnIdle();
    }

    void OnDestroy() => VillagerManager.Instance?.UnregisterVillager(this);

    public void AssignTask(VillagerTask task)
    {
        _currentTask = task;
        StopAllCoroutines();
        StartCoroutine(Execute(task));
    }

    IEnumerator Execute(VillagerTask task)
    {
        switch (task.Type)
        {
            case TaskType.WalkTo:
                yield return WalkTo(task.TargetPosition);
                break;
            case TaskType.PickUpResource:
                yield return DoPickUp(task);
                break;
            case TaskType.DeliverResource:
                yield return DoDeliver(task);
                break;
            case TaskType.Build:
                yield return DoBuild(task);
                break;
        }

        task.OnComplete?.Invoke();
        _currentTask = null;

        if (task.FollowUp != null)
            AssignTask(task.FollowUp);
        else
        {
            _anim.OnIdle();
            TaskDispatcher.Instance.OnTaskCompleted(this);
        }
    }

    IEnumerator WalkTo(Vector3 pos)
    {
        _nav.SetDestination(pos);
        yield return null; // one frame for path to start computing
        while (_nav.pathPending || _nav.remainingDistance > ArriveDist)
            yield return null;
        _nav.ResetPath();
    }

    IEnumerator DoPickUp(VillagerTask task)
    {
        yield return WalkTo(task.TargetPosition);
        yield return new WaitForSeconds(PickupPause);

        if (task.TargetObject != null)
        {
            _carried = task.TargetObject;
            _carried.transform.SetParent(transform);
            _carried.transform.localPosition = new Vector3(0, CarryHeight, 0);
            // Remove collider so it doesn't interfere with raycasts
            if (_carried.TryGetComponent<Collider>(out var col)) col.enabled = false;
        }

        _anim.OnPickUp();
    }

    IEnumerator DoDeliver(VillagerTask task)
    {
        yield return WalkTo(task.TargetPosition);

        if (_carried != null)
        {
            if (task.DepositAtDestination && task.TargetObject != null)
            {
                var site = task.TargetObject.GetComponent<BuildSite>();
                if (site != null)
                    site.ReceiveMaterial(_carried);
                else
                    Object.Destroy(_carried);
            }
            else
            {
                Object.Destroy(_carried);
            }
            _carried = null;
        }

        _anim.OnDeposit();
    }

    IEnumerator DoBuild(VillagerTask task)
    {
        yield return WalkTo(task.TargetPosition);

        var site = task.TargetObject != null
            ? task.TargetObject.GetComponent<BuildSite>()
            : null;

        if (site == null) yield break;

        while (site != null && !site.IsComplete)
        {
            site.AddProgress(Time.deltaTime / site.Definition.constructionTimeBase);
            _anim.OnBuildTick();
            yield return null;
        }
    }
}
