using UnityEngine;
using Unity.Collections;

public static class MeshGeneration
{

    public static MeshData GenerateMesh(ChunkData chunkData, NativeArray<float> heightmap, NativeArray<int> triangles,
    NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<Detail> details, NativeList<DetailPlacement> detailPlacements, int detailChance)
    {
        NativeArray<bool> usedPositions = new NativeArray<bool>((chunkData.chunkSize + 1) * (chunkData.chunkSize + 1),
        Allocator.Temp);
        int chunkWidth = chunkData.chunkSize;
        int chunkHeight = chunkData.chunkSize;
        int tris = 0;
        int vert = 0;

        int heightMultiplier = chunkData.biomeHeightMultiplier;


        for (int i = 0, z = 0; z <= chunkHeight; z++)
        {
            for (int x = 0; x <= chunkWidth; x++)
            {
                vertices[i] = new Vector3(x, heightmap[z * chunkHeight + x] * heightMultiplier, z);
                uvs[i] = new Vector2(x / (float)chunkWidth, z / (float)chunkHeight);
                if (usedPositions[z * chunkData.chunkSize + x] == false)
                {
                    for (int index = 0; index < details.Length; index++)
                    {
                        if (details[index].likelihood > Random.Range(0, 100) && detailChance > Random.Range(0, 100))
                        {
                            detailPlacements.Add(new DetailPlacement(new Vector2(x, z), details[index].gameObjectIndex));
                            int size = details[i].size;
                            for (int detailZ = z - (size - 1); detailZ <= z + (size - 1); detailZ++)
                            {
                                if (detailZ < 0)
                                {
                                    continue;
                                }
                                if (detailZ >= chunkData.chunkSize + 1)
                                {
                                    break;
                                }
                                for (int detailX = x - (size - 1); detailX <= x + (size - 1); detailX++)
                                {
                                    usedPositions[detailZ * chunkData.chunkSize + detailX] = true;
                                }
                            }
                        }
                    }
                }
                i++;
            }
        }

        for (int z = 0; z < chunkHeight; z++)
        {
            for (int x = 0; x < chunkWidth; x++)
            {

                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + chunkWidth + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + chunkWidth + 1;
                triangles[tris + 5] = vert + chunkWidth + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }


        return new MeshData(vertices, uvs, triangles, detailPlacements);
    }


}

public struct MeshData
{
    public NativeArray<Vector3> vertices;
    public NativeArray<Vector2> uvs;
    public NativeArray<int> triangles;
    public NativeList<DetailPlacement> detailPlacements;
    public MeshData(NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles, NativeList<DetailPlacement> detailPlacements)
    {
        this.vertices = vertices;
        this.uvs = uvs;
        this.triangles = triangles;
        this.detailPlacements = detailPlacements;
    }

}