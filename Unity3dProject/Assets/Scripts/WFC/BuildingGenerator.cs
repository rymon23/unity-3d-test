using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

[System.Serializable]
public class BuildingGenerator : MonoBehaviour
{
    public enum BuildingRoofType { Flat, Dome, Pointed, Tent }
    public enum BuildingStackType { Level, Focused, Dome, Castle }
    public enum StructureType { Building, Wall }
    [SerializeField] private BuildingPrototypeDisplaySettings buildingPrototypeDisplaySettings = new BuildingPrototypeDisplaySettings();
    [Header(" ")]

    [SerializeField]
    private HexGridDisplaySettings hexGridDisplaySettings = new HexGridDisplaySettings(
            CellDisplay_Type.DrawLines,
            GridFilter_Level.All,
            GridFilter_Type.All,
            HexCellSizes.Default,
            true
            );

    [Header(" ")]
    [SerializeField] private bool resetPrototypes;
    [Header(" ")]
    [SerializeField] private bool generate_tiles;
    [SerializeField] private bool useCompatibilityCheck;
    [SerializeField] private bool instantiateOnGenerate;

    [Header(" ")]
    // [SerializeField] 
    private bool generate_MeshTiles;
    // [SerializeField] 
    private bool generate_mesh;

    [Header(" ")]
    [SerializeField] private bool showSurfaceBlockEdgeSockets = true;
    [SerializeField] private bool highlight_SurfaceBlockEdgeSide;
    [SerializeField] private HexagonTileSide highlightSide;

    [Header(" ")]
    [SerializeField] private bool showHighlightedCell;
    [Range(0, 468)][SerializeField] private int _highlightedCell;
    private HexagonCellPrototype _currentHighlightedCell = null;

    [Header("Node Grid Settings")]
    [Range(4, 108)][SerializeField] private int nodeGrid_CellSize = 12;
    [Range(12, 108)][SerializeField] private int nodeGrid_GridRadius = 36;
    [Header(" ")]
    [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMin = 1;
    [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMax = 2;
    [Header(" ")]
    [Range(2, 12)][SerializeField] private int nodeGrid_CellLayerOffset = 4;
    [Header(" ")]
    [Range(1, 19)][SerializeField] private int nodeGrid_MaxCellsPerLayer = 7;
    [SerializeField] private HexCellSizes nodeGrid_SnapSize = HexCellSizes.X_4;
    [Header(" ")]
    [SerializeField] private Option_CellGridType nodeGrid_GridType = Option_CellGridType.Defualt;
    [Header(" ")]

    [Header("Tile Grid Settings")]
    [SerializeField] private HexCellSizes tileGrid_CellSize = HexCellSizes.X_4;
    [Range(1, 48)][SerializeField] private int tileGrid_CellLayers = 1;
    [Range(2, 12)][SerializeField] private int tileGrid_CellLayerOffset = 4;
    [Range(12, 108)][SerializeField] private int tileGrid_GridRadius = 36;

    [Header("Building Settings")]
    [Range(0.2f, 0.9f)][SerializeField] private float innerRoomRadiusMult = 0.8f;
    [Header(" ")]
    [Range(1, 5)][SerializeField] private int entrancesMax = 2;
    [Range(1f, 4f)][SerializeField] private float doorwayRadius = 2;
    [Range(1f, 4f)][SerializeField] private float innerEntryRadius = 3;
    public Vector3 extDoor_dimensions = new Vector3(0.6f, 1.4f, 1.3f);
    public Vector3 intDoor_dimensions = new Vector3(0.6f, 1.4f, 1.8f);

    [Header("Surface Block Settings")]
    [Range(0.2f, 10f)][SerializeField] private float blockSize = 1f;
    [Range(12, 128)][SerializeField] private float boundsSize = 25;
    [Header(" ")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private TileDirectory tileDirectory;
    public Transform folder_Main { get; private set; } = null;
    public Transform folder_MeshObject { get; private set; } = null;
    public Transform folder_GeneratedTiles { get; private set; } = null;

    public HexGrid hexNodeGrid = null;
    public HexGrid hexTileGrid = null;
    Dictionary<Vector2, HexagonCellPrototype> stairwayCells = new Dictionary<Vector2, HexagonCellPrototype>();
    List<HexagonTileTemplate> generatedTiles = null;
    Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell = null;
    Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null;
    Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;
    Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles = null;

    [SerializeField] Vector3 defaultBuilding_dimensions = new Vector3(10f, 12f, 6f);
    [Header(" ")]
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMin = 1;
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMax = 10;
    [Range(0.5f, 50f)][SerializeField] float defaultBuilding_size = 2f;
    List<RectangleBounds> buildingBoundsShells = null;
    BuildingPrototype buildingPrototype = null;

    #region Saved State
    Vector3 gridStartPos;
    Vector3 _lastPosition;
    float _boundsSize;
    float _blockSize;
    float _updateDist = 1f;
    #endregion

    private void OnValidate()
    {
        if (
            resetPrototypes
            || _lastPosition != transform.position
            || _boundsSize != boundsSize
            || _blockSize != blockSize
            )
        {
            resetPrototypes = false;

            _lastPosition = transform.position;

            boundsSize = UtilityHelpers.RoundToNearestStep(boundsSize, 2f);
            _boundsSize = boundsSize;

            blockSize = UtilityHelpers.RoundToNearestStep(blockSize, 0.2f);
            _blockSize = blockSize;

            boundsShapesByCellLookup = null;

            gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)nodeGrid_SnapSize);
            gridStartPos.y = HexCoreUtil.Calculate_CellSnapElevation(nodeGrid_CellLayerOffset, transform.position.y);

            if (nodeGrid_CellLayersMin > nodeGrid_CellLayersMax) nodeGrid_CellLayersMax = nodeGrid_CellLayersMin;

            EvaluateGridLayers();

            Vector2Int _cellLayersMinMax = new Vector2Int(nodeGrid_CellLayersMin, nodeGrid_CellLayersMax);

            buildingPrototype = new BuildingPrototype(
                gridStartPos,
                (HexCellSizes)nodeGrid_CellSize,
                _blockSize,
                _cellLayersMinMax,
                nodeGrid_CellLayerOffset,
                nodeGrid_MaxCellsPerLayer,
                nodeGrid_GridRadius,
                transform,
                nodeGrid_GridType
            );
        }


        if (generate_MeshTiles || generate_tiles)
        {
            generate_MeshTiles = false;

            Evalaute_Folder();

            generate_tiles = false;

            if (buildingPrototype == null)
            {
                Debug.LogError("buildingPrototype == null");
            }
            else
            {
                List<HexagonTileTemplate> generatedTiles = buildingPrototype.Generate_Tiles(
                    prefab,
                    folder_GeneratedTiles,
                    useCompatibilityCheck,
                    tileDirectory.GetSocketDirectory(),
                    null,
                    false,
                    true
                );
            }

            //     if (surfaceBlocksGrid != null)
            //     {
            //         // List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
            //         //         SurfaceBlockState.Entry,
            //         //         // SurfaceBlockState.Corner,
            //         //     };

            //         // Debug.Log("Distance From World Center: " + Vector3.Distance(transform.position, Vector3.zero));
            //         Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell = SurfaceBlock.Generate_MeshObjectsByCell(
            //             surfaceBlocksByCell,
            //             prefab,
            //             transform,
            //             null,
            //             false,
            //             folder_GeneratedTiles,
            //             true
            //         );

            //         if (generate_tiles)
            //         {
            //             if (useCompatibilityCheck)
            //             {
            //                 generatedTiles = HexagonTileTemplate.Generate_Tiles_With_WFC_DryRun(gameObjectsByCell, tileDirectory.GetSocketDirectory(), false);
            //             }
            //             else generatedTiles = HexagonTileTemplate.Generate_Tiles(gameObjectsByCell, folder_Main, instantiateOnGenerate);
            //         }
            //     }
            //     generate_tiles = false;
            // }
            // if (generate_mesh)
            // {
            //     generate_mesh = false;

            //     Evalaute_Folder();

            //     // GameObject gameObject = SurfaceBlock.Generate_MeshObject(surfaceBlocksGrid, prefab, transform, null, folder_Main);
            //     List<GameObject> gameObjects = SurfaceBlock.Generate_MeshObjects(surfaceBlocksGrid, prefab, transform, null, folder_Main);
        }
    }

    public void ResetPrototypes()
    {
        resetPrototypes = true;
        OnValidate();
    }

    Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
    private void OnDrawGizmos()
    {
        if (_lastPosition != transform.position)
        {
            if (Vector3.Distance(_lastPosition, transform.position) > _updateDist) ResetPrototypes();
        }

        if (buildingPrototype != null)
        {
            buildingPrototype.Draw(buildingPrototypeDisplaySettings, hexGridDisplaySettings);
        }

        if (buildingPrototypeDisplaySettings.show_buildingBoundsShells && buildingBoundsShells != null)
        {
            foreach (var item in buildingBoundsShells)
            {
                Gizmos.color = Color.white;
                item.Draw();
            }
        }
    }

    public void EvaluateGridLayers()
    {
        int nodeGridHeight = (nodeGrid_CellLayersMax * nodeGrid_CellLayerOffset);
        int tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);

        if (tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
        {
            Debug.Log("Recalculating tile grids... ");
            // Debug.LogError("(tileGridHeight < nodeGridHeight) - nodeGridHeight: " + nodeGridHeight + ", tileGridHeight: " + tileGridHeight);
            int attempts = 99;

            while (attempts > 0 && tileGridHeight < nodeGridHeight)
            {
                tileGrid_CellLayers++;
                tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);
                attempts--;
            }

            while (attempts > 0 && (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
            {
                tileGrid_CellLayers--;
                tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);
                attempts--;
            }

            if (tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
            {
                Debug.LogError("(tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight) - nodeGridHeight: " + nodeGridHeight + ", tileGridHeight: " + tileGridHeight);
            }
        }

        if (nodeGrid_CellLayerOffset < tileGrid_CellLayerOffset) tileGrid_CellLayerOffset = nodeGrid_CellLayerOffset;
    }

    public void Evalaute_Folder()
    {
        if (folder_MeshObject == null)
        {
            folder_MeshObject = new GameObject("Template Folder" + this.gameObject.name).transform;
            folder_MeshObject.transform.SetParent(this.transform);
        }

        if (folder_GeneratedTiles == null)
        {
            folder_GeneratedTiles = new GameObject("Generated Tiles" + this.gameObject.name).transform;
            folder_GeneratedTiles.transform.SetParent(this.transform);
        }

        if (folder_Main == null)
        {
            folder_Main = new GameObject("WFC Tile" + this.gameObject.name).transform;
            folder_Main.transform.SetParent(this.transform);
        }
    }

    public static (Vector3[], Vector3[]) GenerateArchPoints(float width, float height, float depth, int resolution)
    {
        // Create an empty array to store the points
        Vector3[] points = new Vector3[resolution + 1];

        // Create an empty array to store the bottom 2 points of the arch
        Vector3[] bottomPoints = new Vector3[2];

        // Generate the points of the arch
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float x = width * t;
            float y = height * Mathf.Sin(Mathf.PI * t);
            float z = depth * Mathf.Cos(Mathf.PI * t);
            points[i] = new Vector3(x, y, z);

            // Store the bottom 2 points of the arch
            if (i == 0 || i == resolution)
            {
                if (i == 0)
                {
                    bottomPoints[0] = new Vector3(x, 0, z);
                }
                else
                {
                    bottomPoints[1] = new Vector3(x, 0, z);
                }

            }
        }

        return (points, bottomPoints);
    }

    public static Vector3[] GenerateDoorwayPoints(Vector3[] archBottom, float width, float height)
    {
        Vector3[] doorwayPoints = new Vector3[4];
        Vector3 topLeft = archBottom[0];
        Vector3 topRight = archBottom[1];
        Vector3 bottomLeft = new Vector3(topLeft.x, topLeft.y - height, topLeft.z);
        Vector3 bottomRight = new Vector3(topRight.x, topRight.y - height, topRight.z);
        // Vector3 bottomRight = archBottom[0];
        // Vector3 bottomRight = archBottom[1];

        doorwayPoints[0] = topRight;
        doorwayPoints[1] = topLeft;
        doorwayPoints[2] = bottomLeft;
        doorwayPoints[3] = bottomRight;

        return doorwayPoints;
    }

    public static (Vector3[], Vector3[], Vector3[]) GenerateArchAndDoorwayPoints(float width, float height, float depth, int resolution, Transform transform)
    {
        // Generate the points of the arch and doorway
        (Vector3[] archPoints, Vector3[] archBottom) = GenerateArchPoints(width, height, depth, resolution);
        Vector3[] doorwayPoints = GenerateDoorwayPoints(archBottom, width, height);

        // Determine the rotation that aligns the shape's front with the game object's forward direction
        Quaternion rotation = Quaternion.LookRotation(transform.forward) * Quaternion.AngleAxis(90, Vector3.up);


        Vector3 difference = transform.position - archPoints[archPoints.Length - 1];

        // Apply the rotation to each point in the arrays
        for (int i = 0; i < archPoints.Length; i++)
        {
            archPoints[i] = rotation * archPoints[i];
            archPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        difference = transform.position - doorwayPoints[doorwayPoints.Length - 1];
        for (int i = 0; i < doorwayPoints.Length; i++)
        {
            doorwayPoints[i] = rotation * doorwayPoints[i];
            doorwayPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        // Merge the archPoints and doorwayPoints array
        Vector3[] points = new Vector3[archPoints.Length + doorwayPoints.Length];
        archPoints.CopyTo(points, 0);
        doorwayPoints.CopyTo(points, archPoints.Length);

        return (points, archPoints, doorwayPoints);
    }


    public static List<Vector3> GenerateVerticalGrid(Vector3 origin, float gridSize, int numRows, int numColumns)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        for (int row = 0; row < numRows; row++)
        {
            for (int column = 0; column < numColumns; column++)
            {
                float x = origin.x + column * gridSize;
                float y = origin.y;
                float z = origin.z + row * gridSize;

                Vector3 point = new Vector3(x, y, z);
                gridPoints.Add(point);
            }
        }
        return gridPoints;
    }
}