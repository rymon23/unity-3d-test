using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse4 : MonoBehaviour
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

        // First, assign tiles to the border of the grid
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                if (i == 0 || i == patternSizeX - 1 || j == 0 || j == patternSizeZ - 1)
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
        }

        // Then, assign tiles to the remaining elements of the grid
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                if (grid[i, j] == -1)
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
        // Check if current tile is an edge tile
        bool isEdgeTile = (x == 0 || x == patternSizeX - 1 || z == 0 || z == patternSizeZ - 1);

        // Check compatibility with left neighbor
        if ((!isEdgeTile || (isEdgeTile && (z == 0 || z == patternSizeZ - 1))) && x > 0 && !compatibilityMatrix[tileIndex, grid[x - 1, z]])
        {
            return false;
        }

        // Check compatibility with top neighbor
        if ((!isEdgeTile || (isEdgeTile && (x == 0 || x == patternSizeX - 1))) && z > 0 && !compatibilityMatrix[tileIndex, grid[x, z - 1]])
        {
            return false;
        }

        return true;
    }
}