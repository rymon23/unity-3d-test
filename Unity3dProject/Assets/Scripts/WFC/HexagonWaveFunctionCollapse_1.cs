using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;

public class HexagonWaveFunctionCollapse_1 : MonoBehaviour
{
    [SerializeField] private TileSocketMatrixGenerator socketMatrixGenerator;
    [SerializeField] private int outOfBoundsSlotId = 1;
    [SerializeField] private List<HexagonTile> activeTiles;
    [SerializeField] private int matrixLength;
    private int numTiles; // Number of tile prefabs
    public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles
    public int[,] probabilityMatrix;
    [SerializeField] private List<HexagonTile> tilePrefabs;
    public List<HexagonCell> cells;

    void Start()
    {
        UpdateCompatibilityMatrix();

        Invoke("ExecuteWFC", 0.2f);
    }


    private void ExecuteWFC()
    {
        probabilityMatrix = new int[cells.Count, tilePrefabs.Count];

        // Initialize the probability matrix with the same size as the number of cells
        InitializeTileProbabilities();

        // Initialize the first tile randomly
        HexagonCell startCell = SelectRandomStartCell();
        HexagonTile startTile = SelectRandomTile();
        AssignTileToCell(startCell, startTile, 0);
        UpdateNeighboringCellProbabilities(startCell, startTile);

        while (IsUnassignedCells())
        {
            // Select next cell with the highest probability
            HexagonCell nextCell = SelectNextCell();
            // Select next tile with the highest probability for the next cell
            (HexagonTile nextTile, int rotation) = SelectNextTile(nextCell);
            // Assign tile to the next cell
            AssignTileToCell(nextCell, nextTile, rotation);
            // Update neighboring cells probabilities
            UpdateNeighboringCellProbabilities(nextCell, nextTile);
        }

        InstantiateTiles();

        Debug.Log("Execution of WFC Complete");
    }

    public void InitializeTileProbabilities()
    {
        probabilityMatrix = new int[cells.Count, tilePrefabs.Count];

        // Iterate through all cells
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonCell currentCell = cells[i];
            currentCell.highestProbability = 0; // reset highestProbability to 0
                                                // Iterate through all tiles
            for (int j = 0; j < tilePrefabs.Count; j++)
            {
                HexagonTile currentTile = tilePrefabs[j];
                // Check the compatibility of the tile with the current cell
                if (AreTilesCompatible(currentCell, currentTile, 0))
                {
                    probabilityMatrix[currentCell.id, currentTile.id] = 1;
                }
                else
                {
                    probabilityMatrix[currentCell.id, currentTile.id] = 0;
                }
                // Update the highestProbability for the current cell
                if (probabilityMatrix[currentCell.id, currentTile.id] > currentCell.highestProbability)
                {
                    currentCell.highestProbability = probabilityMatrix[currentCell.id, currentTile.id];
                }
            }
        }
    }


    private bool IsUnassignedCells()
    {
        // Iterate through all cells and check if there is any unassigned cell
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].currentTile == null) return true;
        }
        return false;
    }

    private HexagonCell SelectRandomStartCell()
    {
        return cells[UnityEngine.Random.Range(0, cells.Count)];
    }

    private HexagonTile SelectRandomTile()
    {
        return tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)];
    }

    private void AssignTileToCell(HexagonCell cell, HexagonTile tile, int rotation)
    {
        cell.currentTile = tile;
        cell.currentRotation = rotation;
    }

    private HexagonSides GetSharedSide(HexagonCell cellA, HexagonCell cellB)
    {
        for (int i = 0; i < cellA._neighbors.Count; i++)
        {
            if (cellA._neighbors[i] == cellB)
            {
                return (HexagonSides)i;
            }
        }
        return HexagonSides.Invalid;
    }



    HexagonCell SelectNextCell()
    {
        HexagonCell nextCell = null;
        float highestProb = 0f;
        // Iterate through the cells
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonCell currentCell = cells[i];
            // If the current cell is unassigned
            if (currentCell.currentTile == null)
            {
                Debug.Log("currentCell.highestProbability: " + currentCell.highestProbability);

                // Check if the current cell's highest probability is greater than the previous highest
                if (currentCell.highestProbability > highestProb)
                {
                    nextCell = currentCell;
                    highestProb = currentCell.highestProbability;
                }
            }
        }
        return nextCell;
    }

    private (HexagonTile, int) SelectNextTile(HexagonCell cell)
    {
        // Create a list of compatible tiles and their rotations
        List<(HexagonTile, int)> compatibleTiles = new List<(HexagonTile, int)>();
        // Iterate through all tiles
        for (int i = 0; i < tilePrefabs.Count; i++)
        {
            HexagonTile currentTile = tilePrefabs[i];
            // Check the compatibility of the tile with the current cell for each rotation
            for (int j = 0; j < 6; j++)
            {
                if (AreTilesCompatible(cell, currentTile, j))
                {
                    compatibleTiles.Add((currentTile, j));
                }
            }
        }
        // If there are no compatible tiles, return null
        if (compatibleTiles.Count == 0) return (null, 0);
        // Select a random compatible tile and rotation
        int randomIndex = UnityEngine.Random.Range(0, compatibleTiles.Count);
        return compatibleTiles[randomIndex];
    }
    // private (HexagonTile, int) SelectNextTile(HexagonCell cell)
    // {
    //     // Iterate through all tiles
    //     for (int i = 0; i < tilePrefabs.Count; i++)
    //     {
    //         HexagonTile currentTile = tilePrefabs[i];
    //         // Check the compatibility of the tile with the current cell
    //         for (int j = 0; j < 6; j++)
    //         {
    //             if (AreTilesCompatible(cell, currentTile, j))
    //             {
    //                 return (currentTile, j);
    //             }
    //         }
    //     }
    //     // If no compatible tile is found, return null
    //     return (null, 0);
    // }

    private void AssignTileToCell(HexagonCell cell)
    {
        cell.currentTile = tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)];
        cell.currentTile.transform.position = cell.transform.position;
    }

    private bool AreTilesCompatible(HexagonCell current, HexagonTile tile, int rotation)
    {
        for (int i = 0; i < 6; i++)
        {
            HexagonCell neighbor = current._neighbors[i];
            if (neighbor != null && neighbor.currentTile != null)
            {
                int tileSide = tile.sideSocketIds[(i + rotation) % 6];
                int neighborSide = neighbor.currentTile.sideSocketIds[(i + 3) % 6];

                if (!compatibilityMatrix[tileSide, neighborSide])
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void UpdateNeighboringCellProbabilities(HexagonCell currentCell, HexagonTile currentTile)
    {
        // Iterate through the neighboring cells
        for (int i = 0; i < currentCell._neighbors.Count; i++)
        {
            HexagonCell neighbor = currentCell._neighbors[i];
            // Skip the current cell
            if (neighbor.currentTile != null)
            {
                // check for compatibility
                for (int j = 0; j < 6; j++)
                {
                    if (!AreTilesCompatible(neighbor, currentTile, j))
                    {
                        // Mark the current tile as incompatible with the neighbor
                        probabilityMatrix[neighbor.id, currentTile.id] = 0;
                    }
                    else
                    {
                        // Update the highestProbability for the current cell
                        if (probabilityMatrix[neighbor.id, currentTile.id] > neighbor.highestProbability)
                        {
                            neighbor.highestProbability = probabilityMatrix[neighbor.id, currentTile.id];
                        }
                    }
                }
            }
        }
    }

    private void UpdateTileProbabilities(HexagonCell currentCell)
    {
        // Initialize a sum of probabilities for normalization
        int probabilitySum = 0;

        // Iterate through all tile prefabs
        for (int i = 0; i < tilePrefabs.Count; i++)
        {
            // Get the current tile
            HexagonTile currentTile = tilePrefabs[i];
            // Initialize a probability product for the current tile
            int probabilityProduct = 1;

            // Iterate through the neighboring cells
            for (int j = 0; j < currentCell._neighbors.Count; j++)
            {
                HexagonCell neighbor = currentCell._neighbors[j];
                if (neighbor.currentTile != null)
                {
                    // check for compatibility by comparing sideSocketIds
                    int currentTileSideSocketId = currentTile.GetSideSocketId((HexagonSides)j);
                    int neighborTileSideSocketId = neighbor.currentTile.GetSideSocketId((HexagonSides)j);
                    if (currentTileSideSocketId == neighborTileSideSocketId)
                    {
                        probabilityProduct *= 1;
                    }
                    else
                    {
                        probabilityProduct *= 0;
                    }
                }
                else
                {
                    probabilityProduct *= 1;
                }
            }
            // Update the probability for the current tile
            probabilityMatrix[currentCell.id, currentTile.id] = probabilityProduct;
            // Add the current probability to the probability sum
            probabilitySum += probabilityProduct;
        }
        // Normalize the probabilities
        NormalizeProbabilities(currentCell, probabilitySum);
    }

    public void NormalizeProbabilities(HexagonCell currentCell, int probabilitySum)
    {
        if (probabilitySum == 0)
        {
            Debug.LogError("Probability sum is zero, cannot normalize");
            return;
        }
        for (int i = 0; i < numTiles; i++)
        {
            int currentProbability = probabilityMatrix[currentCell.id, i];
            probabilityMatrix[currentCell.id, i] = currentProbability / probabilitySum;
        }
    }
    // private void NormalizeProbabilities(HexagonCell currentCell, int probabilitySum)
    // {
    //     // Iterate through all tiles
    //     for (int i = 0; i < tilePrefabs.Count; i++)
    //     {
    //         // Normalize the probability
    //         probabilityMatrix[currentCell.id, i] /= probabilitySum;
    //     }
    // }

    private void NormalizeProbabilities()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            // Find the sum of all tile probabilities for the current cell
            int sum = 0;
            for (int j = 0; j < tilePrefabs.Count; j++)
            {
                sum += probabilityMatrix[i, j];
            }
            // Divide each probability by the sum to get the normalized probability
            for (int j = 0; j < tilePrefabs.Count; j++)
            {
                probabilityMatrix[i, j] /= sum;
            }
        }
    }

    public int[,] CreateTileProbabilityMatrix(List<HexagonCell> cells, List<HexagonTile> tilePrefabs)
    {
        int numTiles = tilePrefabs.Count;
        int[,] newProbabilityMatrix = new int[cells.Count, numTiles];

        // Iterate through each cell in the grid
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonCell currentCell = cells[i];

            // Iterate through each tile prefab
            for (int j = 0; j < numTiles; j++)
            {
                HexagonTile currentTile = tilePrefabs[j];
                int probability = 1;

                // Assign the probability for the current tile to the current cell
                newProbabilityMatrix[i, j] = probability;
            }
        }

        return newProbabilityMatrix;
    }

    private void Reset()
    {
        // Reset all cells
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonCell cell = cells[i];
            cell.currentTile = null;
        }

        // Reset all tile probabilities
        InitializeTileProbabilities();
    }

    // Instantiate the tiles in the appropriate positions to create the final pattern
    void InstantiateTiles()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonTile prefab = cells[i].currentTile;
            int rotation = cells[i].currentRotation;

            Vector3 position = cells[i].transform.position;
            position.y += 0.2f;
            // Quaternion rotationQuat = Quaternion.Euler(0, 90 * rotation, 0);
            // cell.currentTile.transform.position = cell.transform.position;

            HexagonTile activeTile = Instantiate(prefab, position, Quaternion.identity);
            activeTiles.Add(activeTile);
        }
    }

    private void UpdateCompatibilityMatrix()
    {
        socketMatrixGenerator = GetComponent<TileSocketMatrixGenerator>();

        compatibilityMatrix = null;
        compatibilityMatrix = socketMatrixGenerator.matrix;
        if (compatibilityMatrix.Length == 0)
        {
            Debug.LogError("compatibilityMatrix is unset");
            return;
        }
    }

    // HexagonTile SelectNextTile(HexagonCell cell)
    // {
    //     HexagonTile nextTile = null;
    //     float highestProb = 0f;

    //     // Iterate through the tilePrefabs
    //     for (int i = 0; i < tilePrefabs.Count; i++)
    //     {
    //         HexagonTile currentTile = tilePrefabs[i];
    //         // Check the probability of the current tile in the probability matrix
    //         if (probabilityMatrix[cell.id, i] > highestProb)
    //         {
    //             nextTile = currentTile;
    //             highestProb = probabilityMatrix[cell.id, i];
    //         }
    //     }

    //     return nextTile;
    // }


    // private void OnValidate()
    // {
    //     UpdateCompatibilityMatrix();

    //     // if (compatibilityMatrix == null) compatibilityMatrix = socketMatrixGenerator.matrix;
    //     // if (compatibilityMatrix.Length == 0)
    //     // {
    //     //     Debug.LogError("compatibilityMatrix is unset");
    //     //     return;
    //     // }

    //     if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
    //     matrixLength = socketMatrixGenerator.matrix.Length;
    // }

    private void Awake()
    {
        UpdateCompatibilityMatrix();
    }
}