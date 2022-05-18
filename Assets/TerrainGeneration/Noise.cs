using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // Start is called before the first frame update
    static float[,] noiseMap;


    public static float[,] GenerateNoiseMap(TerrainChunk chunk, int seed, int scale)
    {
        GameObject chunkGameObject = chunk.GetChunkGameObject();
        int width = (chunk.GetChunkSize() + 1 + chunk.GetChunkX());
        int height = (chunk.GetChunkSize() + 1 + chunk.GetChunkZ());
        int chunkX = chunk.GetChunkX();
        int chunkZ = chunk.GetChunkZ();
        noiseMap = new float[width, height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float xOffset = chunkGameObject.transform.position.x;
                float zOffset = chunkGameObject.transform.position.z;
                float perlinX = (float)(x + xOffset) / scale + seed;
                float perlinZ = (float)(z + zOffset) / scale + seed;
                float perlinOutput = Mathf.PerlinNoise(perlinX, perlinZ);
                noiseMap[x, z] = perlinOutput;
            }
        }
        return noiseMap;
    }

}