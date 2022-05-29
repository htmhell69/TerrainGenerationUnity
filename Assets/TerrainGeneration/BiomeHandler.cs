using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class BiomeHandler : MonoBehaviour
{
    [SerializeField] bool isEnabled;
    [SerializeField] Biome[] biomes;
    [Range(1, 100)]
    [SerializeField] int biomeDiversity;
    Biome currentBiome;
    // Start is called before the first frame update
    void Start()
    {
        if (isEnabled && biomes.Length != 0)
        {
            bool hasFoundBiome = false;
            int biomeIndex = 0;
            while (!hasFoundBiome)
            {
                biomeIndex = UnityEngine.Random.Range(0, biomes.Length);
                if (UnityEngine.Random.Range(0, 1) < (float)biomes[biomeIndex].GetLikelihood() / 100)
                {
                    hasFoundBiome = true;
                    currentBiome = biomes[biomeIndex];
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    public bool IsEnabled()
    {
        return isEnabled;
    }
    public Biome SelectBiome()
    {
        if (isEnabled || biomes.Length == 0)
        {
            if (UnityEngine.Random.Range(0, 1) < (float)biomeDiversity / 100)
            {
                bool hasFoundBiome = false;
                int biomeIndex = 0;
                while (!hasFoundBiome)
                {
                    biomeIndex = UnityEngine.Random.Range(0, biomes.Length);
                    if (UnityEngine.Random.Range(0, 1) < (float)biomes[biomeIndex].GetLikelihood() / 100)
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
            return null;
        }
    }



}
[System.Serializable]
public class Biome
{
    [Range(1, 100)]
    [SerializeField] int likelihood;
    [SerializeField] int heightMultiplier;
    [SerializeField] int noiseScale;
    [SerializeField] string name;
    [SerializeField] TerrainColor[] terrainColors;

    public int GetLikelihood()
    {
        return likelihood;
    }
    public int GetHeightMultiplier()
    {
        return heightMultiplier;
    }
    public int GetNoiseScale()
    {
        return noiseScale;
    }

    public string GetName()
    {
        return name;
    }
    public TerrainColor[] GetTerrainColors()
    {
        return terrainColors;
    }
}
[System.Serializable]
public class TerrainColor
{
    [SerializeField] float height;
    [SerializeField] Color color;
    public Color GetColor()
    {
        return color;
    }
    public float GetHeight()
    {
        return height;
    }
}

[System.Serializable]
public class Details
{
    [Range(1, 100)]
    [SerializeField] int likelihood;
    //will show up anwhere lower than this height
    [SerializeField] int height;
}
