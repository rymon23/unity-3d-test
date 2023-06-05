using System;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    [CreateAssetMenu(fileName = "New Location Prefab", menuName = "Location Prefab")]
    public class LocationPrefab : ScriptableObject
    {
        [SerializeField] private LocationMarkerPrefabOption locationMarkerPrefab;
        [SerializeField] private List<LocationNeighborBufferRule> locationNeighborBufferRules;
        public LocationMarkerPrefabOption GetSettings() => locationMarkerPrefab;
        public List<LocationNeighborBufferRule> GetNeighborBufferRules() => locationNeighborBufferRules;
        public Color color = Color.green;
        public bool ShouldPreassign() => ShouldPreassignLocationPrefab(GetSettings());
        public static bool ShouldPreassignLocationPrefab(LocationMarkerPrefabOption prefabOption) =>
                prefabOption.gridPreset == GridPreset.Town ||
                prefabOption.gridPreset == GridPreset.City ||
                prefabOption.worldSpacesMin > 0;
    }
}