// Project: FPS Game
// This script is responsible for spawning ammo pickups at random spawn points in the game world. It checks if the pickups are destroyed and respawns them after a specified delay.
// The script uses Unity's MonoBehaviour class and includes methods for spawning prefabs, checking if they are destroyed, and managing the spawn points.
// It also includes a public variable for the prefab to be spawned, a list of spawn points, and a delay for respawning the pickups.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupsSpawner : MonoBehaviour
{
    public List<Transform> spawnPoints;
    public GameObject prefab;
    public float delay = 2f;
    public int amount; // Delay in seconds
    private void Start()
    {
        SpawnPrefabs();
    }
    public void SpawnPrefabs()
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnPrefab();
        }

        // Start a coroutine to check if the prefabs are destroyed
        StartCoroutine(CheckPrefabDestroyed());
    }

    private void SpawnPrefab()
    {
        // Choose a random transform from the spawnPoints list
        int index = Random.Range(0, spawnPoints.Count);
        Transform spawnPoint = spawnPoints[index];

        // Instantiate the prefab at the chosen spawn point
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }

    private IEnumerator CheckPrefabDestroyed()
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);

            // Check if both prefabs are destroyed
            GameObject[] activePrefabs = GameObject.FindGameObjectsWithTag("ammo");
            if (activePrefabs.Length < 1)
            {
                for (int i = 0; i < amount; i++)
                {
                    SpawnPrefab();
                }
            }
        }
    }
}
