using UnityEngine;

public class OrbitMode : ICameraMode
{
    CameraController _ctrl;
    Camera           _cam;

    Vector3 _focus    = Vector3.zero;
    float   _distance = 30f;
    float   _yaw      = 35f;
    float   _pitch    = 50f;

    // Targets for smooth interpolation
    Vector3 _tFocus;
    float   _tDistance, _tYaw, _tPitch;

    // --- Tunable constants ---
    const float MinDist       = 4f;
    const float MaxDist       = 160f;   // enough to see the full 100×100 map
    const float MinPitch      = 8f;
    const float MaxPitch      = 85f;

    const float KeyPanBase    = 20f;    // world units / sec at reference distance
    const float PanRefDist    = 30f;    // distance at which KeyPanBase applies
    const float MousePanScale = 0.006f; // middle-mouse drag sensitivity (× distance)
    const float ZoomFraction  = 0.18f;  // scroll moves this fraction of current distance
    const float ZoomCursorPull= 0.35f;  // how far focus snaps toward cursor on scroll-in
    const float RotSpeed      = 140f;   // degrees / sec
    const float LerpFast      = 18f;    // zoom + rotate
    const float LerpSlow      = 12f;    // pan

    public void Enable(CameraController ctrl)
    {
        _ctrl = ctrl;
        _cam  = ctrl.Cam;
        _tFocus    = _focus;
        _tDistance = _distance;
        _tYaw      = _yaw;
        _tPitch    = _pitch;
    }

    public void Disable() { }

    public void Update()
    {
        Zoom();     // zoom before pan so cursor-pull uses updated _tDistance
        Pan();
        Rotate();

        float dt  = Time.deltaTime;
        _focus    = Vector3.Lerp(_focus,    _tFocus,    dt * LerpSlow);
        _distance = Mathf.Lerp(_distance,  _tDistance, dt * LerpFast);
        _yaw      = Mathf.Lerp(_yaw,       _tYaw,      dt * LerpFast);
        _pitch    = Mathf.Lerp(_pitch,     _tPitch,    dt * LerpFast);

        Apply();
    }

    // ── Input handlers ────────────────────────────────────────────────────────

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        // Proportional step — feels the same whether close or far
        float delta = scroll * _tDistance * ZoomFraction;
        float prevDist = _tDistance;
        _tDistance = Mathf.Clamp(_tDistance - delta, MinDist, MaxDist);

        // Zooming in: nudge focus toward the world point under the cursor
        if (scroll > 0 && _cam != null)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray   = _cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 cursorWorld = ray.GetPoint(enter);
                float   pullT       = (1f - _tDistance / prevDist) * ZoomCursorPull;
                _tFocus = Vector3.Lerp(_tFocus, cursorWorld, pullT);
            }
        }
    }

    void Pan()
    {
        // Speed scales linearly with distance so movement feels constant in screen space
        float speed = KeyPanBase * (_tDistance / PanRefDist) * Time.deltaTime;
        var   yawRot = Quaternion.Euler(0, _tYaw, 0);

        // Keyboard
        Vector3 keyInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) keyInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) keyInput += Vector3.back;
        if (Input.GetKey(KeyCode.A)) keyInput += Vector3.left;
        if (Input.GetKey(KeyCode.D)) keyInput += Vector3.right;
        if (keyInput != Vector3.zero)
            _tFocus += yawRot * keyInput.normalized * speed;

        // Middle-mouse drag — sensitivity also scales with distance
        if (Input.GetMouseButton(2))
        {
            float sens = _tDistance * MousePanScale;
            _tFocus -= yawRot * new Vector3(
                Input.GetAxis("Mouse X") * sens,
                0,
                Input.GetAxis("Mouse Y") * sens);
        }
    }

    void Rotate()
    {
        if (!Input.GetMouseButton(1)) return;
        float dt = Time.deltaTime;
        _tYaw  += Input.GetAxis("Mouse X") * RotSpeed * dt;
        _tPitch = Mathf.Clamp(
            _tPitch - Input.GetAxis("Mouse Y") * RotSpeed * dt,
            MinPitch, MaxPitch);
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    void Apply()
    {
        var rot = Quaternion.Euler(_pitch, _yaw, 0);
        _ctrl.transform.position = _focus + rot * new Vector3(0, 0, -_distance);
        _ctrl.transform.rotation = rot;
    }
}
