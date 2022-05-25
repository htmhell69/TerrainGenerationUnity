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
    [SerializeField]
    int chunkSize;
    [SerializeField] int maxAmountOfChunks;
    [SerializeField] Material chunkMaterial;
    [SerializeField] int resolution;

    [Header("Height Map Generation")]

    [SerializeField] int seed;
    [SerializeField] int rowMultiplier;
    BiomeHandler biomeHandler;
    int currentAmountOfChunks;
    Vector3 viewerPosition = new Vector3();
    int chunksVisible;

    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();



    void Start()
    {
        biomeHandler = FindObjectOfType<BiomeHandler>();
        chunks = new TerrainChunk[10, 10];
        ModifyTerrainChunk(5, 5);
        ModifyTerrainChunk(5, 4);
        AdjustNoiseScaling(chunks[5, 5], Vector2Int.down);
    }


    public void ModifyTerrainChunk(int chunkX, int chunkZ)
    {
        TerrainChunk currentChunk = chunks[chunkX, chunkZ];
        if (currentChunk == null)
        {
            Biome biome = biomeHandler.SelectBiome();
            currentChunk = chunks[chunkX, chunkZ] = new TerrainChunk(new Vector2(chunkX * chunkSize, chunkZ * chunkSize), chunkSize, chunkX, chunkZ, chunkMaterial, biome);
            GenerateChunkDetails(currentChunk);
        }
        currentChunk.SetVisible(true);
    }



    public void GenerateChunkDetails(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        ChunkData chunkData = GenerateChunkData(chunk);
        MeshGeneration.GenerateMesh(chunk, chunkData.getHeightMap());
    }


    public void AdjustNoiseScaling(TerrainChunk chunk, Vector2Int side)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        Biome biome = chunk.GetBiome();
        MeshFilter meshFilter = chunkGameObject.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        int neighboringChunkX = chunkX + side.x;
        int neighboringChunkZ = chunkZ + side.y;
        LerpVertices(chunk, chunks[neighboringChunkX, neighboringChunkZ], side); 
    }

    public void LerpVertices(TerrainChunk chunk, TerrainChunk neighboringChunk, Vector2Int side)
    {
        MeshFilter chunkMeshFilter = chunk.GetChunkGameObject().GetComponent<MeshFilter>();
        MeshFilter neighboringChunkMeshFilter = neighboringChunk.GetChunkGameObject().GetComponent<MeshFilter>();
        Vector3[] chunkVertices = chunkMeshFilter.mesh.vertices;
        int[] chunkVerticeIndexes = GetSideVertexesOfChunk(chunk, side);

        int[] neighboringChunkVertexIndexes = GetSideVertexesOfChunk(neighboringChunk, side * -1);
        chunkMeshFilter.mesh.MarkDynamic();
        
        for(int i = 0; i < chunkVerticeIndexes.Length; i++)
        {
            chunkVertices[chunkVerticeIndexes[i]].y = neighboringChunkMeshFilter.mesh.vertices[neighboringChunkVertexIndexes[i]].y;
        }
        chunkMeshFilter.mesh.vertices = chunkVertices;
        chunkMeshFilter.mesh.UploadMeshData(false);
    }
    public int[] GetSideVertexesOfChunk(TerrainChunk chunk, Vector2Int side)
    {
        MeshFilter meshFilter = chunk.GetChunkGameObject().GetComponent<MeshFilter>();
        int chunkStartingIndex = 0;
        int chunkIncrementAmount = 1;
        int[] vertices = new int[chunkSize + 1];
        if(side == Vector2Int.up){
            chunkStartingIndex = xZToIndex(0, chunkSize, chunkSize + 1);
        } else if(side == Vector2Int.right) {
            chunkStartingIndex = xZToIndex(chunkSize, 0, chunkSize + 1);   
            chunkIncrementAmount = xZToIndex(0, 1, chunkSize + 1);  
        } else if(side == Vector2Int.down){
            //no implimentation code is needed just thought it would make code neater.
        } else if(side == Vector2Int.left){
            chunkIncrementAmount = xZToIndex(0, 1, chunkSize + 1);
        }

        for (int chunkI = chunkStartingIndex, i = 0; chunkI < (chunkIncrementAmount * (chunkSize + 1))
         + chunkStartingIndex; chunkI += chunkIncrementAmount)
        {
            vertices[i] = chunkI;
            i++;
        }
        
        return vertices;
    }   

    public int xZToIndex(int x, int z, int xSize = 0){
        int index = 0;
        if(xSize == 0){
            index = z * chunkSize + x;
        } else {
            index = z * xSize + x;
        }
        return index;
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
