using UnityEngine;

public class FollowMode : ICameraMode
{
    CameraController _ctrl;
    VillagerAgent    _target;
    float _yaw, _pitch;

    const float EyeHeight = 1.8f;
    const float LookSpeed = 2f;

    public void Enable(CameraController ctrl)
    {
        _ctrl = ctrl;
        if (_target != null) SyncAngles();
    }

    public void Disable() { }

    public void SetTarget(VillagerAgent villager)
    {
        _target = villager;
        if (villager != null) SyncAngles();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _ctrl.SetMode(CameraController.CameraMode.Orbit);
            return;
        }

        if (_target == null) return;

        _yaw   += Input.GetAxis("Mouse X") * LookSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * LookSpeed;
        _pitch  = Mathf.Clamp(_pitch, -45f, 45f);

        _ctrl.transform.position = _target.transform.position + Vector3.up * EyeHeight;
        _ctrl.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
    }

    void SyncAngles()
    {
        _yaw   = _target.transform.eulerAngles.y;
        _pitch = 0f;
    }
}
