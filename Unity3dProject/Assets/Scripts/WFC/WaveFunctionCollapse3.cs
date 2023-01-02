using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse3 : MonoBehaviour
{
    CompatibilityMatrixGenerator compatibilityMatrixGenerator;

    public List<GameObject> tilePrefabs; // List of tile prefabs
    public bool[,] compatibilityMatrix; // Compatibility matrix for tiles
    [Range(5, 50)] public int patternSizeX = 10; // Size of pattern on x axis
    [Range(5, 50)] public int patternSizeZ = 10; // Size of pattern on z axis

    private int[,] grid; // Grid of tiles
    private int numTiles; // Number of tile prefabs

    private void Awake()
    {
        compatibilityMatrixGenerator = GetComponent<CompatibilityMatrixGenerator>();
        if (compatibilityMatrixGenerator != null)
        {
            compatibilityMatrix = compatibilityMatrixGenerator.matrix;
        }
    }

    void Start()
    {
        // Initialize grid and number of tiles
        grid = new int[patternSizeX, patternSizeZ];
        numTiles = tilePrefabs.Count;

        // Set all elements of grid to -1
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                grid[i, j] = -1;
            }
        }

        // Iterate through elements of grid and assign tiles
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                // Generate a list of compatible tiles
                List<int> compatibleTiles = new List<int>();
                for (int k = 0; k < numTiles; k++)
                {
                    if (IsCompatible(k, i, j))
                    {
                        compatibleTiles.Add(k);
                    }
                }

                // Select a random compatible tile
                int selectedTile = compatibleTiles[Random.Range(0, compatibleTiles.Count)];
                grid[i, j] = selectedTile;
            }
        }

        // Instantiate prefabs in the appropriate positions to create the final pattern
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                int tileIndex = grid[i, j];
                GameObject prefab = tilePrefabs[tileIndex];
                Vector3 position = new Vector3(i, 0, j);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }
    }

    // Function to check if a tile is compatible with its neighbors
    bool IsCompatible(int tileIndex, int x, int z)
    {
        // Check compatibility with left neighbor
        if (x > 0 && !compatibilityMatrix[tileIndex, grid[x - 1, z]])
        {
            return false;
        }

        // Check compatibility with top neighbor
        if (z > 0 && !compatibilityMatrix[tileIndex, grid[x, z - 1]])
        {
            return false;
        }

        return true;
    }


    void OnDrawGizmos()
    {
        // Set the color of the gizmos
        Gizmos.color = Color.yellow;

        // Draw the horizontal lines
        for (int z = 0; z <= patternSizeZ; z++)
        {
            Vector3 start = new Vector3(0, 0, z);
            Vector3 end = new Vector3(patternSizeX, 0, z);
            Gizmos.DrawLine(start, end);
        }

        // Draw the vertical lines
        for (int x = 0; x <= patternSizeX; x++)
        {
            Vector3 start = new Vector3(x, 0, 0);
            Vector3 end = new Vector3(x, 0, patternSizeZ);
            Gizmos.DrawLine(start, end);
        }
    }
}