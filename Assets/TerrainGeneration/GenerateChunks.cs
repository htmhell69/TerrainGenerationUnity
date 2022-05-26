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
    [SerializeField] int rowMultiplier;
    [SerializeField] Material chunkMaterial;
    [SerializeField] int resolution;
    [Header("Height Map Generation")]
    [SerializeField] int seed;
    BiomeHandler biomeHandler;
    int currentAmountOfChunks;
    Vector3 viewerPosition = new Vector3();
    int chunksVisible;
    int verticeSize;

    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();


    void Start()
    {
        verticeSize = chunkSize + 1;
        chunks = new TerrainChunk[maxAmountOfChunks, maxAmountOfChunks];
        seed = Random.Range(0, 999999);
        viewer.position = new Vector3((maxAmountOfChunks * chunkSize) / 2, 100, (maxAmountOfChunks * chunkSize) / 2);   
        biomeHandler = FindObjectOfType<BiomeHandler>();
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
        for (int i = 0; i < visibleTerrainChunks.Count; i++)
        {
            AdjustNoiseScaling(visibleTerrainChunks[i]);
        }
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

    ChunkData GenerateChunkData(TerrainChunk chunk)
    {
        Biome currentBiome = chunk.GetBiome();
        float[,] noiseMap = Noise.GenerateNoiseMap(chunk, seed, currentBiome.GetNoiseScale());
        return new ChunkData(noiseMap);
    }

    public void AdjustNoiseScaling(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        Biome biome = chunk.GetBiome();
        MeshFilter meshFilter = chunkGameObject.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        Vector2Int side = new Vector2Int(0, -1);
        for (int z = chunkZ - 1; z <= chunkZ + 1; z += 2)
        {
            if (z >= 0 && z < maxAmountOfChunks && chunks[chunkX, z] != null && chunk.GetBiome().GetName() != chunks[chunkX, z].GetBiome().GetName())
            {
                LerpVertices(chunk, chunks[chunkX, z], side);
                mesh.UploadMeshData(false);
            }
            side *= -1;
        }

        side = new Vector2Int(-1, 0);
        for (int x = chunkX - 1; x <= chunkX + 1; x += 2)
        {
            
            if (x >= 0 && x < maxAmountOfChunks && chunks[x, chunkZ] != null && chunk.GetBiome().GetName() != chunks[x, chunkZ].GetBiome().GetName())
            {
                LerpVertices(chunk, chunks[x, chunkZ], side);
                mesh.UploadMeshData(false);
            }
            side *= -1;
        }
        
    }

    public void LerpVertices(TerrainChunk chunk, TerrainChunk neighboringChunk, Vector2Int side)
    {
        MeshFilter chunkMeshFilter = chunk.GetChunkGameObject().GetComponent<MeshFilter>();
        MeshFilter neighboringChunkMeshFilter = neighboringChunk.GetChunkGameObject().GetComponent<MeshFilter>();
        Vector3[] chunkVertices = chunkMeshFilter.mesh.vertices;
        int[] chunkVerticeIndexes = GetSideVertexesOfChunk(chunk, side);

        int[] neighboringChunkVertexIndexes = GetSideVertexesOfChunk(neighboringChunk, side * -1);
        chunkMeshFilter.mesh.MarkDynamic();

        for (int i = 0; i < chunkVerticeIndexes.Length; i++)
        {
            chunkVertices[chunkVerticeIndexes[i]].y = neighboringChunkMeshFilter.mesh.vertices[neighboringChunkVertexIndexes[i]].y;
        }
        chunkMeshFilter.mesh.vertices = chunkVertices;
        chunkMeshFilter.mesh.UploadMeshData(false);
        ReadjustMeshCollider(chunk);
    }
    public int[] GetSideVertexesOfChunk(TerrainChunk chunk, Vector2Int side)
    {
        MeshFilter meshFilter = chunk.GetChunkGameObject().GetComponent<MeshFilter>();
        int chunkStartingIndex = 1;
        int chunkIncrementAmount = 1;
        int[] vertices = new int[chunkSize - 1];
        //these loops will not be affecting the edges so some of these number may look odd. Edges is for another method.
        if (side == Vector2Int.up)
        {
            chunkStartingIndex = xZToIndex(1, chunkSize, verticeSize);
        }
        else if (side == Vector2Int.right)
        {
            chunkStartingIndex = xZToIndex(chunkSize, 1, verticeSize);
            chunkIncrementAmount = xZToIndex(0, 1, verticeSize);
        }
        else if (side == Vector2Int.down)
        {
            //no implimentation code is needed just thought it would be messy to leave it out.
        }
        else if (side == Vector2Int.left)
        {
            chunkStartingIndex = verticeSize;
            chunkIncrementAmount = xZToIndex(0, 1, verticeSize);
        }

        for (int chunkI = chunkStartingIndex, i = 0; chunkI < (chunkIncrementAmount * (chunkSize - 1))
         + chunkStartingIndex; chunkI += chunkIncrementAmount)
        {
            vertices[i] = chunkI;
            i++;
        }

        return vertices;
    }

    void FixCornerVerticesOffset(){
        
    }

    int GetCentralCornerVertices(TerrainChunk startingChunk, Vector2 side){
        TerrainChunk[] corneringChunks = new TerrainChunk[4];
        int[] vertexIndexes = new int[4];
        int chunkX = startingChunk.GetChunkX();
        int chunkZ = startingChunk.GetChunkZ();
        corneringChunks[0] = startingChunk;
        if(side == new Vector2(0,0))
        {
            vertexIndexes[0] = GetCornerIndex(0,0);
            vertexIndexes[1] = GetCornerIndex(0,1);
            vertexIndexes[2] = GetCornerIndex(1,1);
            vertexIndexes[3] = GetCornerIndex(1,0);
        } 
        else if(side == new Vector2(1,0))
        {
            vertexIndexes[0] = GetCornerIndex(1,0);
            vertexIndexes[1] = GetCornerIndex(1,1);
            vertexIndexes[2] = GetCornerIndex(0,1);
            vertexIndexes[3] = GetCornerIndex(0,0);
        } 
        else if(side == new Vector2(0,1))
        {
            vertexIndexes[0] = GetCornerIndex(0,1);
            vertexIndexes[1] = GetCornerIndex(0,0);
            vertexIndexes[2] = GetCornerIndex(1,0);
            vertexIndexes[3] = GetCornerIndex(1,1);
        } 
        else if(side == new Vector2(1,1))
        {
            vertexIndexes[0] = GetCornerIndex(1,1);
            vertexIndexes[1] = GetCornerIndex(0,1);
            vertexIndexes[2] = GetCornerIndex(0,0);
            vertexIndexes[3] = GetCornerIndex(1,0);
        }
        int xChunkModifier = 1;
        int zChunkModifier = 1;

        if(side.x == 0)
        {
            xChunkModifier = -1;
        }
        if(side.y == 0)
        {
            zChunkModifier = -1;
        }

        corneringChunks[1] = chunks[chunkX, chunkZ + zChunkModifier];
        corneringChunks[2] = chunks[chunkX + xChunkModifier, chunkZ + zChunkModifier];
        corneringChunks[3] = chunks[chunkX + xChunkModifier, chunkZ];

    } 

    public int GetCornerIndex(int x, int z){
        if(x == 0 && z == 0)
        {
            return 0;
        } 
        else if(x == 1 && z == 0) 
        {
            return chunkSize;
        } 
        else if(x == 0 && z == 1)
        {
            return xZToIndex(0, chunkSize, verticeSize);
        } 
        else if(x == 1 && z == 1)
        {
            return xZToIndex(chunkSize, chunkSize, verticeSize);
        } 
        else 
        {
            Debug.LogError("please give a correct side value you gave " + x + "," + z);
            return 0;
        }
    }


    public int xZToIndex(int x, int z, int xSize = 0)
    {
        int index = 0;
        if (xSize == 0)
        {
            index = z * chunkSize + x;
        }
        else
        {
            index = z * xSize + x;
        }
        return index;
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
