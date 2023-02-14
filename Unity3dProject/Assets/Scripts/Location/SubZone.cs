using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

namespace ProceduralBase
{
    public class SubZone : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private HeightmapConverterUtil heightmapConverterUtil;
        [SerializeField] private Location _locationParent;
        [Range(24, 256)] public int radius = 102;
        [SerializeField] private int hexagonSize = 12;
        [SerializeField] private List<int> neighbors;
        [SerializeField] private Vector3[] borderCorners;
        [SerializeField] private Vector3[] zoneConnectors;

        public void MapFromPrototype(SubzonePrototype subzonePrototype, Location locationParent)
        {
            _locationParent = locationParent;
            // radius = subzonePrototype.radius;
            borderCorners = subzonePrototype.borderCorners;
        }

        [Header("Debug Settings")]
        [SerializeField] private bool showBounds;
        [SerializeField] private bool showTileGrid;
        [SerializeField] private bool showEdgeCells;
        [SerializeField] private bool showEntranceCells;
        [SerializeField] private bool showPathCells;
        [SerializeField] private bool showLevelRampCells;
        [SerializeField] private bool showLeveledCellList;
        [SerializeField] private bool showLeveledCellEdgeList;
        [SerializeField] private bool resetLevelRampCells;
        [SerializeField] private bool testCellPath;
        [SerializeField] private bool showCellClusters;
        [Range(-1, 128)][SerializeField] private int highlightCellCluster = 0;

        [Range(3, 12)][SerializeField] private int cellLayerElevation = 6;
        [Range(1, 4)][SerializeField] private int cellLayers = 2;

        [SerializeField] private bool resetHexagonCellPrototypes;

        [SerializeField] private bool generateHexagonCells;
        [SerializeField] private bool generateHexagonTileCellClusters;
        [SerializeField] private bool generateLeveledCellList;
        [SerializeField] private bool generateModelingPlatforms;
        [SerializeField] private bool generateGridMesh;

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonTilePrototype> hexagonCellPrototypes;
        [SerializeField] private List<HexagonCell> hexagonCells;

        [SerializeField] private List<HexagonCell> edgeCells;
        [SerializeField] private List<HexagonCell> entryCells;
        [SerializeField] private List<HexagonCell> levelRampCells;
        [SerializeField] private List<HexagonCell> cellPath;
        public Dictionary<int, List<HexagonCell>> cellPaths;
        Dictionary<int, List<HexagonCell>> elevationCellList;
        Dictionary<int, List<HexagonCell>> elevationEdgeCellList;

        Dictionary<int, List<HexagonCell>> pathsByLevel;
        Dictionary<int, List<HexagonCell>> rampsByLevel;



        [Range(0.5f, 10f)][SerializeField] private float stepSize = 3f;
        public TerrainVertex[,] verticeGrid;

        public List<HexagonCellCluster> cellClusters;

        public void Debug_ShowBounds(bool enable)
        {
            showBounds = enable;
            OnValidate();
        }
        public void Debug_ShowTiles(bool enable)
        {
            showTileGrid = enable;
            OnValidate();
        }
        public void Debug_ResetHexagonellPrototypes(bool enable)
        {
            resetHexagonCellPrototypes = enable;
            OnValidate();
        }
        public void Debug_GenerateHexagonCells(bool enable)
        {
            generateHexagonCells = enable;
            OnValidate();
        }

        [Header("Prefabs")]
        [SerializeField] private GameObject HexagonTileCell_prefab;
        [SerializeField] private GameObject HexagonTilePlatformModel_prefab;

        [Header("WFC")]
        [SerializeField] private HexagonWaveFunctionCollapse_1 waveFunctionCollapse;



        Dictionary<int, List<HexagonTilePrototype>> hexagonCellPrototypesByLayer;
        public void PreprocessCellGrid()
        {

            // Get edges
            // Get path
            // Get entry
            // feed to WFC
        }

        private void GenerateHexagonCellPrototypes()
        {
            hexagonCellPrototypes = LocationUtility.GetTilesWithinRadius(
                                        HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                transform.position,
                                                radius,
                                                hexagonSize),
                                                transform.position,
                                                radius);
            Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonTilePrototype>>();
            newPrototypesByLayer.Add(0, hexagonCellPrototypes);

            if (cellLayers > 1)
            {
                for (int i = 1; i < cellLayers; i++)
                {

                    List<HexagonTilePrototype> newLayer;
                    if (i == 1)
                    {
                        newLayer = AddLayer(hexagonCellPrototypes, cellLayerElevation, i);
                    }
                    else
                    {
                        List<HexagonTilePrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = AddLayer(previousLayer, cellLayerElevation, i);
                    }

                    newPrototypesByLayer.Add(i, newLayer);

                    Debug.Log("Added Layer: " + i + ", Count: " + newLayer.Count);

                    // hexagonCellPrototypesL1 = newLayer;
                }

            }
            hexagonCellPrototypesByLayer = newPrototypesByLayer;
        }
        public static List<HexagonTilePrototype> AddLayer(List<HexagonTilePrototype> prototypes, int layerElevation, int layer)
        {
            List<HexagonTilePrototype> newPrototypes = new List<HexagonTilePrototype>();
            foreach (var prototype in prototypes)
            {
                HexagonTilePrototype newPrototype = new HexagonTilePrototype();
                newPrototype.id = prototype.id + "L" + layer;
                newPrototype.bottomNeighborId = prototype.id;

                newPrototype.size = prototype.size;
                newPrototype.center = new Vector3(prototype.center.x, prototype.center.y + layerElevation, prototype.center.z);
                newPrototype.cornerPoints = prototype.cornerPoints.Select(c => new Vector3(c.x, c.y + layerElevation, c.z)).ToArray();
                newPrototypes.Add(newPrototype);
            }
            return newPrototypes;
        }


        private void Awake()
        {
            waveFunctionCollapse = GetComponent<HexagonWaveFunctionCollapse_1>();

            waveFunctionCollapse.SetCells(hexagonCells);
            waveFunctionCollapse.SetRadius(radius);
        }

        private void OnValidate()
        {
            if (waveFunctionCollapse == null)
            {
                waveFunctionCollapse = GetComponent<HexagonWaveFunctionCollapse_1>();
            }

            bool hasTilePrototypes = hexagonCellPrototypes != null && hexagonCellPrototypes.Count > 0;

            if (resetHexagonCellPrototypes || !hasTilePrototypes)
            {
                resetHexagonCellPrototypes = false;
                GenerateHexagonCellPrototypes();
            }
            else
            {

                if (generateHexagonCells && hasTilePrototypes)
                {
                    generateHexagonCells = false;

                    if (hexagonCellPrototypesByLayer != null && hexagonCellPrototypesByLayer.Count > 0)
                    {
                        GenerateHexagonCellObjects(hexagonCellPrototypesByLayer);
                        HexagonCell.PopulateNeighborsFromCornerPoints(hexagonCells, 0.33f * (hexagonSize / 12f));
                    }
                    else
                    {
                        GenerateHexagonCellObjects(hexagonCellPrototypes);
                        HexagonCell.PopulateNeighborsFromCornerPoints(hexagonCells, 0.33f * (hexagonSize / 12f));
                    }

                    waveFunctionCollapse.SetCells(hexagonCells);
                    waveFunctionCollapse.SetRadius(radius);

                    edgeCells = null;
                    entryCells = null;
                    levelRampCells = null;
                    cellPath = null;
                }

                if (generateHexagonTileCellClusters && hexagonCells.Count > 1)
                {
                    generateHexagonTileCellClusters = false;

                    cellClusters = HexagonCellCluster.GetHexagonCellClusters(hexagonCells,
                                                                        transform.position,
                                                                        WFCCollapseOrder.Default,
                                                                        true);
                }

                if (generateModelingPlatforms && hasTilePrototypes)
                {
                    generateModelingPlatforms = false;
                    GenerateModelingPlatforms(hexagonCellPrototypes);
                }

                if (generateGridMesh)
                {
                    generateGridMesh = false;

                    verticeGrid = HexagonCell.GenerateVertexGrid(transform.position, radius, stepSize);
                    List<HexagonCell> groundCells = hexagonCells.FindAll(c => c.isLeveledGroundCell || (c.GetGridLayer() == 0 && c.isLeveledEdge || !c.isLeveledCell));

                    foreach (var item in groundCells)
                    {
                        item._vertexIndices.Clear();
                    }

                    HexagonCell.AssignVerticesToCells(verticeGrid, groundCells);

                    // HexagonCell.SmoothElevationAlongPath(hexagonCells.FindAll(c => c.isPathCell), verticeGrid);
                    // HexagonCell.SlopeYPositionAlongPath(hexagonCells.FindAll(c => c.isPathCell), verticeGrid);
                    HexagonCell.SmoothElevationAlongPath(hexagonCells.FindAll(c => c.isPathCell || c.isEntryCell), verticeGrid);

                    HexagonCell.CreateMeshFromVertices(verticeGrid, meshFilter);
                    mesh = meshFilter.mesh;
                    // List<Vector3> vertices = HexagonCell.GetOrderedVertices(
                    //                             hexagonCells.FindAll(c => c.isLeveledGroundCell || (c.GetGridLayer() == 0 && c.isLeveledCell == false)));

                    // HexagonCell.CreateGridMesh(vertices, meshFilter);
                    // HexagonCell.CreateGridMesh(hexagonCells.FindAll(c => c.isLeveledGroundCell || (c.GetGridLayer() == 0 && c.isLeveledCell == false)), meshFilter);
                }

                if (bSaveMesh)
                {
                    bSaveMesh = false;
                    if (mesh != null) SaveMeshAsset(mesh, "New HexagonCell Terrain Mesh");
                }


                if (generateLeveledCellList)
                {
                    generateLeveledCellList = false;

                    // leveledCellList = HexagonCellCluster.GetCellsFromConsecutiveNeighboringClusters(cellClusters, 0.7f);
                    elevationCellList = new Dictionary<int, List<HexagonCell>>();
                    elevationCellList.Add(0, new List<HexagonCell>());

                    elevationEdgeCellList = new Dictionary<int, List<HexagonCell>>();
                    elevationEdgeCellList.Add(0, new List<HexagonCell>());

                    elevationCellList[0] = HexagonCell.GetRandomLeveledCells(hexagonCells.FindAll(c => c.isEdgeCell == false), radius * 0.45f, false);
                    // Debug.Log("elevationCellList: " + elevationCellList[0].Count);
                    elevationEdgeCellList[0] = HexagonCell.GetLeveledEdgeCells(elevationCellList[0], false);
                }

                if (showEdgeCells)
                {
                    edgeCells = HexagonCell.GetEdgeCells(hexagonCells);

                    if (edgeCells.Count == 0)
                    {
                        showEdgeCells = false;
                    }
                }


                if (resetLevelRampCells)
                {
                    resetLevelRampCells = false;

                    if (cellPath != null && cellPath.Count > 0)
                    {
                        levelRampCells = HexagonCell.GetRandomLevelConnectorCells(cellPath, 2);
                    }
                }

                if (showCellClusters)
                {
                    if (cellClusters == null)
                    {
                        showCellClusters = false;
                        highlightCellCluster = 0;
                    }
                    else if (cellClusters.Count == 0)
                    {
                        showCellClusters = false;
                        highlightCellCluster = 0;
                    }
                }


                if (testCellPath)
                {
                    testCellPath = false;

                    // if (edgeCells == null || edgeCells.Count == 0) edgeCells = HexagonCell.GetEdgeCells(hexagonCells);

                    // entryCells = HexagonCell.GetRandomEntryCells(edgeCells, 3, false);

                    // List<HexagonCell> _processedCells = new List<HexagonCell>();
                    // _processedCells.AddRange(hexagonCells.FindAll(c => !c.isEdgeCell && c.GetGridLayer() == 0));



                    // cellPath = HexagonCell.GetRandomCellPaths(entryCells, _processedCells, transform.position);

                    if (elevationEdgeCellList != null && elevationEdgeCellList.Count > 0)
                    {
                        // Get random edge
                        HexagonCell topEdge = elevationEdgeCellList[elevationEdgeCellList.Count - 1][0];
                        HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(elevationCellList[0], transform.position);

                        entryCells = HexagonCell.GetRandomEntryCells(edgeCells.FindAll(c => c.GetGridLayer() == 0), 3, false);

                        cellPaths = HexagonCell.GenerateRandomCellPaths(entryCells, elevationEdgeCellList[elevationEdgeCellList.Count - 1][0], elevationCellList, transform.position);
                        // cellPath = HexagonCell.FindPath(entryCells[0], topEdge, true);

                        // cellPaths = new Dictionary<int, List<HexagonCell>>();

                        // cellPaths.Add(0, cellPath);
                        // int paths = 0;

                        // for (int i = 1; i < entryCells.Count; i++)
                        // {
                        //     paths++;
                        //     cellPaths.Add(paths, HexagonCell.FindPath(entryCells[i], centerCell, true));
                        //     paths++;
                        //     cellPaths.Add(paths, HexagonCell.FindPath(entryCells[i], entryCells[i - 1], true));
                        // }
                    }

                    Dictionary<int, List<HexagonCell>> cellsByLevel = HexagonCell.OrganizeCellsByLevel(hexagonCells);

                    (Dictionary<int, List<HexagonCell>> _pathsByLevel, Dictionary<int, List<HexagonCell>> _rampsByLevel)
                        = HexagonCell.GetRandomGridPathsForLevels(cellsByLevel, transform.position, 2, false, 2);

                    pathsByLevel = _pathsByLevel;
                    rampsByLevel = _rampsByLevel;
                }
            }
        }




        private void OnDrawGizmos()
        {
            if (showBounds)
            {
                Gizmos.color = Color.magenta;
                // Vector3 pointWorldPos = transform.TransformPoint(transform.position);
                Gizmos.DrawWireSphere(transform.position, radius * transform.lossyScale.x);
                // ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(UtilityHelpers.GetTransformPositions(_edgePoints), transform);

                if (verticeGrid != null)
                {
                    for (int x = 0; x < verticeGrid.GetLength(0); x++)
                    {
                        for (int z = 0; z < verticeGrid.GetLength(1); z++)
                        {
                            float rad = 0.33f;
                            if (verticeGrid[x, z].isCellCenterPoint)
                            {
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }
                            else
                            {
                                Gizmos.color = Color.black;
                            }
                            Gizmos.DrawSphere(verticeGrid[x, z].position, rad);
                        }
                    }
                }
            }

            if (showTileGrid && hexagonCellPrototypesByLayer != null && hexagonCellPrototypesByLayer.Count > 0)
            {
                Gizmos.color = Color.black;

                for (int i = 0; i < hexagonCellPrototypesByLayer.Count; i++)
                {

                    // Debug.Log("Layer: " + i);

                    // List<HexagonTilePrototype> prototypes = hexagonCellPrototypesByLayer.Select(x => x.Value).ToList();

                    foreach (var kvp in hexagonCellPrototypesByLayer)
                    {
                        int key = kvp.Key;
                        List<HexagonTilePrototype> value = kvp.Value;
                        foreach (HexagonTilePrototype item in value)
                        {
                            Vector3 pointPos = item.center;
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawSphere(pointPos, 0.3f);

                            for (int j = 0; j < item.cornerPoints.Length; j++)
                            {
                                pointPos = item.cornerPoints[j];
                                Gizmos.DrawSphere(pointPos, 0.25f);
                            }

                            Gizmos.color = Color.black;
                            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(item.cornerPoints);
                        }
                    }
                }
            }

            if (showEdgeCells && edgeCells != null)
            {
                Gizmos.color = Color.red;

                foreach (HexagonCell cell in edgeCells)
                {
                    Vector3 pointPos = cell.transform.position;
                    Gizmos.DrawSphere(pointPos, 3f);
                }
            }

            if (showEntranceCells)
            {
                Gizmos.color = Color.yellow;

                if (entryCells != null && entryCells.Count > 0)
                {
                    foreach (HexagonCell cell in entryCells)
                    {
                        Vector3 pointPos = cell.transform.position;
                        Gizmos.DrawSphere(pointPos, 8f);
                    }

                }
                else
                {

                    foreach (HexagonCell cell in edgeCells)
                    {
                        if (cell.isEntryCell)
                        {
                            Vector3 pointPos = cell.transform.position;
                            Gizmos.DrawSphere(pointPos, 4f);
                        }
                    }
                }
            }

            if (showPathCells)
            {
                if (cellPaths != null && cellPaths.Count > 0)
                {
                    foreach (var kvp in cellPaths)
                    {
                        List<HexagonCell> path = kvp.Value;

                        foreach (HexagonCell cell in path)
                        {
                            Gizmos.color = Color.blue;

                            Vector3 pointPos = cell.transform.position;
                            Gizmos.DrawSphere(pointPos, 6f);
                        }
                    }
                }
                // if (entryCells != null && cellPath != null && entryCells.Count > 0 && cellPath.Count > 0)
                // {
                //     foreach (HexagonCell cell in cellPath)
                //     {
                //         Gizmos.color = Color.grey;

                //         Vector3 pointPos = cell.transform.position;
                //         Gizmos.DrawSphere(pointPos, 4f);
                //     }
                // }

                // if (pathsByLevel != null && pathsByLevel.Count > 0)
                // {
                //     Debug.Log("pathsByLevel: " + pathsByLevel.Count);
                //     foreach (var kvp in pathsByLevel)
                //     {
                //         int level = kvp.Key;
                //         List<HexagonCell> path = kvp.Value;

                //         foreach (HexagonCell cell in path)
                //         {
                //             Gizmos.color = Color.blue;

                //             Vector3 pointPos = cell.transform.position;
                //             Gizmos.DrawSphere(pointPos, 3f);
                //         }
                //     }
                // }
                // else
                // {
                //     foreach (var item in hexagonCells)
                //     {
                //         if (item.isPathCell)
                //         {
                //             Gizmos.color = Color.blue;

                //             Vector3 pointPos = item.transform.position;
                //             Gizmos.DrawSphere(pointPos, 3f);
                //         }
                //     }
                // }
            }

            if (showLevelRampCells && levelRampCells != null && levelRampCells.Count > 0)
            {
                foreach (HexagonCell cell in levelRampCells)
                {
                    Gizmos.color = Color.magenta;

                    Vector3 pointPos = cell.transform.position;
                    Gizmos.DrawSphere(pointPos, 3.5f);
                }
            }

            if (showLeveledCellList && elevationCellList != null && elevationCellList.Count > 0)
            {
                foreach (var kvp in elevationCellList)
                {
                    int level = kvp.Key;
                    List<HexagonCell> cellsList = kvp.Value;

                    foreach (HexagonCell cell in cellsList)
                    {
                        Gizmos.color = Color.black;

                        Vector3 pointPos = cell.transform.position;
                        Gizmos.DrawSphere(pointPos, 3.5f);
                    }
                }
            }
            if (showLeveledCellEdgeList && elevationEdgeCellList != null && elevationEdgeCellList.Count > 0)
            {
                foreach (var kvp in elevationEdgeCellList)
                {
                    int level = kvp.Key;
                    List<HexagonCell> cellsList = kvp.Value;

                    foreach (HexagonCell cell in cellsList)
                    {
                        Gizmos.color = Color.red;

                        Vector3 pointPos = cell.transform.position;
                        Gizmos.DrawSphere(pointPos, 4.5f);
                    }
                }
            }
            else
            {
                showLeveledCellEdgeList = false;
            }


            if (showCellClusters)
            {
                if (highlightCellCluster > cellClusters.Count - 1)
                {
                    highlightCellCluster = cellClusters.Count - 1;
                }

                foreach (HexagonCellCluster cluster in cellClusters)
                {
                    Vector3 pointPos = cluster.center;
                    Vector3 foundationPos = cluster.GetFoundationCenter();

                    if (cluster.id == highlightCellCluster)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(pointPos, 9f);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(pointPos, 6f);
                        if (cluster.cells.Count > 5)
                        {
                            Gizmos.DrawWireSphere(pointPos, 24);
                        }
                    }

                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(foundationPos, 3f);

                    Gizmos.color = Color.blue;
                    foreach (HexagonCell cell in cluster.cells)
                    {
                        if (cluster.id == highlightCellCluster)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawSphere(cell.transform.position, 6f);
                        }
                        else
                        {
                            // Vector3 direction = (cell.transform.position - pointPos);
                            // Gizmos.DrawRay(cell.transform.position - pointPos, direction);
                            // Gizmos.DrawSphere(cell.transform.position, 2f);
                        }
                    }
                }
            }

        }

        private void GenerateHexagonCellObjects(List<HexagonTilePrototype> hexagonCellPrototypes)
        {
            List<HexagonCell> newHexagonCells = new List<HexagonCell>();

            Transform folder = new GameObject("Cells").transform;
            folder.transform.SetParent(gameObject.transform);

            foreach (HexagonTilePrototype tilePrototype in hexagonCellPrototypes)
            {
                Vector3 pointPos = tilePrototype.center;

                GameObject newTile = Instantiate(HexagonTileCell_prefab, pointPos, Quaternion.identity);
                HexagonCell hexagonTile = newTile.GetComponent<HexagonCell>();
                hexagonTile._cornerPoints = tilePrototype.cornerPoints;
                hexagonTile.SetSize(hexagonSize);
                hexagonTile.id = tilePrototype.id;
                hexagonTile.name = "HexagonCell_" + tilePrototype.id;

                newHexagonCells.Add(hexagonTile);

                hexagonTile.transform.SetParent(folder);
            }
            hexagonCells = newHexagonCells;
            // cellsByLayer = HexagonCell.OrganizeCellsByLevel(hexagonCells);
        }

        private void GenerateHexagonCellObjects(Dictionary<int, List<HexagonTilePrototype>> hexagonCellPrototypes)
        {
            List<HexagonCell> newHexagonCells = new List<HexagonCell>();

            Transform folder = new GameObject("Cells").transform;
            folder.transform.SetParent(gameObject.transform);

            foreach (var kvp in hexagonCellPrototypes)
            {
                int layer = kvp.Key;
                List<HexagonTilePrototype> prototypes = kvp.Value;

                Transform layerFolder = new GameObject("Layer_" + layer).transform;
                layerFolder.transform.SetParent(folder);

                foreach (HexagonTilePrototype tilePrototype in prototypes)
                {
                    Vector3 pointPos = tilePrototype.center;

                    GameObject newTile = Instantiate(HexagonTileCell_prefab, pointPos, Quaternion.identity);
                    HexagonCell hexagonTile = newTile.GetComponent<HexagonCell>();
                    hexagonTile._cornerPoints = tilePrototype.cornerPoints;
                    hexagonTile.id = tilePrototype.id;
                    hexagonTile.name = "HexagonCell_" + tilePrototype.id;
                    hexagonTile.SetSize(hexagonSize);
                    hexagonTile.SetGridLayer(layer);

                    if (layer > 0)
                    {
                        for (int i = 0; i < newHexagonCells.Count; i++)
                        {
                            if (newHexagonCells[i].GetGridLayer() < hexagonTile.GetGridLayer() && newHexagonCells[i].id == tilePrototype.bottomNeighborId)
                            {
                                hexagonTile._neighbors.Add(newHexagonCells[i]);
                                hexagonTile.layeredNeighbor[0] = newHexagonCells[i]; // set bottom neighbor

                                newHexagonCells[i]._neighbors.Add(hexagonTile);
                                newHexagonCells[i].layeredNeighbor[1] = hexagonTile; //Set top neighbor
                            }
                        }
                    }
                    newHexagonCells.Add(hexagonTile);
                    hexagonTile.transform.SetParent(layerFolder);
                }
            }
            hexagonCells = newHexagonCells;
        }


        private void GenerateModelingPlatforms(List<HexagonTilePrototype> hexagonCellPrototypes)
        {
            foreach (HexagonTilePrototype tilePrototype in hexagonCellPrototypes)
            {
                Vector3 pointPos = tilePrototype.center;
                pointPos.y += 0.2f;

                GameObject newModel = Instantiate(HexagonTilePlatformModel_prefab, pointPos, Quaternion.identity);
            }
        }



        #region Save Mesh
        Mesh mesh;
        [SerializeField] private bool bSaveMesh = false;
        [SerializeField] private Mesh lastSavedMesh;

        void SaveMeshAsset(Mesh mesh, string assetName)
        {
            // Create a new mesh asset
            lastSavedMesh = Instantiate(mesh) as Mesh;
            lastSavedMesh.name = assetName;

            // Save the mesh asset to the project
            AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Terrain/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }
        #endregion


    }


    [System.Serializable]
    public struct SubzonePrototype
    {
        public Vector3 position;
        public float radius;
        public Vector3[] borderCorners;
        public List<HexagonTilePrototype> cells;
    }

    [System.Serializable]
    public struct ZoneConnector
    {
        public Vector3 position;
        public Vector3[] zones;
    }
}