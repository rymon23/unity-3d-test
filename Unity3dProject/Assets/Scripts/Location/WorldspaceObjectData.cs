using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    [System.Serializable]
    public class WorldspaceObjectData
    {
        public WorldspaceObjectData(Vector2 _worldAreaLookup, Vector2 _worldspaceLookup, Transform _parentFolder)
        {
            worldAreaLookup = _worldAreaLookup;
            worldspaceLookup = _worldspaceLookup;
            trees = new List<GameObject>();
            tunnels = new List<GameObject>();
            //temp
            treeSpawnPoints = new List<Vector3>();

            Evalaute_Folders(_parentFolder);
        }

        public Vector2 worldAreaLookup { get; private set; }
        public Vector2 worldspaceLookup { get; private set; }
        public List<GameObject> tunnels = new List<GameObject>();
        public List<GameObject> trees = new List<GameObject>();
        public List<Vector3> treeSpawnPoints = new List<Vector3>();

        public Transform folder_Main { get; private set; } = null;
        public Transform folder_trees { get; private set; } = null;
        public Transform folder_tunnels { get; private set; } = null;

        public Transform MainFolder() => folder_Main;
        public Transform TunnelFolder() => folder_tunnels;
        public Transform TreeFolder() => folder_trees;

        public void Evalaute_Folders(Transform _parentFolder)
        {
            if (folder_Main == null)
            {
                folder_Main = new GameObject("Worldspace Objects_" + worldspaceLookup).transform;
                folder_Main.transform.SetParent(_parentFolder);
            }
            if (folder_trees == null)
            {
                folder_trees = new GameObject("Trees").transform;
                folder_trees.transform.SetParent(folder_Main);
            }
            if (folder_tunnels == null)
            {
                folder_tunnels = new GameObject("Tunnels").transform;
                folder_tunnels.transform.SetParent(folder_Main);
            }
        }
    }


    [System.Serializable]
    public class WorldspaceData
    {
        public WorldspaceData(Vector2 _worldAreaLookup, Vector2 _worldspaceLookup)
        {
            worldAreaLookup = _worldAreaLookup;
            worldspaceLookup = _worldspaceLookup;
        }

        public WorldspaceData(WorldspaceObjectJsonData jsonData)
        {
            worldAreaLookup = jsonData.worldAreaLookup.ToVector2();
            worldspaceLookup = jsonData.worldspaceLookup.ToVector2();
            objectIndex = jsonData.objectIndex;
        }

        public Vector2 worldAreaLookup { get; private set; }
        public Vector2 worldspaceLookup { get; private set; }
        public int objectIndex { get; private set; } = -1;
        public void SetObjectIndex(int index)
        {
            objectIndex = index;
        }

        public static WorldspaceObjectJsonData ConvertToJson(WorldspaceData data)
        {
            WorldspaceObjectJsonData newJsonData = new WorldspaceObjectJsonData();
            newJsonData.worldAreaLookup = new Vector2Serialized(data.worldAreaLookup);
            newJsonData.worldspaceLookup = new Vector2Serialized(data.worldspaceLookup);
            newJsonData.objectIndex = data.objectIndex;
            return newJsonData;
        }
    }


    [System.Serializable]
    public class WorldspaceObjectJsonData
    {
        public Vector2Serialized worldAreaLookup;
        public Vector2Serialized worldspaceLookup;
        public int objectIndex;
    }
}