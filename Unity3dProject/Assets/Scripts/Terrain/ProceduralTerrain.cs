using UnityEngine;
using UnityEditor;

public class ProceduralTerrain : MonoBehaviour
{
    // Terrain data
    TerrainData terrainData;
    // Terrain size and height
    public int terrainSize = 256;
    public int terrainHeight = 64;
    // Noise parameters
    public float noiseScale = 0.1f;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;

    private void Start()
    {
        // Get the terrain data
        terrainData = GetComponent<Terrain>().terrainData;
        // Set the terrain size and height
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);
        // Generate the heightmap data
        float[,] heightmapData = GenerateHeightmap();
        // Apply the heightmap data to the terrain
        terrainData.SetHeights(0, 0, heightmapData);
    }

    // Function to generate the heightmap data
    float[,] GenerateHeightmap()
    {
        // Create a 2D array to store the heightmap data
        float[,] heightmapData = new float[terrainSize, terrainSize];

        // Iterate through the heightmap data and generate the heights using Perlin noise
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                // Generate the Perlin noise
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x / noiseScale * frequency;
                    float sampleY = y / noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Set the height of the current point
                heightmapData[x, y] = noiseHeight;
            }
        }

        return heightmapData;
    }

    private void OnValidate()
    {
        // Set the terrain object as dirty to trigger a rebuild in the editor
        EditorUtility.SetDirty(gameObject);
    }
}
