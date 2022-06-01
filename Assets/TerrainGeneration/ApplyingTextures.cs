using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
public class ApplyingTextures
{
    public static Texture2D TextureFromColorMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }


    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    public static NativeArray<Color> GenerateColorMap(NativeArray<TerrainColor> terrainColors, NativeArray<float> noiseMap, NativeArray<Color> colorMap, int verticeSize)
    {
        for (int z = 0; z < verticeSize; z++)
        {
            for (int x = 0; x < verticeSize; x++)
            {
                float currentHeight = noiseMap[z * verticeSize + x];
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
        return colorMap;
    }

    public static void ApplyTextureToChunk(Color[] colorMap, TerrainChunk chunk, int verticeSize)
    {
        MeshRenderer textureRenderer = chunk.GetChunkGameObject().GetComponent<MeshRenderer>();
        Texture2D texture = ApplyingTextures.TextureFromColorMap(colorMap, verticeSize, verticeSize);
        textureRenderer.sharedMaterial.mainTexture = texture;
    }
}
