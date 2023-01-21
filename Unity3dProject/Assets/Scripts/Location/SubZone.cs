using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    public class SubZone : MonoBehaviour
    {

        [SerializeField] private ZoneCellManager zoneCellManager;
        [SerializeField] private Location _locationParent;
        [Range(24, 256)][SerializeField] private int radius = 102;
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
        [SerializeField] private bool showCellClusters;
        [Range(-1, 128)][SerializeField] private int highlightCellCluster = 0;
        [SerializeField] private bool resetHexagonTilePrototypes;

        [SerializeField] private bool generateHexagonTileCells;
        [SerializeField] private bool generateHexagonTileCellClusters;
        [SerializeField] private bool generateModelingPlatforms;
        List<HexagonTilePrototype> hexagonTilePrototypes;
        [SerializeField] private List<HexagonCell> hexagonTileCells;
        [SerializeField] private List<HexagonCell> edgeCells;
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
        public void Debug_ResetHexagonTilePrototypes(bool enable)
        {
            resetHexagonTilePrototypes = enable;
            OnValidate();
        }
        public void Debug_GenerateHexagonTileCells(bool enable)
        {
            generateHexagonTileCells = enable;
            OnValidate();
        }

        [Header("Prefabs")]
        [SerializeField] private GameObject HexagonTileCell_prefab;
        [SerializeField] private GameObject HexagonTilePlatformModel_prefab;

        [Header("WFC")]
        [SerializeField] private HexagonWaveFunctionCollapse_1 waveFunctionCollapse;

        private void GenerateHexagonTilePrototypes()
        {
            hexagonTilePrototypes = LocationUtility.GetTilesWithinRadius(
                                        HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                transform.position,
                                                radius,
                                                hexagonSize),
                                                transform.position,
                                                radius);
        }

        private void Awake()
        {
            zoneCellManager = GetComponent<ZoneCellManager>();
            waveFunctionCollapse = GetComponent<HexagonWaveFunctionCollapse_1>();
            waveFunctionCollapse.cells = hexagonTileCells;
        }

        private void OnValidate()
        {
            if (zoneCellManager == null)
            {
                zoneCellManager = GetComponent<ZoneCellManager>();
            }

            if (waveFunctionCollapse == null)
            {
                waveFunctionCollapse = GetComponent<HexagonWaveFunctionCollapse_1>();
            }

            bool hasTilePrototypes = hexagonTilePrototypes != null && hexagonTilePrototypes.Count > 0;

            if (resetHexagonTilePrototypes || !hasTilePrototypes)
            {
                resetHexagonTilePrototypes = false;
                GenerateHexagonTilePrototypes();
            }
            else
            {

                if (generateHexagonTileCells && hasTilePrototypes)
                {
                    generateHexagonTileCells = false;

                    GenerateHexagonCellObjects(hexagonTilePrototypes);
                    HexagonCell.PopulateNeighborsFromCornerPoints(hexagonTileCells, 0.33f * (hexagonSize / 12f));

                    waveFunctionCollapse.cells = hexagonTileCells;
                }

                if (generateHexagonTileCellClusters && hexagonTileCells.Count > 1)
                {
                    generateHexagonTileCellClusters = false;

                    cellClusters = HexagonCellCluster.GetHexagonCellClusters(hexagonTileCells,
                                                                        transform.position,
                                                                        WFCCollapseOrder.Default,
                                                                        true);
                }

                if (generateModelingPlatforms && hasTilePrototypes)
                {
                    generateModelingPlatforms = false;
                    GenerateModelingPlatforms(hexagonTilePrototypes);
                }

                if (showEdgeCells)
                {
                    edgeCells = HexagonCell.GetEdgeCells(hexagonTileCells);

                    if (edgeCells.Count == 0)
                    {
                        showEdgeCells = false;
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
            }
        }

        private void OnDrawGizmos()
        {
            if (showBounds)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, radius * transform.lossyScale.x);
                // ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(UtilityHelpers.GetTransformPositions(_edgePoints), transform);
            }

            if (showTileGrid && hexagonTilePrototypes != null && hexagonTilePrototypes.Count > 0)
            {
                Gizmos.color = Color.black;

                // hexagons = HexagonGenerator.GenerateHexagonGrid(6f, 12, 12, Vector3.zero);
                // hexagons = HexagonGenerator.DetermineGridSize(locPoint.position, minVerticePointLevelRadius * 0.8f, hexagonSize, hexagonRowOffsetAdjusterMult);
                for (int i = 0; i < hexagonTilePrototypes.Count; i++)
                {
                    Vector3 pointPos = hexagonTilePrototypes[i].center;//transform.TransformPoint(hexagonTilePrototypes[i].center);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(pointPos, 0.3f);

                    for (int j = 0; j < hexagonTilePrototypes[i].cornerPoints.Length; j++)
                    {
                        pointPos = hexagonTilePrototypes[i].cornerPoints[j];
                        Gizmos.DrawSphere(pointPos, 0.25f);
                    }

                    Gizmos.color = Color.black;
                    ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(hexagonTilePrototypes[i].cornerPoints);
                }
            }

            if (showEdgeCells)
            {
                Gizmos.color = Color.red;

                foreach (HexagonCell cell in edgeCells)
                {
                    Vector3 pointPos = cell.transform.position;
                    Gizmos.DrawSphere(pointPos, 3f);
                }
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

        private void GenerateHexagonCellObjects(List<HexagonTilePrototype> hexagonTilePrototypes)
        {
            List<HexagonCell> newHexagonTileCells = new List<HexagonCell>();
            int cellId = 0;

            Transform folder = new GameObject("Cells").transform;
            folder.transform.SetParent(gameObject.transform);

            foreach (HexagonTilePrototype tilePrototype in hexagonTilePrototypes)
            {
                Vector3 pointPos = tilePrototype.center;

                GameObject newTile = Instantiate(HexagonTileCell_prefab, pointPos, Quaternion.identity);
                HexagonCell hexagonTile = newTile.GetComponent<HexagonCell>();
                hexagonTile._cornerPoints = tilePrototype.cornerPoints;
                hexagonTile.size = hexagonSize;
                hexagonTile.id = cellId;
                hexagonTile.name = "HexagonCell_" + cellId;

                newHexagonTileCells.Add(hexagonTile);

                hexagonTile.transform.SetParent(folder);

                cellId++;
            }
            hexagonTileCells = newHexagonTileCells;
        }

        private void GenerateModelingPlatforms(List<HexagonTilePrototype> hexagonTilePrototypes)
        {
            foreach (HexagonTilePrototype tilePrototype in hexagonTilePrototypes)
            {
                Vector3 pointPos = tilePrototype.center;
                pointPos.y += 0.2f;

                GameObject newModel = Instantiate(HexagonTilePlatformModel_prefab, pointPos, Quaternion.identity);
            }
        }
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