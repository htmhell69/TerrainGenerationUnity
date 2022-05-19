using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GenerateChunks : MonoBehaviour
{
    [Header("Chunks")]
    TerrainChunk[,] chunks;
    [SerializeField] Transform viewer;
    [SerializeField] int viewDistanceInChunks = 5;
    [Range(1, 250)]
    [SerializeField] int chunkSize;
    [SerializeField] int maxAmountOfChunks;
    [SerializeField] Material chunkMaterial;
    [SerializeField] int resolution;

    [Header("Height Map Generation")]
    [SerializeField] int seed;
    BiomeHandler biomeHandler;
    int currentAmountOfChunks;
    Vector3 viewerPosition = new Vector3();
    int chunksVisible;
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();



    void Start()
    {
        biomeHandler = FindObjectOfType<BiomeHandler>();
        seed = UnityEngine.Random.Range(0, 999999);
        viewer.position = new Vector3((maxAmountOfChunks * chunkSize) / 2, 100, (maxAmountOfChunks * chunkSize) / 2);
        chunks = new TerrainChunk[maxAmountOfChunks, maxAmountOfChunks];
    }

    void Update()
    {
        Vector2Int oldChunkPosition = GetChunkFromWorldPos(viewerPosition);
        Vector2Int newChunkPosition = GetChunkFromWorldPos(viewer.position);
        bool isNewChunk = oldChunkPosition != newChunkPosition;
        viewerPosition = viewer.position;
        if (isNewChunk)
        {

            UpdateChunks();
        }
    }

    public void ModifyTerrainChunk(int chunkX, int chunkZ)
    {
        TerrainChunk currentChunk = chunks[chunkX, chunkZ];
        if (currentChunk == null)
        {
            Biome biome = biomeHandler.SelectBiome();
            currentChunk = chunks[chunkX, chunkZ] = new TerrainChunk(new Vector2(chunkX * chunkSize, chunkZ * chunkSize), chunkSize, chunkX, chunkZ, chunkMaterial, biome);
            Debug.Log("new chunk");
        }

        currentChunk.SetVisible(true);
        GenerateChunkDetails(currentChunk);
    }

    public void UpdateChunks()
    {
        int chunkX = Mathf.FloorToInt(viewerPosition.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(viewerPosition.z / chunkSize);

        //hiding chunks that were there last update
        for (int i = 0; i < visibleTerrainChunks.Count; i++)
        {
            int visibleChunkX = visibleTerrainChunks[i].GetChunkX();
            int visibleChunkZ = visibleTerrainChunks[i].GetChunkZ();
            visibleTerrainChunks[i].SetVisible(false);
        }
        visibleTerrainChunks.Clear();


        //updating all chunks in view distance
        for (int z = chunkZ - viewDistanceInChunks; z < chunkZ + viewDistanceInChunks; z++)
        {
            if (z < 0)
            {
                continue;
            }
            if (z >= maxAmountOfChunks)
            {
                break;
            }
            for (int x = chunkX - viewDistanceInChunks; x < chunkX + viewDistanceInChunks; x++)
            {
                if (x < 0)
                {
                    continue;
                }
                if (x >= maxAmountOfChunks)
                {
                    break;
                }
                ModifyTerrainChunk(x, z);
                visibleTerrainChunks.Add(chunks[x, z]);
            }
        }
        //adjusting all new chunks to each other
        for (int i = 0; i < visibleTerrainChunks.Count; i++)
        {
            AdjustNoiseScaling(visibleTerrainChunks[i]);
        }
    }

    public void GenerateChunkDetails(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        ChunkData chunkData = GenerateChunkData(chunk);
        MeshGeneration.GenerateMesh(chunk, chunkData.getHeightMap());
        //FixBiomeOffsets(chunk);
        //chunkGameObject.GetComponent<MeshFilter>().mesh.UploadMeshData(false);
    }
    public void AdjustNoiseScaling(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        Biome biome = chunk.GetBiome();
        MeshFilter meshFilter = chunkGameObject.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;

        mesh.MarkDynamic();
        for (int z = chunkZ - 1; z < chunkZ + 1; z += 2)
        {
            Debug.Log("chunkZ = " + z + " chunkX = " + chunkX);
            Debug.Log("check 1 = " + (z >= 0));
            Debug.Log("check 2 = " + (z <= chunkSize));
            Debug.Log("final check = " + (chunks[chunkX, z] != null));
            Debug.Log("has passed = " + (chunkX > 0 && z <= chunkSize && chunks[chunkX, z] != null));
            if (z >= 0 && z <= chunkSize && chunks[chunkX, z] != null)
            {
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i].y = 1;
                }
                mesh.vertices = verts;
                mesh.UploadMeshData(false);
            }
        }

        for (int x = chunkX - 1; x < chunkX + 1; x += 2)
        {
            Debug.Log("chunkX = " + x + " chunkZ = " + chunkZ);
            Debug.Log("check 1 = " + (x >= 0));
            Debug.Log("check 2 = " + (x <= chunkSize));
            Debug.Log("final check = " + (chunks[x, chunkZ] != null));
            Debug.Log("has passed = " + (x > 0 && x <= chunkSize && chunks[x, chunkZ] != null));
            if (x >= 0 && x <= chunkSize && chunks[x, chunkZ] != null)
            {
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i].y = 1;
                }
                Debug.Log(verts.Length);
                mesh.vertices = verts;
                mesh.UploadMeshData(false);
            }
        }
    }

    ChunkData GenerateChunkData(TerrainChunk chunk)
    {
        Biome currentBiome = chunk.GetBiome();
        float[,] noiseMap = Noise.GenerateNoiseMap(chunk, seed, currentBiome.GetNoiseScale());
        return new ChunkData(noiseMap);
    }

    public Vector2Int GetChunkFromWorldPos(Vector3 worldPos)
    {
        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(worldPos.x / chunkSize), Mathf.FloorToInt(worldPos.z / chunkSize));
        return chunkPos;
    }

    public TerrainChunk GetChunkFromArray(int x, int y)
    {
        return chunks[x, y];
    }

    public void ReadjustMeshCollider(TerrainChunk chunk)
    {
        Mesh mesh = chunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh;
        MeshCollider meshCollider = chunk.GetChunkGameObject().GetComponent<MeshCollider>();

        //fixing lighting issues
        mesh.RecalculateBounds();
        //readjusting collider
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

}

public class TerrainChunk
{
    GameObject chunkObject;
    int x;
    int z;
    int chunkSize;
    Biome biome;

    public TerrainChunk(Vector2 chunkPosition, int size, int chunkX, int chunkY, Material material, Biome biome)
    {
        chunkObject = new GameObject("chunk");
        chunkObject.AddComponent<MeshRenderer>();
        chunkObject.AddComponent<MeshFilter>();
        chunkObject.AddComponent<MeshCollider>();
        x = chunkX;
        z = chunkY;
        chunkObject.transform.position = new Vector3(chunkPosition.x, 0, chunkPosition.y);
        chunkObject.GetComponent<MeshRenderer>().material = material;
        chunkObject.SetActive(false);
        chunkSize = size;
        this.biome = biome;
    }
    public void SetVisible(bool visibility)
    {
        chunkObject.SetActive(visibility);
    }

    public GameObject GetChunkGameObject()
    {
        return chunkObject;
    }
    public int GetChunkX()
    {
        return x;
    }
    public int GetChunkZ()
    {
        return z;
    }
    public int GetChunkSize()
    {
        return chunkSize;
    }

    public Biome GetBiome()
    {
        return biome;
    }

}

public struct ChunkData
{
    //this will have more than height map later on 
    float[,] heightMap;

    public ChunkData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
    public float[,] getHeightMap()
    {
        return heightMap;
    }
}