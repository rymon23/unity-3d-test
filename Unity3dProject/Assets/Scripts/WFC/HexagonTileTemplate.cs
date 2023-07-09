using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{

    [System.Serializable]
    public class HexagonTileTemplate
    {
        public HexagonTileTemplate(GameObject _prefab, List<int>[] _sideEdgeStepProfile, int _size)
        {
            model = _prefab;
            sideEdgeStepProfile = _sideEdgeStepProfile;
            size = _size;
        }

        public List<HexSideEdgeSocket>[] sideEdgeSteps = new List<HexSideEdgeSocket>[8];
        public List<int>[] sideEdgeStepProfile = new List<int>[8];
        public static List<int>[] Generate_SideEdgeStepProfile(HexagonCellPrototype cell)
        {
            List<int>[] new_sideEdgeStepProfile = new List<int>[8];
            foreach (var side in cell.sideEdgeSocket.Keys)
            {
                List<int> stepProfile = new List<int>();
                foreach (var socket in cell.sideEdgeSocket[side])
                {
                    stepProfile.Add(socket.step);
                }
                new_sideEdgeStepProfile[(int)side] = stepProfile.OrderBy(val => val).ToList();
            }
            return new_sideEdgeStepProfile;
        }

        public static List<HexagonTileTemplate> Generate_Tiles(
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            Transform folder
        )
        {
            List<HexagonTileTemplate> new_tiles = new List<HexagonTileTemplate>();
            foreach (var cell in resultsByCell.Keys)
            {
                GameObject gameObject = SurfaceBlock.Generate_MeshObject(resultsByCell[cell], prefab, transform, filterOnStates, folder);
                if (gameObject != null)
                {
                    HexagonTileTemplate tileTemplate = new HexagonTileTemplate(gameObject, Generate_SideEdgeStepProfile(cell), cell.size);
                    new_tiles.Add(tileTemplate);
                }
            }
            return new_tiles;
        }

        [SerializeField] private string _uid;
        public string GetUid() => _uid;
        public bool HasUid() => (_uid != null && _uid != "");
        [SerializeField] private HexCellSizes _cellSize;
        public HexCellSizes GetSize() => _cellSize;
        private int size = 12; // depreciated
        [SerializeField] private TileSeries _tileSeries;
        public TileSeries GetSeries() => _tileSeries;

        #region Model Manipulation
        [SerializeField] private GameObject model;
        [SerializeField] private GameObject modelRoof;
        [SerializeField] private Vector3 modelPosition;
        public void SetModel(GameObject _model)
        {
            model = _model;
        }
        // [Header("Inversion Settings")]
        // [SerializeField] private bool isInvertable;
        // [SerializeField] private float invertedPosition = 0.01f;
        // public bool IsInvertable() => isInvertable;
        // [SerializeField] private bool isModelInverted;
        // public void InvertModel()
        // {
        //     if (invertedPosition != 0.01f) WFCUtilities.InvertTile(model, invertedPosition);
        //     else WFCUtilities.InvertTile(model);

        //     isModelInverted = true;
        // }
        // [SerializeField] private bool isRoofable;
        // public bool IsRoofable() => isRoofable;
        // [SerializeField] private bool isModelRoofed;
        // public void SetModelRoofActive(bool enable)
        // {
        //     modelRoof.SetActive(enable);
        //     isModelRoofed = enable;
        // }
        #endregion

        [Header("Tile Context")]
        [SerializeField] private TileContext _tileContext;
        public TileContext GetTileContext() => _tileContext;

        [Header("Tile Compatibility / Probability")]
        [SerializeField]
        private CellStatus[] _cellStatusFilter = new CellStatus[3] {
                CellStatus.FlatGround,
                CellStatus.AboveGround,
                CellStatus.UnderGround,
            };
        public CellStatus[] GetCellStatusInclusionList() => _cellStatusFilter;
        public bool ShouldExcludeCellStatus(CellStatus status) => (_cellStatusFilter == null) ? false : _cellStatusFilter.Contains(status) == false;
        [SerializeField] private GridExclusionRule _gridExclusionRule;
        public GridExclusionRule GetGridExclusionRule() => _gridExclusionRule;
        public bool IsGridEdgeCompatible() => isEdgeable && (_gridExclusionRule == GridExclusionRule.GridEdgesOnly || _gridExclusionRule == GridExclusionRule.EdgeOnly || _gridExclusionRule == GridExclusionRule.Unset);
        public bool isEdgeable = true; // can be placed on the edge / border or the grid
        public bool isEntrance;
    }
}