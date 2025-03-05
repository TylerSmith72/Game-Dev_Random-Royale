using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using static UnityEngine.Mesh;
using System.Linq;

public class MeshGenerator : MonoBehaviour
{
    private int chunkSize = 32;
    public int xSize = 4096;
    public int zSize = 4096;
    public float scale = 2000f;
    public float heightFactor = 1200f;
    public string seedString = "default";

    public int octaves = 8;
    public float persistence = 0.4f;
    public float lacunarity = 2.0f;

    public Material newMaterial;
    public GameObject terrainManager;
    public Transform player;
    Vector2Int lastPlayerChunkCoord = new Vector2Int(0, 0);
    public int loadRadius = 2;

    private Dictionary<Vector2Int, MeshData> terrainDataDictionary;
    private HashSet<Vector2Int> loadedChunks;

    private float playerFOV = 90f;

    private void Start()
    {

    }

    public void StartTerrain()
    {   
        seedString = gameObject.GetComponent<SeedGenerator>().GetSeed();
        GenerateTerrain();
        StartCoroutine(CheckPlayerChunkPos());
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
                MeshData meshData = GenerateTerrainChunkData(x, z, seed);

                // Save mesh data in chunk position
                Vector2Int chunkPos = new Vector2Int(x / chunkSize, z / chunkSize);
                terrainDataDictionary[chunkPos] = meshData;
            }
        }
    }

    float GetSeedFromString(string seedString)
    {
        int hash = Math.Abs(seedString.GetHashCode());
        float normalizedSeed = hash;
        return normalizedSeed;
    }

    MeshData GenerateTerrainChunkData(int startX, int startZ, float seed)
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

                meshData.vertices[i] = new Vector3(x + startX, y, z + startZ);

                meshData.uvs[i] = new Vector2((float)x / chunkSize, (float)z / chunkSize);

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

        return meshData;
    }

    private IEnumerator CheckPlayerChunkPos() // Update chunks if player moves to new chunk
    {
        while (true)
        {
            Vector3 playerPos = player.transform.position;
            Vector2Int playerChunkCoord = new Vector2Int(Mathf.FloorToInt(playerPos.x / chunkSize), Mathf.FloorToInt(playerPos.z / chunkSize));

            if (lastPlayerChunkCoord != playerChunkCoord)
            {
                lastPlayerChunkCoord = playerChunkCoord;
                UpdateChunks(playerChunkCoord);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UpdateChunks(Vector2Int playerChunkCoord)
    {
        if (player == null)
        {
            Debug.LogError("Player reference is not set in MeshGenerator.");
            return;
        }

        HashSet<Vector2Int> newLoadedChunks = new HashSet<Vector2Int>();
        Vector3 playerForward = new Vector3(player.forward.x, 0, player.forward.z).normalized;

        // newLoadedChunks -> Which chunks should be loaded
        // loadedChunks -> Which chunks are currently loaded

        // Load and Unload chunks around player
        for (int z = -loadRadius; z <= loadRadius; z++)
        {
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + z);

                // Check if the chunk is within a circular radius
                float distance = Vector2Int.Distance(chunkCoord, playerChunkCoord);
                if (distance > loadRadius) continue;

                newLoadedChunks.Add(chunkCoord);

                // Check if chunk data exists (meshData is empty outside of the terrain)
                if (terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
                {
                    if (!loadedChunks.Contains(chunkCoord))
                    {
                        int lod = Mathf.Clamp((int)distance, 1, 6); // Set LOD based on distance, LOD = 1 (close) to LOD = 3 (far)

                        DisplayChunk(chunkCoord.x, chunkCoord.y, lod);
                    }

                    if (loadedChunks.Contains(chunkCoord) && newLoadedChunks.Contains(chunkCoord))
                    {
                        int lod = Mathf.Clamp((int)distance, 1, 6); // Set LOD based on distance, LOD = 1 (close) to LOD = 3 (far)

                        if (lod != terrainDataDictionary[chunkCoord].lod)
                        {
                            //Debug.Log("Updating LOD for chunk: " + chunkCoord + ". Old_LOD: " + terrainDataDictionary[chunkCoord].lod + ". New_LOD: " + lod);
                            terrainDataDictionary[chunkCoord].lod = lod;
                            UpdateChunkLOD(chunkCoord.x, chunkCoord.y, lod);
                        }
                    }
                }
            }
        }

        foreach (var chunk in loadedChunks.ToList())
        {
            if (!newLoadedChunks.Contains(chunk))
            {
                UnloadChunk(chunk.x, chunk.y);
            }
        }

        loadedChunks = newLoadedChunks;
    }

    public void UpdateChunkLOD(int x, int z, int lod)
    {
        UnloadChunk(x, z);

        DisplayChunk(x, z, lod);
    }

    public void DisplayChunk(int x, int z, int lod)
    {
        Vector2Int chunkPos = new Vector2Int(x, z);

        if (terrainDataDictionary.TryGetValue(chunkPos, out MeshData meshData))
        {
            //Debug.Log($"Displaying chunk at: {x}, {z}");
            Mesh mesh = new Mesh();

            // Apply LOD to mesh data
            var (reducedVertices, reducedUVs) = ApplyLOD(meshData.vertices, meshData.uvs, lod);
            mesh.vertices = reducedVertices;
            mesh.uv = reducedUVs;
            mesh.triangles = ApplyLODToTriangles(meshData.triangles, lod);
            mesh.RecalculateNormals();

            // Create Chunks GameObject
            GameObject chunk = new GameObject($"TerrainChunk_{x}_{z}");
            chunk.AddComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = chunk.AddComponent<MeshRenderer>();

            meshRenderer.material = newMaterial;
            chunk.transform.SetParent(terrainManager.transform);
            chunk.layer = LayerMask.NameToLayer("Ground");
            chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        else
        {
            Debug.LogError("Chunk data not found for position: " + chunkPos);
        }
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