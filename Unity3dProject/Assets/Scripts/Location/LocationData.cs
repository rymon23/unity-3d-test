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
        public HexagonCellCluster cluster { get; private set; }
        public LocationType locationType;
        public float radius { get; private set; }
        public Vector3 centerPosition;
        public List<Vector3> boundsCenterPoints;
        public List<Vector3> borderPoints;
        public List<Vector3> entryPoints;

        public LocationData(string id, Vector3 centerPostion, LocationType type, float radius)
        {
            this.id = id;
            this.uid = UtilityHelpers.GenerateUniqueID(id);
            this.locationType = type;
            this.centerPosition = centerPostion;
            this.radius = radius;
            this.boundsCenterPoints = new List<Vector3>();
            this.boundsCenterPoints.Add(centerPosition);
        }

        public LocationData(HexagonCellCluster cluster, LocationType type)
        {
            this.id = id;
            this.uid = UtilityHelpers.GenerateUniqueID(id);
            this.locationType = type;
            this.centerPosition = cluster.centerPosition;
            this.radius = cluster.radius;
            this.boundsCenterPoints = cluster.boundsCenterPoints;
            this.cluster = cluster;
        }

        public static LocationData GenerateLocationDataFromCluster(HexagonCellCluster cluster, LocationType locationType)
        {
            return new LocationData(cluster, locationType);
        }

        public static List<LocationData> GenerateLocationDataFromClusters(List<HexagonCellCluster> clusters, LocationType locationType)
        {
            List<LocationData> locationData = new List<LocationData>();
            foreach (HexagonCellCluster item in clusters)
            {
                locationData.Add(GenerateLocationDataFromCluster(item, LocationType.Outpost));
            }
            return locationData;
        }
    }
}