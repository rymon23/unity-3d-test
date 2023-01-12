using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;

namespace ProceduralBase
{
    public class SubZone : MonoBehaviour
    {
        [SerializeField] private Location _locationParent;
        [Range(24f, 128f)][SerializeField] private float radius = 102f;
        [SerializeField] private int hexagonSize = 12;
        [SerializeField] private List<int> neighbors;
        [SerializeField] private List<HexagonTile> tiles;
        [SerializeField] private Vector3[] borderCorners;
        [SerializeField] private Vector3[] zoneConnectors;

        public void MapFromPrototype(SubzonePrototype subzonePrototype, Location locationParent)
        {
            _locationParent = locationParent;
            radius = subzonePrototype.radius;
            borderCorners = subzonePrototype.borderCorners;
        }

        [Header("Debug Settings")]
        [SerializeField] private bool showBounds;
        [SerializeField] private bool showTileGrid;
        [SerializeField] private bool resetHexagonTilePrototypes;

        [SerializeField] private bool generateHexagonTileCells;

        List<HexagonTilePrototype> hexagonTilePrototypes;
        [SerializeField] private List<HexagonTile> hexagonTileCells;

        [Header("Prefabs")]
        [SerializeField] private GameObject HexagonTileCell_prefab;

        private void GenerateHexagonTilePrototypes()
        {
            hexagonTilePrototypes = LocationUtility.GetTilesWithinRadius(HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(transform.position, radius, hexagonSize),
                                                transform.position,
                                                radius);
        }

        private void OnValidate()
        {
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

                    GenerateHexagonTileCellObjects(hexagonTilePrototypes);

                    HexagonTile.PopulateNeighborsFromCornerPoints(hexagonTileCells, 0.33f);
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

        }

        private void GenerateHexagonTileCellObjects(List<HexagonTilePrototype> hexagonTilePrototypes)
        {
            List<HexagonTile> newHexagonTileCells = new List<HexagonTile>();

            foreach (HexagonTilePrototype tilePrototype in hexagonTilePrototypes)
            {
                Vector3 pointPos = tilePrototype.center;

                GameObject newTile = Instantiate(HexagonTileCell_prefab, pointPos, Quaternion.identity);
                HexagonTile hexagonTile = newTile.GetComponent<HexagonTile>();
                hexagonTile._cornerPoints = tilePrototype.cornerPoints;
                hexagonTile.size = hexagonSize;

                newHexagonTileCells.Add(hexagonTile);

                hexagonTile.transform.SetParent(gameObject.transform);

            }
            hexagonTileCells = newHexagonTileCells;
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