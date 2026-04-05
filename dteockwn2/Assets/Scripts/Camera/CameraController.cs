using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum CameraMode { Orbit, FreeFly, Follow }

    [SerializeField] CameraMode startMode = CameraMode.Orbit;

    public CameraMode ActiveMode { get; private set; }
    public Camera Cam { get; private set; }

    Camera      _cam;
    OrbitMode   _orbit;
    FreeFlyMode _freeFly;
    FollowMode  _follow;
    ICameraMode _current;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        Cam  = _cam;
        _orbit   = new OrbitMode();
        _freeFly = new FreeFlyMode();
        _follow  = new FollowMode();
    }

    void Start() => SetMode(startMode);

    void Update()
    {
        _current?.Update();
        HandleVillagerClick();
    }

    public void SetMode(CameraMode mode)
    {
        _current?.Disable();
        ActiveMode = mode;
        _current = mode switch
        {
            CameraMode.Orbit   => _orbit,
            CameraMode.FreeFly => _freeFly,
            CameraMode.Follow  => _follow,
            _                  => _orbit
        };
        _current.Enable(this);
    }

    /// <summary>Click a villager in Orbit mode to enter Follow mode.</summary>
    void HandleVillagerClick()
    {
        if (ActiveMode != CameraMode.Orbit) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var villager = hit.collider.GetComponent<VillagerAgent>();
            if (villager != null)
            {
                _follow.SetTarget(villager);
                SetMode(CameraMode.Follow);
            }
        }
    }
}
