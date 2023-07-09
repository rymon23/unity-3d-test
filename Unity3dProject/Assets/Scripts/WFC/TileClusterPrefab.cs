using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace WFCSystem
{
    [CreateAssetMenu(fileName = "New Tile Cluster Prefab", menuName = "Tile Cluster Prefab")]
    public class TileClusterPrefab : ScriptableObject
    {
        [SerializeField] private TileClusterPrefabSettings tileClusterPrefabSettings;
        private CellStatus[] _cellStatusFilter = new CellStatus[3] {
                CellStatus.FlatGround,
                CellStatus.AboveGround,
                CellStatus.UnderGround,
            };

        public TileClusterPrefabSettings GetSettings() => tileClusterPrefabSettings;
        public Color color = Color.green;
    }

    [System.Serializable]
    public struct TileClusterPrefabSettings
    {
        public TileClusterPrefabSettings(
            TileDirectory _tileDirectory,
            HexagonSocketDirectory _socketDirectory,
            CellSearchPriority _cellSearchPriority,
            int _hostCellsMin,
            int _hostCellsMax,
            bool _useOnce
        )
        {
            tileDirectory = _tileDirectory;
            socketDirectory = _socketDirectory;
            cellSearchPriority = _cellSearchPriority;

            hostCellsMin = _hostCellsMin;
            hostCellsMax = _hostCellsMax;
            useOnce = _useOnce;
        }
        public TileDirectory tileDirectory;
        public HexagonSocketDirectory socketDirectory;
        [Header(" ")]
        public int hostCellsMin;
        public int hostCellsMax;
        public CellSearchPriority cellSearchPriority;
        public bool useOnce;
    }
}