using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    public class WorldLocationManager : MonoBehaviour
    {
        #region Singleton
        private static WorldLocationManager _instance;

        public static WorldLocationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldLocationManager>();
                }
                return _instance;
            }
        }
        private void Awake()
        {
            // Make sure only one instance of WorldAreaManager exists in the scene
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        #endregion




        // public static Dictionary<Vector2, > Instantiate_WorldLocationObjects(
        //     List<LocationData> locationData,
        //     GameObject locationPrefab,
        //     Transform parentFolder = null
        // )
        // {
        //     if (locationData == null || locationData.Count == 0)
        //     {
        //         Debug.LogError("locationData Null");
        //         return;
        //     }

        //     // GetLocationBorderPointsFromGridEdgeVertices(locationData, vertexGrid);
        //     List<GameObject> newLocationGO = SpawnLocations(locationData, locationPrefab, parentFolder);     
        //     List<Territory> new_territories = new List<Territory>();
        //     List<Location> new_locations = new List<Location>();

        //     if (newLocations.Count > 0)
        //     {
        //         _locations = new List<Location>();
        //         _territories = new List<Territory>();
        //         foreach (var item in newLocations)
        //         {
        //             Location newLoc = item.GetComponent<Location>();
        //             Territory newTerr = item.GetComponent<Territory>();
        //             if (newLoc != null)
        //             {
        //                 newLoc.SetWorldArea(this);
        //                 newLoc.Revaluate();
        //                 _locations.Add(newLoc);
        //             }
        //             if (newTerr != null) _territories.Add(newTerr);
        //         }
        //     }

        //     // Invoke("EvaluateTerritories", 0.6f);
        // }

        // private static List<GameObject> SpawnLocationObjects(
        //     List<LocationData> locationData,
        //     GameObject locationPrefab,
        //     Transform parentFolder = null
        // )
        // {
        //     // if (locationPrefab == null || locationData == null) return;
        //     List<GameObject> newLocations = new List<GameObject>();

        //     Transform parent = new GameObject("Locations").transform;
        //     foreach (LocationData data in locationData)
        //     {
        //         GameObject go = Instantiate(locationPrefab, data.centerPosition, Quaternion.identity);
        //         go.transform.SetParent(parent);

        //         Location location = go.GetComponent<Location>();
        //         Territory newTerr = item.GetComponent<Territory>();

        //         location.SetLocationData(data);
        //         // location.Revaluate();
        //         newLocations.Add(go);
        //     }

        //     if (parentFolder != null) parent.SetParent(parentFolder);

        //     return newLocations;
        // }

        // private static GameObject SpawnLocationObject(
        //     LocationData locationData,
        //     GameObject locationPrefab
        // )
        // {
        //     GameObject go = Instantiate(locationPrefab, locationData.centerPosition, Quaternion.identity);

        //     Location location = go.GetComponent<Location>();
        //     Territory territory = go.GetComponent<Territory>();
        //     location.SetLocationData

        //         location.SetLocationData(data);
        //     // location.Revaluate();
        //     newLocations.Add(go);


        //     return newLocations;
        // }
        // //



        public static (List<LocationPrefab>, List<LocationPrefab>) Evaluate_LocationMarkerPrefabs(List<LocationPrefab> allLocationPrefabs)
        {
            if (allLocationPrefabs == null || allLocationPrefabs.Count == 0) return (allLocationPrefabs, null);

            List<LocationPrefab> filteredDefault = new List<LocationPrefab>();
            List<LocationPrefab> preassignable = new List<LocationPrefab>();

            foreach (var locationPrefab in allLocationPrefabs)
            {
                if (locationPrefab.ShouldPreassign())
                {
                    if (!preassignable.Contains(locationPrefab)) preassignable.Add(locationPrefab);
                }
                else
                {
                    if (!filteredDefault.Contains(locationPrefab)) filteredDefault.Add(locationPrefab);
                }
            }
            return (filteredDefault, preassignable);
        }

        public static bool IsPointWithinLocationExclusionRange(
            Vector3 position,
            LocationPrefab locationPrefab,
            Dictionary<LocationType, List<LocationData>> placedLocationsByType
        )
        {
            List<LocationNeighborBufferRule> neighborBufferRules = locationPrefab.GetNeighborBufferRules();
            if (neighborBufferRules.Count == 0) return false;

            foreach (var rule in neighborBufferRules)
            {
                if (placedLocationsByType.ContainsKey(rule.locationType))
                {
                    // if (placedLocationsByType[rule.locationType].Any(loc => Vector3.Distance(loc.centerPosition, position) < (loc.radius * rule.minDistanceMult)))
                    if (placedLocationsByType[rule.locationType].Any(loc => Vector3.Distance(loc.centerPosition, position) < ((loc.radius / 2f) + rule.minDistance)))
                    {
                        // Debug.LogError("PointWithinLocationExclusionRange - locationType: " + rule.locationType);
                        return true;
                    }
                }
            }
            return false;
        }


        public static HexagonCellPrototype FindViableCellForLocationPrefab(
            LocationPrefab locationPrefab,
            List<HexagonCellPrototype> allAvailableCells,
            Dictionary<LocationType, List<LocationData>> placedLocationsByType
        )
        {
            List<HexagonCellPrototype> possible = allAvailableCells.FindAll(c =>
                                c.IsPreAssigned() == false &&
                                IsPointWithinLocationExclusionRange(c.center, locationPrefab, placedLocationsByType) == false
                            );
            if (possible.Count == 0) return null;

            return possible[0];
        }

        public HexagonCellPrototype World_GetRandomPoint(List<HexagonCellPrototype> allAvailable, List<Vector3> avoidPoints, float avoidRadius)
        {
            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            // HashSet<int> uncheckedIndexs = new HashSet<int>();
            // List<HexagonCellPrototype> shuffled = HexagonCellPrototype.Shuffle(allAvailable);

            HashSet<int> visited = new HashSet<int>();
            bool found = false;
            int attempts = 999;
            HexagonCellPrototype current = null;

            while (!found && attempts > 0)
            {
                attempts--;
                int IX = UnityEngine.Random.Range(0, allAvailable.Count);
                if (visited.Contains(IX)) continue;

                current = allAvailable[IX];
                if (current == null || avoidPoints.Contains(current.center) || current.IsPreAssigned())
                {
                    visited.Add(IX);
                    continue;
                }

                (Vector3 point, float dist) = VectorUtil.GetClosestPoint_XZ_WithDistance(avoidPoints, current.center);
                if (point != Vector3.positiveInfinity && dist < avoidRadius)
                {
                    visited.Add(IX);
                    continue;
                }

                found = true;
                break;
            }

            return current;
        }
    }
}