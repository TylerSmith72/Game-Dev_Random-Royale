using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;


public class SeedGenerator : NetworkBehaviour
{
    private GameObject player;
    public GameObject gameManager;
    public GameObject meshGenerator;
    private Dictionary<int, string> wordDictionary = new Dictionary<int, string>();
    private string seed = "default";

    void Awake()
    {
        PopulateDictionary();
    }

    private void Update()
    {
        if (IsServerStarted && Input.GetKeyDown(KeyCode.L))
        {
            GenerateSeed();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateSeed(); // Only the server should generate the seed
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) // Ensure only new clients get the seed
        {
            RequestSeedFromServer(LocalConnection);
        }
    }

    // Ask the server to send the current seed
    [ServerRpc(RequireOwnership = false)]
    private void RequestSeedFromServer(NetworkConnection conn)
    {
        SendSeedToClientTargetRpc(conn, seed);
    }

    private void GenerateSeed()
    {
        List<string> selectedWords = GetRandomWords(3);
        string joined = string.Join("", selectedWords);
        //joined = "default"; // For Testing
        SetSeed(joined);

        if (IsServerStarted) // Only server should send the RPC
        {
            SetSeedServerRpc(joined);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSeedServerRpc(string newSeed)
    {
        seed = newSeed;
        SendSeedToClients(newSeed);
    }

    [ObserversRpc] // Send seed to all clients
    private void SendSeedToClients(string newSeed)
    {
        if (player != null)
        {
            player.GetComponent<PlayerSetup>().RespawnPlayer();
        } else {
            Debug.Log("Player is null, cannot respawn.");
        }

        SetSeed(newSeed);
        ReloadTerrain();
    }

    [TargetRpc] // Send seed when new player connects
    private void SendSeedToClientTargetRpc(NetworkConnection conn, string newSeed)
    {
        // If hasn't loaded terrain OR seed is different, reload terrain
        if(meshGenerator.GetComponent<MeshGenerator>().hasLoadedTerrain == false || meshGenerator.GetComponent<MeshGenerator>().seedString != newSeed){
            SetSeed(newSeed);
            ReloadTerrain();
        }
        
    }

    public void SetSeed(string newSeed)
    {
        Debug.Log("Set seed to: "+ newSeed);
        seed = newSeed;
    }
    private void ReloadTerrain()
    {
        Debug.Log("Reloading terrain with new seed: " + seed);
        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.StartTerrain(); // Ensure terrain regenerates
    }

    public string GetSeed()
    {
        return seed;
    }

    private void PopulateDictionary()
    {
        wordDictionary.Add(1, "Mountain");
        wordDictionary.Add(2, "River");
        wordDictionary.Add(3, "Cave");
        wordDictionary.Add(4, "Forest");
        wordDictionary.Add(5, "Desert");
        wordDictionary.Add(6, "Valley");
        wordDictionary.Add(7, "Ocean");
        wordDictionary.Add(8, "Glacier");
        wordDictionary.Add(9, "Cliff");
        wordDictionary.Add(10, "Canyon");
        wordDictionary.Add(11, "Marsh");
        wordDictionary.Add(12, "Hills");
        wordDictionary.Add(13, "Meadow");
        wordDictionary.Add(14, "Volcano");
        wordDictionary.Add(15, "Island");
        wordDictionary.Add(16, "Prairie");
        wordDictionary.Add(17, "Lagoon");
        wordDictionary.Add(18, "Peninsula");
        wordDictionary.Add(19, "Plateau");
        wordDictionary.Add(20, "Jungle");
        wordDictionary.Add(21, "Dune");
        wordDictionary.Add(22, "Steppe");
        wordDictionary.Add(23, "Savannah");
        wordDictionary.Add(24, "Geyser");
        wordDictionary.Add(25, "Waterfall");
        Debug.Log("Word dictionary populated with " + wordDictionary.Count + " words.");
    }

    private List<string> GetRandomWords(int count)
    {
        List<string> randomWords = new List<string>();
        List<int> keys = new List<int>(wordDictionary.Keys);
        System.Random random = new System.Random();

        while (randomWords.Count < count && keys.Count > 0)
        {
            // Get a random index
            int randomIndex = random.Next(keys.Count);

            // Fetch the word at that index
            int key = keys[randomIndex];
            randomWords.Add(wordDictionary[key]);

            // Remove the key to avoid duplicates
            keys.RemoveAt(randomIndex);
        }

        return randomWords;
    }

    public void SetPlayer(GameObject player)
    {
        Debug.Log("Setting player in SeedGenerator: " + player);
        this.player = player;
    }
}
