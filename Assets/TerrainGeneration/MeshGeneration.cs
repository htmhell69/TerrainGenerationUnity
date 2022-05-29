using UnityEngine;
using Unity.Collections;

public static class MeshGeneration
{

    public static MeshData GenerateMesh(ChunkData chunkData, NativeArray<float> heightmap)
    {
        int chunkWidth = chunkData.chunkSize;
        int chunkHeight = chunkData.chunkSize;
        NativeArray<int> triangles = new NativeArray<int>(chunkData.chunkSize * chunkData.chunkSize * 6, Allocator.Temp);
        NativeArray<Vector3> vertices = new NativeArray<Vector3>((chunkData.chunkSize + 1) * (chunkData.chunkSize + 1), Allocator.Temp);
        NativeArray<Vector2> uvs = new NativeArray<Vector2>((chunkData.chunkSize + 1) * (chunkData.chunkSize + 1), Allocator.Temp);
        int tris = 0;
        int vert = 0;

        int heightMultiplier = chunkData.biomeHeightMultiplier;

        for (int i = 0, z = 0; z <= chunkHeight; z++)
        {
            for (int x = 0; x <= chunkWidth; x++)
            {
                vertices[i] = new Vector3(x, heightmap[z * chunkHeight + x] * heightMultiplier, z);
                uvs[i] = new Vector2(x / (float)chunkWidth, z / (float)chunkHeight);
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

        return new MeshData(vertices, uvs, triangles);
    }
}

public struct MeshData
{
    public NativeArray<Vector3> vertices;
    public NativeArray<Vector2> uvs;
    public NativeArray<int> triangles;
    public MeshData(NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles)
    {
        this.vertices = vertices;
        this.uvs = uvs;
        this.triangles = triangles;
    }

}