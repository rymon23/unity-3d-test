using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StreetGenerator : MonoBehaviour
{
    public int seed = 0;
    public float scale = 10f;
    public Vector2 offset = Vector2.zero;
    public float minNoise = 0.2f;
    public float maxNoise = 0.8f;
    public float spacing = 1.0f;

    public int width = 100;
    public int height = 100;
    public float heightScale = 1.0f;

    Mesh mesh;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    void Update()
    {
        GenerateStreetMap();
    }

    void GenerateStreetMap()
    {
        Random.InitState(seed);

        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noise = Mathf.PerlinNoise((x + offset.x) / scale, (y + offset.y) / scale);
                float height = Mathf.Lerp(0, heightScale, noise);

                vertices[index] = new Vector3(x * spacing, height, y * spacing);

                if (x < width - 1 && y < height - 1)
                {
                    triangles[index * 6] = index;
                    triangles[index * 6 + 1] = index + width + 1;
                    triangles[index * 6 + 2] = index + width;
                    triangles[index * 6 + 3] = index;
                    triangles[index * 6 + 4] = index + 1;
                    triangles[index * 6 + 5] = index + width + 1;
                }

                index++;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
