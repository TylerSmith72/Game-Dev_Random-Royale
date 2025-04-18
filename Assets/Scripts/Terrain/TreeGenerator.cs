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
    public float forestSize = 0.5f;
    public int treeDensity = 6;
    public float treeFrequency = 100f;
    private GameObject player;
    public GameObject treePrefab;
    //public Transform treeParent;

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
            //UpdateTreeQueue();

            // Load trees in batches
            //LoadTreeBatch();

            // Unload trees outside the radius
            //UnloadTreesOutsideRadius();

            nextTreeUpdateTime = Time.time + treeUpdateInterval; // Throttle the update
        }
    }

    public void StartTreeGeneration(string seedString)
    {
        // Clear previous positions
        TreeData.Clear();

        // Generate all tree positions
        float seed = GetSeedFromString(seedString);
        GenerateTreeData(seed);

        //// Clear previous state (if this is a reset/restart)
        //treeQueue.Clear();
        //loadedPositions.Clear();
        //ActiveTrees.Clear();

        //// Force Update Trees On Load
        //UpdateTreeQueue();
        //LoadTreeBatch();

        AddAllTreesToScene();
    }

    public void GenerateTreeData(float seed)
    {
        int xOffset = xSize / 2;
        int zOffset = zSize / 2;

        // Same seed -> Same random values
        UnityEngine.Random.InitState((int)seed);
        float offsetX = UnityEngine.Random.Range(0, 99999f);
        float offsetZ = UnityEngine.Random.Range(0, 99999f);

        for (int z = -zOffset; z < zSize - zOffset; z += treeDensity)
        {
            for (int x = -xOffset; x < xSize - xOffset; x += treeDensity)
            {
                float noise = Mathf.PerlinNoise(
                    (x + xOffset + offsetX) / (treeFrequency / 10f),
                    (z + zOffset + offsetZ) / (treeFrequency / 10f)
                );

                if (noise > forestSize)
                {
                    Vector3 basePosition = new Vector3(x, 0, z);
                    Vector3 offset = GetDeterministicOffset(basePosition, seed);
                    Vector3 offsetPosition = basePosition + offset;
                    
                    // Determine the chunk that contains the point
                    Vector2Int chunkCoord = new Vector2Int(
                        Mathf.FloorToInt((float)offsetPosition.x / (meshGenerator.vertexSpacing * meshGenerator.chunkSize)),
                        Mathf.FloorToInt((float)offsetPosition.z / (meshGenerator.vertexSpacing * meshGenerator.chunkSize))
                    );

                    // Check if chunk data exists at the offset position
                    if (meshGenerator.terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
                    {
                        // Convert world position to local chunk position
                        float localX = ((offsetPosition.x % (meshGenerator.vertexSpacing * meshGenerator.chunkSize)) + (meshGenerator.vertexSpacing * meshGenerator.chunkSize)) % (meshGenerator.vertexSpacing * meshGenerator.chunkSize);
                        float localZ = ((offsetPosition.z % (meshGenerator.vertexSpacing * meshGenerator.chunkSize)) + (meshGenerator.vertexSpacing * meshGenerator.chunkSize)) % (meshGenerator.vertexSpacing * meshGenerator.chunkSize);

                        // Get the height from mesh data
                        float groundY = GetHeightFromMeshData(meshData, localX, localZ, (int)(meshGenerator.vertexSpacing * meshGenerator.chunkSize));

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
        float offsetX = (float)((random.NextDouble() * 10) - 5);
        float offsetZ = (float)((random.NextDouble() * 10) - 5);

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



    //private void UpdateTreeQueue()
    //{
    //    if (player == null)
    //    {
    //        Debug.LogWarning("Player reference is not set!");
    //        return;
    //    }

    //    Vector3 playerPos = player.transform.position;

    //    foreach (var kvp in TreeData)
    //    {
    //        Vector3 position = kvp.Key;

    //        if (!loadedPositions.Contains(position) && (position - playerPos).sqrMagnitude <= treeRadius * treeRadius)
    //        {
    //            treeQueue.Enqueue(position); // Add to the queue
    //            loadedPositions.Add(position); // Mark as loaded
    //        }
    //    }
    //}

    //private void LoadTreeBatch()
    //{
    //    int treesToInstantiate = Mathf.Min(maxTreesPerFrame, treeQueue.Count); // Limit batch size

    //    for (int i = 0; i < treesToInstantiate; i++)
    //    {
    //        if (treeQueue.TryDequeue(out Vector3 treePosition))
    //        {
    //            Quaternion treeRotation = TreeData[treePosition];
    //            GameObject tree = TreePool.GetTree(treePosition, treeRotation); // Get tree from the pool
    //            ActiveTrees[treePosition] = tree; // Track instantiated trees
    //        }
    //    }
    //}

    //private void UnloadTreesOutsideRadius()
    //{
    //    if (player == null)
    //    {
    //        Debug.LogWarning("Player reference is not set!");
    //        return;
    //    }

    //    Vector3 playerPos = player.transform.position;
    //    List<Vector3> treesToUnload = new List<Vector3>();

    //    // Find trees to unload
    //    foreach (var kvp in ActiveTrees)
    //    {
    //        Vector3 position = kvp.Key;
    //        GameObject tree = kvp.Value;

    //        // Check if the tree is outside the radius
    //        if ((position - playerPos).sqrMagnitude > treeRadius * treeRadius)
    //        {
    //            Destroy(tree); // Destroy the GameObject
    //            treesToUnload.Add(position); // Mark the position for removal
    //        }
    //    }

    //    // Clean up the dictionary and loaded positions
    //    foreach (Vector3 position in treesToUnload)
    //    {
    //        ActiveTrees.Remove(position); // Remove from the ActiveTrees dictionary
    //        loadedPositions.Remove(position); // Remove from the loadedPositions set
    //    }
    //}

    public void AddAllTreesToScene()
    {
        // Check if treePrefab is assigned
        if (treePrefab == null)
        {
            Debug.LogError("Tree prefab is not assigned!");
            return;
        }

        GameObject TreesContainer = new GameObject("Trees"); // Create a new "Trees" GameObject
        TreesContainer.transform.SetParent(transform); // Set the parent to the current GameObject

        // Loop through each tree data and instantiate the trees
        foreach (var kvp in TreeData)
        {
            Vector3 treePosition = kvp.Key;
            Quaternion treeRotation = kvp.Value;

            // Instantiate tree at the position with the rotation and parent it to treeParent
            GameObject tree = Instantiate(treePrefab, treePosition, treeRotation, TreesContainer.transform);
            //GameObject tree = TreePool.GetTree(treePosition, treeRotation);

            // Add to the ActiveTrees dictionary to track instantiated trees
            //ActiveTrees[treePosition] = tree;
        }

        Debug.Log("All trees have been added to the scene.");
    }


    float GetSeedFromString(string seedString)
    {
        int hash = Math.Abs(seedString.GetHashCode());
        float normalizedSeed = hash;
        return normalizedSeed;
    }
}
