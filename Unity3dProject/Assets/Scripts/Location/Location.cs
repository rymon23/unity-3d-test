using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                    _subZones[i].Debug_ShowTiles(showZoneBounds);
                    _subZones[i].Debug_ShowBounds(showTileGrid);
                    _subZones[i].Debug_ResetHexagonTilePrototypes(resetHexagonTilePrototypes);
                    _subZones[i].Debug_GenerateHexagonTileCells(generateHexagonTileCells);
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