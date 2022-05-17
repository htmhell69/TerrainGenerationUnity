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
            Debug.Log("new chunk");
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
    }

    public void GenerateChunkDetails(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        ChunkData chunkData = GenerateChunkData(chunk);
        MeshGeneration.GenerateMesh(chunk, chunkData.getHeightMap());
        FixBiomeOffsets(chunk);
        chunkGameObject.GetComponent<MeshFilter>().mesh.UploadMeshData(false);
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

    public void FixBiomeOffsets(TerrainChunk chunk)
    {
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        for (int z = chunkZ - 1; z <= chunkZ + 1; z += 2)
        {
            TerrainChunk neighboringChunk = chunks[chunkX, z];
            if (neighboringChunk != null && neighboringChunk.GetBiome().GetName() != chunk.GetBiome().GetName())
            {
                int side = 0;
                if (z == chunkZ - 1)
                {
                    neighboringChunk = chunks[chunkX, z];
                    side = 2;
                }
                LerpVertices(neighboringChunk, chunk, side);
            }
        }
        for (int x = chunkX - 1; x <= chunkX + 1; x += 2)
        {
            TerrainChunk neighboringChunk = chunks[x, chunkZ];
            if (neighboringChunk != null && neighboringChunk.GetBiome().GetName() != chunk.GetBiome().GetName())
            {
                int side = 1;
                if (x == chunkX - 1)
                {
                    neighboringChunk = chunks[x, chunkZ];
                    side = 3;
                }
                LerpVertices(neighboringChunk, neighboringChunk, side);
            }
            ReadjustMeshCollider(chunk);
        }
    }
    //side 0 is top side, 1 is right side, 2 is bottom side, 3 is left side
    public void LerpVertices(TerrainChunk chunk, TerrainChunk neighboringChunk, int side)
    {
        int[] neighborVerticesIndex = new int[chunkSize];
        int[] chunkVerticesIndex = new int[chunkSize];

        Vector3[] chunkVertices = chunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh.vertices;
        Vector3[] neighborChunkVertices = neighboringChunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh.vertices;
        int iNeighborChunk = 0;
        int iChunk = 0;
        int incrementAmountNeighbor = 1;
        int incrementAmountChunk = 1;
        Debug.Log(side);
        chunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh.MarkDynamic();
        if (side == 0)
        {
            iChunk = chunkSize * chunkSize - 1;
        }
        else if (side == 1)
        {
            iChunk = chunkSize;
            incrementAmountNeighbor = chunkSize;
            incrementAmountChunk = chunkSize;
        }
        else if (side == 2)
        {
            iChunk = 0;
            iNeighborChunk = chunkSize * chunkSize - 1;
        }
        else if (side == 3)
        {
            iNeighborChunk = chunkSize;
        }
        //getting array of neighbor chunk vertices indexes
        for (int vertIndex = iNeighborChunk, i = 0; i < incrementAmountNeighbor * chunkSize; i += incrementAmountNeighbor)
        {
            neighborVerticesIndex[i] = vertIndex;
            i++;

        }
        //getting array of current chunk vertices indexes
        for (int vertIndex = iChunk, i = 0; i < incrementAmountChunk * chunkSize; i += incrementAmountChunk)
        {
            chunkVerticesIndex[i] = vertIndex;
            i++;
        }
        //using those arrays to fix offsets
        for (int i = 0; i < chunkSize; i++)
        {
            Vector3 neighborchunkVertice = neighborChunkVertices[neighborVerticesIndex[i]];
            chunkVertices[chunkVerticesIndex[i]].y = neighborchunkVertice.y;
        }
        chunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh.vertices = chunkVertices;
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


