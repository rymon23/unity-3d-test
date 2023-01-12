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
    }

    [System.Serializable]
    public struct LocationPrototype
    {
        public Vector3 position;
        public float radius;
        public List<SubzonePrototype> subzonePrototypes;
        public List<ZoneConnector> subzoneConnectors;
    }

    // [System.Serializable]
    // public struct LocationPrototype
    // {
    //     public Vector3 position;
    //     public float radius;
    //     public Vector3 zonesCenter;
    //     public Vector3[] subZonePoints;
    //     public Vector3[] zoneConnectors;
    //     public ZoneConnectorPair[] zoneConnectorPairs;
    //     public List<Vector3[]> zoneCorners;
    //     public List<Vector3[]> zoneRoadPoints;
    //     public List<Vector3[,]> zoneGrid;

    //     public List<List<Hexagon>> zoneHexGrid;
    // }
}