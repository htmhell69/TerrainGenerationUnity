using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
public static class Noise
{
    // Start is called before the first frame update
    public static NativeArray<float> GenerateNoiseMap(ChunkData chunkData)
    {
        int chunkSize = chunkData.chunkSize;
        Vector2Int chunkPosition = chunkData.chunkPosition;
        Vector3 chunkWorldPosition = chunkData.worldPosition;
        int width = (chunkSize + 1);
        int height = (chunkSize + 1);
        int chunkX = chunkPosition.x;
        int chunkZ = chunkPosition.y;
        NativeArray<float> noiseMap = new NativeArray<float>(height * width, Allocator.TempJob);
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float xOffset = chunkWorldPosition.x;
                float zOffset = chunkWorldPosition.z;
                float perlinX = (float)(x + xOffset) / chunkData.biomeScale + chunkData.seed;
                float perlinZ = (float)(z + zOffset) / chunkData.biomeScale + chunkData.seed;
                float perlinOutput = Mathf.PerlinNoise(perlinX, perlinZ);
                noiseMap[z * height + x] = perlinOutput;
            }
        }
        return noiseMap;
    }

    public static float GetNoiseValue(ChunkData chunkData, int x, int z)
    {
        int chunkSize = chunkData.chunkSize;
        Vector2Int chunkPosition = chunkData.chunkPosition;
        Vector3 chunkWorldPosition = chunkData.worldPosition;
        int width = (chunkSize + 1);
        int height = (chunkSize + 1);
        int chunkX = chunkPosition.x;
        int chunkZ = chunkPosition.y;
        float xOffset = chunkWorldPosition.x;
        float zOffset = chunkWorldPosition.z;
        float perlinX = (float)(x + xOffset) / chunkData.biomeScale + chunkData.seed;
        float perlinZ = (float)(z + zOffset) / chunkData.biomeScale + chunkData.seed;
        float perlinOutput = Mathf.PerlinNoise(perlinX, perlinZ);
        return perlinOutput;
    }

}
