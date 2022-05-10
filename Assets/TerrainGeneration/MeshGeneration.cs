using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGeneration
{
    public static void GenerateMesh(TerrainChunk chunk, float[,] heightmap, int heightMultiplier)
    {
        GameObject gameObject = chunk.GetChunkGameObject();
        Mesh mesh = new Mesh();
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        int chunkWidth = chunk.GetChunkSize();
        int chunkHeight = chunk.GetChunkSize();
        int[] triangles = new int[chunkWidth * chunkHeight * 6];
        Vector3[] vertices = new Vector3[(chunkWidth + 1) * (chunkHeight + 1)];
        int tris = 0;
        int vert = 0;
        meshFilter.mesh = mesh;

        for (int i = 0, z = 0; z <= chunkHeight; z++)
        {
            for (int x = 0; x <= chunkWidth; x++)
            {
                vertices[i] = new Vector3(x, heightmap[x, z] * heightMultiplier, z);
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


        mesh.vertices = vertices;
        mesh.triangles = triangles;
        //fixing lighting issues
        mesh.RecalculateBounds();
        //readjusting collider
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

}
