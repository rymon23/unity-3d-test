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

            Evalaute_Folders(_parentFolder);
        }

        public Vector2 worldAreaLookup;
        public List<GameObject> terrainChunks = new List<GameObject>();
        public Transform folder_Main { get; private set; } = null;
        public Transform folder_terrain { get; private set; } = null;
        public Transform folder_tunnels { get; private set; } = null;
        public Transform folder_trees { get; private set; } = null;
        public Transform folder_water { get; private set; } = null;
        public Transform MainFolder() => folder_Main;
        public Transform TerrainFolder() => folder_terrain;

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
            if (folder_tunnels == null)
            {
                folder_tunnels = new GameObject("Tunnels").transform;
                folder_tunnels.transform.SetParent(folder_Main);
            }
            if (folder_trees == null)
            {
                folder_trees = new GameObject("Trees").transform;
                folder_trees.transform.SetParent(folder_Main);
            }
        }
    }
}