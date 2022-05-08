using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // Start is called before the first frame update
    static float[,] noiseMap;


    public static float[,] GenerateNoiseMap(int width, int height, int chunkX, int chunkZ, int seed, int scale)
    {
        noiseMap = new float[width, height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float perlinX = x / scale + seed + (width * chunkX);
                float perlinY = z / scale + seed + (height * chunkZ);
                float perlinOutput = Mathf.PerlinNoise(x, z);
                noiseMap[x, z] = perlinOutput;
            }
        }
        return noiseMap;
    }

}
