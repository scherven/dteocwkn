using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    public Transform tile;
    private InputAction pointAction;
    private List<List<Transform>> tiles = new List<List<Transform>>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pointAction = InputSystem.actions.FindAction("Point");
        Debug.Log("got point action" + pointAction);

        Transform spawnPoint = transform.Find("SpawnPoint");
        tiles = new List<List<Transform>>();

        for (int i = -5; i < 5; i++)
        {
            List<Transform> row = new List<Transform>();
            for (int j = -5; j < 5; j++)
            {
                Transform newTile = Instantiate(tile, spawnPoint.position, Quaternion.identity, transform);
                newTile.position = new Vector3(spawnPoint.position.x + i, spawnPoint.position.y + j, spawnPoint.position.z);
                newTile.localScale = new Vector3(6.25f, 6.25f, 1);
                newTile.name = "Tile_" + i + "_" + j;

                row.Add(newTile);
            }

            tiles.Add(row);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 pointerPosition = pointAction.ReadValue<Vector2>();
        // Debug.Log(pointerPosition);

        // var rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pointerPosition));
        // if (!rayHit.collider) return;

        // Transform hitTransform = rayHit.collider.transform;
        // Debug.Log("hit transform " + hitTransform.name);
    }
}
