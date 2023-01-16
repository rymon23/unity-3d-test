using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

public class VerticalWFC : MonoBehaviour
{
    [SerializeField] private TileSocketMatrixGenerator socketMatrixGenerator;
    [SerializeField] private TileDirectory tileDirectory;
    [SerializeField] private int outOfBoundsSlotId = 1; // Edge socket
    [SerializeField] private List<VerticalTile> activeTiles;
    [SerializeField] private int matrixLength;
    private int numTiles; // Number of tile prefabs
    public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles
    [SerializeField] private List<VerticalTile> tilePrefabs;
    [SerializeField] private List<VerticalTile> tilePrefabs_Peaks;
    [SerializeField] private List<VerticalTile> tilePrefabs_Bases;
    Dictionary<int, VerticalTile> tileLookupByid;
    public List<VerticalCell> cells;
    float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };

    void Start()
    {
        // EvaluateTiles();
        UpdateCompatibilityMatrix();

        Invoke("ExecuteWFC", 0.2f);
    }


    private void ExecuteWFC()
    {
        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();
        if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
        matrixLength = socketMatrixGenerator.matrix.Length;

        // EvaluateCells();

        // Initialize the first tile randomly
        VerticalCell baseCell = SelectRandomStartCell();
        VerticalTile baseTile = SelectRandomTile();

        AssignTileToCell(baseCell, baseTile, 0);

        while (IsUnassignedCells())
        {
            // Select next cell with the highest probability
            VerticalCell nextCell = SelectNextCell();

            // Select next tile with the highest probability for the next cell
            (VerticalTile nextTile, List<int> rotations) = SelectNextTile(nextCell);

            if (nextTile == null) return;

            // Assign tile to the next cell
            AssignTileToCell(nextCell, nextTile, rotations);
        }

        InstantiateTiles();

        Debug.Log("Execution of WFC Complete");
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

    private VerticalCell SelectRandomStartCell()
    {
        return cells[UnityEngine.Random.Range(0, cells.Count)];
    }

    private VerticalTile SelectRandomTile()
    {
        return tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)];
    }

    private void AssignTileToCell(VerticalCell cell, VerticalTile tile, int rotation = 0)
    {
        cell.currentTile = tile;
        cell.currentRotation = rotation;
    }
    private void AssignTileToCell(VerticalCell cell, VerticalTile tile, List<int> rotations)
    {
        cell.currentTile = tile;
        cell.currentRotation = rotations[UnityEngine.Random.Range(0, rotations.Count)];
    }
    VerticalCell SelectNextCell()
    {
        VerticalCell nextCell = null;
        float highestProb = 0f;
        // Iterate through the cells
        for (int i = 0; i < cells.Count; i++)
        {
            VerticalCell currentCell = cells[i];
            // If the current cell is unassigned
            if (currentCell.currentTile == null)
            {
                currentCell.highlight = true;
                // Debug.Log("currentCell.highestProbability: " + currentCell.highestProbability);

                // // Check if the current cell's highest probability is greater than the previous highest
                // if (currentCell.highestProbability > highestProb)
                // {
                nextCell = currentCell;
                // highestProb = currentCell.highestProbability;
                // }
            }
        }
        return nextCell;
    }

    private (VerticalTile, List<int>) SelectNextTile(VerticalCell cell)
    {
        // Create a list of compatible tiles and their rotations
        List<(VerticalTile, List<int>)> compatibleTilesAndRotations = new List<(VerticalTile, List<int>)>();

        // // Iterate through all tiles
        // for (int i = 0; i < tilePrefabs.Count; i++)
        // {
        //     VerticalTile currentTile = tilePrefabs[i];

        //     List<int> compatibleTileRotations = GetCompatibleTileRotations(cell, currentTile);

        //     if (compatibleTileRotations.Count > 0)
        //     {
        //         compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));
        //     }
        // }

        // If there are no compatible tiles, return null
        if (compatibleTilesAndRotations.Count == 0)
        {
            Debug.Log("No compatible tiles for cell: " + cell.id);
            return (null, null);
        }

        // Select a random compatible tile and rotation
        int randomIndex = UnityEngine.Random.Range(0, compatibleTilesAndRotations.Count);
        return compatibleTilesAndRotations[randomIndex];
    }

    // private List<int> GetCompatibleTileRotations(VerticalCell currentCell, VerticalTile currentTile)
    // {
    //     int[] neighborTileSockets = currentCell.GetNeighborTileSockets();

    //     for (int i = 0; i < neighborTileSockets.Length; i++)
    //     {
    //         Debug.Log("Cell: " + currentCell.id + ", neighborTileSocket: side: " + (HexagonSides)i + ", socket: " + neighborTileSockets[i]);
    //     }
    //     List<int> compatibleRotations = new List<int>();

    //     // Check every rotation
    //     for (int rotation = 0; rotation < 6; rotation++)
    //     {
    //         bool compatibile = true;
    //         // Check that all neighborTileSockets are compatibile
    //         for (int nIX = 0; nIX < neighborTileSockets.Length; nIX++)
    //         {
    //             int rotatedSideSocket = currentTile.GetRotatedSideSocketId((HexagonSides)nIX, rotation);
    //             if (neighborTileSockets[nIX] != -1 && !compatibilityMatrix[rotatedSideSocket, neighborTileSockets[nIX]])
    //             {
    //                 compatibile = false;
    //                 break;
    //             }
    //         }

    //         if (compatibile) compatibleRotations.Add(rotation);
    //     }
    //     Debug.Log("Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
    //     return compatibleRotations;
    // }

    private bool AreTilesCompatible(VerticalCell current, VerticalTile tile, int rotation)
    {
        Debug.Log("AreTilesCompatible - tile: " + tile.id);
        if (tile == null) return false;





        // // Check neighbors for compatibility
        // for (int neighborSideIndex = 0; neighborSideIndex < 6; neighborSideIndex++)
        // {
        //     // Debug.Log("current.neighborsBySide.Count: " + current.neighborsBySide.Count + ", current.neighborsBySide[]: " + current.neighborsBySide[neighborSideIndex]);
        //     VerticalCell neighbor = current.neighborsBySide[neighborSideIndex];
        //     int tileSideSocket = tile.edgeSockets[(neighborSideIndex + rotation) % 6];

        //     // Handle Edge Cells
        //     if (neighbor == null)
        //     {
        //         int rotatedSideSocket = tile.GetRotatedSideSocketId((HexagonSides)neighborSideIndex, rotation);
        //         Debug.Log("Edge Cell: " + tile.id + ", rotatedSideSocket: " + rotatedSideSocket);

        //         if (!compatibilityMatrix[rotatedSideSocket, outOfBoundsSlotId])
        //         {
        //             Debug.Log("Tile incompatible with Edge: " + tile.id + ", rotatedSideSocket: " + rotatedSideSocket + ",  outOfBoundsSlotId: " + outOfBoundsSlotId);

        //             return false;
        //         }
        //     }
        //     else
        //     {

        //         if (neighbor.currentTile != null)
        //         {
        //             int neighborRelativeSide = current.GetNeighborsRelativeSide((HexagonSides)neighborSideIndex);
        //             int neighborSideSocket = neighbor.currentTile.edgeSockets[(neighborRelativeSide + rotation) % 6];

        //             int rotatedSideSocket = tile.GetRotatedSideSocketId((HexagonSides)neighborSideIndex, rotation);


        //             // Debug.Log("compatibilityMatrix.length: " + compatibilityMatrix.GetLength(tileSide) + ", tileSide: " + tileSide + ", neighborSideSocket: " + neighborSideSocket);
        //             Debug.Log("compatibilityMatrix.length: " + compatibilityMatrix.Length + ", tileSide: " + tileSideSocket + ", neighborSideSocket: " + neighborSideSocket);

        //             // if (!compatibilityMatrix[tileSideSocket, neighborSideSocket])
        //             if (!compatibilityMatrix[rotatedSideSocket, neighborSideSocket])
        //             {
        //                 return false;
        //             }
        //         }
        //     }

        // }
        return true;
    }

    private void AssignTileToCell(VerticalCell cell)
    {
        cell.currentTile = tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)];
        cell.currentTile.transform.position = cell.transform.position;
    }

    private void Reset()
    {
        // Reset all cells
        for (int i = 0; i < cells.Count; i++)
        {
            VerticalCell cell = cells[i];
            cell.currentTile = null;
        }
    }

    // Instantiate the tiles in the appropriate positions to create the final pattern
    void InstantiateTiles()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            VerticalTile prefab = cells[i].currentTile;
            int rotation = cells[i].currentRotation;

            Vector3 position = cells[i].transform.position;
            position.y += 0.2f;

            VerticalTile activeTile = Instantiate(prefab, position, Quaternion.identity);
            activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
            activeTiles.Add(activeTile);
        }
    }

    public void ShuffleTiles(List<VerticalTile> tiles)
    {
        int n = tiles.Count;
        for (int i = 0; i < n; i++)
        {
            // Get a random index from the remaining elements
            int r = i + UnityEngine.Random.Range(0, n - i);
            // Swap the current element with the random one
            VerticalTile temp = tiles[r];
            tiles[r] = tiles[i];
            tiles[i] = temp;
        }
    }

    // private void EvaluateTiles()
    // {
    //     tileLookupByid = tileDirectory.CreateTileDictionary();
    //     tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

    //     foreach (VerticalTile prefab in tilePrefabs)
    //     {
    //         int id = prefab.id;
    //     }

    //     ShuffleTiles(tilePrefabs);

    //     tilePrefabs_edgable = tilePrefabs.FindAll(x => x.isEdgeable).ToList();

    // }
    // private void EvaluateCells()
    // {
    //     // Place edgeCells first
    //     List<VerticalCell> edgeCells = VerticalCell.GetEdgeCells(cells);
    //     List<VerticalCell> _processedCells = new List<VerticalCell>();

    //     _processedCells.AddRange(edgeCells);
    //     _processedCells.AddRange(cells.Except(edgeCells));

    //     cells = _processedCells;
    // }

    private void UpdateCompatibilityMatrix()
    {
        socketMatrixGenerator = GetComponent<TileSocketMatrixGenerator>();

        // compatibilityMatrix = null;
        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();

        if (compatibilityMatrix.Length == 0)
        {
            Debug.LogError("compatibilityMatrix is unset");
            return;
        }
    }

    private void Awake()
    {
        UpdateCompatibilityMatrix();
    }
}