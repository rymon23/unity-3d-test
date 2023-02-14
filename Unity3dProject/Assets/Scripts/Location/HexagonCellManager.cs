using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class HexagonCellManager : MonoBehaviour
    {
        [Header("Cell Grid Settings")]
        [Range(8, 324)] public int radius = 72;
        [Range(3, 24)][SerializeField] private int cellSize = 12;
        [Range(1, 12)][SerializeField] private int cellLayers = 2;
        [Range(3, 12)][SerializeField] private int cellLayerElevation = 6;
        [SerializeField] private bool enableGridGenerationCenterOffeset;
        [SerializeField] private Vector2 gridGenerationCenterPosXZOffeset = new Vector2(-1.18f, 0.35f);

        [Header("Generate")]
        [SerializeField] private bool generateCells;

        #region Saved State
        Vector3 _position;
        int _radius;
        int _cellSize;
        int _cellLayers;
        int _cellLayerElevation;
        #endregion

        [Header("Cell Data")]
        [SerializeField] private List<HexagonCell> allCells;
        public List<HexagonCell> GetCells() => allCells;
        private Dictionary<int, List<HexagonTilePrototype>> cellPrototypesByLayer;
        public List<HexagonCellCluster> cellClusters;


        [Header("Debug Settings")]
        [SerializeField] private bool showGrid;
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

        public void GenerateCells(bool force)
        {
            if (force || allCells == null || allCells.Count == 0)
            {
                Debug.Log("GenerateCells");
                Generate_HexagonCellPrototypes();
                Generate_HexagonCells(cellPrototypesByLayer, HexagonCell_prefab);
                HexagonCell.PopulateNeighborsFromCornerPoints(allCells, 0.33f * (cellSize / 12f));
            }
        }

        public void InitialSetup()
        {
        }

        private bool _shouldUpdate;
        private void OnValidate()
        {
            if (cellPrototypesByLayer == null
                || _position != transform.position
                || _cellSize != cellSize
                || _cellLayers != cellLayers
                || _cellLayerElevation != cellLayerElevation)
            {
                _position = transform.position;
                _shouldUpdate = true;
            }

            if (radius != _radius)
            {
                if (radius % 2 != 0) radius += 1;
                _radius = radius;
                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                _shouldUpdate = false;
                Generate_HexagonCellPrototypes();
            }

            if (generateCells)
            {
                GenerateCells(generateCells);
                generateCells = false;
            }
        }

        private void Awake()
        {
            InitialSetup();
        }

        private void Start()
        {
            InitialSetup();

            GenerateCells(false);
        }


        private void Generate_HexagonCellPrototypes()
        {
            Vector3 centerPos = transform.position;
            if (enableGridGenerationCenterOffeset)
            {
                centerPos.x += gridGenerationCenterPosXZOffeset.x;
                centerPos.z += gridGenerationCenterPosXZOffeset.y;
            }
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
                        newLayer = AddCellPrototypeLayer(newCellPrototypes, cellLayerElevation, i);
                    }
                    else
                    {
                        List<HexagonTilePrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = AddCellPrototypeLayer(previousLayer, cellLayerElevation, i);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                    // Debug.Log("Added Layer: " + i + ", Count: " + newLayer.Count);
                }
            }
            cellPrototypesByLayer = newPrototypesByLayer;
        }

        public static List<HexagonTilePrototype> AddCellPrototypeLayer(List<HexagonTilePrototype> prototypes, int layerElevation, int layer)
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
                    HexagonCell hexagonTile = newTile.GetComponent<HexagonCell>();
                    hexagonTile._cornerPoints = cellPrototype.cornerPoints;
                    hexagonTile.id = cellPrototype.id;
                    hexagonTile.name = "HexagonCell_" + cellPrototype.id;
                    hexagonTile.SetSize(cellPrototype.size);
                    hexagonTile.SetGridLayer(layer);

                    if (layer > 0)
                    {
                        for (int i = 0; i < newHexagonCells.Count; i++)
                        {
                            if (newHexagonCells[i].GetGridLayer() < hexagonTile.GetGridLayer() && newHexagonCells[i].id == cellPrototype.bottomNeighborId)
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
            allCells = newHexagonCells;
        }

        private void OnDrawGizmos()
        {

            Gizmos.color = Color.magenta;
            if (showBounds) Gizmos.DrawWireSphere(transform.position, radius);

            if (showGrid) DrawHexagonCellPrototypes(cellPrototypesByLayer);

            if (showCells != ShowCellState.None)
            {
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


        #region STATIC METHODS


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
