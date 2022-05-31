using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
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
    [SerializeField] int resolution;
    [Header("Height Map Generation")]
    [SerializeField] int seed;
    BiomeHandler biomeHandler;
    int currentAmountOfChunks;
    Vector3 viewerPosition = new Vector3();
    int chunksVisible;
    int verticeSize;
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
    List<GetMeshData> meshJobData = new List<GetMeshData>();
    List<JobHandle> meshJob = new List<JobHandle>();
    List<TerrainChunk> toBeAdjusted = new List<TerrainChunk>();



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
        JobHandling();

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
    public void JobHandling()
    {
        for (int i = 0; i < meshJob.Count; i++)
        {
            if (meshJob[i].IsCompleted)
            {
                meshJob[i].Complete();
                TerrainChunk currentChunk = chunks[meshJobData[i].chunkData.chunkPosition.x,
                meshJobData[i].chunkData.chunkPosition.y];
                PostMeshGeneration(currentChunk, meshJobData[i].meshData);
                toBeAdjusted.Add(currentChunk);
                meshJobData[i].noiseMap.Dispose();
                meshJobData[i].meshData.vertices.Dispose();
                meshJobData[i].meshData.triangles.Dispose();
                meshJobData[i].meshData.uvs.Dispose();
                meshJob.RemoveAt(i);
                meshJobData.RemoveAt(i);
            }
        }

        if (toBeAdjusted.Count != 0 && meshJob.Count == 0 && meshJobData.Count == 0)
        {
            while (toBeAdjusted.Count > 0)
            {
                TerrainChunk currentChunk = toBeAdjusted[0];
                Debug.Log(currentChunk.GetChunkX() + "," + currentChunk.GetChunkZ());
                Mesh mesh = currentChunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh;
                AdjustNoiseScaling(currentChunk);
                FixCornerVerticesOffset(currentChunk);
                mesh.UploadMeshData(false);
                ApplyColorsToChunk(currentChunk, mesh.vertices);
                ReadjustMeshCollider(currentChunk);

                toBeAdjusted.RemoveAt(0);
            }

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
    }
    public void ModifyTerrainChunk(int chunkX, int chunkZ)
    {
        TerrainChunk currentChunk = chunks[chunkX, chunkZ];
        if (currentChunk == null)
        {
            Biome biome = biomeHandler.SelectBiome();
            currentChunk = chunks[chunkX, chunkZ] = new TerrainChunk(new Vector2(chunkX * chunkSize, chunkZ * chunkSize), chunkSize, chunkX, chunkZ, biome);
            GenerateChunkDetails(currentChunk);
        }
        currentChunk.SetVisible(true);
    }

    public void PostMeshGeneration(TerrainChunk chunk, MeshData meshData)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.triangles = meshData.triangles.ToArray();
        mesh.uv = meshData.uvs.ToArray();
        chunk.GetChunkGameObject().GetComponent<MeshFilter>().mesh = mesh;
        mesh.UploadMeshData(false);
        ReadjustMeshCollider(chunk);
    }
    public void ApplyColorsToChunk(TerrainChunk chunk, Vector3[] vertices)
    {
        TerrainColor[] terrainColors = chunk.GetBiome().terrainColors;
        Color[] colorMap = new Color[verticeSize * verticeSize];
        for (int z = 0; z < verticeSize; z++)
        {
            for (int x = 0; x < verticeSize; x++)
            {
                float currentHeight = vertices[z * verticeSize + x].y;
                for (int i = 0; i < terrainColors.Length; i++)
                {
                    if (currentHeight <= terrainColors[i].height)
                    {
                        colorMap[z * verticeSize + x] = terrainColors[i].color;
                        break;
                    }
                }
            }
        }

        MeshRenderer textureRenderer = chunk.GetChunkGameObject().GetComponent<MeshRenderer>();
        Texture2D texture = ApplyingTextures.TextureFromColorMap(colorMap, verticeSize, verticeSize);
        textureRenderer.sharedMaterial.mainTexture = texture;
    }


    public void GenerateChunkDetails(TerrainChunk chunk)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        ChunkData chunkData = new ChunkData(new Vector2Int(chunkX, chunkZ), chunkGameObject.transform.position,
        seed, chunkSize, chunk.GetBiome().noiseScale, chunk.GetBiome().heightMultiplier);
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(verticeSize * verticeSize, Allocator.Persistent);
        NativeArray<Vector2> uvs = new NativeArray<Vector2>(verticeSize * verticeSize, Allocator.Persistent);
        NativeArray<int> triangles = new NativeArray<int>(chunkSize * chunkSize * 6, Allocator.Persistent);
        NativeArray<float> noiseMap = new NativeArray<float>(verticeSize * verticeSize, Allocator.Persistent);
        GetMeshData job = new GetMeshData(chunkData, vertices, uvs, triangles, noiseMap);
        meshJobData.Add(job);
        meshJob.Add(job.Schedule());
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
            if (z >= 0 && z < maxAmountOfChunks && chunks[chunkX, z] != null && chunk.GetBiome().id != chunks[chunkX, z].GetBiome().id)
            {
                LerpVertices(chunk, chunks[chunkX, z], side);
                mesh.UploadMeshData(false);
            }
            side *= -1;
        }

        side = new Vector2Int(-1, 0);
        for (int x = chunkX - 1; x <= chunkX + 1; x += 2)
        {

            if (x >= 0 && x < maxAmountOfChunks && chunks[x, chunkZ] != null && chunk.GetBiome().id != chunks[x, chunkZ].GetBiome().id)
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
    }
    public int[] GetSideVertexesOfChunk(TerrainChunk chunk, Vector2Int side)
    {
        MeshFilter meshFilter = chunk.GetChunkGameObject().GetComponent<MeshFilter>();
        int chunkStartingIndex = 0;
        int chunkIncrementAmount = 1;
        int[] vertices = new int[chunkSize + 1];
        //these loops will not be affecting the edges so some of these number may look odd. Edges is for another method.
        if (side == Vector2Int.up)
        {
            chunkStartingIndex = xZToIndex(0, chunkSize, verticeSize);
        }
        else if (side == Vector2Int.right)
        {
            chunkStartingIndex = xZToIndex(chunkSize, 0, verticeSize);
            chunkIncrementAmount = xZToIndex(0, 1, verticeSize);
        }
        else if (side == Vector2Int.down)
        {
            //no implimentation code is needed just thought it would be messy to leave it out.
        }
        else if (side == Vector2Int.left)
        {
            chunkStartingIndex = 0;
            chunkIncrementAmount = xZToIndex(0, 1, verticeSize);
        }

        for (int chunkI = chunkStartingIndex, i = 0; chunkI < (chunkIncrementAmount * (chunkSize + 1))
         + chunkStartingIndex; chunkI += chunkIncrementAmount)
        {
            vertices[i] = chunkI;
            i++;
        }

        return vertices;
    }

    void FixCornerVerticesOffset(TerrainChunk chunk)
    {
        AdjustCorner(chunk, new Vector2Int(0, 0));
        AdjustCorner(chunk, new Vector2Int(1, 0));
        AdjustCorner(chunk, new Vector2Int(0, 1));
        AdjustCorner(chunk, new Vector2Int(1, 1));
    }

    void AdjustCorner(TerrainChunk chunk, Vector2Int side)
    {


        ChunkVertice[] vertices = GetCentralCornerVertices(chunk, side);
        List<int> chunksToModify = new List<int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Debug.Log("vertice at " + i + " is " + vertices[i]);
            if (vertices[i] != null)
            {
                chunksToModify.Add(i);
            }
        }
        Debug.Log("amount to modify is " + chunksToModify.Count);


        float[] heightValues = new float[chunksToModify.Count];

        for (int i = 0; i < chunksToModify.Count; i++)
        {
            int index = chunksToModify[i];
            int chunkX = vertices[index].GetChunk().x;
            int chunkZ = vertices[index].GetChunk().y;
            heightValues[i] = chunks[chunkX, chunkZ].GetChunkGameObject().GetComponent<MeshFilter>().
                mesh.vertices[vertices[index].GetVerticeIndex()].y;
        }

        float newHeightValue = 0;
        for (int i = 0; i < heightValues.Length; i++)
        {
            newHeightValue += heightValues[i];
        }
        newHeightValue /= heightValues.Length;

        for (int i = 0; i < chunksToModify.Count; i++)
        {
            int index = chunksToModify[i];
            int chunkX = vertices[index].GetChunk().x;
            int chunkZ = vertices[index].GetChunk().y;
            MeshFilter chunkMeshFilter = chunks[chunkX, chunkZ].GetChunkGameObject().GetComponent<MeshFilter>();
            Vector3[] chunkVertices = chunkMeshFilter.mesh.vertices;
            chunkVertices[vertices[index].GetVerticeIndex()].y = newHeightValue;
            chunkMeshFilter.mesh.vertices = chunkVertices;
            chunkMeshFilter.mesh.UploadMeshData(false);
            ReadjustMeshCollider(chunks[chunkX, chunkZ]);
        }
    }

    ChunkVertice[] GetCentralCornerVertices(TerrainChunk startingChunk, Vector2 side)
    {
        int chunkX = startingChunk.GetChunkX();
        int chunkZ = startingChunk.GetChunkZ();
        ChunkVertice[] verticeArray = new ChunkVertice[4];
        TerrainChunk[] corneringChunks = new TerrainChunk[4];
        int[] verticeIndexes = new int[4];
        corneringChunks[0] = startingChunk;
        if (side == new Vector2(0, 0))
        {
            verticeIndexes[0] = GetCornerIndex(0, 0);
            verticeIndexes[1] = GetCornerIndex(0, 1);
            verticeIndexes[2] = GetCornerIndex(1, 1);
            verticeIndexes[3] = GetCornerIndex(1, 0);
        }
        else if (side == new Vector2(1, 0))
        {
            verticeIndexes[0] = GetCornerIndex(1, 0);
            verticeIndexes[1] = GetCornerIndex(1, 1);
            verticeIndexes[2] = GetCornerIndex(0, 1);
            verticeIndexes[3] = GetCornerIndex(0, 0);
        }
        else if (side == new Vector2(0, 1))
        {
            verticeIndexes[0] = GetCornerIndex(0, 1);
            verticeIndexes[1] = GetCornerIndex(0, 0);
            verticeIndexes[2] = GetCornerIndex(1, 0);
            verticeIndexes[3] = GetCornerIndex(1, 1);
        }
        else if (side == new Vector2(1, 1))
        {
            verticeIndexes[0] = GetCornerIndex(1, 1);
            verticeIndexes[1] = GetCornerIndex(0, 1);
            verticeIndexes[2] = GetCornerIndex(0, 0);
            verticeIndexes[3] = GetCornerIndex(1, 0);
        }
        int xChunkModifier = 1;
        int zChunkModifier = 1;

        if (side.x == 0)
        {
            xChunkModifier = -1;
        }
        if (side.y == 0)
        {
            zChunkModifier = -1;
        }

        corneringChunks[1] = chunks[chunkX, chunkZ + zChunkModifier];
        corneringChunks[2] = chunks[chunkX + xChunkModifier, chunkZ + zChunkModifier];
        corneringChunks[3] = chunks[chunkX + xChunkModifier, chunkZ];

        for (int i = 0; i < 4; i++)
        {
            if (corneringChunks[i] != null)
            {
                int x = corneringChunks[i].GetChunkX();
                int z = corneringChunks[i].GetChunkZ();
                verticeArray[i] = new ChunkVertice(new Vector2Int(x, z), verticeIndexes[i]);
            }

        }
        return verticeArray;
    }

    public int GetCornerIndex(int x, int z)
    {
        if (x == 0 && z == 0)
        {
            return 0;
        }
        else if (x == 1 && z == 0)
        {
            return chunkSize;
        }
        else if (x == 0 && z == 1)
        {
            return xZToIndex(0, chunkSize, verticeSize);
        }
        else if (x == 1 && z == 1)
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

public class ChunkVertice
{
    Vector2Int chunk;
    int verticeIndex;
    public ChunkVertice(Vector2Int chunk, int verticeIndex)
    {
        this.chunk = chunk;
        this.verticeIndex = verticeIndex;
    }
    public int GetVerticeIndex()
    {
        return verticeIndex;
    }
    public Vector2Int GetChunk()
    {
        return chunk;
    }
}

public class TerrainChunk
{
    GameObject chunkObject;
    int x;
    int z;
    int chunkSize;
    Biome biome;

    public TerrainChunk(Vector2 chunkPosition, int size, int chunkX, int chunkY, Biome biome)
    {
        chunkObject = new GameObject("chunk");
        chunkObject.AddComponent<MeshRenderer>();
        chunkObject.AddComponent<MeshFilter>();
        chunkObject.AddComponent<MeshCollider>();
        x = chunkX;
        z = chunkY;
        chunkObject.transform.position = new Vector3(chunkPosition.x, 0, chunkPosition.y);
        chunkObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Specular"));
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
    public Vector2Int chunkPosition;
    public Vector3 worldPosition;
    public int seed;
    public int chunkSize;
    public int biomeScale;
    public int biomeHeightMultiplier;
    public ChunkData(Vector2Int chunkPosition, Vector3 worldPosition, int seed, int chunkSize, int biomeNoiseScale, int biomeHeightMultiplier)
    {
        this.chunkPosition = chunkPosition;
        this.worldPosition = worldPosition;
        this.seed = seed;
        this.chunkSize = chunkSize;
        this.biomeScale = biomeNoiseScale;
        this.biomeHeightMultiplier = biomeHeightMultiplier;
    }
}

public struct GetMeshData : IJob
{
    public ChunkData chunkData;
    public NativeArray<float> noiseMap;
    public MeshData meshData;
    public void Execute()
    {
        for (int z = 0; z < chunkData.chunkSize + 1; z++)
        {
            for (int x = 0; x < chunkData.chunkSize + 1; x++)
            {
                noiseMap[z * (chunkData.chunkSize + 1) + x] = Noise.GetNoiseValue(chunkData, x, z);
            }
        }
        MeshData temporaryMeshData = MeshGeneration.GenerateMesh(chunkData, noiseMap, meshData.triangles, meshData.vertices, meshData.uvs);
        meshData = temporaryMeshData;
    }

    public GetMeshData(ChunkData chunkData, NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles, NativeArray<float> noiseMap)
    {
        this.chunkData = chunkData;
        this.meshData = new MeshData(vertices, uvs, triangles);
        this.noiseMap = noiseMap;
    }


}

public struct GetColorMap : IJob
{
    public float[,] noiseMap;
    public Color[] colorMap;
    public void Execute()
    {

    }
}
