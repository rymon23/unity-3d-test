using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    [System.Serializable]
    public class WorldAreaObjectData
    {
        public WorldAreaObjectData(Vector2 _worldAreaLookup, Transform _parentFolder)
        {
            worldAreaLookup = _worldAreaLookup;
            terrainChunks = new List<GameObject>();
            worldspaceObjectData = new List<WorldspaceObjectData>();

            Evalaute_Folders(_parentFolder);
        }

        public Vector2 worldAreaLookup;
        public List<GameObject> terrainChunks = new List<GameObject>();
        public List<WorldspaceObjectData> worldspaceObjectData = new List<WorldspaceObjectData>();
        public Transform folder_Main { get; private set; } = null;
        public Transform folder_terrain { get; private set; } = null;
        public Transform folder_water { get; private set; } = null;
        // public Transform folder_tunnels { get; private set; } = null;

        public Transform MainFolder() => folder_Main;
        public Transform TerrainFolder() => folder_terrain;
        public WorldspaceObjectData GetWorldspaceObjectData(int index) => worldspaceObjectData.Count > index ? worldspaceObjectData[index] : null;
        public WorldspaceObjectData AddWorldspaceObjectData(Vector2 worldspaceLookup, Dictionary<Vector2, Dictionary<Vector2, WorldspaceData>> _worldspaceData_ByArea)
        {
            if (_worldspaceData_ByArea.ContainsKey(worldAreaLookup) == false) _worldspaceData_ByArea.Add(worldAreaLookup, new Dictionary<Vector2, WorldspaceData>());

            if (_worldspaceData_ByArea[worldAreaLookup].ContainsKey(worldspaceLookup) == false)
            {
                WorldspaceData newWorldspaceData = new WorldspaceData(worldAreaLookup, worldspaceLookup);
                _worldspaceData_ByArea[worldAreaLookup].Add(worldspaceLookup, newWorldspaceData);

                WorldspaceObjectData newWorldspaceObjectData = new WorldspaceObjectData(worldAreaLookup, worldspaceLookup, MainFolder());

                int new_objectIndex = -1;
                if (worldspaceObjectData.Count == 0)
                {
                    new_objectIndex = 0;
                    worldspaceObjectData.Add(newWorldspaceObjectData);
                }
                else
                {
                    worldspaceObjectData.Add(newWorldspaceObjectData);
                    new_objectIndex = worldspaceObjectData.Count - 1;
                }

                if (new_objectIndex > -1) _worldspaceData_ByArea[worldAreaLookup][worldspaceLookup].SetObjectIndex(new_objectIndex);

                return newWorldspaceObjectData;
            }
            else
            {
                return GetWorldspaceObjectData(_worldspaceData_ByArea[worldAreaLookup][worldspaceLookup].objectIndex);
            }
        }
        public void Evalaute_Folders(Transform _parentFolder)
        {
            if (folder_Main == null)
            {
                folder_Main = new GameObject("Area Objects_" + worldAreaLookup).transform;
                folder_Main.transform.SetParent(_parentFolder);
            }
            if (folder_terrain == null)
            {
                folder_terrain = new GameObject("Terrain Chunks").transform;
                folder_terrain.transform.SetParent(folder_Main);
            }
            if (folder_water == null)
            {
                folder_water = new GameObject("Water").transform;
                folder_water.transform.SetParent(folder_Main);
            }
            // if (folder_tunnels == null)
            // {
            //     folder_tunnels = new GameObject("Tunnels").transform;
            //     folder_tunnels.transform.SetParent(folder_Main);
            // }
        }
    }
}