using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public enum CellClusterType { Path, Edge, Outpost, Other, Tunnel }
    public enum GridFilter_Level { None, All, HostCells = 12, MicroCells = 4, X_36 = 36, X_108 = 108 }
    public enum GridFilter_Type
    {
        None, All, Unset, Highlight, OriginalGridEdge, AnyEdge, GridEdge, HostChildEdge, InnerEdge, Path,
        Ground, FlatGround, Cluster, Removed, UnderGround, Underwater, AboveGround,
        Entrance, Tunnel, TunnelGroundEntry, TunnelFloor, TunnelAir,
    }
    public enum CellDisplay_Type
    {
        DrawCenterAndLines,
        DrawLines,
        DrawCenter,
    }

    public enum GridPreset
    {
        Unset = 0,
        Outpost,
        Town,
        City,
        Cave,
        CreatureDen
    }

    [System.Serializable]
    public struct HexagonCellManagerSettings
    {
        public HexagonCellManagerSettings(
         int _radius,
         int _cellSize,
         bool _useCorners,
         GridPreset _gridPreset,
         float _centerPosYOffset,
         bool _useGridErosion,
         float _erosionRadiusMult,
         int _erosionClumpSets,
         int _maxCellsToRemove,
         int _cellLayers,
         int _cellLayersMax,
         bool _randomizeCellLayers,
         int _cellLayerElevation,
         bool _enableClusters,
         int _clustersMax,
         float _cluster_radiusMultMin,
         float _cluster_radiusMultMax,
         bool _randomizeClusterSearchRadius,
         int _cluster_memberMin,
         int _cluster_memberMax,
         bool _enableTunnels
        )
        {
            radius = _radius;
            cellSize = _cellSize;
            useCorners = _useCorners;
            gridPreset = _gridPreset;
            centerPosYOffset = _centerPosYOffset;
            useGridErosion = _useGridErosion;
            erosionRadiusMult = _erosionRadiusMult;
            erosionClumpSets = _erosionClumpSets;
            maxCellsToRemove = _maxCellsToRemove;
            cellLayers = _cellLayers;
            cellLayersMax = _cellLayersMax;
            randomizeCellLayers = _randomizeCellLayers;
            cellLayerElevation = _cellLayerElevation;
            enableClusters = _enableClusters;
            clustersMax = _clustersMax;
            cluster_radiusMultMin = _cluster_radiusMultMin;
            cluster_radiusMultMax = _cluster_radiusMultMax;
            randomizeClusterSearchRadius = _randomizeClusterSearchRadius;
            cluster_memberMin = _cluster_memberMin;
            cluster_memberMax = _cluster_memberMax;
            enableTunnels = _enableTunnels;
        }

        public int radius;
        public int cellSize;
        public bool useCorners;
        public GridPreset gridPreset;
        public float centerPosYOffset;
        public bool useGridErosion;
        public float erosionRadiusMult;
        public int erosionClumpSets;
        public int maxCellsToRemove;
        public int cellLayers;
        public int cellLayersMax;
        public bool randomizeCellLayers;
        public int cellLayerElevation;
        public bool enableClusters;
        public int clustersMax;
        public float cluster_radiusMultMin;
        public float cluster_radiusMultMax;
        public bool randomizeClusterSearchRadius;
        public int cluster_memberMin;
        public int cluster_memberMax;
        public bool enableTunnels;
    }

    public class HexagonCellManager : MonoBehaviour
    {
        public void ApplySettings(HexagonCellManagerSettings settings)
        {
            radius = settings.radius;
            cellSize = settings.cellSize;
            useCorners = settings.useCorners;
            gridPreset = settings.gridPreset;
            centerPosYOffset = settings.centerPosYOffset;
            useGridErosion = settings.useGridErosion;
            erosionRadiusMult = settings.erosionRadiusMult;
            erosionClumpSets = settings.erosionClumpSets;
            maxCellsToRemove = settings.maxCellsToRemove;
            cellLayers = settings.cellLayers;
            cellLayersMax = settings.cellLayersMax;
            randomizeCellLayers = settings.randomizeCellLayers;
            cellLayerElevation = settings.cellLayerElevation;
            enableClusters = settings.enableClusters;
            clustersMax = settings.clustersMax;
            cluster_radiusMultMin = settings.cluster_radiusMultMin;
            cluster_radiusMultMax = settings.cluster_radiusMultMax;
            randomizeClusterSearchRadius = settings.randomizeClusterSearchRadius;
            cluster_memberMin = settings.cluster_memberMin;
            cluster_memberMax = settings.cluster_memberMax;
            enableTunnels = settings.enableTunnels;

            Debug.Log("HexagonCellManagerSettings applied to Cell Manager: " + gameObject.name);
        }

        [Header("Cell Grid Settings")]
        [Range(4, 324)][SerializeField] private int radius = 72;
        public int GetRadius() => radius;
        public void SetRadius(int value)
        {
            radius = value;
        }

        [SerializeField] private bool isManaged;
        [SerializeField] private bool isTile;
        [SerializeField] private float managedElevation = 0;
        public void SetManagedElevation(float elevationY)
        {
            isManaged = true;
            managedElevation = elevationY;
        }
        public void SetElevationOffset(int value)
        {
            centerPosYOffset = value;
        }

        [Range(2, 128)][SerializeField] private int cellSize = 12;
        [SerializeField] private bool useCorners;
        [SerializeField] private GridPreset gridPreset;
        [SerializeField] private float centerPosYOffset = 0;
        public void CalculateCenterElevationOffset(float averageTerrainElevation)
        {
            float initialGridHeight = GetInitialGridHeight();

            float avg = UtilityHelpers.CalculateAverage(initialGridHeight, averageTerrainElevation);
            float offset;
            if (avg > averageTerrainElevation)
            {
                offset = averageTerrainElevation - avg;
            }
            else
            {
                offset = avg;
            }

            Debug.Log("centerPosYOffset: " + offset + ", averageTerrainElevation: " + averageTerrainElevation + ", initialGridHeight: " + initialGridHeight);

            if (centerPosYOffset != offset) centerPosYOffset = offset;

        }
        public GridPreset GetGridPreset() => gridPreset;

        [Header("Grid Erosion ")]
        [SerializeField] private bool useGridErosion;
        [Range(0.1f, 0.7f)][SerializeField] private float erosionRadiusMult = 0.24f;
        [Range(1, 8)][SerializeField] private int erosionClumpSets = 5;
        [Range(4, 64)][SerializeField] private int maxCellsToRemove = 32;

        [Header("Layer Settings")]
        [Range(1, 24)][SerializeField] private int cellLayers = 2;
        [Range(3, 24)][SerializeField] private int cellLayersMax = 6;
        [SerializeField] private bool randomizeCellLayers;
        [Range(2, 12)][SerializeField] private int cellLayerElevation = 6;
        public int GetCellInLayerElevation() => cellLayerElevation;
        public float GetInitialGridHeight() => cellPrototypesByLayer_V2 != null ? (cellPrototypesByLayer_V2.Count * cellLayerElevation) : (cellLayersMax * cellLayerElevation);

        [Header("Cluster Settings")]
        [SerializeField] private bool enableClusters = true;
        [Range(1, 24)][SerializeField] private int clustersMax = 1;
        [Range(1.5f, 6f)][SerializeField] private float cluster_radiusMultMin = 1.5f;
        [Range(2f, 12f)][SerializeField] private float cluster_radiusMultMax = 3f;
        [SerializeField] private bool randomizeClusterSearchRadius;
        // [Range(2, 24)][SerializeField] private int cluster_memberMin = 2;
        [Range(2, 24)][SerializeField] private int cluster_memberMin = 2;
        [Range(2, 24)][SerializeField] private int cluster_memberMax = 6;
        [SerializeField] private bool enableTunnels = true;

        [Header(" ")]
        [SerializeField] private bool resetPrototypes;

        [Header("Generate")]
        [SerializeField] private bool generateCells;
        [Header(" ")]
        [SerializeField] private bool generateOnStart;


        #region Saved State
        Vector3 _position;
        int _radius;
        int _cellSize;
        int _cellLayers;
        int _cellLayerElevation;
        float _centerPosYOffset;
        bool _trackPrototypes;
        #endregion
        private Vector3[] cornerPoints;
        private Vector3[] sidePoints;
        private void RecalculateEdgePoints()
        {
            cornerPoints = HexCoreUtil.GenerateHexagonPoints(transform.position, radius);
            sidePoints = HexagonGenerator.GenerateHexagonSidePoints(cornerPoints);
        }

        [Header("Cell Data")]
        [SerializeField] private Dictionary<int, List<HexagonCell>> allCellsByLayer;
        [SerializeField] private Dictionary<int, List<HexagonCell>> allCellsByLayer_X4;
        [SerializeField] private List<HexagonCell> allCells;
        public (Dictionary<int, List<HexagonCell>>, List<HexagonCell>) GetCells() => (allCellsByLayer, allCells);
        public (Dictionary<int, List<HexagonCell>>, List<HexagonCell>, Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>>) GetCellsSet() => (allCellsByLayer, allCells, allCellsByLayer_X4_ByCluster);
        public List<HexagonCell> GetAllCellsList() => allCells;
        [SerializeField] private List<HexagonCell> allCells_X4;

        public Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> allCellsByLayer_X4_ByCluster;


        [Header("Prototype Data")]
        [SerializeField] private int totalPrototypes = 0;
        [SerializeField] private int totalPrototypesX4 = 0;
        public Dictionary<int, List<HexagonCellPrototype>> cellPrototypesByLayer_V2;
        [SerializeField] private List<HexagonCellPrototype> allPrototypesList;
        public List<HexagonCellPrototype> GetAllPrototypesList() => allPrototypesList;
        public List<HexagonCellPrototype> GetAllPrototypeEdgesOfType(EdgeCellType type) => allPrototypesList.FindAll(c => c._edgeCellType == type);
        public List<HexagonCellPrototype> GetAllPrototypesOfCellStatus(CellStatus status) => allPrototypesList.FindAll(c => c.GetCellStatus() == status);

        public Dictionary<int, List<HexagonCellPrototype>> cellPrototypes_X4_ByLayer;
        public Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>> cellPrototypesByLayer_X4_ByParent { get; private set; }
        public Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>> cellPrototypesByLayer_X4_ByCluster { get; private set; }

        [Header("Cluster Data")]
        [SerializeField] private List<HexagonCellCluster> allCellPrototypeClusters;
        public List<HexagonCellCluster> GetPrototypeClustersOfType(CellClusterType type) => allCellPrototypeClusters.FindAll(c => c.clusterType == type);
        [SerializeField] private List<HexagonCell> allClusteredCells;
        [SerializeField] private bool _isClusterParent;

        [Header("Debug Settings")]
        [SerializeField] private bool showGrid;
        [Header(" ")]
        [SerializeField] private bool showNeighborGridEdges;
        [SerializeField] private bool showTunnelClusters;
        [Header(" ")]
        [SerializeField] private GridFilter_Level gridFilter_Level;
        [SerializeField] private GridFilter_Type gridFilter_Type;
        [Header(" ")]
        [SerializeField] private bool showBounds;

        [Header("Cell Debug")]
        [SerializeField] private ShowCellState showCells;
        private enum ShowCellState { None, All, Edge, Entry, Path }
        [SerializeField] private List<HexagonCell> edgeCells;
        [SerializeField] private List<HexagonCell> entryCells;
        [SerializeField] private List<HexagonCell> levelRampCells;
        [SerializeField] private List<HexagonCell> cellPath;
        Dictionary<int, List<HexagonCell>> pathsByLevel;
        Dictionary<int, List<HexagonCell>> rampsByLevel;

        [Header("Prefabs")]
        [SerializeField] private GameObject HexagonCell_prefab;
        [SerializeField] private GameObject HexagonTilePlatformModel_prefab;


        [SerializeField] private GameObject debug_TunnelParent;
        private List<Color> _temp_randomColors;


        public void SetClusterParent()
        {
            _isClusterParent = true;
        }
        public bool GetClusterParent() => _isClusterParent;
        public int CalculateRandomLayers() => UnityEngine.Random.Range(cellLayers, cellLayersMax + 1);
        public int CalculateRandomClusters() => UnityEngine.Random.Range(0, clustersMax + 1);
        public bool ArePrototypesInitialized() => (cellPrototypesByLayer_V2 != null && cellPrototypesByLayer_V2.Count > 0);


        #region Core / Init
        public void InitialSetup() { }
        private bool _shouldUpdate;
        private void OnValidate()
        {
            if (_temp_randomColors == null || _temp_randomColors.Count < 2)
            {
                _temp_randomColors = UtilityHelpers.GenerateUniqueRandomColors(10);
            }

            if (
                ArePrototypesInitialized() == false
                || resetPrototypes == true
                || _position != transform.position
                || _cellSize != cellSize
                || _cellLayers != cellLayers
                || _cellLayerElevation != cellLayerElevation
                || _centerPosYOffset != centerPosYOffset
                || cornerPoints == null || sidePoints == null
                )
            {
                _centerPosYOffset = centerPosYOffset;
                _position = transform.position;
                resetPrototypes = false;

                // if (cellSize % 2 != 0) cellSize += 1;
                // _cellSize = cellSize;

                // int cellRadiusMult = cellSize * 3;
                // if (cellSize * 3 > radius) radius = cellSize * 3;

                // int remmainder = radius % cellRadiusMult;
                // Debug.Log("radius: " + radius + ", remmainder: " + remmainder + ", cellRadiusMult: " + cellRadiusMult);

                // if (remmainder != 0)
                // {
                //     radius -= remmainder;
                //     // if (_radius <= radius) radius += remmainder;
                //     // if (_radius > radius) radius -= remmainder;
                // }
                // _radius = radius;

                _shouldUpdate = true;
            }

            if (_radius != radius)
            {
                if (radius % 2 != 0) radius += 1;
                _radius = radius;
                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                _shouldUpdate = false;

                RecalculateEdgePoints();
                GeneratePrototypeGridsByLayer();
            }

            if (generateCells)
            {
                GenerateCells(generateCells);
                generateCells = false;
            }
        }

        private void Awake() => InitialSetup();

        private void Start()
        {
            InitialSetup();

            if (generateOnStart) GenerateCells(false);
        }

        #endregion


        #region Cell Prototype Methods
        public void ResetPrototypes()
        {
            resetPrototypes = true;
            OnValidate();
        }

        public void GeneratePrototypeGridsByLayer(IHexCell parentCell = null)
        {
            Vector3 centerPos = gameObject.transform.position;

            if (isManaged)
            {
                centerPos.y = managedElevation;
            }
            else
            {
                centerPos.y += centerPosYOffset;
            }

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(
                            centerPos,
                            radius,
                            cellSize,
                            cellLayers,
                            cellLayerElevation,
                            null,
                            0,
                            null,
                            null, //_cornersToUse,
                            useGridErosion,
                            isTile
                        );


            // Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(
            //     centerPos,
            //     radius,
            //     cellSize,
            //     cellLayers,
            //     cellLayerElevation,
            //     null,
            //     0,
            //     transform,
            //     null,
            //     useGridErosion
            // );

            List<HexagonCellPrototype> _allPrototypesList = new List<HexagonCellPrototype>();

            int count = 0;
            foreach (var kvp in newPrototypesByLayer)
            {
                count += kvp.Value.Count;
                HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);

                _allPrototypesList.AddRange(kvp.Value.FindAll(p => p.IsRemoved() == false));
            }

            cellPrototypesByLayer_V2 = newPrototypesByLayer;
            allPrototypesList = _allPrototypesList;
            totalPrototypes = count;

            allCellPrototypeClusters = new List<HexagonCellCluster>();
            // Debug.Log("GenerateGridsByLayer - totalPrototypes: " + totalPrototypes + ", allPrototypesList: " + allPrototypesList.Count);
        }

        public void EvaluateNewPrototypeGridEdgeNeihgbors(List<HexagonCellPrototype> neighborCellGridEdges)
        {
            List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Default);
            gridEdges.AddRange(neighborCellGridEdges);
            HexagonCellPrototype.PopulateNeighborsFromCornerPoints(gridEdges, transform);
        }


        public List<HexagonCellPrototype> Generate_GridPreset_Outpost_WPath(TerrainVertex[,] vertexGrid, float cellVertexSearchRadiusMult = 1.4f)
        {
            HexagonCellPrototype.CleanupCellIslandLayerPrototypes(cellPrototypesByLayer_V2, 3);

            GenerateCellClusters_Random(CellClusterType.Outpost);

            bool pathIgnoresEdgeCells = true;
            List<HexagonCellPrototype> path = PathBetweenClusters();
            if (path != null) CreatePathCluster(path);

            if (enableTunnels) GenerateCellCluster_Random_Underground(); // GenerateCellClusters_Cave();
            //     // cellManager.GenerateCellClusters_Cave(maxTunnelMemberSize, _tunnelMeshParent);

            List<HexagonCellPrototype> unused = RemoveUnusedPrototypes();

            if (vertexGrid != null)
            {
                HexGridVertexUtil.UnassignCellVertices(unused, vertexGrid);
                HexGridVertexUtil.AssignTerrainVerticesToGroundPrototypes(cellPrototypesByLayer_V2, vertexGrid, cellVertexSearchRadiusMult);
            }
            return path;
        }

        public List<HexagonCellPrototype> Generate_GridPreset_City_WPath()
        {

            List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Default);
            List<HexagonCellPrototype> entries = HexagonCellPrototype.PickRandomEntryFromGridEdges(gridEdges, 3, true);

            bool pathIgnoresEdgeCells = useGridErosion;
            List<HexagonCellPrototype> path = HexGridPathingUtil.GenerateRandomPath(entries, allPrototypesList, gameObject.transform.position, pathIgnoresEdgeCells);
            if (path != null) CreatePathCluster(path);

            Debug.Log("gridEdges: " + gridEdges.Count + ", entries: " + entries.Count + ", allPrototypesList: " + allPrototypesList.Count);

            GenerateCellClusters_GridEdge(gridEdges);

            return path;
        }

        public HexagonCellCluster CreatePathCluster(List<HexagonCellPrototype> path)
        {
            if (path == null || path.Count == 0) return null;

            int clusterid = allCellPrototypeClusters.Count == 0 ? 0 : allCellPrototypeClusters.Count + 1;

            HexagonCellCluster newPathCluster = new HexagonCellCluster(path[0].GetId(), path, CellClusterType.Path, ClusterGroundCellLayerRule.Unset);
            allCellPrototypeClusters.Add(newPathCluster);

            return newPathCluster;
        }

        #endregion

        public List<HexagonCellPrototype> Setup_WorldAreaGrid_MainPath(GridPreset gridPreset = GridPreset.Unset)
        {
            List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Default);
            List<HexagonCellPrototype> pathEdgeNodes = Setup_GridEdgePathConnectors(gridEdges, UnityEngine.Random.Range(2, 3));
            List<HexagonCellPrototype> mainPath = HexagonCellPrototype.GenerateRandomPathBetweenCells(pathEdgeNodes, false, false);

            return mainPath;
        }

        public List<HexagonCellPrototype> Setup_GridEdgePathConnectors(List<HexagonCellPrototype> allGridEdgeCells, int minimun = 3)
        {
            List<HexagonCellPrototype> gridEdgePathConnectors = new List<HexagonCellPrototype>();

            // int desiredAmount = minimumPerSide * 6;
            int desiredAmount = minimun;
            List<HexagonCellPrototype> result = HexagonCellPrototype.PickRandomEntryFromGridEdges(allGridEdgeCells, minimun, true);
            foreach (var item in result)
            {
                item.SetOriginalGridEdge(true);

                item.isWorldSpaceEdgePathConnector = true;
                item.SetPathCell(true, PathCellType.End);
            }

            // List<HexagonCellPrototype> available = allGridEdgeCells.FindAll(e => e.IsEdge() && e.IsGround() && !e.IsPreAssigned());
            // List<HexagonCellPrototype> hasNonParentNeighbor = available.FindAll(e => e.isEdgeOfParent && e.neighbors.Any(n => n.parentId != e.parentId));
            // List<HexagonCellPrototype> result = new List<HexagonCellPrototype>();

            // if (hasNonParentNeighbor.Count > 0)
            // {
            //     for (int i = 0; i < hasNonParentNeighbor.Count; i++)
            //     {
            //         if (result.Count >= desiredAmount) break;
            //         if (result.Contains(hasNonParentNeighbor[i]) == false)
            //         {
            //             hasNonParentNeighbor[i].isWorldSpaceEdgePathConnector = true;
            //             hasNonParentNeighbor[i].isPath = true;
            //             hasNonParentNeighbor[i].isPathEnd = true;

            //             result.Add(hasNonParentNeighbor[i]);
            //         }
            //     }
            // }
            // for (int i = 0; i < available.Count; i++)
            // {
            //     if (result.Count >= desiredAmount) break;
            //     if (result.Contains(available[i]) == false)
            //     {
            //         available[i].isWorldSpaceEdgePathConnector = true;
            //         available[i].isPath = true;
            //         available[i].isPathEnd = true;

            //         result.Add(available[i]);
            //     }
            // }
            return result;
        }

        public void EvaluatePrototypeGridEdges()
        {
            foreach (var kvp in cellPrototypesByLayer_V2)
            {
                HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Default);
            }
        }

        public List<HexagonCellPrototype> RemoveUnusedPrototypes()
        {
            /// ///  Restore the spaces back to default terrain
            /// <summary> TODO:
            ///  Delete prototype layer stacks that are not being used 
            /// 
            /// </summary>
            List<HexagonCellPrototype> unusedPrototypes = new List<HexagonCellPrototype>();
            List<int> layerKeys = new List<int>();
            foreach (var kvp in cellPrototypesByLayer_V2)
            {
                layerKeys.Add(kvp.Key);
            }

            foreach (int layer in layerKeys)
            {
                List<HexagonCellPrototype> found = cellPrototypesByLayer_V2[layer].FindAll(p =>
                    p.IsRemoved() || (p.IsPreAssigned() == false));

                unusedPrototypes.AddRange(found);

                // foreach (HexagonCellPrototype item in found)
                // {
                //     if (item.IsGround()) {
                //     List<HexagonCellPrototype> layerStack = HexagonCellPrototype.GetAllUpperCellsInLayerStack(item);
                //     unusedPrototypes.AddRange(layerStack.FindAll(p => p.IsPreAssigned() == false));
                //     }
                // }
                cellPrototypesByLayer_V2[layer] = cellPrototypesByLayer_V2[layer].Except(found).ToList();
            }

            if (unusedPrototypes.Count > 0)
            {
                foreach (HexagonCellPrototype item in unusedPrototypes)
                {
                    item.SetCellStatus(CellStatus.Remove);
                }
            }

            EvaluatePrototypeGridEdges();

            return unusedPrototypes;
        }

        public List<HexagonCellPrototype> PathBetweenCells(List<HexagonCellPrototype> targetCells)
        {
            return HexagonCellPrototype.GenerateRandomPathBetweenCells(targetCells, false, false);
        }

        public List<HexagonCellPrototype> PathBetweenClusters()
        {
            /// <summary> TODO:
            ///  Iterate over the clusters 
            ///     for each cluster, path to the other clusters 
            ///         get a cluster edge cell and path to a cluster edge cell in the other cluster 
            ///             prefer the cells with the most neighbors  
            /// </summary>
            if (allCellPrototypeClusters.Count < 2)
            {
                Debug.LogError("Not enough clusters to path between");
                return null;
            }

            List<HexagonCellPrototype> pathFocusCells = new List<HexagonCellPrototype>();

            foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
            {
                if (cluster.clusterType == CellClusterType.Path) continue;
                List<HexagonCellPrototype> edges = cluster.GetClusterEdgeCells();
                if (edges.Count > 0)
                {

                    pathFocusCells.Add(edges[0]);
                }
            }
            return HexagonCellPrototype.GenerateRandomPathBetweenCells(pathFocusCells, false, false);
        }


        Dictionary<Vector3, List<List<Vector3>>> temp_vertexSurfaceSets;
        public void GenerateCellCluster_Random_Underground(int maxMembers = 22, GameObject tunnelMeshParentGameObject = null)
        {
            List<HexagonCellPrototype> availableUnderGroundStartCells = GetAllPrototypesOfCellStatus(CellStatus.UnderGround).FindAll
                (c => c.IsPreAssigned() == false && c.GetLayer() >= 1
                    && c.layerNeighbors[1] != null && c.layerNeighbors[1].IsGround() && c.layerNeighbors[1].IsPreAssigned() == false && c.layerNeighbors[1].HasPreassignedNeighbor() == false
                    && c.GetGetNeighborsWithStatus(CellStatus.UnderGround).FindAll(n => n.IsPreAssigned() == false).Count > 1
                ).OrderByDescending(x => x.GetGetNeighborsWithStatus(CellStatus.UnderGround).Count).ToList();


            if (availableUnderGroundStartCells.Count == 0)
            {
                Debug.LogError("NO available UnderGround Start Cells");
                return;
            }

            List<HexagonCellPrototype> availableGroundCells = GetAllPrototypesOfCellStatus(CellStatus.GenericGround).FindAll
                (c => c.IsPreAssigned() == false && c.GetLayer() >= 1 && availableUnderGroundStartCells.Contains(c.layerNeighbors[0]));

            if (availableGroundCells.Count == 0)
            {
                Debug.LogError("NO availableGroundCells");
                return;
            }

            List<HexagonCellPrototype> finalResult = new List<HexagonCellPrototype>();

            foreach (var underGroundStartCell in availableUnderGroundStartCells)
            {
                HexagonCellPrototype groundEntry = underGroundStartCell.layerNeighbors[1];
                if (groundEntry == null || !groundEntry.IsGround() || groundEntry.HasPreassignedNeighbor() || underGroundStartCell.layerNeighbors[0] == null) continue;

                List<HexagonCellPrototype> newTunnelCluster = HexGridPathingUtil.GetTunnelPath(underGroundStartCell, maxMembers, CellSearchPriority.SideNeighbors);
                // List<HexagonCellPrototype> newTunnelCluster = HexagonCellPrototype.GetConsecutiveNeighborsCluster(underGroundStartCell, CellStatus.UnderGround, maxMembers);

                if (newTunnelCluster.Count == 0)
                {
                    // Debug.LogError("GenerateCellClusters_Cave - NO newTunnelClusters");
                    continue;
                }

                HexCellUtil.SetTunnelCells(newTunnelCluster);

                underGroundStartCell.isTunnelStart = true;
                groundEntry.SetTunnelGroundEntry(TunnelEntryType.Basement);

                finalResult.Add(groundEntry);
                finalResult.Add(underGroundStartCell);
                finalResult.AddRange(newTunnelCluster);
                break;
            }

            int clusterid = allCellPrototypeClusters.Count + 1;
            HexagonCellCluster newCluster = new HexagonCellCluster(finalResult[0].GetId(), finalResult, CellClusterType.Tunnel, ClusterGroundCellLayerRule.Unset);
            allCellPrototypeClusters.Add(newCluster);

            Debug.Log("New Tunnel Cluster, member count: " + finalResult.Count + ", maxMembers: " + maxMembers);

            // CreateTunnelMeshFromCluster(newCluster, tunnelMeshParentGameObject);
        }



        public static HexagonCellCluster GenerateCluster_UnderGroundTunnel(List<HexagonCellPrototype> availableCells, int maxMembers = 15)
        {
            List<HexagonCellPrototype> availableUnderGroundStartCells = availableCells.FindAll
                (c =>
                    c.IsUnderGround() &&
                    c.IsPreAssigned() == false &&
                    c.GetLayer() >= 1 &&
                    c.layerNeighbors[1] != null && c.layerNeighbors[1].IsGround() && c.layerNeighbors[1].IsPreAssigned() == false && c.layerNeighbors[1].HasPreassignedNeighbor() == false
                    && c.GetGetNeighborsWithStatus(CellStatus.UnderGround).FindAll(n => n.IsPreAssigned() == false).Count > 1
                ).OrderByDescending(x => x.GetGetNeighborsWithStatus(CellStatus.UnderGround).Count).ToList();

            if (availableUnderGroundStartCells.Count == 0)
            {
                Debug.LogError("NO available UnderGround Start Cells");
                return null;
            }

            // List<HexagonCellPrototype> availableGroundCells = GetAllPrototypesOfCellStatus(CellStatus.GenericGround).FindAll
            //     (c => c.IsPreAssigned() == false && c.GetLayer() >= 1 && availableUnderGroundStartCells.Contains(c.layerNeighbors[0]));
            // if (availableGroundCells.Count == 0)
            // {
            //     Debug.LogError("NO availableGroundCells");
            //     return;
            // }

            List<HexagonCellPrototype> finalResult = new List<HexagonCellPrototype>();

            foreach (var underGroundStartCell in availableUnderGroundStartCells)
            {
                HexagonCellPrototype groundEntry = underGroundStartCell.layerNeighbors[1];
                if (groundEntry == null || !groundEntry.IsGround() || groundEntry.HasPreassignedNeighbor() || underGroundStartCell.layerNeighbors[0] == null) continue;

                List<HexagonCellPrototype> newTunnelList = HexGridPathingUtil.GetTunnelPath(underGroundStartCell, maxMembers, CellSearchPriority.SideNeighbors);
                // List<HexagonCellPrototype> newTunnelList = HexagonCellPrototype.GetConsecutiveNeighborsCluster(underGroundStartCell, CellStatus.UnderGround, maxMembers);

                if (newTunnelList.Count == 0)
                {
                    // Debug.LogError("GenerateCellClusters_Cave - NO newTunnelLists");
                    continue;
                }

                HexCellUtil.SetTunnelCells(newTunnelList);

                underGroundStartCell.isTunnelStart = true;
                groundEntry.SetTunnelGroundEntry(TunnelEntryType.Basement);

                finalResult.Add(groundEntry);
                finalResult.Add(underGroundStartCell);
                finalResult.AddRange(newTunnelList);
                break;
            }

            HexagonCellCluster newCluster = new HexagonCellCluster(finalResult[0].GetId(), finalResult, CellClusterType.Tunnel, ClusterGroundCellLayerRule.Unset);

            Debug.Log("New Tunnel Cluster, member count: " + finalResult.Count + ", maxMembers: " + maxMembers);
            return newCluster;
        }



        public static HexagonCellCluster GenerateCluster_UnderGroundTunnel(HexagonCellCluster existingCluster, int maxMembers = 15, CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors)
        {
            List<HexagonCellPrototype> availableGroundStartCells = existingCluster.prototypes.FindAll
                (c =>
                    c.IsGround() &&
                    c.GetLayer() >= 1 &&
                    c.layerNeighbors[0] != null && c.layerNeighbors[0].IsUnderGround() && c.layerNeighbors[0].IsPreAssigned() == false &&
                    // c.layerNeighbors[0].HasPreassignedNeighbor() == false &&
                    c.GetGetNeighborsWithStatus(CellStatus.UnderGround).FindAll(n => n.IsPreAssigned() == false).Count > 1
                ).OrderByDescending(x => x.GetGetNeighborsWithStatus(CellStatus.UnderGround).Count).ToList();

            if (availableGroundStartCells.Count == 0)
            {
                Debug.LogError("NO available UnderGround Start Cells");
                return null;
            }

            List<HexagonCellPrototype> finalResult = new List<HexagonCellPrototype>();

            foreach (var groundStartCell in availableGroundStartCells)
            {
                HexagonCellPrototype undergroundStart = groundStartCell.layerNeighbors[0];
                if (undergroundStart == null ||
                    !undergroundStart.IsUnderGround() ||
                    undergroundStart.layerNeighbors[0] == null
                    // undergroundStart.HasPreassignedNeighbor() ||
                    ) continue;

                List<HexagonCellPrototype> newTunnelPath = HexGridPathingUtil.GetTunnelPath(undergroundStart, maxMembers, searchPriority);

                if (newTunnelPath.Count == 0)
                {
                    // Debug.LogError("GenerateCellClusters_Cave - NO newTunnelPaths");
                    continue;
                }

                HexCellUtil.SetTunnelCells(newTunnelPath);

                groundStartCell.SetTunnelGroundEntry(TunnelEntryType.Basement);
                undergroundStart.isTunnelStart = true;

                finalResult.Add(groundStartCell);
                finalResult.Add(undergroundStart);
                finalResult.AddRange(newTunnelPath);
                break;
            }

            if (finalResult == null || finalResult.Count == 0)
            {
                Debug.LogError("Failed to generate new Tunnel cluster");
                return null;
            }

            HexagonCellCluster newCluster = new HexagonCellCluster(finalResult[0].GetId(), finalResult, CellClusterType.Tunnel, ClusterGroundCellLayerRule.Unset);

            Debug.Log("Created new Tunnel Cluster - member count: " + finalResult.Count + ", maxMembers: " + maxMembers);
            return newCluster;
        }

        public void GenerateCellClusters_Cave(int maxMembers = 9, GameObject tunnelMeshParentGameObject = null)
        {
            List<HexagonCellPrototype> availableUnderGroundCells = GetAllPrototypesOfCellStatus(CellStatus.UnderGround).FindAll
                (c => c.IsPreAssigned() == false && c.GetLayer() >= 1 && c.layerNeighbors[1] != null
                    && c.GetGetNeighborsWithStatus(CellStatus.GenericGround).FindAll(n => n.IsPreAssigned() == false).Count > 1
                );

            if (availableUnderGroundCells.Count == 0)
            {
                Debug.LogError("GenerateCellClusters_Cave - NO availableUnderGroundCells");
                return;
            }

            List<Vector3> tunnelEntryCorners = new List<Vector3>();

            List<HexagonCellPrototype> finalResult = new List<HexagonCellPrototype>();

            foreach (var item in availableUnderGroundCells)
            {
                List<HexagonCellPrototype> groundNeighbors = item.GetGetNeighborsWithStatus(CellStatus.GenericGround).FindAll(n => n.IsPreAssigned() == false && item.IsSameLayer(n));
                if (groundNeighbors.Count == 0)
                {
                    Debug.LogError("GenerateCellClusters_Cave - NO groundNeighbors");
                    continue;
                }

                List<HexagonCellPrototype> underGroundNeighbors = item.GetGetNeighborsWithStatus(CellStatus.UnderGround).FindAll(n => n.IsPreAssigned() == false);
                if (underGroundNeighbors.Count == 0)
                {
                    Debug.LogError("GenerateCellClusters_Cave - NO underGroundNeighbors");
                    continue;
                }

                List<HexagonCellPrototype> newTunnelCluster = HexagonCellPrototype.GetConsecutiveNeighborsCluster(item, CellStatus.UnderGround, maxMembers);

                if (newTunnelCluster.Count == 0)
                {
                    // Debug.LogError("GenerateCellClusters_Cave - NO newTunnelClusters");
                    continue;
                }

                HexagonCellPrototype groundEntry = groundNeighbors[UnityEngine.Random.Range(0, groundNeighbors.Count)];
                groundEntry.SetTunnelGroundEntry(TunnelEntryType.Basement);
                item.isTunnelStart = true;

                HexCellUtil.SetTunnelCells(newTunnelCluster);

                for (int side = 0; side < groundEntry.neighborsBySide.Length; side++)
                {
                    if (groundEntry.neighborsBySide[side] != null && groundEntry.neighborsBySide[side].isTunnelStart)
                    {
                        tunnelEntryCorners = HexagonCellPrototype.GenerateSideSurfaceVertices(groundEntry, side, cellLayerElevation);
                        Vector3 sidePT = groundEntry.sidePoints[side];
                        tunnelEntryCorners.Add(new Vector3(sidePT.x, sidePT.y + cellLayerElevation / 2f, sidePT.z));
                        break;
                    }
                }

                finalResult.Add(groundEntry);
                finalResult.Add(item);
                finalResult.AddRange(newTunnelCluster);
                break;

            }

            int clusterid = allCellPrototypeClusters.Count + 1;
            HexagonCellCluster newCluster = new HexagonCellCluster(finalResult[0].GetId(), finalResult, CellClusterType.Tunnel, ClusterGroundCellLayerRule.Unset);

            newCluster.tunnelEntryCorners = tunnelEntryCorners;

            allCellPrototypeClusters.Add(newCluster);
            Debug.Log("GenerateCellClusters_Cave - newTunnelCluster member count:" + finalResult.Count);

            // CreateTunnelMeshFromCluster(newCluster, tunnelMeshParentGameObject);
        }

        public void CreateTunnelMeshFromClusters(List<GameObject> tunnelMeshParentGameObjects = null)
        {
            if (!debug_TunnelParent)
            {
                Debug.LogError("NO debug_TunnelParent");
                return;
            }

            List<HexagonCellCluster> tunnelClusters = GetPrototypeClustersOfType(CellClusterType.Tunnel);

            if (tunnelMeshParentGameObjects == null) tunnelMeshParentGameObjects = new List<GameObject>();

            if (tunnelClusters.Count == 0)
            {
                if (tunnelMeshParentGameObjects.Count > 0)
                {
                    foreach (var item in tunnelMeshParentGameObjects)
                    {
                        if (item != null) MeshUtil.ClearMeshOnGameObject(item);
                    }
                }
                return;
            }

            for (int i = 0; i < tunnelClusters.Count; i++)
            {
                Mesh tunnelMesh = HexagonCellPrototype.GenerateTunnelMeshFromFromProtoTypes(tunnelClusters[i].prototypes.FindAll(p => p.GetCellStatus() == CellStatus.UnderGround), cellLayerElevation, transform);
                if (tunnelMesh == null)
                {
                    Debug.LogError("tunnelMesh is null");
                }

                tunnelMesh.name = "Tunnel Mesh";
                tunnelMesh.RecalculateNormals();
                tunnelMesh.RecalculateBounds();

                if (tunnelMeshParentGameObjects.Count - 1 < i || tunnelMeshParentGameObjects[i] == null)
                {
                    GameObject go = MeshUtil.InstantiatePrefabWithMesh(debug_TunnelParent, tunnelMesh, transform.position);
                    tunnelMeshParentGameObjects.Add(go);
                }
                else
                {

                    MeshFilter meshFilter = tunnelMeshParentGameObjects[i].GetComponent<MeshFilter>();
                    MeshCollider meshCollider = tunnelMeshParentGameObjects[i].GetComponent<MeshCollider>();
                    meshFilter.sharedMesh = tunnelMesh;
                    meshCollider.sharedMesh = tunnelMesh;

                    tunnelClusters[i].tunnelVertexPoints = tunnelMesh.vertices.ToList();
                }
            }
        }

        public static List<GameObject> CreateTunnelGameObjectsClusters(
            List<HexagonCellCluster> tunnelClusters,
            GameObject meshObjectPrefab,
            Transform transform,
            int cellLayerElevation
        )
        {
            if (!meshObjectPrefab)
            {
                Debug.LogError("NO meshObjectPrefab");
                return null;
            }

            List<GameObject> tunnelMeshParentGameObjects = new List<GameObject>();

            for (int i = 0; i < tunnelClusters.Count; i++)
            {
                Mesh tunnelMesh = HexagonCellPrototype.GenerateTunnelMeshFromFromProtoTypes(
                    tunnelClusters[i].prototypes.FindAll(p => p.GetCellStatus() == CellStatus.UnderGround),
                    cellLayerElevation,
                    transform
                );

                if (tunnelMesh == null)
                {
                    Debug.LogError("tunnelMesh is null");
                }

                tunnelMesh.name = "Tunnel Mesh";
                tunnelMesh.RecalculateNormals();
                tunnelMesh.RecalculateBounds();

                GameObject tunnelObject = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, tunnelMesh, transform.position);
                tunnelMeshParentGameObjects.Add(tunnelObject);
            }

            return tunnelMeshParentGameObjects;
        }

        public static GameObject CreateTunnelGameObjectCluster(
            HexagonCellCluster tunnelCluster,
            GameObject meshObjectPrefab,
            Transform transform,
            int cellLayerElevation
        )
        {
            if (!meshObjectPrefab)
            {
                Debug.LogError("NO meshObjectPrefab");
                return null;
            }

            Mesh tunnelMesh = HexagonCellPrototype.GenerateTunnelMeshFromFromProtoTypes(
                tunnelCluster.prototypes.FindAll(p => p.GetCellStatus() == CellStatus.UnderGround),
                cellLayerElevation,
                transform
            );

            if (tunnelMesh == null)
            {
                Debug.LogError("tunnelMesh is null");
            }

            tunnelMesh.name = "Tunnel Mesh";
            tunnelMesh.RecalculateNormals();
            tunnelMesh.RecalculateBounds();

            Vector3 position = transform.position;
            position.y = 0;
            GameObject tunnelMeshGameObject = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, tunnelMesh, position);

            return tunnelMeshGameObject;
        }

        public void CreateTunnelMeshFromCluster(HexagonCellCluster cluster, GameObject tunnelMeshParentGameObject = null)
        {
            // temp_vertexSurfaceSets = HexagonCellPrototype.GenerateTunnelVertexPointsFromPrototypes(newCluster.prototypes, cellLayerElevation);

            // List<Mesh> tunnelMeshes = HexagonCellPrototype.GenerateTunnelMeshesFromFromProtoTypes(newCluster.prototypes.FindAll(p => p.GetCellStatus() == CellStatus.UnderGround), cellLayerElevation, transform);
            // if (tunnelMeshes != null && debug_TunnelParent != null)
            // {
            //     Transform tunnelParent = new GameObject("Tunnel System").transform;

            //     foreach (var item in tunnelMeshes)
            //     {
            //         if (item != null)
            //         {
            //             GameObject go = InstantiatePrefabWithMesh(debug_TunnelParent, item);
            //             go.transform.SetParent(tunnelParent);
            //         }
            //     }
            // }
            Mesh tunnelMesh = HexagonCellPrototype.GenerateTunnelMeshFromFromProtoTypes(cluster.prototypes.FindAll(p => p.GetCellStatus() == CellStatus.UnderGround), cellLayerElevation, transform);
            // // Mesh tunnelMesh = HexagonCellPrototype.GenerateTunnelMeshFromFromProtoTypes(newCluster.prototypes, cellLayerElevation);

            if (tunnelMesh == null)
            {
                return;
            }

            if (tunnelMeshParentGameObject != null)
            {
                MeshFilter meshFilter = tunnelMeshParentGameObject.GetComponent<MeshFilter>();
                MeshCollider meshCollider = tunnelMeshParentGameObject.GetComponent<MeshCollider>();
                tunnelMesh.name = "Tunnel Mesh";

                tunnelMesh.RecalculateNormals();
                tunnelMesh.RecalculateBounds();

                meshFilter.sharedMesh = tunnelMesh;
                meshCollider.sharedMesh = tunnelMesh;

                cluster.tunnelVertexPoints = tunnelMesh.vertices.ToList();
            }
            else if (debug_TunnelParent != null)
            {
                Transform tunnelParent = new GameObject("Tunnel System").transform;

                GameObject go = MeshUtil.InstantiatePrefabWithMesh(debug_TunnelParent, tunnelMesh, transform.position);
                go.transform.SetParent(tunnelParent);

                cluster.tunnelVertexPoints = tunnelMesh.vertices.ToList();
            }
        }


        public void GenerateCellClusters_Random(CellClusterType clusterType = CellClusterType.Other, bool excludeEdgeOfParentGrid = true)
        {
            int minSearchRadius = (int)(cellSize * cluster_radiusMultMin);
            Vector2 searchRadiusMinMax;

            if (randomizeClusterSearchRadius)
            {
                searchRadiusMinMax = new Vector2(minSearchRadius, (int)(cellSize * cluster_radiusMultMax));
            }
            else searchRadiusMinMax = new Vector2(minSearchRadius, minSearchRadius);

            Dictionary<string, List<HexagonCellPrototype>> newClusters = HexagonCellPrototype.GetRandomCellClusters(
                    GetAllPrototypesOfCellStatus(CellStatus.GenericGround),
                    searchRadiusMinMax,
                    clustersMax,
                    new Vector2(cluster_memberMin, cluster_memberMax),
                    false,
                    true,
                    excludeEdgeOfParentGrid
                    );

            if (newClusters == null)
            {
                return;
            }

            List<HexagonCellCluster> _allCellPrototypeClusters = new List<HexagonCellCluster>();
            foreach (var kvp in newClusters)
            {
                _allCellPrototypeClusters.Add(new HexagonCellCluster(kvp.Key, kvp.Value, clusterType, ClusterGroundCellLayerRule.NormalizeLayerDifference));
            }
            allCellPrototypeClusters.AddRange(_allCellPrototypeClusters);
        }



        public static List<HexagonCellPrototype> SelectCellsInRadiusOfCell(
            List<HexagonCellPrototype> cells,
            HexagonCellPrototype centerCell,
            float radius,
            int maximum = -1,
            List<CellStatus> ignoresStatus = null
        )
        {
            Vector2 centerPos = new Vector2(centerCell.center.x, centerCell.center.z);

            List<HexagonCellPrototype> selectedCells = new List<HexagonCellPrototype>();
            bool limitToMax = (maximum > 0);
            int found = 0;
            bool useIgnoreStatus = (ignoresStatus != null);
            //Iterate through each cell in the input list
            foreach (HexagonCellPrototype cell in cells)
            {
                if (useIgnoreStatus && ignoresStatus.Contains(cell.GetCellStatus())) continue;

                Vector2 cellPos = new Vector2(cell.center.x, cell.center.z);

                // Check if the distance between the cell and the center cell is within the given radius
                if (Vector2.Distance(centerPos, cellPos) <= radius)
                {
                    //If the distance is within the radius, add the current cell to the list of selected cells
                    selectedCells.Add(cell);
                    found++;
                    if (limitToMax && found >= maximum) break;
                }
            }
            //Return the list of selected cells
            return selectedCells;
        }

        public static List<HexagonCellPrototype> SelectCellsWithinInRadius_Expanding(
            List<HexagonCellPrototype> availableCells,
            HexagonCellPrototype centerCell,
            float radiusMax,
            int maximum = -1,
            List<CellStatus> ignoresStatus = null
        )
        {

            availableCells.Sort((a, b) => VectorUtil.DistanceXZ(a.center, centerCell.center).CompareTo(VectorUtil.DistanceXZ(b.center, centerCell.center)));
            Vector2 centerPos = new Vector2(centerCell.center.x, centerCell.center.z);

            List<HexagonCellPrototype> selectedCells = new List<HexagonCellPrototype>();
            bool limitToMax = (maximum > 0);
            int found = 0;
            bool useIgnoreStatus = (ignoresStatus != null);
            HashSet<string> added = new HashSet<string>();
            float currentRadius = centerCell.size;

            while (currentRadius < radiusMax)
            {
                if (limitToMax)
                {
                    currentRadius += (centerCell.size * 1.2f);
                }
                else currentRadius = radiusMax;

                foreach (HexagonCellPrototype cell in availableCells)
                {
                    if (useIgnoreStatus && ignoresStatus.Contains(cell.GetCellStatus())) continue;

                    if (added.Contains(cell.GetId())) continue;

                    Vector2 cellPos = new Vector2(cell.center.x, cell.center.z);

                    if (Vector2.Distance(centerPos, cellPos) <= currentRadius)
                    {
                        added.Add(cell.GetId());
                        selectedCells.Add(cell);
                        found++;
                        if (limitToMax && found >= maximum) break;
                    }
                }
            }

            return selectedCells;
        }

        public static HexagonCellCluster Generate_ClusterSubGridFromPrototypesByLayer(
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer,
            CellClusterType clusterType,
            bool keepAllPrototypes,
            List<CellStatus> focusCellsIgnoresStatus = null,
            HashSet<Vector2> checkGridConnectorByWorldspaceCoords = null
        )
        {
            if (prototypesByLayer == null || prototypesByLayer.Count == 0)
            {
                Debug.LogError("NO prototypesByLayer");
                return null;
            }
            List<HexagonCellPrototype> finalMembers = new List<HexagonCellPrototype>();

            foreach (var kvp in prototypesByLayer)
            {
                HexagonCellPrototype.ResetNeighbors(kvp.Value, EdgeCellType.Default);
                if (keepAllPrototypes)
                {
                    finalMembers.AddRange(kvp.Value);
                }
                else finalMembers.AddRange(kvp.Value.FindAll(p => focusCellsIgnoresStatus.Contains(p.GetCellStatus()) == false));
            }

            if (finalMembers != null && finalMembers.Count > 1)
            {
                return new HexagonCellCluster(
                        finalMembers[0].GetId(),
                        finalMembers,
                        clusterType,
                        ClusterGroundCellLayerRule.Unset);
            }
            else return null;
        }

        public static HexagonCellCluster Generate_ClusterSubGridWithinStartCellRadius(
            HexagonCellPrototype startCell,
            List<HexagonCellPrototype> availableCells,
            CellClusterType clusterType,
            Vector2 membersMinMax,
            float radius,
            bool keepAllPrototypes,
            List<CellStatus> focusCellsIgnoresStatus = null,
            HashSet<Vector2> checkGridConnectorByWorldspaceCoords = null
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = Select_PrototypeStacks_WithinStartCellRadius(
                    startCell,
                    availableCells,
                    membersMinMax,
                    radius,
                    focusCellsIgnoresStatus
                );

            if (prototypesByLayer == null || prototypesByLayer.Count == 0)
            {
                Debug.LogError("NO prototypesByLayer");
                return null;
            }

            List<HexagonCellPrototype> finalMembers = new List<HexagonCellPrototype>();

            foreach (var kvp in prototypesByLayer)
            {
                HexagonCellPrototype.ResetNeighbors(kvp.Value, EdgeCellType.Default);
                if (keepAllPrototypes)
                {
                    finalMembers.AddRange(kvp.Value);
                }
                else
                {
                    finalMembers.AddRange(kvp.Value.FindAll(p => focusCellsIgnoresStatus.Contains(p.GetCellStatus()) == false));
                }
            }

            if (finalMembers != null && finalMembers.Count > 1)
            {
                return new HexagonCellCluster(
                        startCell.GetId(),
                        finalMembers,
                        clusterType,
                        ClusterGroundCellLayerRule.Unset);
            }
            else
            {
                return null;
            }
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Select_NormalizedPrototypeSubGridByLayer(List<HexagonCellPrototype> selectedPrototypes)
        {
            Vector2Int lowest_HighestLayers = HexagonCellPrototype.GetLayerBoundsOfList(selectedPrototypes);

            Dictionary<int, List<HexagonCellPrototype>> fullStackSelectedPrototypesByLayer = Select_PrototypeStacks(selectedPrototypes);

            Dictionary<int, List<HexagonCellPrototype>> finalPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (var kvp in fullStackSelectedPrototypesByLayer)
            {
                int layer = kvp.Key;
                if (layer >= lowest_HighestLayers.x && layer <= lowest_HighestLayers.y)
                {
                    finalPrototypesByLayer.Add(layer, kvp.Value);
                }
            }

            return finalPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Select_PrototypeStacks_FromConsecutiveSearch(
            HexagonCellPrototype startCell,
            Vector2Int membersMinMax,
            List<CellStatus> ignoresStatus = null,
            CellSearchPriority searchPriority = CellSearchPriority.SideAndSideLayerNeighbors,
            bool excludeEdgeOfParentGrid = false
        )
        {
            int memberCount = UnityEngine.Random.Range(membersMinMax.x, membersMinMax.y);
            List<HexagonCellPrototype> foundGroup = HexGridPathingUtil.GetConsecutiveNeighborsCluster(startCell, memberCount, searchPriority, ignoresStatus, excludeEdgeOfParentGrid);

            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = Select_PrototypeStacks(foundGroup, ignoresStatus);

            return prototypesByLayer;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> Select_PrototypeStacks_WithinStartCellRadius(
            HexagonCellPrototype startCell,
            List<HexagonCellPrototype> availableCells,
            Vector2 membersMinMax,
            float radius,
            List<CellStatus> ignoresStatus = null
        )
        {
            int memberCount = (int)UnityEngine.Random.Range(membersMinMax.x, membersMinMax.y);

            List<HexagonCellPrototype> foundGroup = SelectCellsWithinInRadius_Expanding(availableCells, startCell, radius, memberCount, ignoresStatus);

            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = Select_PrototypeStacks(foundGroup, ignoresStatus);

            return prototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Select_PrototypeStacks(
            List<HexagonCellPrototype> prototypes,
            List<CellStatus> ignoresStatus = null
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (var prototype in prototypes)
            {
                List<HexagonCellPrototype> stackCells = HexagonCellPrototype.GetAllCellsInLayerStack(prototype);
                foreach (var item in stackCells)
                {
                    if (prototypesByLayer.ContainsKey(item.GetLayer()) == false)
                    {
                        prototypesByLayer.Add(item.GetLayer(), new List<HexagonCellPrototype>());
                    }
                    prototypesByLayer[item.GetLayer()].Add(item);
                }
            }
            return prototypesByLayer;
        }

        public static HexagonCellCluster Generate_ClusterFromStartCell_Radius(
            HexagonCellPrototype startCell,
            List<HexagonCellPrototype> availableCells,
            Vector2 membersMinMax,
            float radius,
            CellClusterType clusterType = CellClusterType.Other,
            List<CellStatus> ignoresStatus = null,
            bool excludeEdgeOfParentGrid = true
        )
        {
            int memberCount = (int)UnityEngine.Random.Range(membersMinMax.x, membersMinMax.y);

            List<HexagonCellPrototype> foundGroup = SelectCellsWithinInRadius_Expanding(availableCells, startCell, radius, memberCount, ignoresStatus);

            if (foundGroup != null && foundGroup.Count > 1)
            {
                return new HexagonCellCluster(
                        startCell.GetId(),
                        foundGroup,
                        clusterType,
                        ClusterGroundCellLayerRule.Unset);
            }
            else
            {
                return null;
            }
        }

        public static HexagonCellCluster Generate_ClusterFromStartCell(
            HexagonCellPrototype startCell,
            int memberCount,
            CellClusterType clusterType = CellClusterType.Other,
            List<CellStatus> ignoresStatus = null,
            bool excludeEdgeOfParentGrid = true,
            CellSearchPriority searchPriority = CellSearchPriority.SideAndSideLayerNeighbors
        )
        {
            // Debug.LogError("Generate_ClusterFromStartCell desired memberCount: " + memberCount);

            List<HexagonCellPrototype> foundGroup = HexGridPathingUtil.GetConsecutiveNeighborsCluster(startCell, memberCount, searchPriority, ignoresStatus, excludeEdgeOfParentGrid);
            if (foundGroup != null && foundGroup.Count > 1)
            {
                return new HexagonCellCluster(
                        startCell.GetId(),
                        foundGroup,
                        clusterType,
                        ClusterGroundCellLayerRule.Unset);
            }
            else
            {
                return null;
            }
        }

        public void GenerateCellClusters_GridEdge(List<HexagonCellPrototype> gridEdges)
        {
            List<HexagonCellPrototype> groundGridEdges = gridEdges.FindAll(c => c.IsEntry() == false && c.IsPath() == false && c.IsGround());
            if (allCellPrototypeClusters == null) allCellPrototypeClusters = new List<HexagonCellCluster>();
            allCellPrototypeClusters.Add(new HexagonCellCluster(groundGridEdges[0].GetId(), groundGridEdges, CellClusterType.Edge, ClusterGroundCellLayerRule.NormalizeLayerDifference));
        }


        public void GenerateMicroGridFromClusters(int layers, bool useEvenTopLayer)
        {
            HashSet<string> hostIds = new HashSet<string>();
            Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>> _cellPrototypesByLayer_X4_ByCluster = new Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>>();

            foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
            {
                // Skip path for now
                // if (cluster.clusterType == CellClusterType.Path) continue;

                List<HexagonCellPrototype> filteredHosts = new List<HexagonCellPrototype>();
                int duplicatesFound = 0;
                int highestGroundLayer = 0;

                foreach (HexagonCellPrototype item in cluster.prototypes)
                {
                    if (hostIds.Contains(item.uid) == false)
                    {
                        hostIds.Add(item.uid);
                        filteredHosts.Add(item);

                        if (highestGroundLayer < item.GetLayer()) highestGroundLayer = item.GetLayer();
                    }
                    else
                    {
                        duplicatesFound++;
                        // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                    }
                }

                int topLayerTarget = highestGroundLayer + layers;

                Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonGrid.Generate_ProtoypeGrid_FromHosts(filteredHosts, 4, layers, cellLayerElevation, gameObject.transform, true, useEvenTopLayer, topLayerTarget);
                foreach (var kvp in newPrototypesByLayer)
                {
                    HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);

                    // HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform, 0.3f);
                    // HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Default);
                }
                cluster.prototypesByLayer_X4 = newPrototypesByLayer;

                _cellPrototypesByLayer_X4_ByCluster.Add(cluster, newPrototypesByLayer);
            }
            cellPrototypesByLayer_X4_ByCluster = _cellPrototypesByLayer_X4_ByCluster;
        }

        public void GenerateMicroGridFromHosts(List<HexagonCellPrototype> allHosts, int cellSize, int cellLayers, bool useEvenTopLayer)
        {
            //TEMP
            HashSet<string> hostIds = new HashSet<string>();
            // foreach (var item in allHosts)
            // {
            //     if (hostIds.Contains(item.id) == false)
            //     {
            //         hostIds.Add(item.id);
            //     }
            //     else
            //     {
            //         Debug.LogError("Duplicate host id found: " + item.id);
            //     }
            // }
            List<HexagonCellPrototype> filteredHosts = new List<HexagonCellPrototype>();
            int duplicatesFound = 0;
            int highestGroundLayer = 0;

            foreach (var item in allHosts)
            {
                if (hostIds.Contains(item.uid) == false)
                {
                    hostIds.Add(item.uid);
                    filteredHosts.Add(item);

                    if (highestGroundLayer < item.GetLayer()) highestGroundLayer = item.GetLayer();
                }
                else
                {
                    duplicatesFound++;
                    // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                }
            }

            if (duplicatesFound > 0) Debug.LogError("GenerateMicroGridFromHosts - Duplicate hosts found: " + duplicatesFound);
            Debug.Log("GenerateMicroGridFromHosts - allHosts: " + allHosts.Count + ", filteredHosts: " + filteredHosts.Count);

            int topLayerTarget = highestGroundLayer + cellLayers;

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonGrid.Generate_ProtoypeGrid_FromHosts(filteredHosts, 4, cellLayers, cellLayerElevation, gameObject.transform, true, useEvenTopLayer, topLayerTarget);
            int count = 0;
            foreach (var kvp in newPrototypesByLayer)
            {
                HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);

                // HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform, 0.3f);
                // List<HexagonCellPrototype> newGridEdges = HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Default);

                count += kvp.Value.Count;
            }

            cellPrototypes_X4_ByLayer = newPrototypesByLayer;
            totalPrototypesX4 = count;

            if (cellSize == 4)
            {
                int expectedCount = filteredHosts.Count * 7;

                if (count != expectedCount)
                {
                    Debug.LogError("GenerateMicroGridFromHosts - Hosts: " + filteredHosts.Count + ", sub cells: " + count + ", expectedCount: " + expectedCount);

                }
                else Debug.Log("GenerateMicroGridFromHosts - Hosts: " + filteredHosts.Count + ", sub cells: " + count + ", expectedCount: " + expectedCount);
            }
        }


        public void GenerateCells(bool force, bool regeneratePrototypes = false)
        {
            if (force || allCells == null || allCells.Count == 0)
            {
                // Debug.Log("Generate Cells = prototypesByLayer:" + prototypesByLayer.Count); //+ ", cellPrototypesByLayer_V2: " + cellPrototypesByLayer_V2.Count);

                if (regeneratePrototypes || ArePrototypesInitialized() == false)
                {
                    if (randomizeCellLayers) cellLayers = CalculateRandomLayers();
                    if (!_isClusterParent) GeneratePrototypeGridsByLayer();
                }

                // Generate_HexagonCells(cellPrototypesByLayer, HexagonCell_prefab);
                allCellsByLayer = GenerateHexagonCellsFromPrototypes(cellPrototypesByLayer_V2, HexagonCell_prefab, gameObject.transform);

                List<HexagonCell> _allCells = new List<HexagonCell>();
                foreach (var kvp in allCellsByLayer)
                {
                    HexCellUtil.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(cellSize));
                    _allCells.AddRange(kvp.Value);
                }
                allCells = _allCells;

                // if (cellPrototypes_X4_ByLayer != null && cellPrototypes_X4_ByLayer.Count > 0)
                // {
                //     allCellsByLayer_X4 = GenerateHexagonCellsFromPrototypes(cellPrototypes_X4_ByLayer, HexagonCell_prefab, gameObject.transform);
                //     List<HexagonCell> _allCell_X4 = new List<HexagonCell>();
                //     foreach (var kvp in allCellsByLayer_X4)
                //     {
                //         HexagonCell.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(4));
                //         HexagonCell.GetEdgeCells(kvp.Value);
                //         _allCell_X4.AddRange(kvp.Value);
                //     }
                //     allCells_X4 = _allCell_X4;
                // }

                if (allCellPrototypeClusters != null && allCellPrototypeClusters.Count > 0)
                {
                    Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> _allCellsByLayer_X4_ByCluster = new Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>>();

                    foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
                    {
                        Dictionary<int, List<HexagonCell>> newPrototypesByLayer_X4 = GenerateHexagonCellsFromPrototypes(cluster.prototypesByLayer_X4, HexagonCell_prefab, gameObject.transform, "Cells_X4");
                        foreach (var kvp in newPrototypesByLayer_X4)
                        {
                            HexCellUtil.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(4));
                            HexCellUtil.GetEdgeCells(kvp.Value);
                        }
                        _allCellsByLayer_X4_ByCluster.Add(cluster, newPrototypesByLayer_X4);
                    }
                    allCellsByLayer_X4_ByCluster = _allCellsByLayer_X4_ByCluster;
                }

            }
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            if (showBounds)
            {

                Gizmos.color = Color.magenta;
                for (int j = 0; j < cornerPoints.Length; j++)
                {
                    Gizmos.DrawSphere(cornerPoints[j], 1);
                }

                VectorUtil.DrawHexagonPointLinesInGizmos(cornerPoints);

                // List<Vector3> centerPoints = HexGridUtil.GenerateHexGridCenterPoints(transform.position, 1.3f, radius, null, transform);
                // Gizmos.color = Color.magenta;
                // foreach (var item in centerPoints)
                // {
                //     Gizmos.DrawSphere(item, 0.4f);
                //     // Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(item, radius);
                //     // ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(corners);
                // }


                // List<Vector3> sideNeighborCenterPoints = HexagonCellPrototype.GenerateHexagonCenterPoints(transform.position, radius, false, false);

                // Gizmos.color = Color.magenta;
                // foreach (var item in sideNeighborCenterPoints)
                // {
                //     Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(item, radius);
                //     ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(corners);
                // }

                // Gizmos.DrawWireSphere(transform.position, (radius * 3) * 3);
            }

            // if (showNeighborGridEdges)
            // {
            //     List<HexagonCellPrototype> gridEdgeCellsWithNeighborGridNeighbor = GetAllPrototypeEdgesOfType(EdgeCellType.Default).FindAll(e => e.neighbors.Any(n => n.parentId != e.parentId));

            //     HexagonCellPrototype.DrawHexagonCellPrototypes(gridEdgeCellsWithNeighborGridNeighbor, transform, GridFilter_Type.All);
            // }

            if (showTunnelClusters)
            {
                if (allCellPrototypeClusters != null && allCellPrototypeClusters.Count > 0)
                {

                    foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
                    {
                        if (cluster.clusterType == CellClusterType.Tunnel)
                        {

                            if (cluster.tunnelEntryCorners != null)
                            {
                                foreach (Vector3 pt in cluster.tunnelEntryCorners)
                                {
                                    Gizmos.color = Color.white;
                                    Gizmos.DrawSphere(pt, 0.1f);
                                }
                            }

                            Color selectedColor = _temp_randomColors != null ? _temp_randomColors[UnityEngine.Random.Range(0, _temp_randomColors.Count)] : Color.grey;
                            Gizmos.color = selectedColor;

                            foreach (var prototype in cluster.prototypes)
                            {
                                if (prototype.isTunnelGroundEntry && prototype.IsGround())
                                {
                                    Gizmos.color = Color.yellow;

                                    Gizmos.DrawSphere(prototype.center, 1.5f);
                                    Gizmos.DrawWireSphere(prototype.center, (prototype.size * 0.73f));
                                    Gizmos.DrawWireSphere(prototype.center, (prototype.MaxBoundsRadius()));

                                    // List<Vector3> pts = VectorUtil.GenerateDottedLine(prototype.cornerPoints.ToList(), 8);
                                    // foreach (var item in pts)
                                    // {
                                    //     Gizmos.color = Color.yellow;
                                    //     Gizmos.DrawWireSphere(item, 0.66f);
                                    // }

                                    // List<Vector3> allPTS = new List<Vector3>();
                                    // List<Vector3> sideCorners = HexagonCellPrototype.GetCornersOnSide(prototype, 1);
                                    // int density = 12;
                                    // List<Vector3> dottedEdgeBottom = VectorUtil.GenerateDottedLine(sideCorners, density);
                                    // List<Vector3> dottedEdgeTop = VectorUtil.DuplicatePositionsToNewYPos(dottedEdgeBottom, 3);

                                    // Vector3[] sideCornersInner = VectorUtil.DuplicateCornerPointsTowardsCenter(prototype.center, sideCorners[0], sideCorners[1], 3f);
                                    // List<Vector3> dottedEdgeBottomInner = VectorUtil.GenerateDottedLine(sideCornersInner.ToList(), density);

                                    // allPTS.AddRange(dottedEdgeBottom);
                                    // allPTS.AddRange(dottedEdgeTop);
                                    // allPTS.AddRange(dottedEdgeBottomInner);

                                    // foreach (var item in allPTS)
                                    // {
                                    //     Gizmos.color = Color.yellow;
                                    //     Gizmos.DrawSphere(item, 0.3f);
                                    //     Gizmos.DrawWireSphere(item, 1f);
                                    // }



                                    continue;
                                }
                                Gizmos.color = selectedColor;

                                if (prototype.isTunnelStart)
                                {
                                    Gizmos.DrawSphere(prototype.center, 1f);
                                    continue;
                                }

                                Gizmos.DrawWireSphere(prototype.center, 0.4f);
                            }

                            // foreach (Vector3 tunnelPoint in cluster.tunnelVertexPoints)
                            // {
                            //     //             Gizmos.DrawSphere(vert, 0.66f);

                            // }

                            // Dictionary<Vector3, List<List<Vector3>>> vertexSurfaceSets = HexagonCellPrototype.GenerateTunnelVertexPointsFromPrototypes(cluster.prototypes, cellLayerElevation);
                            // Dictionary<Vector3, List<List<Vector3>>> vertexSurfaceSets = HexagonCellPrototype.GenerateTunnelVertexPointsFromPrototypes(cluster.prototypes, cellLayerElevation);

                            // foreach (var kvp in temp_vertexSurfaceSets)
                            // {
                            //     Vector3 meshFacingPosition = kvp.Key;
                            //     Gizmos.color = Color.white;
                            //     Gizmos.DrawSphere(meshFacingPosition, 0.5f);

                            //     foreach (List<Vector3> surface in kvp.Value)
                            //     {
                            //         Gizmos.color = _temp_randomColors != null ? _temp_randomColors[UnityEngine.Random.Range(0, _temp_randomColors.Count)] : Color.yellow;
                            //         foreach (var vert in surface)
                            //         {
                            //             Gizmos.DrawSphere(vert, 0.66f);

                            //             foreach (var vertB in surface)
                            //             {
                            //                 if (vert == vertB) continue;

                            //                 Gizmos.DrawLine(vert, vertB);
                            //             }
                            //         }
                            //     }
                            // }

                            // List<Vector3> allVertices = HexagonCellPrototype.GenerateTunnelVertexPointsFromProtoTypes(cluster.prototypes, cellLayerElevation);
                            // foreach (var vert in allVertices)
                            // {
                            //     Gizmos.color = Color.blue;
                            //     Gizmos.DrawSphere(vert, 0.33f);
                            // }
                        }

                    }


                }
            }


            if (showGrid)
            {
                // DrawHexagonCellPrototypes(cellPrototypesByLayer);
                if (gridFilter_Level == GridFilter_Level.All || gridFilter_Level == GridFilter_Level.HostCells)
                {
                    HexagonCellPrototype.DrawHexagonCellPrototypeGrid(cellPrototypesByLayer_V2, gameObject.transform, gridFilter_Type);
                }
                if (gridFilter_Level == GridFilter_Level.All || gridFilter_Level == GridFilter_Level.MicroCells)
                {
                    HexagonCellPrototype.DrawHexagonCellPrototypeGrid(cellPrototypes_X4_ByLayer, gameObject.transform, gridFilter_Type);

                    foreach (var cluster in allCellPrototypeClusters)
                    {
                        HexagonCellPrototype.DrawHexagonCellPrototypeGrid(cluster.prototypesByLayer_X4, gameObject.transform, gridFilter_Type);
                    }
                }
                // HexagonCellPrototype centerHex = new HexagonCellPrototype(transform.position, 24);
                // foreach (Vector3 side in centerHex.sidePoints)
                // {   
                //     Vector3 newCenter =  new Vector3( centerHex.center.x, centerHex.center.z)
                //     HexagonCellPrototype newHex = new HexagonCellPrototype(transform.position, 24);

                // }
                // HexagonGenerator.GenerateHexagonPoints(transform.position,24);
            }
            else
            {
                // List<int> cornersToUse = new List<int>()
                // {
                //     (int)HexagonCorner.FrontA,
                //     (int)HexagonCorner.FrontB,
                //     // (int)HexagonCorner.FrontLeftA,
                //     // (int)HexagonCorner.FrontLeftB,
                //     // (int)HexagonCorner.FrontRightA,
                //     // (int)HexagonCorner.FrontRightB
                //     (int)HexagonCorner.BackA,
                //     (int)HexagonCorner.BackB,

                // };
                // List<Vector3> centerPts = HexagonCellPrototype.GenerateHexagonCenterPoints(transform.position, cellSize, cornersToUse, true);
                // foreach (var item in centerPts)
                // {
                //     HexagonCellPrototype hex = new HexagonCellPrototype(item, cellSize);
                //     HexagonCellPrototype.DrawHexagonCellPrototype(hex, 0.4f);
                // }
                // HexagonCellPrototype centerHex = new HexagonCellPrototype(transform.position, cellSize);
            }

            if (showCells != ShowCellState.None)
            {
                if (allCells == null || allCells.Count == 0) return;

                bool showAll = showCells == ShowCellState.All;

                foreach (var cell in allCells)
                {
                    if (cell == null) continue;

                    bool show = false;
                    float rad = 4f;
                    Gizmos.color = Color.black;

                    if ((showAll || showCells == ShowCellState.Entry) && cell.isEntryCell)
                    {
                        Gizmos.color = Color.yellow;
                        show = true;
                    }
                    else if ((showAll || showCells == ShowCellState.Edge) && cell.isEdgeCell)
                    {
                        Gizmos.color = Color.red;
                        show = true;
                    }
                    else if ((showAll || showCells == ShowCellState.Path) && cell.isPathCell)
                    {
                        Gizmos.color = Color.green;
                        show = true;
                    }
                    if (showAll || show) Gizmos.DrawSphere(cell.transform.position, rad);
                }
            }
        }



        // public static List<Vector3> GenerateHexagonCenterPoints(Vector3 center, int size)
        // {
        //     HexagonCellPrototype centerHex = new HexagonCellPrototype(center, size);
        //     List<Vector3> results = new List<Vector3>();
        //     results.Add(center);

        //     List<Vector3>[] hexagonSideCenterPoints = new List<Vector3>[6];
        //     for (int i = 0; i < 6; i++)
        //     {
        //         Vector3 sidePoint = Vector3.Lerp(centerHex.cornerPoints[i], centerHex.cornerPoints[(i + 1) % 6], 0.5f);
        //         Vector3 direction = (sidePoint - center).normalized;
        //         float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
        //         int currentSide = (i + 5) % 6;

        //         hexagonSideCenterPoints[currentSide] = new List<Vector3>();

        //         int row = 1;
        //         sidePoint = center + direction * ((edgeDistance * 2f) * row);
        //         hexagonSideCenterPoints[currentSide].Add(sidePoint);
        //         results.Add(sidePoint);
        //         row++;
        //     }

        //     List<Vector3>[] hexagonCornerCenterPoints = new List<Vector3>[6];
        //     for (int i = 0; i < 6; i++)
        //     {
        //         float angle = 60f * i;
        //         float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
        //         float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

        //         Vector3 cornerPoint = new Vector3(x, center.y, z);
        //         Vector3 direction = (cornerPoint - center).normalized;
        //         float edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

        //         hexagonCornerCenterPoints[i] = new List<Vector3>();

        //         int row = 1;

        //         cornerPoint = center + direction * ((edgeDistance * 3f) * row);
        //         hexagonCornerCenterPoints[i].Add(cornerPoint);
        //         results.Add(cornerPoint);

        //     }

        //     return results;
        // }

        // public static List<Vector3> GenerateHexagonCenterPoints(Vector3 center, int size, int rows)
        // {
        //     HexagonCellPrototype centerHex = new HexagonCellPrototype(center, size);
        //     List<Vector3> results = new List<Vector3>();
        //     List<Vector3>[] hexagonSideCenterPoints = new List<Vector3>[6];
        //     for (int i = 0; i < 6; i++)
        //     {
        //         Vector3 sidePoint = Vector3.Lerp(centerHex.cornerPoints[i], centerHex.cornerPoints[(i + 1) % 6], 0.5f);
        //         Vector3 direction = (sidePoint - center).normalized;
        //         float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
        //         int currentSide = (i + 5) % 6;

        //         hexagonSideCenterPoints[currentSide] = new List<Vector3>();

        //         int row = 1;
        //         do
        //         {
        //             sidePoint = center + direction * ((edgeDistance * 2f) * row);
        //             hexagonSideCenterPoints[currentSide].Add(sidePoint);
        //             results.Add(sidePoint);
        //             row++;
        //         } while (row < rows);
        //     }

        //     List<Vector3>[] hexagonCornerCenterPoints = new List<Vector3>[6];
        //     for (int i = 0; i < 6; i++)
        //     {
        //         float angle = 60f * i;
        //         float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
        //         float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

        //         Vector3 cornerPoint = new Vector3(x, center.y, z);
        //         Vector3 direction = (cornerPoint - center).normalized;
        //         float edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

        //         hexagonCornerCenterPoints[i] = new List<Vector3>();

        //         int row = 1;
        //         do
        //         {
        //             cornerPoint = center + direction * ((edgeDistance * 3f) * row);
        //             hexagonCornerCenterPoints[i].Add(cornerPoint);
        //             results.Add(cornerPoint);
        //             row++;
        //         } while (row < rows);
        //     }

        //     return results;
        // }

        public void CreateMicroCellClusterPrototypesFromHosts(HexagonCell parentCell, List<HexagonCell> cellsToAsign, int layers = -1)
        {
            // Assign child cells parent id
            string parentCellId = parentCell.id;
            foreach (var cell in cellsToAsign)
            {
                cell.SetClusterCellParentId(parentCellId);
            }

            // Consolidate host cells 
            List<HexagonCell> newClusteredCells = new List<HexagonCell>();
            newClusteredCells.Add(parentCell);
            newClusteredCells.AddRange(cellsToAsign);

            allClusteredCells = newClusteredCells;

            if (layers != -1)
            {
                cellLayers = layers;
            }
            else if (randomizeCellLayers) cellLayers = CalculateRandomLayers();

            // cellPrototypesByLayer = Generate_ClusterOfMicroCellGridProtoypesFromHosts(parentCell, allClusteredCells, cellLayers, cellLayerElevation);
            cellPrototypesByLayer_V2 = HexagonGrid.Generate_MicroCellGridProtoypes_FromHosts(parentCell, allClusteredCells, cellLayers, cellLayerElevation);
        }

        public bool InitializeMicroClusterGrid(HexagonCell parentCell, List<HexagonCell> allUnassisngedCells, int cellLayers, int cellLayerElevation = 4)
        {
            if (parentCell == null)
            {
                Debug.LogError("No parentCell found");
                return false;
            }

            // Get child cells
            List<HexagonCell> childCells = HexagonCell.GetChildrenForMicroClusterParent(parentCell);
            if (childCells == null || childCells.Count == 0)
            {
                Debug.LogError("No childCells found");
                return false;
            }

            allClusteredCells.Add(parentCell);
            allClusteredCells.AddRange(childCells);

            // cellPrototypesByLayer = Generate_ClusterOfMicroCellGridProtoypesFromHosts(parentCell, childCells, cellLayers, cellLayerElevation);
            cellPrototypesByLayer_V2 = HexagonGrid.Generate_MicroCellGridProtoypes_FromHosts(parentCell, childCells, cellLayers, cellLayerElevation);
            return true;
        }


        #region STATIC METHODS
        public static string PrintHexCellData(IHexCell hexCell) => "id: " + hexCell.GetId() + "\n uid: " + hexCell.Get_Uid() + "\n center: " + hexCell.GetPosition();
        public static void LogHexCellData(IHexCell hexCell) => Debug.Log("HexCell Data - id: " + hexCell.GetId() + "\n uid: " + hexCell.Get_Uid() + "\n center: " + hexCell.GetPosition());
        public static void LogHexCellData(List<IHexCell> hexCells)
        {
            foreach (var item in hexCells)
            {
                LogHexCellData(item);
            }
        }

        private static Dictionary<int, List<HexagonCell>> GenerateHexagonCellsFromPrototypes(Dictionary<int, List<HexagonCellPrototype>> cellPrototypesByLayer, GameObject cellPrefab, Transform transform, string folderName = "Cells")
        {
            Dictionary<int, List<HexagonCell>> newCellsByLayer = new Dictionary<int, List<HexagonCell>>();

            Transform folder = new GameObject(folderName).transform;
            folder.transform.SetParent(transform);
            List<HexagonCell> prevLayerHexagonCells = new List<HexagonCell>();

            // Dictionary<string, Transform> parentFolders = new Dictionary<string, Transform>();

            foreach (var kvp in cellPrototypesByLayer)
            {
                int layer = kvp.Key;
                List<HexagonCellPrototype> prototypes = kvp.Value;

                Transform layerFolder = new GameObject("Layer_" + layer).transform;
                layerFolder.transform.SetParent(folder);

                List<HexagonCell> newHexagonCells = new List<HexagonCell>();

                foreach (HexagonCellPrototype cellPrototype in prototypes)
                {
                    // Ignore disposable prototypes
                    if (cellPrototype.GetCellStatus() == CellStatus.Remove || cellPrototype.GetCellStatus() == CellStatus.UnderGround) continue;

                    Vector3 pointPos = cellPrototype.center;

                    GameObject newTile = Instantiate(cellPrefab, pointPos, Quaternion.identity);
                    HexagonCell hexCell = newTile.GetComponent<HexagonCell>();
                    hexCell._cornerPoints = cellPrototype.cornerPoints;
                    hexCell.id = cellPrototype.id;
                    hexCell.SetParentCellId(cellPrototype.parentId);

                    hexCell.name = "Cell_[" + cellPrototype.id + "-L" + cellPrototype.layer + "] ";
                    if (hexCell.GetParenCellId() != null) hexCell.name = cellPrototype.id + "-L" + cellPrototype.layer;

                    hexCell.SetSize(cellPrototype.size);
                    hexCell.SetGridLayer(layer);

                    if (cellPrototype.IsEdge()) hexCell.SetEdgeCell(true, EdgeCellType.Default);
                    if (cellPrototype.IsPath()) hexCell.SetPathCell(true);

                    if (cellPrototype.isGridHost)
                    {
                        hexCell.SetGridHost(true);
                        if (cellPrototype.clusterParent != null)
                        {
                            cellPrototype.clusterParent.cells.Add(hexCell);
                        }
                    }

                    hexCell.SetCellStatus(cellPrototype.GetCellStatus());

                    hexCell.isGroundRamp = cellPrototype.isGroundRamp;

                    // hexCell._vertexIndices = cellPrototype._vertexIndices;
                    // hexCell._vertexIndicesBySide = cellPrototype._vertexIndicesBySide;

                    if (layer > 0)
                    {
                        for (int i = 0; i < prevLayerHexagonCells.Count; i++)
                        {
                            if (prevLayerHexagonCells[i].GetLayer() < hexCell.GetLayer() && prevLayerHexagonCells[i].id == cellPrototype.bottomNeighborId)
                            {
                                hexCell._neighbors.Add(prevLayerHexagonCells[i]);
                                hexCell.SetBottomNeighbor(prevLayerHexagonCells[i]); // set bottom neighbor

                                prevLayerHexagonCells[i]._neighbors.Add(hexCell);
                                prevLayerHexagonCells[i].SetTopNeighbor(hexCell); //Set top neighbor
                            }
                        }
                    }
                    newHexagonCells.Add(hexCell);

                    // if (cellPrototype.parentId != "" && cellPrototype.parentId != null)
                    // {

                    //     if (parentFolders.ContainsKey(cellPrototype.parentId) == false)
                    //     {
                    //         parentFolders.Add(cellPrototype.parentId, new GameObject("Host-" + cellPrototype.parentId).transform);
                    //         parentFolders[cellPrototype.parentId].SetParent(layerFolder);
                    //     }
                    //     hexCell.transform.SetParent(parentFolders[cellPrototype.parentId]);
                    // }


                    hexCell.transform.SetParent(layerFolder);
                }

                newCellsByLayer.Add(layer, newHexagonCells);
                prevLayerHexagonCells = newHexagonCells;
            }

            return newCellsByLayer;
        }






        #endregion
    }

}
