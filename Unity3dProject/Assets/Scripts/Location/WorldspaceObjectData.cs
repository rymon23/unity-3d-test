using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    [System.Serializable]
    public class WorldspaceObjectData
    {
        public WorldspaceObjectData(Vector2 _worldspaceLookup, Transform _parentFolder)
        {
            worldspaceLookup = _worldspaceLookup;
            trees = new List<GameObject>();

            Evalaute_Folders(_parentFolder);
        }

        public Vector2 worldAreaLookup { get; private set; }
        public Vector2 worldspaceLookup;

        public int objectIndex { get; private set; } = -1;
        public void SetChunkObjectIndex(int index)
        {
            objectIndex = index;
        }

        public List<GameObject> trees = new List<GameObject>();

        public Transform folder_Main { get; private set; } = null;
        public Transform folder_trees { get; private set; } = null;

        public Transform MainFolder() => folder_Main;
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
        }

        public WorldspaceObjectJsonData ConvertToJson() => ConvertToJson(this);
        public static WorldspaceObjectJsonData ConvertToJson(WorldspaceObjectData chunkData)
        {
            WorldspaceObjectJsonData newJsonData = new WorldspaceObjectJsonData();
            newJsonData.worldAreaLookup = new Vector2Serialized(chunkData.worldAreaLookup);
            newJsonData.worldspaceLookup = new Vector2Serialized(chunkData.worldspaceLookup);
            newJsonData.objectIndex = chunkData.objectIndex;
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