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
    private GameObject player;
    public GameObject treePrefab;
    public GameObject treeParent;

    public MeshGenerator meshGenerator;

    private float treeUpdateInterval = 0.5f; // Update every 0.5 seconds
    private float nextTreeUpdateTime = 0f;

    private Dictionary<Vector3, Quaternion> TreeData = new Dictionary<Vector3, Quaternion>();
    private Dictionary<Vector3, GameObject> ActiveTrees = new Dictionary<Vector3, GameObject>();

    private Queue<Vector3> treeQueue = new Queue<Vector3>(); // To store trees waiting to be instantiated
    private HashSet<Vector3> loadedPositions = new HashSet<Vector3>(); // To track already loaded trees
    private int maxTreesPerFrame = 20; // Number of trees to load per tree update

    public void SetPlayer(GameObject playerObject)
    {
        player = playerObject;
    }

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

        for (int z = -zOffset; z < zSize - zOffset; z += 7)
        {
            for (int x = -xOffset; x < xSize - xOffset; x += 7)
            {
                float noise = Mathf.PerlinNoise(
                    (x + xOffset + offsetX) / (treeFrequency / 10f),
                    (z + zOffset + offsetZ) / (treeFrequency / 10f)
                );

                if (noise > treeDensity)
                {
                    Vector3 basePosition = new Vector3(x, 0, z);
                    Vector3 offset = GetDeterministicOffset(basePosition, seed);
                    Vector3 offsetPosition = basePosition + offset;
                    
                    // Determine the chunk that contains the point
                    Vector2Int chunkCoord = new Vector2Int(
                        Mathf.FloorToInt((float)offsetPosition.x / chunkSize),
                        Mathf.FloorToInt((float)offsetPosition.z / chunkSize)
                    );

                    // Check if chunk data exists at the offset position
                    if (meshGenerator.terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
                    {
                        // Convert world position to local chunk position
                        float localX = ((offsetPosition.x % chunkSize) + chunkSize) % chunkSize;
                        float localZ = ((offsetPosition.z % chunkSize) + chunkSize) % chunkSize;

                        // Get the height from mesh data
                        float groundY = GetHeightFromMeshData(meshData, localX, localZ, chunkSize);

                        if (groundY > 200 || groundY < 10)
                        {
                            continue;
                        }

                        Vector3 finalTreePosition = new Vector3(offsetPosition.x, groundY, offsetPosition.z);

                        Quaternion treeRotation = GetDeterministicRotation(finalTreePosition, seed);

                        // Store tree position and rotation
                        TreeData[finalTreePosition] = treeRotation;
                    }
                    else
                    {
                        Debug.LogWarning($"No MeshData found for chunk: {chunkCoord}");
                    }
                }
            }
        }
    }

    private Vector3 GetDeterministicOffset(Vector3 position, float seed)
    {
        int positionHash = position.GetHashCode();
        int combinedSeed = Mathf.Abs(positionHash) + (int)seed;

        System.Random random = new System.Random(combinedSeed);

        // Random offset between -3.75 and 3.75 meters on the X and Z axes
        float offsetX = (float)(random.NextDouble() * 7.5 - 3.75);
        float offsetZ = (float)(random.NextDouble() * 7.5 - 3.75);

        return new Vector3(offsetX, 0, offsetZ); // No offset on the Y-axis
    }

    private Quaternion GetDeterministicRotation(Vector3 position, float seed)
    {
        int positionHash = position.GetHashCode();
        int combinedSeed = Mathf.Abs(positionHash) + (int)seed;

        System.Random random = new System.Random(combinedSeed);
        float randomYRotation = (float)(random.NextDouble() * 360.0);

        return Quaternion.Euler(0, randomYRotation, 0); // Y-axis rotation only
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
        if (player == null)
        {
            Debug.LogWarning("Player reference is not set!");
            return;
        }

        Vector3 playerPos = player.transform.position;

        foreach (var kvp in TreeData)
        {
            Vector3 position = kvp.Key;

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
                Quaternion treeRotation = TreeData[treePosition];
                GameObject tree = Instantiate(treePrefab, treePosition, treeRotation, treeParent.transform);
                ActiveTrees[treePosition] = tree; // Track instantiated trees
            }
        }
    }

    private void UnloadTreesOutsideRadius()
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference is not set!");
            return;
        }

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
