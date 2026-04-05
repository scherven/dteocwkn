using UnityEngine;

// Stub base class. Override in a subclass to drive actual animations.
public class VillagerAnimator : MonoBehaviour
{
    public virtual void OnPickUp()    { }
    public virtual void OnDeposit()   { }
    public virtual void OnBuildTick() { }
    public virtual void OnIdle()      { }
}
