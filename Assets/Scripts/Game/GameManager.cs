using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    public PlayerSpawner playerSpawner;
    public GameObject terrainManagerObject;

    public void Awake()
    {
        terrainManagerObject = FindObjectOfType<SeedGenerator>()?.gameObject;
        Debug.Log("TerrainManager found: " + (terrainManagerObject != null));
    }

    // public void SetPlayer(GameObject player)
    // {
    //     Debug.Log("Setting player in GameManager: " + player);
    //     this.player = player;
    // }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRequestRespawn(NetworkConnection conn){
        Debug.Log("Respawning player...");

        // Ensure the connection has a player object to respawn
        if (conn.FirstObject != null)
        {
            ServerManager.Despawn(conn.FirstObject);

            // Get a random spawn point
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (playerSpawner != null && playerSpawner.Spawns.Length > 0)
            {
                int randomIndex = Random.Range(0, playerSpawner.Spawns.Length);
                Transform spawnPoint = playerSpawner.Spawns[randomIndex];
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
            }

            GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            ServerManager.Spawn(newPlayer, conn);
            
            newPlayer.GetComponent<PlayerSetup>().SetGameManager(gameObject);
            terrainManagerObject.GetComponent<SeedGenerator>().SetPlayer(newPlayer);
            Debug.Log("Player respawned successfully.");
        }
        else
        {
            Debug.LogWarning("No player object found for the connection.");
        }
    }

    public void RegisterPlayer(GameObject player)
    {
        Debug.Log("Registering player in GameManager: " + player);
        player.GetComponent<PlayerSetup>().SetGameManager(this.gameObject);
    }
}
