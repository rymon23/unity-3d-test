using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


namespace WFCSystem
{
    public enum TileObjectType { Unset = 0, TileDirectory = 1, SocketDirectory, TileClusterPrefab }

    [CreateAssetMenu(fileName = "New Tile Directory", menuName = "Tile Directory")]
    public class TileDirectory : ScriptableObject
    {
        [System.Serializable]
        public struct Option_Tile
        {
            public HexagonTileCore tilePrefab;
            public int id { get; private set; }
            public void SetId(int value)
            {
                id = value;
            }
            [Range(0f, 1f)] public float probabilityWeight;
        }

        [System.Serializable]
        public struct Option_TileClusterPrefab
        {
            public TileClusterPrefab tileClusterPrefab;
            public int id { get; private set; }
            public void SetId(int value)
            {
                id = value;
            }
            [Range(0f, 1f)] public float probabilityWeight;
        }

        [Header("Socket Directory")]
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        public HexagonSocketDirectory GetSocketDirectory() => socketDirectory;

        [Header("Tile Prefabs")]
        [SerializeField] private List<Option_Tile> tileOptions;

        [Header("Tile Cluster Prefabs")]
        [SerializeField] private List<Option_TileClusterPrefab> tileClusterPrefabOptions;
        [Header(" ")]

        [Header(" ")]
        [SerializeField] private bool revaluate_Options;
        [SerializeField] private bool auto_revaluate;
        [Header(" ")]
        [SerializeField] private bool revaluate_DirectoryAssets;
        [Header(" ")]

        [Header("Auto-Fill")]
        [SerializeField] private bool autoFillFromFolder;
        [Header(" ")]
        [SerializeField] private List<HexCellSizes> filter_TileSizes;
        [SerializeField] private List<TileSeries> filter_TileSeries;
        [SerializeField]
        private List<TileSeries> exclude_TileSeries = new List<TileSeries>() {
            TileSeries._Template_
        };

        [SerializeField]
        private List<string> exclude_partialNames = new List<string>() {
            "TH_X12__Any"
        };

        [SerializeField] private TileContext tileContext;
        [Header(" ")]
        [Header(" ")]
        [SerializeField] private List<HexCellSizes> tileSizes;
        public bool HasTileSize(HexCellSizes size) => GetTileSizes().Any(s => s == size);
        public List<HexCellSizes> GetTileSizes()
        {
            tileSizes = GetTileSizes(ExtractTiles());
            return tileSizes;
        }
        public static List<HexCellSizes> GetTileSizes(List<HexagonTileCore> tiles)
        {
            List<HexCellSizes> sizesFound = new List<HexCellSizes>();
            foreach (var tile in tiles)
            {
                if (sizesFound.Contains(tile.GetSize())) continue;
                sizesFound.Add(tile.GetSize());
            }
            return sizesFound;
        }


        [SerializeField] private List<TileClusterPrefab> assets_tileClusterPrefabs = new List<TileClusterPrefab>();
        [SerializeField] private List<GameObject> assets_tilePrefabs = new List<GameObject>();

        [SerializeField]
        private string[] _assetPaths = {
            "Assets/Prefabs/WFC",
            // "Assets/Prefabs/WFC/HexTiles",
            };


        private int _tileOptionsLength;
        private int _tileClusterPrefabOptionsLength;


        public List<HexagonTileCore> GetTiles(bool enableLog)
        {
            if (tileOptions.Count == 0)
            {
                Debug.LogError("No tileOptions, " + this.name);
                return null;
            }

            List<HexagonTileCore> tiles = new List<HexagonTileCore>();
            for (int i = 0; i < tileOptions.Count; i++)
            {
                tiles.Add(tileOptions[i].tilePrefab);
            }

            if (enableLog) Debug.Log("Tiles found: " + tiles.Count);

            return tiles;
        }
        public Dictionary<int, HexagonTileCore> CreateTileDictionary()
        {
            Dictionary<int, HexagonTileCore> tileDictionary = new Dictionary<int, HexagonTileCore>();
            for (int i = 0; i < tileOptions.Count; i++)
            {

                tileDictionary.Add(tileOptions[i].id, tileOptions[i].tilePrefab);
            }
            return tileDictionary;
        }

        public Dictionary<int, TileClusterPrefab> CreateTileClusterPrefabDictionary()
        {
            Dictionary<int, TileClusterPrefab> tileDictionary = new Dictionary<int, TileClusterPrefab>();
            for (int i = 0; i < tileClusterPrefabOptions.Count; i++)
            {
                tileDictionary.Add(tileClusterPrefabOptions[i].id, tileClusterPrefabOptions[i].tileClusterPrefab);
            }
            return tileDictionary;
        }

        public List<HexagonTileCore> ExtractTiles()
        {
            List<HexagonTileCore> result = new List<HexagonTileCore>();
            for (int i = 0; i < tileOptions.Count; i++)
            {
                result.Add(tileOptions[i].tilePrefab);
            }
            return result;
        }

        private void Evaluate_TileOptions()
        {
            HashSet<int> foundIds = new HashSet<int>();
            List<Option_Tile> new_tileOptions = new List<Option_Tile>();
            foreach (var item in tileOptions)
            {
                if (item.tilePrefab == null) continue;

                int hashCode = item.tilePrefab.GetHashCode();
                if (foundIds.Contains(hashCode)) continue;

                item.SetId(hashCode);
                foundIds.Add(hashCode);

                new_tileOptions.Add(item);
            }
            tileOptions = new_tileOptions;
        }

        private void Evaluate_TileClusterPrefabOptions()
        {
            HashSet<int> foundIds = new HashSet<int>();
            List<Option_TileClusterPrefab> new_tileClusterPrefabOptions = new List<Option_TileClusterPrefab>();
            foreach (var item in tileClusterPrefabOptions)
            {
                int hashCode = item.tileClusterPrefab.GetHashCode();
                if (item.tileClusterPrefab == null || foundIds.Contains(hashCode)) continue;

                item.SetId(hashCode);
                foundIds.Add(hashCode);

                new_tileClusterPrefabOptions.Add(item);
            }
            tileClusterPrefabOptions = new_tileClusterPrefabOptions;
        }


        private void EvaluateTiles()
        {
            Evaluate_TileOptions();
            Evaluate_TileClusterPrefabOptions();
        }



        private void CheckForAssets()
        {
            List<GameObject> found = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", _assetPaths);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                IHexagonTile tile = (go != null) ? go.GetComponent<IHexagonTile>() : null;
                if (tile != null) found.Add(go);
            }
            assets_tilePrefabs = found;
        }

        private void CheckForTileClusterPrefabs()
        {
            List<TileClusterPrefab> found = new List<TileClusterPrefab>();
            string[] guids = AssetDatabase.FindAssets("t:TileClusterPrefab", _assetPaths);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TileClusterPrefab go = AssetDatabase.LoadAssetAtPath<TileClusterPrefab>(path);
                if (go != null) found.Add(go);
            }
            assets_tileClusterPrefabs = found;
        }

        private void AutoFillFromFolder()
        {
            List<Option_Tile> new_tileOptions = new List<Option_Tile>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", _assetPaths);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                HexagonTileCore hexTile = (go != null) ? go.GetComponent<HexagonTileCore>() : null;
                bool filter_Size = (filter_TileSizes != null && filter_TileSizes.Count > 0);
                bool filter_Series = (filter_TileSeries != null && filter_TileSeries.Count > 0);
                bool exclude_Series = (exclude_TileSeries != null && exclude_TileSeries.Count > 0);
                bool exclude_partials = (exclude_partialNames != null && exclude_partialNames.Count > 0);

                if (hexTile != null)
                {

                    HexagonCellManager cellManager = go.GetComponent<HexagonCellManager>();
                    if (cellManager != null)
                    {
                        Debug.LogError("excluded on cellManager detected: " + hexTile.name);
                        continue;
                    }

                    if (socketDirectory != null && hexTile.GetSocketDirectory() != socketDirectory)
                    {
                        Debug.LogError("tile socketDirectory does not match: " + hexTile.GetSocketDirectory().name);
                        continue;
                    }
                    if (filter_Size && filter_TileSizes.Contains(hexTile.GetSize()) == false)
                    {
                        Debug.LogError("size: " + hexTile.GetSize() + " - " + hexTile.name);
                        continue;
                    }

                    if (exclude_Series && exclude_TileSeries.Contains(hexTile.GetSeries()))
                    {
                        Debug.LogError("excluded series: " + hexTile.GetSeries());
                        continue;
                    }

                    if (filter_Series && filter_TileSeries.Contains(hexTile.GetSeries()) == false)
                    {
                        Debug.LogError("series: " + hexTile.GetSeries());
                        continue;
                    }

                    if (exclude_partials && exclude_partialNames.Contains(hexTile.name))
                    {
                        Debug.LogError("excluded on name: " + hexTile.name);
                        continue;
                    }

                    // if (hexTile.GetTileContext() != tileContext) continue;

                    Option_Tile entry = new Option_Tile();
                    entry.tilePrefab = hexTile;
                    new_tileOptions.Add(entry);
                }
            }

            Debug.Log("Tiles found: " + new_tileOptions.Count);

            if (new_tileOptions.Count > 0) tileOptions = new_tileOptions;
        }


        private void OnValidate()
        {
            if (revaluate_DirectoryAssets)
            {
                revaluate_DirectoryAssets = false;

                CheckForAssets();
                CheckForTileClusterPrefabs();
            }

            if (autoFillFromFolder)
            {
                autoFillFromFolder = false;
                AutoFillFromFolder();
                revaluate_Options = true;
            }

            if (revaluate_Options
            || (
                auto_revaluate && (
                    _tileOptionsLength != tileOptions.Count ||
                    _tileClusterPrefabOptionsLength != tileClusterPrefabOptions.Count
                    )
                )
            )
            {
                revaluate_Options = false;

                EvaluateTiles();
                _tileOptionsLength = tileOptions.Count;
                _tileClusterPrefabOptionsLength = tileClusterPrefabOptions.Count;

                GetTileSizes();

                GetTiles(true);
            }
        }

    }
}