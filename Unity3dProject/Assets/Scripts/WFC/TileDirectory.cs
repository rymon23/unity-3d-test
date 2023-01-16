using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New Tile Directory", menuName = "Tile Directory")]
public class TileDirectory : ScriptableObject
{
    [System.Serializable]
    public struct TileEntry
    {
        public HexagonTile tilePrefab;
        public int id;
        public float probability;
        // public Color color;
    }

    [System.Serializable]
    public struct TileClusterEntry
    {
        public HexagonTileCluster tileClusterPrefab;
        public int id;
        public float probability;
    }

    [SerializeField] private TileEntry[] tileEntries;
    [SerializeField] private TileClusterEntry[] tileClusterEntries;
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

    public Dictionary<int, HexagonTileCluster> CreateTileClusterDictionary()
    {
        Dictionary<int, HexagonTileCluster> tileDictionary = new Dictionary<int, HexagonTileCluster>();
        for (int i = 0; i < tileClusterEntries.Length; i++)
        {
            tileDictionary.Add(tileClusterEntries[i].id, tileClusterEntries[i].tileClusterPrefab);
        }
        return tileDictionary;
    }

    private void EvaluateTiles()
    {
        // Tiles
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

        // Tile Clusters
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
            found1.Add(go);
        }
        tilePrefabs = found1;
    }


    private void OnValidate()
    {
        CheckForAssets();

        if (revaluate || tileEntries != null && tileEntries.Length != currentSize)
        {
            revaluate = false;
            currentSize = tileEntries.Length;

            EvaluateTiles();
        }
    }
}
