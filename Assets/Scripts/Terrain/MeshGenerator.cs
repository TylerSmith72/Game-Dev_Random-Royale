using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Linq;

public class MeshGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public int chunkSize = 32;
    public int xSize = 4096;
    public int zSize = 4096;
    public float scale = 2000f;
    public float heightFactor = 1200f;
    public string seedString = "default";
    public float vertexSpacing = 4f;

    [Header("Fractal Perlin Noise")]
    public int octaves = 8;
    public float persistence = 0.4f;
    public float lacunarity = 2.0f;

    [Header("Materials")]
    public Material terrainMaterial;
    public GameObject terrainManager;

    [Header("Player")]
    public GameObject player;
    Vector2Int lastPlayerChunkCoord = new Vector2Int(0, 0);
    public int loadRadius = 2;

    //private Quadtree<Vector2Int> quadtree;
    public Dictionary<Vector2Int, MeshData> terrainDataDictionary;
    private HashSet<Vector2Int> loadedChunks;
    private List<Vector2Int> chunksToUpdate;
    private int batchSize = 1;

    private bool hasGeneratedData = false;

    private float playerFOV = 90f;

    public void SetPlayer(GameObject playerTransform)
    {
        player = playerTransform;
        Debug.Log("Player set for MeshGenerator: " + player.name);
    }

    public void Start()
    {
        chunksToUpdate = new List<Vector2Int>();
    }

    public void Update()
    {
        if (hasGeneratedData && chunksToUpdate.Count > 0)
        {
            // Check which chunk LODs need to be updated from chunksToUpdate list
            for (int i = 0; i < batchSize; i++) // One chunk LOD update per frame
            {
                UpdateChunkLOD(chunksToUpdate[i].x, chunksToUpdate[i].y, terrainDataDictionary[chunksToUpdate[i]].lod);
                chunksToUpdate.RemoveAt(i);
                
            }            
        }
    }

    public void StartTerrain()
    {
        seedString = gameObject.GetComponent<SeedGenerator>().GetSeed();
        //quadtree = new Quadtree<Vector2Int>(0, new Rect(-xSize / 2, -zSize / 2, xSize, zSize));

        GenerateTerrain();
        TreeGenerator treeGenerator = gameObject.GetComponent<TreeGenerator>();
        treeGenerator.StartTreeGeneration(seedString);

        if (hasGeneratedData)
        {
            Debug.Log("Generating terrain...");
            //StartCoroutine(CheckPlayerChunkPos());
            // Load all chunks in high quality
            StartCoroutine(DisplayChunksInBatches());
        }

        //ForceUpdateChunks();
    }

    void GenerateTerrain()
    {
        float seed = GetSeedFromString(seedString);
        //Debug.Log(seed);

        terrainDataDictionary = new Dictionary<Vector2Int, MeshData>();
        loadedChunks = new HashSet<Vector2Int>();
        

        int xOffset = xSize / 2;
        int zOffset = zSize / 2;

        for (int z = -zOffset; z < zSize - zOffset; z += chunkSize)
        {
            for (int x = -xOffset; x < xSize - xOffset; x += chunkSize)
            {
                Vector2Int chunkCoord = new Vector2Int(x / chunkSize, z / chunkSize);
                //quadtree.Insert(chunkCoord, new Rect(x, z, chunkSize, chunkSize));
                StartCoroutine(GenerateTerrainChunkData(x, z, seed));
            }
        }

        hasGeneratedData = true;
    }

    float GetSeedFromString(string seedString)
    {
        int hash = Math.Abs(seedString.GetHashCode());
        float normalizedSeed = hash;
        return normalizedSeed;
    }

    IEnumerator GenerateTerrainChunkData(int startX, int startZ, float seed)
    {
        // Same seed -> Same random values
        UnityEngine.Random.InitState((int)seed);
        float offsetX = UnityEngine.Random.Range(0, 99999f);
        float offsetZ = UnityEngine.Random.Range(0, 99999f);

        MeshData meshData = new MeshData(chunkSize);

        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float baseHeight = FractalPerlinNoise((x + startX + offsetX) / scale, (z + startZ + offsetZ) / scale, heightFactor);

                float y = baseHeight - (heightFactor / 3);

                meshData.vertices[i] = new Vector3((x + startX) * vertexSpacing, y, (z + startZ) * vertexSpacing);

                meshData.uvs[i] = new Vector2((float)(x * vertexSpacing) / chunkSize, (float)(z * vertexSpacing) / chunkSize);

                i++;
            }
        }

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                meshData.triangles[tris + 0] = vert + 0;
                meshData.triangles[tris + 1] = vert + chunkSize + 1;
                meshData.triangles[tris + 2] = vert + 1;
                meshData.triangles[tris + 3] = vert + 1;
                meshData.triangles[tris + 4] = vert + chunkSize + 1;
                meshData.triangles[tris + 5] = vert + chunkSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // Save mesh data in chunk position
        Vector2Int chunkPos = new Vector2Int(startX / chunkSize, startZ / chunkSize);
        terrainDataDictionary[chunkPos] = meshData;

        yield return null;
    }

    private IEnumerator DisplayChunksInBatches()
    {
        // Chunks to load
        Queue<Vector2Int> chunksToLoad = new Queue<Vector2Int>();

        // Add all chunks to queue
        foreach (var chunkCoord in terrainDataDictionary.Keys)
        {
            chunksToLoad.Enqueue(chunkCoord);
        }

        while (chunksToLoad.Count > 0)
        {
            // Load 1-2 chunks per frame
            for (int i = 0; i < 2 && chunksToLoad.Count > 0; i++)
            {
                Vector2Int chunkCoord = chunksToLoad.Dequeue();
                DisplayChunks(chunkCoord, 2);  // Display in high quality (LOD = 5)

                // Optional: You can add a small delay to further control the batch size
                yield return null;
            }

            // Wait until the next frame to process more chunks
            yield return null;
        }

        Debug.Log("All chunks have been loaded.");
    }


    //private IEnumerator CheckPlayerChunkPos() // Update chunks if player moves to new chunk
    //{
    //    while (player == null)
    //    {
    //        Debug.LogWarning("Waiting for player to be set in MeshGenerator...");
    //        yield return null; // Wait for the next frame.
    //    }

    //    while (true)
    //    {
    //        Vector3 playerPos = player.transform.position;
    //        Vector2Int playerChunkCoord = new Vector2Int(Mathf.FloorToInt(playerPos.x / (vertexSpacing * chunkSize)), Mathf.FloorToInt(playerPos.z / (vertexSpacing * chunkSize)));

    //        if (lastPlayerChunkCoord != playerChunkCoord)
    //        {
    //            lastPlayerChunkCoord = playerChunkCoord;
    //            UpdateChunks(playerChunkCoord);
    //        }

    //        yield return new WaitForSeconds(0.5f);
    //    }
    //}

    //public void ForceUpdateChunks()
    //{
    //    Vector3 playerPos = player.transform.position;
    //    Vector2Int playerChunkCoord = new Vector2Int(Mathf.FloorToInt(playerPos.x / (vertexSpacing * chunkSize)), Mathf.FloorToInt(playerPos.z / (vertexSpacing * chunkSize)));
    //    UpdateChunks(playerChunkCoord);
    //}

    //private void UpdateChunks(Vector2Int playerChunkCoord)
    //{
    //    if (player == null)
    //    {
    //        Debug.LogError("Player reference is not set in MeshGenerator.");
    //        return;
    //    }

    //    HashSet<Vector2Int> newLoadedChunks = new HashSet<Vector2Int>();

    //    // newLoadedChunks -> Which chunks should be loaded
    //    // loadedChunks -> Which chunks are currently loaded
    //    // lowDetailChunks -> Chunks to be combined

    //    Rect playerBounds = new Rect(playerChunkCoord.x * chunkSize - loadRadius * chunkSize, playerChunkCoord.y * chunkSize - loadRadius * chunkSize, loadRadius * 2 * chunkSize, loadRadius * 2 * chunkSize);
    //    List<Vector2Int> nearbyChunks = quadtree.Retrieve(new List<Vector2Int>(), playerBounds);

    //    // Load and Unload chunks around player
    //    foreach (var chunkCoord in nearbyChunks)
    //    {
    //        // Check if the chunk is within a circular radius
    //        if ((chunkCoord - playerChunkCoord).sqrMagnitude > loadRadius * loadRadius)
    //            continue;

    //        newLoadedChunks.Add(chunkCoord);

    //        // Check if chunk data exists (meshData is empty outside of the terrain)
    //        if (terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
    //        {
    //            int lodDistance; // Represents the chunk's distance category
    //            int maxHighDetailDistance = 1; // 3x3 area around the player (1 chunk radius)
    //            int mediumDetailDistance = 2; // Surrounding 5x5 area (2 chunk radius)

    //            // Calculate Manhattan distance between chunk and player
    //            int manhattanDistance = Mathf.Max(Mathf.Abs(chunkCoord.x - playerChunkCoord.x), Mathf.Abs(chunkCoord.y - playerChunkCoord.y));

    //            // Assign LOD based on the Manhattan distance
    //            if (manhattanDistance <= maxHighDetailDistance)
    //            {
    //                lodDistance = 5; // High detail (3x3 area)
    //            }
    //            else if (manhattanDistance == mediumDetailDistance)
    //            {
    //                lodDistance = 6; // Medium detail (surrounding row)
    //            }
    //            else
    //            {
    //                lodDistance = 7; // Low detail (everything further)
    //            }


    //            int lod = Mathf.Clamp((int)lodDistance, 1, (int)Mathf.Log(chunkSize, 2.0f) + 1); // Set LOD based on distance

    //            // Create New Chunk
    //            if (!loadedChunks.Contains(chunkCoord))
    //            {
    //                DisplayChunks(chunkCoord, lod);
    //            }

    //            // Update Existing Chunk if LOD has changed
    //            if (loadedChunks.Contains(chunkCoord) && newLoadedChunks.Contains(chunkCoord))
    //            {
    //                if (lod != meshData.lod)
    //                {
    //                    meshData.lod = lod;
    //                    chunksToUpdate.Add(chunkCoord);
    //                }
    //            }
    //        }
    //    }

    //    // Check loaded chunks to see if they should still be loaded
    //    foreach (var chunk in loadedChunks.ToList())
    //    {
    //        if (!newLoadedChunks.Contains(chunk))
    //        {
    //            UnloadChunk(chunk.x, chunk.y);
    //        }
    //    }

    //    loadedChunks = newLoadedChunks;        
    //}

    public void UpdateChunkLOD(int x, int z, int lod)
    {
        UnloadChunk(x, z);

        DisplayChunks(new Vector2Int(x, z), lod);
    }

    public void DisplayChunks(Vector2Int chunkCoord, int lod)
    {
        List<Vector3> combinedVertices = new List<Vector3>();
        List<Vector2> combinedUVs = new List<Vector2>();
        List<int> combinedTriangles = new List<int>();

        int vertexOffset = 0;

        if (terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
        {
            // Apply LOD to mesh data
            var (reducedVertices, reducedUVs) = ApplyLOD(meshData.vertices, meshData.uvs, lod);
            int[] reducedTriangles = ApplyLODToTriangles(meshData.triangles, lod);

            combinedVertices.AddRange(reducedVertices);
            combinedUVs.AddRange(reducedUVs);

            for (int i = 0; i < reducedTriangles.Length; i++)
            {
                combinedTriangles.Add(reducedTriangles[i] + vertexOffset);
            }

            vertexOffset += reducedVertices.Length;
        }
        else
        {
            Debug.LogError("Chunk data not found for position: " + chunkCoord);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.vertices = combinedVertices.ToArray();
        combinedMesh.uv = combinedUVs.ToArray();
        combinedMesh.triangles = combinedTriangles.ToArray();
        combinedMesh.RecalculateNormals();

        // Create Chunk GameObject
        GameObject chunk;
        chunk = new GameObject($"TerrainChunk_{chunkCoord.x}_{chunkCoord.y}");

        chunk.AddComponent<MeshFilter>().mesh = combinedMesh;
        var meshRenderer = chunk.AddComponent<MeshRenderer>();

        meshRenderer.material = terrainMaterial;
        chunk.transform.SetParent(terrainManager.transform);
        chunk.layer = LayerMask.NameToLayer("Ground");
        chunk.AddComponent<MeshCollider>().sharedMesh = combinedMesh;
    }

    public void UnloadChunk(int x, int z)
    {
        string chunkName = $"TerrainChunk_{x}_{z}";
        GameObject chunk = GameObject.Find(chunkName);

        if (chunk != null)
        {
            Destroy(chunk);
        }
    }

    private (Vector3[], Vector2[]) ApplyLOD(Vector3[] vertices, Vector2[] uvs, int lod)
    {
        if (lod == 1) return (vertices, uvs);

        int divisor = (int)Mathf.Pow(2, lod - 1);
        int newSize = (chunkSize / divisor + 1) * (chunkSize / divisor + 1);

        Vector3[] reducedVertices = new Vector3[newSize];
        Vector2[] reducedUVs = new Vector2[newSize];

        for (int i = 0, z = 0; z <= chunkSize; z += divisor)
        {
            for (int x = 0; x <= chunkSize; x += divisor)
            {
                reducedVertices[i] = vertices[z * (chunkSize + 1) + x];
                reducedUVs[i] = uvs[z * (chunkSize + 1) + x];
                i++;
            }
        }

        return (reducedVertices, reducedUVs);
    }

    private int[] ApplyLODToTriangles(int[] triangles, int lod)
    {
        if (lod == 1) return triangles;

        int divisor = (int)Mathf.Pow(2, lod - 1);
        int newTriCount = (chunkSize / divisor) * (chunkSize / divisor) * 6;

        int[] reducedTriangles = new int[newTriCount];

        int vert = 0, tris = 0;
        for (int z = 0; z < chunkSize / divisor; z++)
        {
            for (int x = 0; x < chunkSize / divisor; x++)
            {
                reducedTriangles[tris + 0] = vert + 0;
                reducedTriangles[tris + 1] = vert + chunkSize / divisor + 1;
                reducedTriangles[tris + 2] = vert + 1;
                reducedTriangles[tris + 3] = vert + 1;
                reducedTriangles[tris + 4] = vert + chunkSize / divisor + 1;
                reducedTriangles[tris + 5] = vert + chunkSize / divisor + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        return reducedTriangles;
    }

    float FractalPerlinNoise(float x, float z, float heightFactor)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (total / maxValue) * heightFactor;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public Vector2[] uvs;
    public int[] triangles;
    public int lod;

    public MeshData(int chunkSize, int lod = 2)
    {
        vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        uvs = new Vector2[(chunkSize + 1) * (chunkSize + 1)];
        triangles = new int[chunkSize * chunkSize * 6];
        this.lod = lod;
    }
}

//public class Quadtree<T>
//{
//    private readonly int maxObjects;
//    private readonly int maxLevels;
//    private readonly int level;
//    private readonly List<T> objects;
//    private readonly Rect bounds;
//    private readonly Quadtree<T>[] nodes;

//    public Quadtree(int level, Rect bounds, int maxObjects = 10, int maxLevels = 5)
//    {
//        this.level = level;
//        this.bounds = bounds;
//        this.maxObjects = maxObjects;
//        this.maxLevels = maxLevels;
//        objects = new List<T>();
//        nodes = new Quadtree<T>[4];
//    }

//    public void Clear()
//    {
//        objects.Clear();
//        for (int i = 0; i < nodes.Length; i++)
//        {
//            if (nodes[i] != null)
//            {
//                nodes[i].Clear();
//                nodes[i] = null;
//            }
//        }
//    }

//    private void Split()
//    {
//        int subWidth = (int)(bounds.width / 2);
//        int subHeight = (int)(bounds.height / 2);
//        int x = (int)bounds.x;
//        int y = (int)bounds.y;

//        nodes[0] = new Quadtree<T>(level + 1, new Rect(x + subWidth, y, subWidth, subHeight));
//        nodes[1] = new Quadtree<T>(level + 1, new Rect(x, y, subWidth, subHeight));
//        nodes[2] = new Quadtree<T>(level + 1, new Rect(x, y + subHeight, subWidth, subHeight));
//        nodes[3] = new Quadtree<T>(level + 1, new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
//    }

//    private int GetIndex(Rect pRect)
//    {
//        int index = -1;
//        double verticalMidpoint = bounds.x + (bounds.width / 2);
//        double horizontalMidpoint = bounds.y + (bounds.height / 2);

//        bool topQuadrant = (pRect.y < horizontalMidpoint && pRect.y + pRect.height < horizontalMidpoint);
//        bool bottomQuadrant = (pRect.y > horizontalMidpoint);

//        if (pRect.x < verticalMidpoint && pRect.x + pRect.width < verticalMidpoint)
//        {
//            if (topQuadrant)
//            {
//                index = 1;
//            }
//            else if (bottomQuadrant)
//            {
//                index = 2;
//            }
//        }
//        else if (pRect.x > verticalMidpoint)
//        {
//            if (topQuadrant)
//            {
//                index = 0;
//            }
//            else if (bottomQuadrant)
//            {
//                index = 3;
//            }
//        }

//        return index;
//    }

//    private List<int> GetIndices(Rect pRect)
//    {
//        List<int> indices = new List<int>();
//        double verticalMidpoint = bounds.x + (bounds.width / 2);
//        double horizontalMidpoint = bounds.y + (bounds.height / 2);

//        bool topQuadrant = pRect.y < horizontalMidpoint;
//        bool bottomQuadrant = pRect.y + pRect.height > horizontalMidpoint;
//        bool leftQuadrant = pRect.x < verticalMidpoint;
//        bool rightQuadrant = pRect.x + pRect.width > verticalMidpoint;

//        if (topQuadrant)
//        {
//            if (rightQuadrant) indices.Add(0); // Top-right
//            if (leftQuadrant) indices.Add(1);  // Top-left
//        }
//        if (bottomQuadrant)
//        {
//            if (leftQuadrant) indices.Add(2);  // Bottom-left
//            if (rightQuadrant) indices.Add(3); // Bottom-right
//        }

//        return indices;
//    }

//    public void Insert(T obj, Rect pRect)
//    {
//        if (nodes[0] != null)
//        {
//            int index = GetIndex(pRect);

//            if (index != -1)
//            {
//                nodes[index].Insert(obj, pRect);
//                return;
//            }
//        }

//        objects.Add(obj);

//        if (objects.Count > maxObjects && level < maxLevels)
//        {
//            if (nodes[0] == null)
//            {
//                Split();
//            }

//            int i = 0;
//            while (i < objects.Count)
//            {
//                int index = GetIndex(pRect);
//                if (index != -1)
//                {
//                    nodes[index].Insert(objects[i], pRect);
//                    objects.RemoveAt(i);
//                }
//                else
//                {
//                    i++;
//                }
//            }
//        }
//    }

//    public List<T> Retrieve(List<T> returnObjects, Rect pRect)
//    {
//        var indices = GetIndices(pRect);
//        foreach (int index in indices)
//        {
//            if (nodes[index] != null)
//            {
//                nodes[index].Retrieve(returnObjects, pRect);
//            }
//        }

//        returnObjects.AddRange(objects);
//        return returnObjects;
//    }
//}