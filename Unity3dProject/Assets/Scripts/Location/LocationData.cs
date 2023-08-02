using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        Building,
        Tunnel
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

    public class LocationData
    {
        public string id { get; private set; }
        public string uid { get; private set; }
        public LocationType locationType;
        public HexagonCellCluster cluster { get; private set; }
        public HashSet<Vector2> cellMemberLookups { get; private set; }
        public Bounds bounds;
        public float radius { get; private set; }
        public Vector3 centerPosition;
        public List<Vector3> boundsCenterPoints;
        public Vector3[] borderPoints;
        public Vector3[] entryPoints;
        public Vector3[] spawnPoints;
        public LocationMarkerPrefabOption prefabSettings { get; private set; }

        // public LocationData(string id, Vector3 centerPostion, LocationType type, LocationMarkerPrefabOption _prefabSettings, float radius)
        // {
        //     this.id = id;
        //     this.uid = UtilityHelpers.GenerateUniqueID(id);
        //     this.locationType = type;
        //     this.prefabSettings = _prefabSettings;
        //     this.centerPosition = centerPostion;
        //     this.radius = radius;
        //     this.boundsCenterPoints = new List<Vector3>();
        //     this.boundsCenterPoints.Add(centerPosition);
        // }

        public LocationData(HexagonCellCluster cluster, LocationType type, LocationMarkerPrefabOption _prefabSettings)
        {
            this.id = id;
            this.uid = UtilityHelpers.GenerateUniqueID(id);
            this.locationType = type;
            this.prefabSettings = _prefabSettings;
            this.centerPosition = cluster.centerPosition;
            this.boundsCenterPoints = cluster.boundsCenterPoints;
            this.cluster = cluster;
            this.bounds = cluster.CalculateBounds();
            this.radius = cluster.radius;
            // this.radius = VectorUtil.CalculateBoundingSphereRadius(this.bounds);
            // Debug.Log("New Location: " + locationType + ", radius: " + radius);
        }

        public static LocationData GenerateLocationDataFromCluster(HexagonCellCluster cluster, LocationType locationType, LocationMarkerPrefabOption prefabSettings)
        {
            return new LocationData(cluster, locationType, prefabSettings);
        }

        public static List<LocationData> GenerateLocationDataFromClusters(List<HexagonCellCluster> clusters, LocationType locationType, LocationMarkerPrefabOption prefabSettings)
        {
            List<LocationData> locationData = new List<LocationData>();
            foreach (HexagonCellCluster item in clusters)
            {
                locationData.Add(GenerateLocationDataFromCluster(item, locationType, prefabSettings));
            }
            return locationData;
        }
    }
}