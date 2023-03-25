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
        public CellStatus GetCellStatus();
        public EdgeCellType GetEdgeCellType();
        public Vector3 GetPosition();
        public Vector3[] GetCorners();
        public Vector3[] GetSides();
        // public List<IHexCell> GetNeighbors();
    }

    public enum GridFilter_Level { None, All, HostCells, MicroCells, }
    public enum GridFilter_Type { None, All, Edge, Path, Ground, Entrance }

    public class HexagonCellManager : MonoBehaviour
    {
        [Header("Cell Grid Settings")]
        [Range(4, 648)] public int radius = 72;
        [Range(2, 128)][SerializeField] private int cellSize = 12;

        [Header(" ")]
        [SerializeField] private float centerPosYOffset = 0;
        [SerializeField] private bool enableGridGenerationCenterOffeset;
        [SerializeField] private Vector2 gridGenerationCenterPosXZOffeset = new Vector2(-1.18f, 0.35f);

        [Header("Layer Settings")]
        [Range(2, 12)][SerializeField] private int cellLayerElevation = 6;
        [Range(1, 24)][SerializeField] private int cellLayers = 2;
        [Range(3, 24)][SerializeField] private int cellLayersMax = 6;
        [SerializeField] private bool randomizeCellLayers;
        [Header(" ")]
        [SerializeField] private bool resetPrototypes;

        [Header("Generate")]
        [SerializeField] private bool generateCells;

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
        public List<HexagonCell> GetAllCellsList() => allCells;
        [SerializeField] private List<HexagonCell> allCells_X4;


        [Header("Prototypes")]
        private Dictionary<int, List<HexagonTilePrototype>> cellPrototypesByLayer;

        public Dictionary<int, List<HexagonCellPrototype>> cellPrototypesByLayer_V2;
        [SerializeField] private List<HexagonCellPrototype> allPrototypesList;
        public List<HexagonCellPrototype> GetAllPrototypesList() => allPrototypesList;
        public List<HexagonCellPrototype> GetAllPrototypeEdgesOfType(EdgeCellType type) => allPrototypesList.FindAll(c => c._edgeCellType == type);
        public Dictionary<int, List<HexagonCellPrototype>> cellPrototypes_X4_ByLayer;

        [SerializeField] private int totalPrototypes = 0;
        [SerializeField] private int totalPrototypesX4 = 0;
        public List<HexagonCellCluster> cellClusters;

        [Header("Debug Settings")]
        [SerializeField] private bool showGrid;
        [SerializeField] private GridFilter_Level gridFilter_Level;
        [SerializeField] private GridFilter_Type gridFilter_Type;

        [SerializeField] private bool showBounds;
        [SerializeField] private bool generateOnStart;
        // [SerializeField] private bool trackPrototypes;

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

        [Header("Cluster Cell Manager")]
        [SerializeField] private List<HexagonCell> allClusteredCells;
        [SerializeField] private HexagonCell clusterParentCell;
        [SerializeField] private bool _isClusterParent;
        public void SetClusterParent()
        {
            _isClusterParent = true;
        }
        public bool GetClusterParent() => _isClusterParent;
        public int CalculateRandomLayers() => UnityEngine.Random.Range(cellLayers, cellLayersMax + 1);
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
                Debug.Log("radius: " + radius + ", remmainder: " + remmainder + ", cellRadiusMult: " + cellRadiusMult);

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
                // Generate_HexagonCellPrototypes();
                RecalculateEdgePoints();
                GenerateGridsByLayer();
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


        #endregion
        public void ResetPrototypes()
        {
            resetPrototypes = true;
            OnValidate();
        }



        public void GenerateGridsByLayer()
        {
            Vector3 centerPos = gameObject.transform.position;
            centerPos.y += centerPosYOffset;

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(centerPos, radius, cellSize, cellLayers, cellLayerElevation, null, 0, gameObject.transform);
            List<HexagonCellPrototype> _allPrototypesList = new List<HexagonCellPrototype>();

            int count = 0;
            foreach (var kvp in newPrototypesByLayer)
            {
                count += kvp.Value.Count;

                // HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value);
                HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform);

                _allPrototypesList.AddRange(kvp.Value.FindAll(p => p.cellStatus != CellStatus.Remove));
                //     // allPrototypesList.AddRange(kvp.Value.FindAll(p => p.IsDisposable() == false));

                HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);
            }

            cellPrototypesByLayer_V2 = newPrototypesByLayer;
            allPrototypesList = _allPrototypesList;
            totalPrototypes = count;

            Debug.Log("GenerateGridsByLayer - totalPrototypes: " + totalPrototypes + ", allPrototypesList: " + allPrototypesList.Count);
        }

        public (List<HexagonCellPrototype>, List<HexagonCellPrototype>) GeneratePrototypeGridEntryAndPaths()
        {
            List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Grid);
            List<HexagonCellPrototype> entryPrototypes = HexagonCellPrototype.PickRandomEntryFromGridEdges(gridEdges, 3, true);

            Debug.Log("gridEdges: " + gridEdges.Count + ", entryPrototypes: " + entryPrototypes.Count + ", allPrototypesList: " + allPrototypesList.Count);

            List<HexagonCellPrototype> path = HexagonCellPrototype.GenerateRandomPath(entryPrototypes, allPrototypesList, gameObject.transform.position);
            // foreach (var item in path)
            // {
            //     Debug.Log("paths - X12, path - id: " + item.id + ", uid: " + item.uid);
            // }
            // return (null, null);
            return (path, entryPrototypes);
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
            // int length = cellPrototypes_X4_ByLayer.Keys.Count;
            // for (int i = 0; i < length; i++)
            // {
            //     if (cellPrototypes_X4_ByLayer.ContainsKey(i))
            //     {
            //         List<HexagonCellPrototype> prototypes = HexagonCellPrototype.PopulateNeighborsFromCornerPoints(cellPrototypes_X4_ByLayer[i], 0.3f);
            //         cellPrototypes_X4_ByLayer[i].Clear();
            //         cellPrototypes_X4_ByLayer[i].AddRange(prototypes);

            //         count += cellPrototypes_X4_ByLayer[i].Count;
            //     }
            // }
            foreach (var kvp in newPrototypesByLayer)
            {
                HexagonCellPrototype.PopulateNeighborsFromCornerPoints(kvp.Value, transform, 0.3f);

                List<HexagonCellPrototype> newGridEdges = HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Grid);

                count += kvp.Value.Count;
            }
            foreach (var kvp in newPrototypesByLayer)
            {
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

        private void Generate_HexagonCellPrototypes()
        {

            Vector3 centerPos = transform.position;
            Vector2 _gridGenCenterPosXZOffeset = Vector2.zero;
            if (enableGridGenerationCenterOffeset)
            {
                _gridGenCenterPosXZOffeset = gridGenerationCenterPosXZOffeset;
                centerPos.x += gridGenerationCenterPosXZOffeset.x;
                centerPos.z += gridGenerationCenterPosXZOffeset.y;
            }

            // Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = HexagonCellManager.Generate_HexagonCellPrototypes(transform.position, 12, 4, cellLayers, cellLayerElevation, _gridGenCenterPosXZOffeset, 0, null, transform);
            // cellPrototypesByLayer = newPrototypesByLayer;

            List<HexagonTilePrototype> newCellPrototypes = LocationUtility.GetTilesWithinRadius(
                                        HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                centerPos,
                                                radius,
                                                cellSize),
                                                centerPos,
                                                radius);
            Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonTilePrototype>>();
            newPrototypesByLayer.Add(0, newCellPrototypes);

            if (cellLayers > 1)
            {
                for (int i = 1; i < cellLayers; i++)
                {

                    List<HexagonTilePrototype> newLayer;
                    if (i == 1)
                    {
                        newLayer = DuplicateCellPrototypesToNewLayer(newCellPrototypes, cellLayerElevation, i);
                    }
                    else
                    {
                        List<HexagonTilePrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateCellPrototypesToNewLayer(previousLayer, cellLayerElevation, i);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                    // Debug.Log("Added Layer: " + i + ", Count: " + newLayer.Count);
                }
            }
            cellPrototypesByLayer = newPrototypesByLayer;
        }

        public static List<HexagonTilePrototype> DuplicateCellPrototypesToNewLayer(List<HexagonTilePrototype> prototypes, int layerElevation, int layer)
        {
            List<HexagonTilePrototype> newPrototypes = new List<HexagonTilePrototype>();
            foreach (var prototype in prototypes)
            {
                HexagonTilePrototype newPrototype = new HexagonTilePrototype();
                newPrototype.id = prototype.id + layer;
                newPrototype.parentId = prototype.parentId;
                newPrototype.name = "Cell_Prototype-" + prototype.id + "-L" + layer;

                newPrototype.layer = layer;
                newPrototype.bottomNeighborId = prototype.id;
                newPrototype.size = prototype.size;
                newPrototype.center = new Vector3(prototype.center.x, prototype.center.y + layerElevation, prototype.center.z);
                newPrototype.cornerPoints = prototype.cornerPoints.Select(c => new Vector3(c.x, c.y + layerElevation, c.z)).ToArray();
                newPrototypes.Add(newPrototype);
            }
            return newPrototypes;
        }



        public void GenerateCells(bool force, bool regeneratePrototypes = false, Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = null)
        {
            if (force || allCells == null || allCells.Count == 0)
            {
                // Debug.Log("Generate Cells");
                // Debug.Log("Generate Cells = prototypesByLayer:" + prototypesByLayer.Count); //+ ", cellPrototypesByLayer_V2: " + cellPrototypesByLayer_V2.Count);

                if (regeneratePrototypes || ArePrototypesInitialized() == false)
                {
                    if (randomizeCellLayers) cellLayers = CalculateRandomLayers();
                    if (!_isClusterParent) GenerateGridsByLayer(); //Generate_HexagonCellPrototypes(); 
                }

                // Generate_HexagonCells(cellPrototypesByLayer, HexagonCell_prefab);
                allCellsByLayer = GenerateHexagonCellsFromPrototypes(prototypesByLayer != null ? prototypesByLayer : cellPrototypesByLayer_V2, HexagonCell_prefab, gameObject.transform);

                List<HexagonCell> _allCells = new List<HexagonCell>();
                foreach (var kvp in allCellsByLayer)
                {
                    HexagonCell.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(cellSize));
                    _allCells.AddRange(kvp.Value);
                }
                allCells = _allCells;

                if (cellPrototypes_X4_ByLayer != null && cellPrototypes_X4_ByLayer.Count > 0)
                {
                    allCellsByLayer_X4 = GenerateHexagonCellsFromPrototypes(cellPrototypes_X4_ByLayer, HexagonCell_prefab, gameObject.transform);
                    List<HexagonCell> _allCell_X4 = new List<HexagonCell>();
                    foreach (var kvp in allCellsByLayer_X4)
                    {
                        HexagonCell.PopulateNeighborsFromCornerPoints(kvp.Value, HexagonCell.GetCornerNeighborSearchDist(4));
                        HexagonCell.GetEdgeCells(kvp.Value);
                        _allCell_X4.AddRange(kvp.Value);
                    }
                    allCells_X4 = _allCell_X4;
                }
            }
        }


        private void Generate_HexagonCells(Dictionary<int, List<HexagonTilePrototype>> cellPrototypesByLayer, GameObject cellPrefab)
        {
            List<HexagonCell> newHexagonCells = new List<HexagonCell>();

            Transform folder = new GameObject("Cells").transform;
            folder.transform.SetParent(gameObject.transform);

            foreach (var kvp in cellPrototypesByLayer)
            {
                int layer = kvp.Key;
                List<HexagonTilePrototype> prototypes = kvp.Value;

                Transform layerFolder = new GameObject("Layer_" + layer).transform;
                layerFolder.transform.SetParent(folder);

                foreach (HexagonTilePrototype cellPrototype in prototypes)
                {
                    Vector3 pointPos = cellPrototype.center;

                    GameObject newTile = Instantiate(cellPrefab, pointPos, Quaternion.identity);
                    HexagonCell hexCell = newTile.GetComponent<HexagonCell>();
                    hexCell._cornerPoints = cellPrototype.cornerPoints;
                    hexCell.id = cellPrototype.id;
                    hexCell.SetParentCellId(cellPrototype.parentId);

                    hexCell.name = "Cell_[" + cellPrototype.id + "-L" + cellPrototype.layer + "] ";
                    if (hexCell.GetParenCellId() != null) hexCell.name = "Sub-" + hexCell.name;

                    hexCell.SetSize(cellPrototype.size);
                    hexCell.SetGridLayer(layer);

                    if (layer > 0)
                    {
                        for (int i = 0; i < newHexagonCells.Count; i++)
                        {
                            if (newHexagonCells[i].GetGridLayer() < hexCell.GetGridLayer() && newHexagonCells[i].id == cellPrototype.bottomNeighborId)
                            {
                                hexCell._neighbors.Add(newHexagonCells[i]);
                                // hexCell.layeredNeighbor[0] = newHexagonCells[i]; // set bottom neighbor
                                hexCell.SetBottomNeighbor(newHexagonCells[i]); // set bottom neighbor

                                newHexagonCells[i]._neighbors.Add(hexCell);
                                // newHexagonCells[i].layeredNeighbor[1] = hexCell; //Set top neighbor
                                newHexagonCells[i].SetTopNeighbor(hexCell); //Set top neighbor
                            }
                        }
                    }
                    newHexagonCells.Add(hexCell);
                    hexCell.transform.SetParent(layerFolder);
                }
            }
            allCells = newHexagonCells;
        }


        [SerializeField] private int _temp_rows = 4;

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
                // for (int j = 0; j < sidePoints.Length; j++)
                // {
                //     Gizmos.DrawSphere(sidePoints[j], 0.3f);
                // }
                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(cornerPoints);
                // Gizmos.DrawWireSphere(transform.position, radius);
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
                // HexagonCellPrototype centerHex = new HexagonCellPrototype(transform.position, cellSize);
                // Vector3[] centerPoints = GenerateHexagonSidePoints(centerHex.cornerPoints, cellSize * 1.8f, centerHex.center);

                // Vector3[] centerCornersExtended = GenerateHexagonCornerPoints(centerHex.center, cellSize);

                // // Vector3[] centerPoints = GenerateGridPoints(transform.position, cellSize * 2f, radius);
                // HexagonCellPrototype.DrawHexagonCellPrototype(centerHex, 0.4f);

                // HexagonCellPrototype parentHex = new HexagonCellPrototype(transform.position, radius);
                // HexagonCellPrototype.DrawHexagonCellPrototype(parentHex, 0.4f);

                // List<HexagonCellPrototype> prototypes = HexagonCellPrototype.GenerateHexGrid(transform.position, cellSize, radius);
                // HexagonCellPrototype.DrawHexagonCellPrototypes(prototypes, gameObject.transform);
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

        private static Dictionary<int, List<HexagonCell>> GenerateHexagonCellsFromPrototypes(Dictionary<int, List<HexagonCellPrototype>> cellPrototypesByLayer, GameObject cellPrefab, Transform transform)
        {
            Dictionary<int, List<HexagonCell>> newCellsByLayer = new Dictionary<int, List<HexagonCell>>();

            Transform folder = new GameObject("Cells").transform;
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


        // public static (List<HexagonCell>, List<HexagonCell>) CreateAndCollapseHostClusterAndMicoGrid(List<HexagonCell> availableCells_BaseLayer, List<HexagonTileCore> clusterParentPrefabs, Transform transform, int layersMin, int layersMsx, int hostCellsMin = 2, int hostCellsMax = 7, bool randomizeHostCount = true, bool randomizeCellLayers = true, int cellLayerElevation = 4)
        // {
        //     if (clusterParentPrefabs == null || clusterParentPrefabs.Count == 0)
        //     {
        //         Debug.LogError("No prefabs for clusterParentPrefabs!");
        //         return false;
        //     }

        //     bool sucess = false;

        //     // List<HexagonCell> cellsToAssign = WFCUtilities.SelectRandomCells(availableCells_BaseLayer, cluster_minHosts, cluster_maxHosts);
        //     List<HexagonCell> cellsToAssign = new List<HexagonCell>();

        //     HexagonCell parentCell = availableCells_BaseLayer[UnityEngine.Random.Range(0, availableCells_BaseLayer.Count)];

        //     int hostCount = hostCellsMin;
        //     if (randomizeHostCount) hostCount = UnityEngine.Random.Range(hostCellsMin, hostCellsMax);

        //     cellsToAssign = HexagonCell.GetChildrenForMicroClusterParent(parentCell, 0, hostCount - 1);
        //     if (cellsToAssign == null || cellsToAssign.Count == 0)
        //     {
        //         Debug.LogError("No childCells found");
        //         return false;
        //     }

        //     cellsToAssign.Add(parentCell);

        //     HexagonTileCore clusterParentPrefab = clusterParentPrefabs[UnityEngine.Random.Range(0, clusterParentPrefabs.Count)];

        //     int cellLayers = layersMin;
        //     if (randomizeCellLayers) cellLayers = UnityEngine.Random.Range(layersMin, layersMsx);

        //     (HexagonCellManager parentCellManager, List<HexagonCell> clusterCells) = WFCUtilities.SetupMicroCellClusterFromHosts(cellsToAssign, clusterParentPrefab, cellLayers, cellLayerElevation, transform, true);

        //     parentCellManager.SetClusterParent();
        //     parentCellManager.gameObject.name += "_ClusterParent";

        //     HexGridArea gridArea = parentCellManager.gameObject.GetComponent<HexGridArea>();
        //     gridArea.InitialSetup();
        //     gridArea.Generate();

        //     return sucess;
        // }


        public static Dictionary<int, List<HexagonTilePrototype>> Generate_ClusterOfMicroCellGridProtoypesFromHosts(HexagonCell parentCell, List<HexagonCell> childCells, int cellLayers, int cellLayerElevation = 4, bool useCorners = true)
        {
            Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = null;
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
                Dictionary<int, List<HexagonTilePrototype>> prototypesByLayer = Generate_HexagonCellPrototypesByLayer(center, 12, 4, cellLayers, cellLayerElevation, gridGenerationCenterPosXZOffeset, layerBaseOffset, hostCell);

                if (newPrototypesByLayer == null)
                {
                    newPrototypesByLayer = prototypesByLayer;
                }
                else
                {
                    foreach (var kvp in prototypesByLayer)
                    {
                        int key = kvp.Key;
                        List<HexagonTilePrototype> prototypes = kvp.Value;

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


            // Remove the corner cells in the same point;
            if (useCorners)
            {
                foreach (var kvp in newPrototypesByLayer)
                {
                    int key = kvp.Key;
                    List<HexagonTilePrototype> prototypes = kvp.Value;

                    RemoveExcessPrototypesByDistance(prototypes);
                }
            }

            return newPrototypesByLayer;
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

            foreach (HexagonCellPrototype hostCell in allHosts)
            {
                Vector3 center = hostCell.center;
                int layerBaseOffset = hostCell.layer;

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(center, hostCell.size, cellSize, cellLayers, cellLayerElevation, hostCell, layerBaseOffset, transform, useCorners);

                int passes = 0;
                foreach (var kvp in prototypesByLayer)
                {
                    int key = kvp.Key;
                    List<HexagonCellPrototype> prototypes = kvp.Value;

                    Debug.Log("host cell: " + hostCell.id + ", layer:" + key + ", prototypes: " + prototypes.Count + ", iterations: " + passes);
                    passes++;

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

        public static void RemoveExcessPrototypesByDistance(List<HexagonTilePrototype> prototypes, float distanceThreshold = 0.5f)
        {
            // prototypes.Sort((a, b) => a.center.CompareTo(b.center));
            prototypes.Sort((a, b) => Vector3.Distance(a.center, Vector3.zero).CompareTo(Vector3.Distance(b.center, Vector3.zero)));
            int removed = 0;

            for (int i = prototypes.Count - 1; i >= 0; i--)
            {
                HexagonTilePrototype current = prototypes[i];

                for (int j = i - 1; j >= 0; j--)
                {
                    HexagonTilePrototype other = prototypes[j];

                    if (Vector3.Distance(current.center, other.center) < distanceThreshold)
                    {
                        prototypes.RemoveAt(j);
                        removed++;
                    }
                }
            }
            Debug.Log("RemoveExcessPrototypesByDistance: removed: " + removed);
        }

        public static Dictionary<int, List<HexagonTilePrototype>> Generate_HexagonCellPrototypesByLayer(Vector3 centerPos, float radius, int cellSize, int cellLayers, int cellLayerElevation, Vector2 gridGenerationCenterPosXZOffeset, int baseLayerOffset, HexagonCell parentCell = null, Transform transform = null, bool useCorners = true)
        {
            centerPos.x += gridGenerationCenterPosXZOffeset.x;
            centerPos.z += gridGenerationCenterPosXZOffeset.y;

            List<HexagonTilePrototype> newCellPrototypes = LocationUtility.GetTilesWithinRadius(
                                        HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                centerPos,
                                                radius,
                                                cellSize, parentCell),
                                                centerPos,
                                                radius);
            Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonTilePrototype>>();

            int startingLayer = 0 + baseLayerOffset;
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            // TEMP
            if (useCorners && transform != null || parentCell != null)
            {
                Transform tran = transform ? transform : parentCell.gameObject.transform;
                Vector3[] corners = HexagonGenerator.GenerateHexagonPoints(tran.position, 12);

                List<HexagonTilePrototype> cornerPrototypesByLayer = Generate_HexagonCellPrototypesAtPoints(corners, cellSize, parentCell);
                newPrototypesByLayer[startingLayer].AddRange(cornerPrototypesByLayer);
            }

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonTilePrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateCellPrototypesToNewLayer(newPrototypesByLayer[startingLayer], cellLayerElevation, i);
                    }
                    else
                    {
                        List<HexagonTilePrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateCellPrototypesToNewLayer(previousLayer, cellLayerElevation, i);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                    // Debug.Log("Added Layer: " + i + ", Count: " + newLayer.Count);
                }
            }
            return newPrototypesByLayer;
        }

        public static List<HexagonTilePrototype> Generate_HexagonCellPrototypesAtPoints(Vector3[] centerPoints, int cellSize, HexagonCell parentCell = null)
        {
            List<HexagonTilePrototype> prototypes = new List<HexagonTilePrototype>();
            for (var i = 0; i < centerPoints.Length; i++)
            {

                Vector3[] hexagonPoints = HexagonGenerator.GenerateHexagonPoints(centerPoints[i], cellSize);
                HexagonTilePrototype prototype = new HexagonTilePrototype();

                int idFragment = Mathf.Abs((int)(centerPoints[i].z + centerPoints[i].x));

                if (parentCell != null)
                {
                    prototype.parentId = parentCell.id;
                    prototype.id = "p_" + parentCell.id + "-";
                }
                prototype.id += "X" + cellSize + "-" + idFragment + "-" + i;
                prototype.name = "Cell_Prototype-" + prototype.id;
                prototype.size = cellSize;
                prototype.cornerPoints = hexagonPoints;
                prototype.center = centerPoints[i];
                prototypes.Add(prototype);
            }
            return prototypes;
        }

        public static void DrawHexagonCellPrototypes(List<HexagonTilePrototype> cellPrototypesByLayer)
        {
            if (cellPrototypesByLayer == null) return;

            foreach (HexagonTilePrototype item in cellPrototypesByLayer)
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

        public static void DrawHexagonCellPrototypes(Dictionary<int, List<HexagonTilePrototype>> cellPrototypesByLayer)
        {
            if (cellPrototypesByLayer == null) return;

            foreach (var kvp in cellPrototypesByLayer)
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
        #endregion
    }

}
