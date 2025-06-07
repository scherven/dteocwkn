using UnityEngine;

public class World : MonoBehaviour
{
    public Transform tile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform spawnPoint = transform.Find("SpawnPoint");

        for (int i = -20; i < 20; i++)
        {
            for (int j = -20; j < 20; j++)
            {
                Transform newTile = Instantiate(tile, spawnPoint.position, Quaternion.identity, transform);
                newTile.position = new Vector3(spawnPoint.position.x + i, spawnPoint.position.y + j, spawnPoint.position.z);
                newTile.localScale = new Vector3(6, 6, 1);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
