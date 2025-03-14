using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
    public int xSize = 4096;
    public int zSize = 4096;
    public int chunkSize = 32;
    public float treeDensity = 100f;
    public float treeFrequency = 100f;
    public float treeRadius = 1000f;
    public GameObject player;
    public GameObject treePrefab;
    public GameObject treeParent;

    public MeshGenerator meshGenerator;

    private float treeUpdateInterval = 0.5f; // Update every 0.5 seconds
    private float nextTreeUpdateTime = 0f;

    private HashSet<Vector3> TreePositions = new HashSet<Vector3>();
    private Dictionary<Vector3, GameObject> ActiveTrees = new Dictionary<Vector3, GameObject>();

    private Queue<Vector3> treeQueue = new Queue<Vector3>(); // To store trees waiting to be instantiated
    private HashSet<Vector3> loadedPositions = new HashSet<Vector3>(); // To track already loaded trees
    private int maxTreesPerFrame = 20; // Number of trees to load per tree update

    void Update()
    {
        if (Time.time >= nextTreeUpdateTime)
        {
            // Update the queue with positions within the radius
            UpdateTreeQueue();

            // Load trees in batches
            LoadTreeBatch();

            // Unload trees outside the radius
            UnloadTreesOutsideRadius();

            nextTreeUpdateTime = Time.time + treeUpdateInterval; // Throttle the update
        }
    }

    public void StartTreeGeneration(string seedString)
    {
        // Generate all tree positions
        float seed = GetSeedFromString(seedString);
        GenerateTreeData(seed);

        // Clear previous state (if this is a reset/restart)
        treeQueue.Clear();
        loadedPositions.Clear();
        ActiveTrees.Clear();

        // Force Update Trees On Load
        UpdateTreeQueue();
        LoadTreeBatch();
    }

    public void GenerateTreeData(float seed)
    {
        int xOffset = xSize / 2;
        int zOffset = zSize / 2;

        // Same seed -> Same random values
        UnityEngine.Random.InitState((int)seed);
        float offsetX = UnityEngine.Random.Range(0, 99999f);
        float offsetZ = UnityEngine.Random.Range(0, 99999f);

        for (int z = -zOffset; z < zSize - zOffset; z += 5)
        {
            for (int x = -xOffset; x < xSize - xOffset; x += 5)
            {
                float noise = Mathf.PerlinNoise(
                    (x + xOffset + offsetX) * (treeFrequency / 100f),
                    (z + zOffset + offsetZ) * (treeFrequency / 100f)
                );

                if (noise > treeDensity)
                {
                    // Determine the chunk that contains the point
                    Vector2Int chunkCoord = new Vector2Int(
                        Mathf.FloorToInt((float)x / chunkSize),
                        Mathf.FloorToInt((float)z / chunkSize)
                    );

                    // Check if chunk data exists
                    if (meshGenerator.terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
                    {
                        // Convert world position to local chunk position
                        float localX = ((x % chunkSize) + chunkSize) % chunkSize;
                        float localZ = ((z % chunkSize) + chunkSize) % chunkSize;

                        // Get the height from mesh data
                        float groundY = GetHeightFromMeshData(meshData, localX, localZ, chunkSize) + 3;

                        if (groundY > 200 || groundY < 10)
                        {
                            continue;
                        }

                        // Add the tree position with the correct y value
                        TreePositions.Add(new Vector3(x, groundY, z));
                    }
                    else
                    {
                        Debug.LogWarning($"No MeshData found for chunk: {chunkCoord}");
                    }
                }
            }
        }
    }

    private float GetHeightFromMeshData(MeshData meshData, float localX, float localZ, int chunkSize)
    {
        int resolution = Mathf.RoundToInt(Mathf.Sqrt(meshData.vertices.Length)); // Assuming a grid layout
        float vertexSpacing = (float)chunkSize / (resolution - 1); // Distance between vertices

        // Calculate indices of the vertices surrounding the point
        int xIndex = Mathf.FloorToInt(localX / vertexSpacing);
        int zIndex = Mathf.FloorToInt(localZ / vertexSpacing);

        // Ensure indices are within bounds
        xIndex = Mathf.Clamp(xIndex, 0, resolution - 1);
        zIndex = Mathf.Clamp(zIndex, 0, resolution - 1);

        int vertexIndex = zIndex * resolution + xIndex;

        // Return the y-value of the vertex
        return meshData.vertices[vertexIndex].y;
    }



    private void UpdateTreeQueue()
    {
        Vector3 playerPos = player.transform.position;

        foreach (Vector3 position in TreePositions)
        {
            if (!loadedPositions.Contains(position) && (position - playerPos).sqrMagnitude <= treeRadius * treeRadius)
            {
                treeQueue.Enqueue(position); // Add to the queue
                loadedPositions.Add(position); // Mark as loaded
            }
        }
    }

    private void LoadTreeBatch()
    {
        int treesToInstantiate = Mathf.Min(maxTreesPerFrame, treeQueue.Count); // Limit batch size

        for (int i = 0; i < treesToInstantiate; i++)
        {
            if (treeQueue.TryDequeue(out Vector3 treePosition))
            {
                GameObject tree = Instantiate(treePrefab, treePosition, Quaternion.identity, treeParent.transform);
                ActiveTrees[treePosition] = tree; // Track instantiated trees
            }
        }
    }

    private void UnloadTreesOutsideRadius()
    {
        Vector3 playerPos = player.transform.position;
        List<Vector3> treesToUnload = new List<Vector3>();

        // Find trees to unload
        foreach (var kvp in ActiveTrees)
        {
            Vector3 position = kvp.Key;
            GameObject tree = kvp.Value;

            // Check if the tree is outside the radius
            if ((position - playerPos).sqrMagnitude > treeRadius * treeRadius)
            {
                Destroy(tree); // Destroy the GameObject
                treesToUnload.Add(position); // Mark the position for removal
            }
        }

        // Clean up the dictionary and loaded positions
        foreach (Vector3 position in treesToUnload)
        {
            ActiveTrees.Remove(position); // Remove from the ActiveTrees dictionary
            loadedPositions.Remove(position); // Remove from the loadedPositions set
        }
    }

    float GetSeedFromString(string seedString)
    {
        int hash = Math.Abs(seedString.GetHashCode());
        float normalizedSeed = hash;
        return normalizedSeed;
    }
}
