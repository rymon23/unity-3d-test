using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{

    public interface IHexCell
    {
        public string GetId();
        public string Get_Uid();
        public int GetSize();
        public int GetLayer();
        public bool IsEdge();
        public bool IsEntry();
        public bool IsPath();
        public bool IsGround();
        public bool IsGridHost();
        public void SetGridHost(bool enable);
        public CellStatus GetCellStatus();
        public EdgeCellType GetEdgeCellType();
        public Vector3 GetPosition();
        public Vector3[] GetCorners();
        public Vector3[] GetSides();
        // public List<IHexCell> GetNeighbors();
    }

    public enum CellClusterType { Path, Edge, Outpost, Other }

    public enum GridFilter_Level { None, All, HostCells, MicroCells, }
    public enum GridFilter_Type { None, All, Edge, Path, Ground, Entrance, Cluster, Removed }

    public enum GridPreset
    {
        Unset = 0,
        Outpost,
        Town,
        City,
    }

    /// <summary>
    /// 
    ///  Outpost Vars 
    ///      min/max hostCells in a outpost
    ///       
    ///      min/ max outposts alowed in WorldArea
    ///     
    /// 
    /// </summary>

    public class HexagonCellManager : MonoBehaviour
    {
        [Header("Cell Grid Settings")]
        [Range(4, 648)] public int radius = 72;
        [Range(2, 128)][SerializeField] private int cellSize = 12;
        [SerializeField] private bool useCorners;
        [SerializeField] private float centerPosYOffset = 0;
        [SerializeField] private GridPreset gridPreset;
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

        [Header("Cluster Settings")]
        [SerializeField] private bool enableClusters = true;
        [Range(1, 24)][SerializeField] private int clustersMax = 2;
        [Range(1.5f, 6f)][SerializeField] private float cluster_radiusMultMin = 1.5f;
        [Range(2f, 12f)][SerializeField] private float cluster_radiusMultMax = 3f;
        [SerializeField] private bool randomizeClusterSearchRadius;
        // [Range(2, 24)][SerializeField] private int cluster_memberMin = 2;
        [Range(2, 24)][SerializeField] private int cluster_memberMax = 7;
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
            cornerPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, radius);
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
        public List<HexagonCellPrototype> GetAllPrototypesOfCellStatus(CellStatus status) => allPrototypesList.FindAll(c => c.cellStatus == status);

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
        [SerializeField] private GridFilter_Level gridFilter_Level;
        [SerializeField] private GridFilter_Type gridFilter_Type;
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

                if (cellSize % 2 != 0) cellSize += 1;
                _cellSize = cellSize;

                int cellRadiusMult = cellSize * 3;
                if (cellSize * 3 > radius) radius = cellSize * 3;

                int remmainder = radius % cellRadiusMult;
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

        public void GeneratePrototypeGridsByLayer()
        {
            Vector3 centerPos = gameObject.transform.position;
            centerPos.y += centerPosYOffset;

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(centerPos, radius, cellSize, cellLayers, cellLayerElevation, null, 0, gameObject.transform, null, useGridErosion);
            List<HexagonCellPrototype> _allPrototypesList = new List<HexagonCellPrototype>();

            int count = 0;
            foreach (var kvp in newPrototypesByLayer)
            {
                count += kvp.Value.Count;
                HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform);

                _allPrototypesList.AddRange(kvp.Value.FindAll(p => p.IsRemoved() == false));
                //     // allPrototypesList.AddRange(kvp.Value.FindAll(p => p.IsDisposable() == false));

                HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);
            }

            cellPrototypesByLayer_V2 = newPrototypesByLayer;
            allPrototypesList = _allPrototypesList;
            totalPrototypes = count;

            allCellPrototypeClusters = new List<HexagonCellCluster>();
            // Debug.Log("GenerateGridsByLayer - totalPrototypes: " + totalPrototypes + ", allPrototypesList: " + allPrototypesList.Count);
        }

        public (List<HexagonCellPrototype>, List<HexagonCellPrototype>) GeneratePrototypeGridEntryAndPaths()
        {
            List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> entryPrototypes = new List<HexagonCellPrototype>();
            bool pathIgnoresEdgeCells = (useGridErosion || gridPreset == GridPreset.Outpost);

            if (gridPreset == GridPreset.City)
            {
                List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Grid);
                entryPrototypes = HexagonCellPrototype.PickRandomEntryFromGridEdges(gridEdges, 3, true);

                Debug.Log("gridEdges: " + gridEdges.Count + ", entryPrototypes: " + entryPrototypes.Count + ", allPrototypesList: " + allPrototypesList.Count);

                path = HexagonCellPrototype.GenerateRandomPath(entryPrototypes, allPrototypesList, gameObject.transform.position, pathIgnoresEdgeCells);

                GenerateCellClusters_GridEdge(gridEdges);
            }
            else if (gridPreset == GridPreset.Outpost)
            {
                GenerateCellClusters_Random(CellClusterType.Outpost);
                path = PathBetweenClusters();
            }

            if (path.Count > 0)
            {
                int clusterid = allCellPrototypeClusters.Count == 0 ? 0 : allCellPrototypeClusters.Count + 1;
                allCellPrototypeClusters.Add(new HexagonCellCluster(clusterid, path, CellClusterType.Path));
            }

            return (path, entryPrototypes);
        }

        #endregion


        public void EvaluatePrototypeGridEdges()
        {
            foreach (var kvp in cellPrototypesByLayer_V2)
            {
                HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);
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
                    item.cellStatus = CellStatus.Remove;
                }
            }

            EvaluatePrototypeGridEdges();

            return unusedPrototypes;
        }

        public List<HexagonCellPrototype> PathBetweenClusters()
        {
            /// <summary> TODO:
            ///  Iterate over the clusters 
            ///     for each cluster, path to the other clusters 
            ///         get a cluster edge cell and path to a cluster edge cell in the other cluster 
            ///             prefer the cells with the most neighbors  
            /// </summary>
            List<HexagonCellPrototype> pathFocusCells = new List<HexagonCellPrototype>();
            foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
            {
                if (cluster.clusterType == CellClusterType.Path) continue;
                List<HexagonCellPrototype> edges = cluster.GetClusterEdgeCells();
                if (edges.Count > 0) pathFocusCells.Add(edges[0]);
            }
            return HexagonCellPrototype.GenerateRandomPathBetweenCells(pathFocusCells, false, false);
        }

        public void GenerateCellClusters_Random(CellClusterType clusterType = CellClusterType.Other)
        {
            int minSearchRadius = (int)(cellSize * cluster_radiusMultMin);
            Vector2 searchRadiusMinMax;

            if (randomizeClusterSearchRadius)
            {
                searchRadiusMinMax = new Vector2(minSearchRadius, (int)(cellSize * cluster_radiusMultMax));
            }
            else searchRadiusMinMax = new Vector2(minSearchRadius, minSearchRadius);


            Dictionary<int, List<HexagonCellPrototype>> newClusters = HexagonCellPrototype.GetRandomCellClusters(
                    GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(c => c.IsPreAssigned() == false),
                    searchRadiusMinMax,
                    clustersMax,
                    cluster_memberMax
                    );

            List<HexagonCellCluster> _allCellPrototypeClusters = new List<HexagonCellCluster>();
            foreach (var kvp in newClusters)
            {
                int clusterid = allCellPrototypeClusters.Count == 0 ? 0 : allCellPrototypeClusters.Count + kvp.Key;
                _allCellPrototypeClusters.Add(new HexagonCellCluster(clusterid, kvp.Value, clusterType));
            }
            allCellPrototypeClusters.AddRange(_allCellPrototypeClusters);
        }

        public void GenerateCellClusters_GridEdge(List<HexagonCellPrototype> gridEdges)
        {
            List<HexagonCellPrototype> groundGridEdges = gridEdges.FindAll(c => c.isEntry == false && c.isPath == false && c.IsGround());
            if (allCellPrototypeClusters == null)
            {
                allCellPrototypeClusters = new List<HexagonCellCluster>();
                allCellPrototypeClusters.Add(new HexagonCellCluster(0, groundGridEdges, CellClusterType.Edge));
            }
            else
            {
                int clusterid = allCellPrototypeClusters.Count + 1;
                allCellPrototypeClusters.Add(new HexagonCellCluster(clusterid, groundGridEdges, CellClusterType.Edge));
            }
        }


        public void GenerateMicroGridFromClusters()
        {
            HashSet<string> hostIds = new HashSet<string>();
            Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>> _cellPrototypesByLayer_X4_ByCluster = new Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>>();

            foreach (HexagonCellCluster cluster in allCellPrototypeClusters)
            {
                // Skip path for now
                // if (cluster.clusterType == CellClusterType.Path) continue;

                List<HexagonCellPrototype> filteredHosts = new List<HexagonCellPrototype>();
                int duplicatesFound = 0;

                foreach (HexagonCellPrototype item in cluster.prototypes)
                {
                    if (hostIds.Contains(item.uid) == false)
                    {
                        hostIds.Add(item.uid);
                        filteredHosts.Add(item);
                    }
                    else
                    {
                        duplicatesFound++;
                        // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                    }
                }

                Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellManager.GenerateProtoypeGridFromHosts(filteredHosts, 4, 3, cellLayerElevation, gameObject.transform, true);
                foreach (var kvp in newPrototypesByLayer)
                {
                    HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform, 0.3f);
                    HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);
                }
                cluster.prototypesByLayer_X4 = newPrototypesByLayer;

                _cellPrototypesByLayer_X4_ByCluster.Add(cluster, newPrototypesByLayer);
            }
            cellPrototypesByLayer_X4_ByCluster = _cellPrototypesByLayer_X4_ByCluster;
        }

        public void GenerateMicroGridFromHosts(List<HexagonCellPrototype> allHosts, int cellSize, int cellLayers)
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

            foreach (var item in allHosts)
            {
                if (hostIds.Contains(item.uid) == false)
                {
                    hostIds.Add(item.uid);
                    filteredHosts.Add(item);
                }
                else
                {
                    duplicatesFound++;
                    // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                }
            }

            if (duplicatesFound > 0) Debug.LogError("GenerateMicroGridFromHosts - Duplicate hosts found: " + duplicatesFound);
            Debug.Log("GenerateMicroGridFromHosts - allHosts: " + allHosts.Count + ", filteredHosts: " + filteredHosts.Count);

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellManager.GenerateProtoypeGridFromHosts(filteredHosts, 4, cellLayers, cellLayerElevation, gameObject.transform, true);
            int count = 0;
            foreach (var kvp in newPrototypesByLayer)
            {
                HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform, 0.3f);

                List<HexagonCellPrototype> newGridEdges = HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);

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
                    HexagonCell.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(cellSize));
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
                            HexagonCell.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(4));
                            HexagonCell.GetEdgeCells(kvp.Value);
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

                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(cornerPoints);
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

        public static Vector3[] GenerateHexagonCornerPoints(Vector3 center, float radius)
        {
            Vector3[] points = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 corner = new Vector3(x, center.y, z);
                Vector3 direction = (corner - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(corner.x, corner.z), new Vector2(center.x, center.z));
                corner = center + direction * edgeDistance * 3f;
                points[i] = corner;
            }
            return points;
        }

        public static Vector3[] GenerateHexagonSidePoints(Vector3[] corners, float offset, Vector3 center)
        {
            Vector3[] hexagonSides = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                // Find the center point between the current corner and the next corner
                // Vector3 center = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);

                // Calculate the direction from the center to the current corner

                // Calculate the side point by offsetting from the center along the direction
                // Vector3 side = center + direction * offset;

                Vector3 side = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);
                Vector3 direction = (side - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(side.x, side.z), new Vector2(center.x, center.z));
                side = center + direction * edgeDistance * 2f;

                hexagonSides[(i + 5) % 6] = side; // Places index 0 at the front side
            }
            return hexagonSides;
        }


        public Vector3[] GenerateGridPoints(Vector3 startingPoint, float size, float radius)
        {
            List<Vector3> points = new List<Vector3>();

            float halfSize = size / 2.0f;
            int numPointsPerAxis = Mathf.FloorToInt(radius * 2.0f / size);
            float stepSize = radius * 2.0f / numPointsPerAxis;

            for (int i = 0; i < numPointsPerAxis; i++)
            {
                for (int j = 0; j < numPointsPerAxis; j++)
                {
                    Vector3 point = startingPoint + new Vector3(i * stepSize - radius + halfSize, 0.0f, j * stepSize - radius + halfSize);

                    if (Vector3.Distance(startingPoint, point) <= radius)
                    {
                        points.Add(point);
                    }
                }
            }

            return points.ToArray();
        }



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
            cellPrototypesByLayer_V2 = GenerateClusterOfMicroCellGridProtoypesFromHosts(parentCell, allClusteredCells, cellLayers, cellLayerElevation);
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
            cellPrototypesByLayer_V2 = GenerateClusterOfMicroCellGridProtoypesFromHosts(parentCell, childCells, cellLayers, cellLayerElevation);
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
                    if (cellPrototype.cellStatus == CellStatus.Remove || cellPrototype.cellStatus == CellStatus.UnderGround) continue;

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

                    if (cellPrototype.isEdge) hexCell.SetEdgeCell(true, EdgeCellType.Grid);
                    if (cellPrototype.isPath) hexCell.SetPathCell(true);

                    if (cellPrototype.isGridHost)
                    {
                        hexCell.SetGridHost(true);
                        if (cellPrototype.clusterParent != null)
                        {
                            cellPrototype.clusterParent.cells.Add(hexCell);
                        }
                    }

                    hexCell.cellStatus = cellPrototype.cellStatus;

                    hexCell.isGroundRamp = cellPrototype.isGroundRamp;

                    hexCell._vertexIndices = cellPrototype._vertexIndices;
                    hexCell._vertexIndicesBySide = cellPrototype._vertexIndicesBySide;

                    if (layer > 0)
                    {
                        for (int i = 0; i < prevLayerHexagonCells.Count; i++)
                        {
                            if (prevLayerHexagonCells[i].GetGridLayer() < hexCell.GetGridLayer() && prevLayerHexagonCells[i].id == cellPrototype.bottomNeighborId)
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

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateClusterOfMicroCellGridProtoypesFromHosts(HexagonCell parentCell, List<HexagonCell> childCells, int cellLayers, int cellLayerElevation = 4, bool useCorners = true)
        {
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = null;
            Vector2 gridGenerationCenterPosXZOffeset = new Vector2(-1.18f, 0.35f);

            List<HexagonCell> allHostCells = new List<HexagonCell>();
            allHostCells.Add(parentCell);
            allHostCells.AddRange(childCells);

            foreach (HexagonCell hostCell in allHostCells)
            {
                Vector3 center = hostCell.transform.position;
                int layerBaseOffset = 0;

                if (hostCell.GetGridLayer() != parentCell.GetGridLayer())
                {
                    int layerDifference = hostCell.GetGridLayer() - parentCell.GetGridLayer();
                    layerBaseOffset = 1 * layerDifference;

                    center.y = parentCell.transform.position.y + (cellLayerElevation * layerDifference);
                }

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(center, 12, 4, cellLayers, cellLayerElevation, gridGenerationCenterPosXZOffeset, layerBaseOffset, hostCell?.id);

                if (newPrototypesByLayer == null)
                {
                    newPrototypesByLayer = prototypesByLayer;
                }
                else
                {
                    foreach (var kvp in prototypesByLayer)
                    {
                        int key = kvp.Key;
                        List<HexagonCellPrototype> prototypes = kvp.Value;

                        if (newPrototypesByLayer.ContainsKey(key) == false)
                        {
                            newPrototypesByLayer.Add(key, prototypes);
                        }
                        else
                        {
                            newPrototypesByLayer[key].AddRange(prototypes);
                        }
                    }
                }
            }

            // // Remove the corner cells in the same point;
            // if (useCorners)
            // {
            //     foreach (var kvp in newPrototypesByLayer)
            //     {
            //         int key = kvp.Key;
            //         List<HexagonCellPrototype> prototypes = kvp.Value;

            //         HexagonCellPrototype.RemoveExcessPrototypesByDistance(prototypes);
            //     }
            // }

            return newPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateProtoypeGridFromHosts(List<HexagonCellPrototype> allHosts, int cellSize, int cellLayers, int cellLayerElevation = 4, Transform transform = null, bool useCorners = false)
        {
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            List<int> cornersToUse = new List<int>()
                {
                    (int)HexagonCorner.FrontA,
                    (int)HexagonCorner.FrontB,
                };

            foreach (HexagonCellPrototype hostCell in allHosts)
            {
                Vector3 center = hostCell.center;
                int layerBaseOffset = hostCell.layer;

                List<int> _cornersToUse = new List<int>();
                _cornersToUse.AddRange(cornersToUse);

                bool anyGridHostCellsInBackNeighborStack = HexagonCellPrototype.HasGridHostCellsOnSideNeighborStack(hostCell, (int)HexagonSide.Back);

                if (anyGridHostCellsInBackNeighborStack == false)
                // if (backNeighbor == null || (backNeighbor != null && backNeighbor.isPath == false))
                {
                    _cornersToUse.Add((int)HexagonCorner.BackA);
                    _cornersToUse.Add((int)HexagonCorner.BackB);
                }

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(center, hostCell.size, cellSize, cellLayers, cellLayerElevation, hostCell, layerBaseOffset, transform, _cornersToUse);
                hostCell.SetGridHost(true);
                hostCell.cellPrototypes_X4_ByLayer = prototypesByLayer;

                foreach (var kvp in prototypesByLayer)
                {
                    int key = kvp.Key;
                    List<HexagonCellPrototype> prototypes = kvp.Value;
                    if (newPrototypesByLayer.ContainsKey(key) == false)
                    {
                        newPrototypesByLayer.Add(key, prototypes);
                    }
                    else
                    {
                        newPrototypesByLayer[key].AddRange(prototypes);
                    }
                }
            }
            // // Remove the corner cells in the same point;
            // if (useCorners)
            // {
            //     foreach (var kvp in newPrototypesByLayer)
            //     {
            //         int key = kvp.Key;
            //         List<HexagonCellPrototype> prototypes = kvp.Value;

            //         HexagonCellPrototype.RemoveExcessPrototypesByDistance(prototypes);
            //     }
            // }

            return newPrototypesByLayer;
        }

        #endregion
    }

}
