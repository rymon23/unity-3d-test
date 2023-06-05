using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    [System.Serializable]
    public class WorldRegionObjectData
    {
        public WorldRegionObjectData(Vector2 _cellLookup, Transform _parentFolder)
        {
            cellLookup = _cellLookup;
            Evalaute_Folders(_parentFolder);
        }

        public Vector2 cellLookup;
        [SerializeField] private List<WorldAreaObjectData> _worldAreaObjectData = null;

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
                folder_Main = new GameObject("Region Object Data_" + cellLookup).transform;
                folder_Main.transform.SetParent(_parentFolder);
            }
        }
    }
}