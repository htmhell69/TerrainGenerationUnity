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

    }

    // Update is called once per frame
    void Update()
    {

    }
    bool IsEnabled()
    {
        return isEnabled;
    }
    Biome SelectBiome()
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



}
[System.Serializable]
public class Biome
{
    [Range(1, 100)]
    [SerializeField] int likelihood;
    [SerializeField] int heightScale;
    public int GetLikelihood()
    {
        return likelihood;
    }
}
