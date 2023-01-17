using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VerticalWFC : MonoBehaviour
{
    [SerializeField] private TileSocketMatrixGenerator socketMatrixGenerator;
    [SerializeField] private TileDirectory tileDirectory;
    [SerializeField] private VerticalCellManager cellManager;
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
        EvaluateTiles();

        UpdateCompatibilityMatrix();

        Invoke("ExecuteWFC", 0.2f);
    }

    private void ExecuteWFC()
    {
        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();
        if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
        matrixLength = socketMatrixGenerator.matrix.Length;

        EvaluateCells();

        InitialTileAssignment();

        VerticalCell previousCell = cells[0];

        // Skip the first index
        for (int i = 1; i < cells.Count; i++)
        {
            CollapseCell(cells[i], previousCell);
            previousCell = cells[i];
        }

        InstantiateTiles();

        Debug.Log("Execution of WFC Complete");
    }

    private void CollapseCell(VerticalCell cell, VerticalCell previousCell)
    {
        (VerticalTile nextTile, List<int> rotations) = SelectNextTile(cell, previousCell);

        // Assign tile to the next cell
        AssignTileToCell(cell, nextTile, rotations);
    }

    private void InitialTileAssignment()
    {
        // Set Base Level
        VerticalCell startCell = cells[0];
        VerticalTile startTile = tilePrefabs_Bases[UnityEngine.Random.Range(0, tilePrefabs_Bases.Count)];
        AssignTileToCell(startCell, startTile, 0);
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

    private (VerticalTile, List<int>) SelectNextTile(VerticalCell cell, VerticalCell previousCell)
    {
        // Create a list of compatible tiles and their rotations
        List<(VerticalTile, List<int>)> compatibleTilesAndRotations = new List<(VerticalTile, List<int>)>();

        int[] lastTileTopEdgeSockets = previousCell.currentTile.GetTopEdgeSockets();

        // Iterate through all tiles
        for (int i = 0; i < tilePrefabs.Count; i++)
        {
            VerticalTile currentTile = tilePrefabs[i];

            List<int> compatibleTileRotations = GetCompatibleTileRotations(cell, currentTile, lastTileTopEdgeSockets);

            if (compatibleTileRotations.Count > 0)
            {
                compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));
            }
        }
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

    private List<int> GetCompatibleTileRotations(VerticalCell currentCell, VerticalTile currentTile, int[] lastTileTopEdgeSockets)
    {
        List<int> compatibleRotations = new List<int>();

        // Check every rotation
        for (int rotation = 0; rotation < 6; rotation++)
        {
            bool compatibile = true;
            // Check that all neighborTileSockets are compatibile
            for (int edgeIndex = 0; edgeIndex < lastTileTopEdgeSockets.Length; edgeIndex++)
            {
                int rotatedSideSocket = currentTile.GetRotatedEdgeSocketId((VerticalEdges)edgeIndex + 4, rotation);
                if (lastTileTopEdgeSockets[edgeIndex] != -1 && !compatibilityMatrix[rotatedSideSocket, lastTileTopEdgeSockets[edgeIndex]])
                {
                    compatibile = false;
                    break;
                }
            }

            if (compatibile) compatibleRotations.Add(rotation);
        }
        // Debug.Log("Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
        return compatibleRotations;
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
            if (i != 0)
            {
                position = cells[i - 1]._centerPoints[1];
            }
            VerticalTile activeTile = Instantiate(prefab, position, Quaternion.identity);
            // activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
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

    private void EvaluateCells()
    {
        cellManager = GetComponent<VerticalCellManager>();
        cellManager.EvaluateCells();

        cells = new List<VerticalCell>();
        cells = cellManager.cells;

        if (cells == null) Debug.LogError("Cells List is unset");
        if (cells.Count == 0) Debug.LogError("Cells List is empty ");
    }

    private void EvaluateTiles()
    {
        tileLookupByid = tileDirectory.CreateVerticalTileDictionary();
        List<VerticalTile> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

        tilePrefabs_Bases = _tilePrefabs.FindAll(x => x.IsBaseFloor()).ToList();

        tilePrefabs = new List<VerticalTile>();
        tilePrefabs.AddRange(_tilePrefabs.Except(tilePrefabs_Bases));

        foreach (VerticalTile prefab in tilePrefabs)
        {
            int id = prefab.GetID();
        }

        ShuffleTiles(tilePrefabs);
        ShuffleTiles(tilePrefabs_Bases);
    }


    private void UpdateCompatibilityMatrix()
    {
        socketMatrixGenerator = GetComponent<TileSocketMatrixGenerator>();
        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();

        if (compatibilityMatrix.Length == 0)
        {
            Debug.LogError("compatibilityMatrix is unset");
            return;
        }
    }
    private void Awake()
    {
        cellManager = GetComponent<VerticalCellManager>();

        UpdateCompatibilityMatrix();
    }
}