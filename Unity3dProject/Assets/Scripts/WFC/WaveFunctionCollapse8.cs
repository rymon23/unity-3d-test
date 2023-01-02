using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse8 : MonoBehaviour
{
    private TileSocketMatrixGenerator socketMatrixGenerator;
    [SerializeField] private List<Tile> tilePrefabs; // List of tile prefabs
    public bool[,] compatibilityMatrix; // Compatibility matrix for tiles
    [Range(5, 144)] public int patternSizeX = 10; // Size of pattern on x axis
    [Range(5, 144)] public int patternSizeZ = 10; // Size of pattern on z axis
    [Range(0.5f, 12f)] public float cellWidth = 1f; // Width of grid cells
    [Range(0.5f, 12f)] public float cellHeight = 1f; // Height of grid cells

    private (int, int)[,] grid; // Grid of tiles and their rotations
    private int numTiles; // Number of tile prefabs

    [SerializeField] private int matrixLength;
    [SerializeField] private bool showTileSocketLabels;
    private bool _showTileSocketLabels;
    [SerializeField] private List<Tile> activeTiles;
    [SerializeField] private int outOfBoundsSlotId = 1;

    void Start()
    {
        UpdateCompatibilityMatrix();

        if (compatibilityMatrix == null) compatibilityMatrix = socketMatrixGenerator.matrix;
        if (compatibilityMatrix.Length == 0)
        {
            Debug.LogError("compatibilityMatrix is unset");
            return;
        }

        // Initialize grid and number of tiles
        grid = new (int, int)[patternSizeX, patternSizeZ];
        numTiles = tilePrefabs.Count;

        activeTiles = new List<Tile>();

        // Set all elements of grid to (-1, -1)
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                grid[i, j] = (-1, -1);
            }
        }

        AssignTiles();

        InstantiateTiles();
    }

    // void AssignTiles()
    // {
    //     // Iterate through elements of grid and assign border tiles
    //     for (int i = 0; i < patternSizeX; i++)
    //     {
    //         for (int j = 0; j < patternSizeZ; j++)
    //         {
    //             if (i == 0 || i == patternSizeX - 1 || j == 0 || j == patternSizeZ - 1)
    //             {
    //                 // Select a random compatible tile and rotation
    //                 (int, int) selectedTile = SelectRandomCompatibleTile(i, j);
    //                 grid[i, j] = selectedTile;
    //             }
    //         }
    //     }

    //     // Iterate through elements of grid and assign inner tiles
    //     for (int i = 1; i < patternSizeX - 1; i++)
    //     {
    //         for (int j = 1; j < patternSizeZ - 1; j++)
    //         {
    //             // Select a random compatible tile and rotation
    //             (int, int) selectedTile = SelectRandomCompatibleTile(i, j);
    //             grid[i, j] = selectedTile;
    //         }
    //     }
    // }
    void AssignTiles()
    {
        // Iterate through elements of grid and assign border tiles
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                if (i == 0 || i == patternSizeX - 1 || j == 0 || j == patternSizeZ - 1)
                {
                    // Select a random compatible tile and rotation that matches the specific slotId for the outside of the grid
                    (int, int) selectedTile = SelectRandomCompatibleTile(i, j, outOfBoundsSlotId);
                    grid[i, j] = selectedTile;
                }
                else
                {
                    // Select a random compatible tile and rotation
                    (int, int) selectedTile = SelectRandomCompatibleTile(i, j);
                    grid[i, j] = selectedTile;
                }
            }
        }
    }



    // Instantiate the tiles in the appropriate positions to create the final pattern
    void InstantiateTiles()
    {
        for (int i = 0; i < patternSizeX; i++)
        {
            for (int j = 0; j < patternSizeZ; j++)
            {
                (int tileIndex, int rotation) = grid[i, j];
                Tile prefab = tilePrefabs[tileIndex];
                Vector3 position = new Vector3(i * cellWidth, 0, j * cellHeight);
                Quaternion rotationQuat = Quaternion.Euler(0, 90 * rotation, 0);
                Tile activeTile = Instantiate(prefab, position, rotationQuat);

                activeTiles.Add(activeTile);
                activeTile.showSocketLabels = showTileSocketLabels;
            }
        }
    }

    // Method to generate a list of compatible tile rotations for a given grid cell
    List<int> GetCompatibleRotations(int x, int z, int slotId)
    {
        // Initialize list of compatible rotations
        List<int> compatibleRotations = new List<int>();

        // Iterate through all tile prefabs
        for (int i = 0; i < numTiles; i++)
        {
            // Iterate through all possible rotations of the current tile
            for (int j = 0; j < 4; j++)
            {
                // Check if the current tile and rotation is compatible with the given grid cell and slotId
                if (compatibilityMatrix[i, slotId] && IsCompatible(x, z, i, j))
                {
                    // Add the current rotation to the list of compatible rotations
                    compatibleRotations.Add(j);
                }
            }
        }

        // Return the list of compatible rotations
        return compatibleRotations;
    }


    // Method to select a random compatible tile rotation from the list
    int SelectRandomCompatibleRotation(List<int> rotations) => rotations[Random.Range(0, rotations.Count)];

    // // Function to select a random compatible tile and rotation
    (int, int) SelectRandomCompatibleTile(int x, int z)
    {
        // Generate a list of compatible tiles and rotations
        List<(int, int)> compatibleTiles = new List<(int, int)>();
        for (int i = 0; i < numTiles; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (IsCompatible(i, x, z, j))
                {
                    compatibleTiles.Add((i, j));
                }
            }
        }

        // Select a random compatible tile and rotation
        (int, int) selectedTile = compatibleTiles[Random.Range(0, compatibleTiles.Count)];
        return selectedTile;
    }
    (int, int) SelectRandomCompatibleTile(int x, int z, int slotId)
    {
        // Initialize list of compatible tiles and rotations
        List<(int, int)> compatibleTiles = new List<(int, int)>();

        // Iterate through all tile prefabs
        for (int i = 0; i < numTiles; i++)
        {
            // Iterate through all possible rotations of the current tile
            for (int j = 0; j < 4; j++)
            {
                // Debug.Log("slotId: " + slotId + ", compatibilityMatrix: [" + i + "," + slotId + " ] :" + compatibilityMatrix[i, slotId]);

                // Check if the current tile and rotation is compatible with the given grid cell and slotId
                if (compatibilityMatrix[i, slotId] && IsCompatible(i, x, z, j))
                {
                    // Add the current tile and rotation to the list of compatible tiles
                    compatibleTiles.Add((i, j));
                }
            }
        }

        // Debug.Log("compatibleTiles: " + compatibleTiles.Count);

        // Select a random compatible tile and rotation
        (int, int) selectedTile = compatibleTiles[Random.Range(0, compatibleTiles.Count)];
        return selectedTile;
    }

    bool IsCompatible(int tileIndex, int x, int z, int rotation)
    {
        // Get the prefab for the selected tile
        Tile tile = tilePrefabs[tileIndex];

        // Check if the current grid cell is a border tile
        bool isBorderTile = (x == 0 || x == patternSizeX - 1 || z == 0 || z == patternSizeZ - 1);


        // Check compatibility with left neighbor
        if (x > 0)
        {
            (int leftTileIndex, int leftRotation) = grid[x - 1, z];
            if (leftTileIndex >= 0)
            {
                Tile leftTile = tilePrefabs[leftTileIndex];
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Right], leftTile.rotatedSideSocketIds[leftRotation][(int)Tile.TileSide.Left]])
                {

                    Debug.Log("Incompatible -  tile: " + tile.name + " w leftTile: " + leftTile.name);
                    Debug.Log("tile - Rotation: " + rotation + "  " + leftTile.name + " leftRotation: " + leftRotation);

                    return false;
                }
            }
        }
        else
        {
            if (isBorderTile)
            {
                // Check compatibility of border tile's left side socket with outOfBoundsSlotId
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Right], outOfBoundsSlotId])
                {
                    return false;
                }
            }

            // No left neighbor, so compatibility is automatically true
        }

        // Check compatibility with top neighbor
        if (z > 0)
        {
            (int topTileIndex, int topRotation) = grid[x, z - 1];
            if (topTileIndex >= 0)
            {
                Tile topTile = tilePrefabs[topTileIndex];
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Back], topTile.rotatedSideSocketIds[topRotation][(int)Tile.TileSide.Front]])
                {

                    Debug.Log("Incompatible -  tile: " + tile.name + " w topTile: " + topTile.name);
                    Debug.Log("tile - Rotation: " + rotation + "  " + topTile.name + " leftRotation: " + topRotation);

                    return false;
                }
            }
        }
        else
        {
            if (isBorderTile)
            {
                // Check compatibility of border tile's left side socket with outOfBoundsSlotId
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Back], outOfBoundsSlotId])
                {
                    return false;
                }
            }

            // No top neighbor, so compatibility is automatically true
        }

        // Check compatibility with right neighbor
        if (x < patternSizeX - 1)
        {
            (int rightTileIndex, int rightRotation) = grid[x + 1, z];
            Debug.Log("rightTileIndex: " + rightTileIndex);

            if (rightTileIndex >= 0)
            {
                Tile rightTile = tilePrefabs[rightTileIndex];
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Left], rightTile.rotatedSideSocketIds[rightRotation][(int)Tile.TileSide.Right]])
                {

                    Debug.Log("Incompatible -  tile: " + tile.name + " w rightTile: " + rightTile.name);
                    Debug.Log("tile - Rotation: " + rotation + "  " + rightTile.name + " leftRotation: " + rightRotation);

                    return false;
                }
            }
        }
        else
        {
            if (isBorderTile)
            {
                // Check compatibility of border tile's left side socket with outOfBoundsSlotId
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Left], outOfBoundsSlotId])
                {
                    return false;
                }
            }
            // No right neighbor, so compatibility is automatically true
        }

        // Check compatibility with bottom neighbor
        if (z < patternSizeZ - 1)
        {
            (int bottomTileIndex, int bottomRotation) = grid[x, z + 1];
            Debug.Log("bottomTileIndex: " + bottomTileIndex);

            if (bottomTileIndex >= 0)
            {
                Tile bottomTile = tilePrefabs[bottomTileIndex];
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Front], bottomTile.rotatedSideSocketIds[bottomRotation][(int)Tile.TileSide.Back]])
                {

                    Debug.Log("Incompatible -  tile: " + tile.name + " w bottomTile: " + bottomTile.name);
                    Debug.Log("tile - Rotation: " + rotation + "  " + bottomTile.name + " leftRotation: " + bottomRotation);

                    return false;
                }
            }
        }
        else
        {
            if (isBorderTile)
            {
                // Check compatibility of border tile's left side socket with outOfBoundsSlotId
                if (!compatibilityMatrix[tile.rotatedSideSocketIds[rotation][(int)Tile.TileSide.Front], outOfBoundsSlotId])
                {
                    return false;
                }
            }
            // No bottom neighbor, so compatibility is automatically true
        }

        return true;
    }

    private void UpdateCompatibilityMatrix()
    {
        socketMatrixGenerator = GetComponent<TileSocketMatrixGenerator>();

        if (socketMatrixGenerator != null)
        {
            compatibilityMatrix = socketMatrixGenerator.matrix;
        }
        // if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
        // matrixLength = socketMatrixGenerator.matrix.Length;
    }
    private void OnValidate()
    {
        UpdateCompatibilityMatrix();
    }

    private void Awake()
    {
        UpdateCompatibilityMatrix();
    }

    private void FixedUpdate()
    {
        if (showTileSocketLabels != _showTileSocketLabels)
        {
            _showTileSocketLabels = showTileSocketLabels;
            for (int i = 0; i < activeTiles.Count; i++)
            {
                activeTiles[i].showSocketLabels = showTileSocketLabels;
            }
        }
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