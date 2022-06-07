using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
public class BiomeHandler : MonoBehaviour
{
    [SerializeField] Biome[] biomes;
    [Range(1, 100)]
    [SerializeField] int biomeDiversity;
    Biome currentBiome;
    // Start is called before the first frame update
    void Start()
    {
        if (biomes.Length != 0)
        {
            bool hasFoundBiome = false;
            int biomeIndex = 0;
            while (!hasFoundBiome)
            {
                biomeIndex = UnityEngine.Random.Range(0, biomes.Length);
                if (UnityEngine.Random.Range(0, 1) < (float)biomes[biomeIndex].likelihood / 100)
                {
                    hasFoundBiome = true;
                    currentBiome = biomes[biomeIndex];
                }
            }
        }
    }
    public Biome SelectBiome()
    {
        if (biomes.Length != 0)
        {
            if (UnityEngine.Random.Range(0, 1) < (float)biomeDiversity / 100)
            {
                bool hasFoundBiome = false;
                int biomeIndex = 0;
                while (!hasFoundBiome)
                {
                    biomeIndex = UnityEngine.Random.Range(0, biomes.Length);
                    if (UnityEngine.Random.Range(0, 1) < (float)biomes[biomeIndex].likelihood / 100)
                    {
                        hasFoundBiome = true;
                        currentBiome = biomes[biomeIndex];
                    }
                }
                return biomes[biomeIndex];
            }
            else
            {
                return currentBiome;
            }
        }
        else
        {
            return new Biome();
        }
    }


}
[System.Serializable]
public struct Biome
{
    [Range(1, 100)]
    public int likelihood;
    public int heightMultiplier;
    public int noiseScale;
    public int id;
    public TerrainColor[] terrainColors;
    public Detail[] details;
    [Range(0.001f, 100)]
    public float detailChance;
    public GameObject[] detailGameObjects;
}
[System.Serializable]
public struct TerrainColor
{
    public float height;
    public Color color;
}

[System.Serializable]
public struct Detail
{
    [Range(0.001f, 100)]
    public float likelihood;
    public int maxHeight;
    public int minHeight;
    public int gameObjectIndex;
    public int size;
    public float smoothness;
}
