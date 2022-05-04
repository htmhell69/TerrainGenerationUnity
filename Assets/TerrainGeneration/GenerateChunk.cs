using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayingChunks : MonoBehaviour
{
    Dictionary<Vector2, Chunk> allChunks = new Dictionary<Vector2, Chunk>();
    int amountOfChunks;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddChunk()
    {
        amountOfChunks++;
    }

    public class Chunk
    {
        bool isVisible;
        GameObject chunkObject;
        Vector2 position;

        Chunk(Vector2 position)
        {
            chunkObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            chunkObject.transform.position = new Vector3(position.x, 0, position.y);
        }
    }

}
