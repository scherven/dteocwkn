using UnityEngine;

public class FreeFlyMode : ICameraMode
{
    CameraController _ctrl;
    float _yaw, _pitch;

    const float MoveSpeed = 20f;
    const float LookSpeed = 2f;

    public void Enable(CameraController ctrl)
    {
        _ctrl  = ctrl;
        _yaw   = ctrl.transform.eulerAngles.y;
        _pitch = ctrl.transform.eulerAngles.x;
    }

    public void Disable() { }

    public void Update()
    {
        _yaw   += Input.GetAxis("Mouse X") * LookSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * LookSpeed;
        _pitch  = Mathf.Clamp(_pitch, -89f, 89f);
        _ctrl.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) move += Vector3.back;
        if (Input.GetKey(KeyCode.A)) move += Vector3.left;
        if (Input.GetKey(KeyCode.D)) move += Vector3.right;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;

        _ctrl.transform.position += _ctrl.transform.rotation * move * MoveSpeed * Time.deltaTime;
    }
}
