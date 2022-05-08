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

    [Header("Height Map Generation")]
    [SerializeField] int scale = 5;
    [SerializeField] int seed;
    int currentAmountOfChunks;
    Vector3 viewerPosition = new Vector3();
    int chunksVisible;
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();



    void Start()
    {
        seed = Random.Range(0, 999999);
        viewer.position = new Vector3((maxAmountOfChunks * chunkSize) / 2, 10, (maxAmountOfChunks * chunkSize) / 2);
        chunks = new TerrainChunk[maxAmountOfChunks, maxAmountOfChunks];
    }

    void Update()
    {
        Vector2Int oldChunkPosition = new Vector2Int(Mathf.FloorToInt(viewerPosition.z / chunkSize), Mathf.FloorToInt(viewerPosition.x / chunkSize));
        Vector2Int newChunkPosition = new Vector2Int(Mathf.FloorToInt(viewer.position.z / chunkSize), Mathf.FloorToInt(viewer.position.x / chunkSize));
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
            currentChunk = chunks[chunkX, chunkZ] = new TerrainChunk(new Vector2(chunkX * chunkSize, chunkZ * chunkSize), chunkSize, chunkX, chunkZ, chunkMaterial);
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
        MeshGeneration.GenerateMesh(chunk, Noise.GenerateNoiseMap(chunkSize, chunkSize, chunkX, chunkZ, seed, scale));
    }



}

public class TerrainChunk
{
    GameObject chunkObject;
    int x;
    int z;
    int chunkSize;

    public TerrainChunk(Vector2 chunkPosition, int size, int chunkX, int chunkY, Material material)
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

}
