using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse6 : MonoBehaviour
{
    private CompatibilityMatrixGenerator compatibilityMatrixGenerator;
    [SerializeField] private List<Tile> tilePrefabs; // List of tile prefabs
    public bool[,] compatibilityMatrix; // Compatibility matrix for tiles
    [Range(5, 144)] public int patternSizeX = 10; // Size of pattern on x axis
    [Range(5, 144)] public int patternSizeZ = 10; // Size of pattern on z axis
    [Range(0.5f, 12f)] public float cellWidth = 1f; // Width of grid cells
    [Range(0.5f, 12f)] public float cellHeight = 1f; // Height of grid cells

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
                Tile prefab = tilePrefabs[tileIndex];
                Vector3 position = new Vector3(i * cellWidth, 0, j * cellHeight);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }
    }

    // Function to check if a tile is compatible with its neighbors
    bool IsCompatible(int tileIndex, int x, int z)
    {
        Tile tile = tilePrefabs[tileIndex];

        // Check compatibility with left neighbor
        if (x > 0)
        {
            Tile leftTile = tilePrefabs[grid[x - 1, z]];
            if (!compatibilityMatrix[tile.GetSideSocketId(Tile.TileSide.Right), leftTile.GetSideSocketId(Tile.TileSide.Left)])
            {
                return false;
            }
        }

        // Check compatibility with top neighbor
        if (z > 0)
        {
            Tile topTile = tilePrefabs[grid[x, z - 1]];
            if (!compatibilityMatrix[tile.GetSideSocketId(Tile.TileSide.Back), topTile.GetSideSocketId(Tile.TileSide.Front)])
            {
                return false;
            }
        }
        return true;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        // Draw vertical lines
        for (int i = 0; i < patternSizeX + 1; i++)
        {
            Vector3 startPos = new Vector3(i * cellWidth, 0, 0);
            Vector3 endPos = new Vector3(i * cellWidth, 0, patternSizeZ * cellHeight);
            Gizmos.DrawLine(startPos, endPos);
        }

        // Draw horizontal lines
        for (int j = 0; j < patternSizeZ + 1; j++)
        {
            Vector3 startPos = new Vector3(0, 0, j * cellHeight);
            Vector3 endPos = new Vector3(patternSizeX * cellWidth, 0, j * cellHeight);
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}