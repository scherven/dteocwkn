using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float Speed = 0.1f;

    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal") * Speed;
        float yAxisValue = Input.GetAxis("Vertical") * Speed;
        transform.position = new Vector3(transform.position.x + xAxisValue, transform.position.y + yAxisValue, transform.position.z);
    }
}
