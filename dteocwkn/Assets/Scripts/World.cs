using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour
{
    public GameObject tilePrefab;
    private InputAction pointAction;
    private List<List<GameObject>> tiles = new List<List<GameObject>>();
    private GameObject highlightedTile = null;

    void Start()
    {
        pointAction = InputSystem.actions.FindAction("Point");
        Debug.Log("got point action" + pointAction);

        Transform spawnPoint = transform.Find("SpawnPoint");
        tiles = new List<List<GameObject>>();

        for (int i = -5; i < 5; i++)
        {
            List<GameObject> row = new List<GameObject>();
            for (int j = -5; j < 5; j++)
            {
                GameObject newTile = Instantiate(tilePrefab, transform);
                newTile.transform.position = new Vector3(
                    spawnPoint.position.x + i, 
                    0, // Y is now height (keep at 0 for flat ground)
                    spawnPoint.position.z + j
                );
                newTile.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f); // Slightly smaller than 1 to show grid gaps
                newTile.name = "Tile_" + i + "_" + j;

                row.Add(newTile);
            }

            tiles.Add(row);
        }
    }

    void Update()
    {
        Vector2 pointerPosition = pointAction.ReadValue<Vector2>();
        
        // Raycast from camera to world
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if we hit a tile
            if (hitObject.name.StartsWith("Tile_"))
            {
                // Unhighlight previous tile
                if (highlightedTile != null && highlightedTile != hitObject)
                {
                    Tile prevTileScript = highlightedTile.GetComponent<Tile>();
                    if (prevTileScript != null)
                    {
                        prevTileScript.Highlight(false);
                    }
                }
                
                // Highlight new tile
                highlightedTile = hitObject;
                Tile tileScript = hitObject.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.Highlight(true);
                }
            }
        }
        else
        {
            // No hit, unhighlight
            if (highlightedTile != null)
            {
                Tile tileScript = highlightedTile.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.Highlight(false);
                }
                highlightedTile = null;
            }
        }
    }
}