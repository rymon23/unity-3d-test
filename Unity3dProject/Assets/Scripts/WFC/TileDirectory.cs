using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace WFCSystem
{
    [CreateAssetMenu(fileName = "New Tile Directory", menuName = "Tile Directory")]
    public class TileDirectory : ScriptableObject
    {
        [System.Serializable]
        public struct TileEntry
        {
            public HexagonTile tilePrefab;
            public int id;
            public float probability;
        }


        [System.Serializable]
        public struct MicroTileEntry
        {
            public HexagonTileCore tilePrefab;
            public int id;
            public float probability;
        }

        [System.Serializable]
        public struct TileClusterEntry
        {
            public HexagonTileCluster tileClusterPrefab;
            public int id;
            public float probability;
        }

        [Header("Tile Prefabs")]
        [SerializeField] private MicroTileEntry[] microTileEntries;

        [Header("Auto-Fill")]
        [SerializeField] private TileContext tileContext;
        [SerializeField] private bool autoFillFromFolder;

        [Header("Cluster Tile Prefabs")]
        [SerializeField] private TileClusterEntry[] tileClusterEntries;
        [SerializeField] private TileEntry[] tileEntries;
        [SerializeField] private VerticalTileEntry[] verticalTileEntries;

        [SerializeField] private bool revaluate;
        private int currentSize = 0;

        public List<GameObject> tilePrefabs = new List<GameObject>();

        public Dictionary<int, HexagonTile> CreateTileDictionary()
        {
            Dictionary<int, HexagonTile> tileDictionary = new Dictionary<int, HexagonTile>();
            for (int i = 0; i < tileEntries.Length; i++)
            {
                tileDictionary.Add(tileEntries[i].id, tileEntries[i].tilePrefab);
            }
            return tileDictionary;
        }
        public Dictionary<int, HexagonTileCore> CreateHexTileDictionary()
        {
            Dictionary<int, HexagonTileCore> tileDictionary = new Dictionary<int, HexagonTileCore>();
            for (int i = 0; i < microTileEntries.Length; i++)
            {
                tileDictionary.Add(microTileEntries[i].id, microTileEntries[i].tilePrefab);
            }
            return tileDictionary;
        }

        public Dictionary<int, HexagonTileCluster> CreateTileClusterDictionary()
        {
            Dictionary<int, HexagonTileCluster> tileDictionary = new Dictionary<int, HexagonTileCluster>();
            for (int i = 0; i < tileClusterEntries.Length; i++)
            {
                tileDictionary.Add(tileClusterEntries[i].id, tileClusterEntries[i].tileClusterPrefab);
            }
            return tileDictionary;
        }

        public Dictionary<int, VerticalTile> CreateVerticalTileDictionary()
        {
            Dictionary<int, VerticalTile> tileDictionary = new Dictionary<int, VerticalTile>();
            for (int i = 0; i < verticalTileEntries.Length; i++)
            {
                tileDictionary.Add(verticalTileEntries[i].id, verticalTileEntries[i].tilePrefab);
            }
            return tileDictionary;
        }

        private void EvaluateTiles()
        {
            // Hex Tiles
            for (int i = 0; i < tileEntries.Length; i++)
            {
                if (tileEntries[i].tilePrefab == null)
                {
                    tileEntries[i].id = -1;
                    continue;
                }
                tileEntries[i].tilePrefab.id = i;
                tileEntries[i].id = i;
            }

            // Micro Hex Tiles
            for (int i = 0; i < microTileEntries.Length; i++)
            {
                if (microTileEntries[i].tilePrefab == null)
                {
                    microTileEntries[i].id = -1;
                    continue;
                }
                microTileEntries[i].tilePrefab.SetId(i);
                microTileEntries[i].id = i;
            }

            // Hex Tile Clusters
            for (int i = 0; i < tileClusterEntries.Length; i++)
            {
                if (tileClusterEntries[i].tileClusterPrefab == null)
                {
                    tileClusterEntries[i].id = -1;
                    continue;
                }
                tileClusterEntries[i].id = i;
                tileClusterEntries[i].tileClusterPrefab.id = i;
            }

            // Vertical Tiles
            for (int i = 0; i < verticalTileEntries.Length; i++)
            {
                if (verticalTileEntries[i].tilePrefab == null)
                {
                    verticalTileEntries[i].id = -1;
                    continue;
                }
                verticalTileEntries[i].tilePrefab.SetID(i);
                verticalTileEntries[i].id = i;
            }
        }

        [SerializeField]
        private string[] _assetPaths = {
        "Assets/Prefabs/WFC",
        // "Assets/Prefabs/WFC/HexTiles",
        };

        private void CheckForAssets()
        {
            // Get Tiles
            List<GameObject> found1 = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", _assetPaths);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                IHexagonTile tile = (go != null) ? go.GetComponent<IHexagonTile>() : null;
                if (tile != null) found1.Add(go);
            }
            tilePrefabs = found1;
        }

        private void AutoFillFromFolder()
        {
            List<MicroTileEntry> newTileEntries = new List<MicroTileEntry>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", _assetPaths);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                HexagonTileCore hexTile = (go != null) ? go.GetComponent<HexagonTileCore>() : null;
                if (hexTile != null)
                {
                    if (hexTile.GetTileContext() == tileContext)
                    {
                        MicroTileEntry entry = new MicroTileEntry();
                        entry.tilePrefab = hexTile;
                        newTileEntries.Add(entry);
                    }
                    // }
                    // else
                    // {
                    //     Debug.Log("hexTile is null");
                }
            }

            if (newTileEntries.Count > 0) microTileEntries = newTileEntries.ToArray();
        }


        private void OnValidate()
        {
            CheckForAssets();

            if (autoFillFromFolder)
            {
                autoFillFromFolder = false;
                AutoFillFromFolder();
                revaluate = true;
            }
            if (revaluate || tileEntries != null && tileEntries.Length != currentSize)
            {
                revaluate = false;
                currentSize = tileEntries.Length;

                EvaluateTiles();
            }


        }


        [System.Serializable]
        public struct VerticalTileEntry
        {
            public VerticalTile tilePrefab;
            public int id;
            public float probability;
        }
    }
}