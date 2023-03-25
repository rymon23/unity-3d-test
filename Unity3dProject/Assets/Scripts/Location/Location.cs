using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

namespace ProceduralBase
{
    public enum LocationType
    {
        Unset = 0,
        Outpost,
        City,
        Dungeon,
        Settlement,
        Town,
    }
    public enum LocationDistrictType
    {
        Unset = 0,
        Mixed,
        Residential,
        Royal,
        Business,
        Military,
    }


    public class Location : MonoBehaviour
    {
        [SerializeField] private List<Vector3> locationCenterPoints;
        [SerializeField] private List<SubZone> _subZones;
        [SerializeField] private float totalSize;
        [SerializeField] private float radius;
        public List<GameObject> objects;
        public ProceduralTerrainMesh9.Vertex[] verticeData;

        public void MapFromPrototype(LocationPrototype prototype, List<SubZone> subZones)
        {
            radius = prototype.radius;
            _subZones = subZones;
        }

        //Districts

        //Roads
        #region TO DO
        public void PopulateLocation() { }
        #endregion

        //tiles

        //buildings

        //Entrances 

        //Faction Owner

        //Neighbors

        //Population

        //Economy

        //Governance

        //Spawn Points


        [Header("Sub-Zone Controller")]
        [SerializeField] private bool showZoneBounds;
        [SerializeField] private bool showTileGrid;
        [SerializeField] private bool resetHexagonTilePrototypes;
        [SerializeField] private bool generateHexagonTileCells;
        private bool _showZoneBounds;
        private bool _showTileGrid;
        private bool _resetHexagonTilePrototypes;
        private bool _generateHexagonTileCells;


        private void OnValidate()
        {
            if (_showTileGrid != showTileGrid ||
                _showZoneBounds != showZoneBounds ||
                _resetHexagonTilePrototypes != resetHexagonTilePrototypes ||
                _generateHexagonTileCells != generateHexagonTileCells)
            {

                for (int i = 0; i < _subZones.Count; i++)
                {
                    if (_subZones[i] != null)
                    {
                        _subZones[i].Debug_ShowTiles(showZoneBounds);
                        _subZones[i].Debug_ShowBounds(showTileGrid);
                        _subZones[i].Debug_ResetHexagonellPrototypes(resetHexagonTilePrototypes);
                        _subZones[i].Debug_GenerateHexagonCells(generateHexagonTileCells);
                    }
                }

                if (_resetHexagonTilePrototypes != resetHexagonTilePrototypes ||
                _generateHexagonTileCells != generateHexagonTileCells)
                {
                    resetHexagonTilePrototypes = false;
                    generateHexagonTileCells = false;
                }

                _showTileGrid = showTileGrid;
                _showZoneBounds = showZoneBounds;
                _resetHexagonTilePrototypes = resetHexagonTilePrototypes;
                _generateHexagonTileCells = generateHexagonTileCells;
            }

            if (generateSubzoneCells)
            {
                generateSubzoneCells = false;
                GenerateSubZones();
            }
        }



        public int maxSubzones = 3;
        public List<HexagonCellPrimitive> subZoneCells;
        public List<Vector3> zoneConnectorPoints;
        private void GenerateSubZones()
        {
            List<HexagonTilePrototype> hexagonTilePrototypes = LocationUtility.GetTilesWithinRadius(
                                        HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                transform.position,
                                                withinRadius,
                                                zoneSize),
                                                transform.position,
                                                withinRadius);
            List<HexagonCellPrimitive> newSubZoneCells = HexagonCellPrimitive.GenerateHexagonCellPrimitives(hexagonTilePrototypes, hexagonCellPrimitive_prefab, transform);
            HexagonCellPrimitive.PopulateNeighborsFromCornerPoints(newSubZoneCells, 0.33f * (zoneSize / 12f));

            subZoneCells = HexagonCellPrimitive.GetRandomConsecutiveSetOfCount(newSubZoneCells, maxSubzones, transform.position);

            List<HexagonCellPrimitive> toRemove = new List<HexagonCellPrimitive>();
            toRemove.AddRange(newSubZoneCells.Except(subZoneCells));

            for (int i = 0; i < toRemove.Count; i++)
            {
                toRemove[i].gameObject.SetActive(false);
                // Destroy(toRemove[i].gameObject);
            }

            if (subZoneCells.Count > 0)
            {
                Transform folder = new GameObject("Zones").transform;
                folder.transform.SetParent(transform);

                foreach (HexagonCellPrimitive item in subZoneCells)
                {
                    item.gameObject.transform.SetParent(folder);
                }
            }

            zoneConnectorPoints = HexagonCellPrimitive.GetZoneConnectorPoints(subZoneCells, 0.33f * (zoneSize / 12f));
        }


        //         public static void DeleteGameObjects(List<HexagonCellPrimitive> gameObjects)
        //         {
        // #if UNITY_EDITOR
        //             foreach (HexagonCellPrimitive go in gameObjects)
        //             {
        //                 Selection.activeGameObject = go.gameObject;
        //                 UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Delete");
        //             }
        // #endif
        //         }


        [Range(264, 640)] public int withinRadius = 500;
        [Range(24, 360)] public int zoneSize = 102;

        public List<HexagonTilePrototype> hexagonTilePrototypesTEMP;
        [SerializeField] private bool showZBounds;
        [SerializeField] private bool generateSubzoneCells;
        int _withinRadius;
        int _zoneSize;

        public GameObject hexagonCellPrimitive_prefab;

        private void OnDrawGizmos()
        {
            if (showZBounds)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, withinRadius * transform.lossyScale.x);


                if (withinRadius != _withinRadius || zoneSize != _zoneSize || hexagonTilePrototypesTEMP == null || hexagonTilePrototypesTEMP.Count == 0)
                {

                    hexagonTilePrototypesTEMP = LocationUtility.GetTilesWithinRadius(
                                            HexagonGenerator.DetermineHexagonTilePrototypeGrideSize(
                                                    transform.position,
                                                    withinRadius,
                                                    zoneSize),
                                                    transform.position,
                                                    withinRadius);
                }

                if (hexagonTilePrototypesTEMP.Count == 0) return;

                Gizmos.color = Color.red;

                for (int i = 0; i < hexagonTilePrototypesTEMP.Count; i++)
                {
                    Vector3 pointPos = hexagonTilePrototypesTEMP[i].center;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(pointPos, 0.3f);

                    for (int j = 0; j < hexagonTilePrototypesTEMP[i].cornerPoints.Length; j++)
                    {
                        pointPos = hexagonTilePrototypesTEMP[i].cornerPoints[j];
                        Gizmos.DrawSphere(pointPos, 0.25f);
                    }

                    Gizmos.color = Color.black;
                    ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(hexagonTilePrototypesTEMP[i].cornerPoints);
                }
            }


            if (zoneConnectorPoints.Count > 0)
            {
                foreach (Vector3 item in zoneConnectorPoints)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(item, 3f);
                }
            }

        }

        public List<HexagonCellPrimitive> GenerateHexagonCellPrimitives(List<HexagonTilePrototype> hexagonTilePrototypes, GameObject prefab, Transform transform)
        {
            List<HexagonCellPrimitive> hexagonCellPrimitives = new List<HexagonCellPrimitive>();
            int cellId = 0;

            Transform folder = new GameObject("HexagonCellPrimitives").transform;
            folder.transform.SetParent(transform);

            foreach (HexagonTilePrototype prototype in hexagonTilePrototypes)
            {
                Vector3 pointPos = prototype.center;

                GameObject newTile = Instantiate(prefab, pointPos, Quaternion.identity);
                HexagonCellPrimitive hexagonTile = newTile.GetComponent<HexagonCellPrimitive>();
                hexagonTile._cornerPoints = prototype.cornerPoints;
                hexagonTile.size = prototype.size;
                hexagonTile.SetID(cellId);
                hexagonTile.name = "HexagonCellPrimitive_" + cellId;

                hexagonCellPrimitives.Add(hexagonTile);

                hexagonTile.transform.SetParent(folder);

                cellId++;
            }
            return hexagonCellPrimitives;
        }


    }

    [System.Serializable]
    public struct LocationPrototype
    {
        public Vector3 position;
        public float radius;
        public List<SubzonePrototype> subzonePrototypes;
        public List<ZoneConnector> subzoneConnectors;
    }
}