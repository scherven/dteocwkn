using UnityEngine;
using UnityEngine.EventSystems;

public class CameraControls : MonoBehaviour
{
    public float Speed = 0.1f;

    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal") * Speed;
        float yAxisValue = Input.GetAxis("Vertical") * Speed;
        transform.position = new Vector3(transform.position.x + xAxisValue, transform.position.y + yAxisValue, transform.position.z);
    }
    void Start()
    {
        addPhysicsRaycaster();
    }

    void addPhysicsRaycaster()
    {
        Physics2DRaycaster physicsRaycaster = GameObject.FindObjectOfType<Physics2DRaycaster>();
        if (physicsRaycaster == null)
        {
            Camera.main.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }
}
