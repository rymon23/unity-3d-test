using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using WFCSystem;

namespace ProceduralBase
{
    public interface ILocation
    {
        public Territory GetTerritory();
        public LocationData GetLocationData();
        public void SetLocationData(LocationData data);
        public float GetRadius();
        public Faction GetFactionOwner();
        public Vector3[] GetEntryPoints();
        public Vector3[] GetSpawnPoints();
        public Vector3[] GetBorderPoints();
    }

    public class Location : MonoBehaviour, ILocation
    {
        public static float borderPointsRadiusMult = 1.25f;
        [SerializeField] private WorldArea _worldArea;
        public WorldArea GetWorldArea() => _worldArea;
        public void SetWorldArea(WorldArea worldArea)
        {
            _worldArea = worldArea;
        }
        [SerializeField] private Territory territory;
        [SerializeField] private LocationData locationData;
        [SerializeField] private List<Vector3> locationCenterPoints;

        [SerializeField] private int borderWaypointMax = 10;

        #region Interface Methods
        public Territory GetTerritory() => territory;
        public LocationData GetLocationData() => locationData;
        public void SetLocationData(LocationData data)
        {
            locationData = data;
        }
        public float GetRadius() => locationData.radius;
        [SerializeField] private float _radius_debug;

        public Faction GetFactionOwner()
        {
            if (territory != null) return territory.GetOwner();
            return null;
        }
        public Vector3[] GetEntryPoints() => locationData?.entryPoints;
        public Vector3[] GetSpawnPoints() => locationData?.spawnPoints;
        public Vector3[] GetBorderPoints() => locationData?.borderPoints;

        #endregion

        #region Tiles
        List<GameObject> tiles;
        #endregion

        public Vector3? GetRandomBorderPoint()
        {
            if (locationData.borderPoints == null) return null;
            return locationData?.borderPoints[Random.Range(0, locationData.borderPoints.Length)];
        }

        private void GenerateBorderPoints()
        {
            locationData.borderPoints = GenerateBorderPoints(this.transform, locationData.radius * borderPointsRadiusMult, borderWaypointMax);
        }

        public static Vector3[] GenerateBorderPoints(Transform transform, float radius, int amount, float distanceOffset = 6f)
        {
            List<Vector3> newPoints = new List<Vector3>();
            float angle = 0f;
            float incAmount = (360f / amount);

            int attempts = 999;

            while (attempts > 0 && newPoints.Count < amount)
            {
                Vector3 pos = transform.position + Quaternion.AngleAxis(angle, transform.up) * (Vector3.forward * radius);
                bool getPoint = UtilityHelpers.GetCloseNavMeshPoint(pos, distanceOffset, out Vector3 point, 50);
                if (getPoint)
                {
                    newPoints.Add(point);
                    angle += incAmount;
                }
                {
                    attempts--;
                }
            }

            return newPoints.ToArray();
        }

        #region TO DO
        public void PopulateLocation() { }
        #endregion

        //buildings
        //Neighbors
        //Population


        private void InitialSetup()
        {
            territory = GetComponent<Territory>();
            _radius_debug = GetRadius();
        }

        public void Revaluate()
        {
            InitialSetup();
            GenerateBorderPoints();
        }

        private void Start()
        {
            InitialSetup();
        }

        [Header("Debug Settings")]
        [SerializeField] private bool showBorderPoints;

        private void OnDrawGizmos()
        {
            // Gizmos.DrawWireSphere(this.transform.position, radius);
            // Gizmos.DrawWireSphere(this.transform.position, GetMinInvasionRadius());
            Gizmos.color = Color.green;
            float pointSize = 1f;

            if (showBorderPoints && locationData?.borderPoints != null && locationData.borderPoints.Length > 0)
            {
                foreach (Vector3 item in locationData.borderPoints)
                {
                    Gizmos.DrawSphere(item, pointSize);
                }
                // }
                // else
                // {
                // Gizmos.DrawWireSphere(this.transform.position + (Vector3.forward * radius), pointSize);
                // Gizmos.DrawWireSphere(this.transform.position + (-Vector3.forward * radius), pointSize);
                // Gizmos.DrawWireSphere(this.transform.position + (Vector3.right * radius), pointSize);
                // Gizmos.DrawWireSphere(this.transform.position + (-Vector3.right * radius), pointSize);
            }
        }
    }

    [System.Serializable]
    public struct LocationPrototype
    {
        public Vector3 position;
        public float radius;
    }

    [System.Serializable]
    public struct LocationNeighborBufferRule
    {
        public LocationType locationType;
        // [Range(0f, 10f)] public float minDistanceMult;
        [Range(0f, 964f)] public float minDistance;
    }

    [System.Serializable]
    public struct LocationMarkerPrefabOption
    {
        public LocationMarkerPrefabOption(
            GridPreset _gridPreset,
            LocationType _locationType,
            TileDirectory _tileDirectory,
            HexagonSocketDirectory _socketDirectory,
            CellSearchPriority _cellSearchPriority,
            bool _enableTunnels,
            int _defaultNeightborBufferDistanceMin,
            int _priority,
            int _cluster_memberMin,
            int _cluster_memberMax,
            int _cluster_TunnelMemberMin,
            int _cluster_TunnelMemberMax,
            int _worldSpacesMin,
            int _worldSpacesMax,
            Vector2 _elevationRangeMinMax
        )
        {
            gridPreset = _gridPreset;
            locationType = _locationType;
            cellSearchPriority = _cellSearchPriority;
            enableTunnels = _enableTunnels;
            defaultNeightborBufferDistanceMin = _defaultNeightborBufferDistanceMin;
            priority = _priority;
            tileDirectory = _tileDirectory;
            socketDirectory = _socketDirectory;
            cluster_memberMin = _cluster_memberMin;
            cluster_memberMax = _cluster_memberMax;
            cluster_TunnelMemberMin = _cluster_memberMin;
            cluster_TunnelMemberMax = _cluster_memberMax;
            worldSpacesMin = _worldSpacesMin;
            worldSpacesMax = _worldSpacesMax;
            elevationRangeMinMax = _elevationRangeMinMax;
        }
        public LocationType locationType;
        public GridPreset gridPreset;
        [Header(" ")]
        public TileDirectory tileDirectory;
        public HexagonSocketDirectory socketDirectory;
        [Header(" ")]
        public Vector2 elevationRangeMinMax;
        [Header(" ")]
        public int defaultNeightborBufferDistanceMin;
        public int priority;
        public int cluster_memberMin;
        public int cluster_memberMax;

        [Header(" ")]
        public int worldSpacesMin;
        public int worldSpacesMax;

        [Header(" ")]
        public bool enableTunnels;
        public int cluster_TunnelMemberMin;
        public int cluster_TunnelMemberMax;
        public CellSearchPriority cellSearchPriority;

    }

}
