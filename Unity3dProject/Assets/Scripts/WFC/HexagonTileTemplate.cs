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
        public bool isEmptySpace { get; private set; }
        public Dictionary<HexagonTileSide, TileSocketProfile> socketProfileBySide { get; private set; } = null;
        public Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> socketProfileBySideByRotation { get; private set; } = null;


        public HexagonTileTemplate(HexagonCellPrototype cell)
        {
            Debug.LogError("Creating empty tile!");
            this._uid = UtilityHelpers.GenerateUniqueID("EMPTY");
            this.isEmptySpace = true;

            this.model = null;
            this.size = cell.size;
            this.name = "EMPTY TILE_X" + this.size;
            this.socketProfileBySide = cell.tileSocketProfileBySide;
            this.isEdgeable = cell.IsEdge();

            if (this.socketProfileBySide == null)
            {
                Debug.LogError("socketProfileBySide == null");
            }
            else EvaluateSockets();
        }

        public HexagonTileTemplate(GameObject _prefab, HexagonCellPrototype cell)
        {
            this.model = _prefab;
            this.size = cell.size;
            this._uid = UtilityHelpers.GenerateUniqueID(_prefab);
            this.name = model.name;
            this.socketProfileBySide = cell.tileSocketProfileBySide;

            this.isEdgeable = cell.IsEdge();

            // if (cell.GetCellStatus() == C )
            // this._excludeLayerState = ExcludeLayerState.Unset;
            if (this.socketProfileBySide == null)
            {
                Debug.LogError("socketProfileBySide == null");
            }
            else EvaluateSockets();
        }

        public static List<HexagonTileTemplate> Generate_Tiles(Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell, Transform folder = null, bool instantiateOnGenerate = false)
        {
            List<HexagonTileTemplate> new_tiles = new List<HexagonTileTemplate>();
            List<HexagonTileTemplate> emptyTiles = new List<HexagonTileTemplate>();

            foreach (var cell in gameObjectsByCell.Keys)
            {
                GameObject gameObject = gameObjectsByCell[cell];
                HexagonTileTemplate tileTemplate;
                if (gameObject != null)
                {
                    tileTemplate = new HexagonTileTemplate(gameObject, cell);
                    // if (disableObject) gameObject.SetActive(false);
                    if (instantiateOnGenerate) WFCUtilities_V2.InstantiateTileAtCell(tileTemplate, cell, folder);
                }
                else
                {
                    tileTemplate = new HexagonTileTemplate(cell);
                    emptyTiles.Add(tileTemplate);
                }

                if (tileTemplate != null)
                {
                    cell.SetTile(tileTemplate, 0, false);

                    new_tiles.Add(tileTemplate);
                }
            }
            Debug.Log("Total tiles generated: " + new_tiles.Count + ", empty tiles: " + emptyTiles.Count);
            return new_tiles;
        }

        public static List<HexagonTileTemplate> Generate_Tiles_With_WFC_DryRun(Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell, HexagonSocketDirectory socketDirectory, bool disableObject = false)
        {
            int totalTiles = 0;
            List<HexagonTileTemplate> new_tiles = new List<HexagonTileTemplate>();
            List<HexagonTileTemplate> emptyTiles = new List<HexagonTileTemplate>();

            foreach (var cell in gameObjectsByCell.Keys)
            {
                GameObject gameObject = gameObjectsByCell[cell];
                HexagonTileTemplate tileTemplate;
                if (gameObject != null)
                {
                    tileTemplate = new HexagonTileTemplate(gameObject, cell);

                    if (disableObject) gameObject.SetActive(false);
                }
                else
                {
                    tileTemplate = new HexagonTileTemplate(cell);
                    emptyTiles.Add(tileTemplate);
                }

                if (tileTemplate != null)
                {
                    cell.SetTile(tileTemplate, 0, false);

                    new_tiles.Add(tileTemplate);
                    totalTiles++;
                }
            }
            bool passed = DryRun_CompatibilityCheck(gameObjectsByCell, socketDirectory, totalTiles);
            return new_tiles;
        }

        // public static List<HexagonTileTemplate> Generate_Tiles(
        //     Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell,
        //     GameObject prefab,
        //     Transform transform,
        //     List<SurfaceBlockState> filterOnStates,
        //     Transform folder
        // )
        // {
        //     List<HexagonTileTemplate> new_tiles = new List<HexagonTileTemplate>();
        //     foreach (var cell in resultsByCell.Keys)
        //     {
        //         GameObject gameObject = SurfaceBlock.Generate_MeshObject(resultsByCell[cell], prefab, transform, cell.center, filterOnStates, folder);
        //         if (gameObject != null)
        //         {
        //             HexagonTileTemplate tileTemplate = new HexagonTileTemplate(gameObject, cell);
        //             new_tiles.Add(tileTemplate);
        //         }
        //     }
        //     return new_tiles;
        // }

        public void EvaluateSockets(bool log = false)
        {
            if (socketProfileBySide.Count == 0)
            {
                Debug.LogError("socketProfileBySide.Count " + socketProfileBySide.Count);
                return;
            }
            for (int _side = 0; _side < 8; _side++)
            {
                HexagonTileSide side = (HexagonTileSide)_side;
                if (socketProfileBySide.ContainsKey(side) == false)
                {
                    Debug.LogError("NO tileSocketProfile on side: " + side);
                    continue;
                }
                if (socketProfileBySide[side].socketFaceByLookup == null)
                {
                    if (socketProfileBySide[side].defaultID == -1 || log) Debug.LogError("NO socketFaceByLookup on side: " + side + ", defaultId: " + socketProfileBySide[side].defaultID);
                }
                else if (log) Debug.Log("Found " + socketProfileBySide[side].socketFaceByLookup.Count + " sockets on side: " + side);
            }
        }


        [SerializeField] private string _uid;
        public string GetUid() => _uid;
        public bool HasUid() => (_uid != null && _uid != "");
        public string name { get; private set; }
        [SerializeField] private HexCellSizes _cellSize;
        public HexCellSizes GetSize() => _cellSize;
        private int size = 12; // depreciated
        [SerializeField] private TileSeries _tileSeries;
        public TileSeries GetSeries() => _tileSeries;

        #region Model Manipulation
        public GameObject model { get; private set; }
        [SerializeField] private GameObject modelRoof;
        [SerializeField] private Vector3 modelPosition;
        public void SetModel(GameObject _model)
        {
            model = _model;
        }

        public Dictionary<Vector3, Vector3> markerPoints_spawn { get; private set; } = null;

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
        [SerializeField] private ExcludeLayerState _excludeLayerState = ExcludeLayerState.Unset;
        public ExcludeLayerState GetExcludeLayerState() => _excludeLayerState;
        public enum ExcludeLayerState { Unset = 0, BaseLayerOnly, TopLayerOnly, NoBaseLayer, NoTopLayer }

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

        static public bool DryRun_CompatibilityCheck(Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell, HexagonSocketDirectory socketDirectory, int totalTiles)
        {
            Debug.Log("Dry-run compatibility check...");
            bool fail = false;
            int passed = 0;
            foreach (var cell in gameObjectsByCell.Keys)
            {
                HexagonTileTemplate currentTile = cell.currentTile_V2;
                if (currentTile == null)
                {
                    Debug.Log("Current cell has no tile. Skipping");
                    continue;
                }
                currentTile.EvaluateSockets();

                Dictionary<HexagonTileSide, TileSocketProfile> neighborTileSocketsBySide = cell.GetNeighborTileSocketProfilesOnSides();
                HexagonCellPrototype[] neighborTileSides = cell.GetNeighborTileSides();
                int rotation = 0;

                for (int _side = 0; _side < neighborTileSides.Length; _side++)
                {
                    HexagonCellPrototype neighborCell = neighborTileSides[_side];
                    HexagonTileSide side = (HexagonTileSide)_side;

                    if (neighborCell != null && neighborCell.currentTile_V2 == null)
                    {
                        if (currentTile.socketProfileBySide[(HexagonTileSide)side].socketFaceByLookup == null) Debug.Log("Expected Empty space - NO tile on side: " + (HexagonTileSide)side);
                    }

                    bool compatibile = IsTileCompatible(neighborTileSocketsBySide, cell.currentTile_V2, cell, neighborCell, _side, rotation, false, socketDirectory, true);
                    if (!compatibile)
                    {
                        Debug.LogError("incompatibile on side: " + (HexagonTileSide)side);
                        cell.Highlight(true);
                        cell.HighlightSide(true, (HexagonTileSide)side);

                        if (neighborCell != null)
                        {
                            neighborCell.Highlight(true);
                            neighborCell.currentTile_V2?.EvaluateSockets();
                        }
                        else Debug.LogError("neighborCell == null on side: " + (HexagonTileSide)side);

                        fail = true;
                        break;
                    }
                }

                if (fail) break;
                passed++;
            }
            Debug.Log("Checks Passed: " + passed + ", of " + totalTiles);
            return (passed == totalTiles);
        }

        static public List<int[]> GetCompatibileTileRotations(
            HexagonCellPrototype currentCell,
            HexagonTileTemplate currentTile,
            bool inverted,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities,
            bool allowRotation
        )
        {
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();
            List<int[]> compatibleRotations = new List<int[]>();
            Dictionary<HexagonTileSide, TileSocketProfile> neighborTileSocketsBySide = currentCell.GetNeighborTileSocketProfilesOnSides();
            HexagonCellPrototype[] neighborTileSides = currentCell.GetNeighborTileSides();

            int rotations = allowRotation ? 6 : 1;
            // Check rotations
            for (int rotation = 0; rotation < rotations; rotation++)
            {
                bool compatibile = true;
                for (int _side = 0; _side < neighborTileSides.Length; _side++)
                {
                    HexagonCellPrototype neighborCell = neighborTileSides[_side];
                    compatibile = IsTileCompatible(neighborTileSocketsBySide, currentTile, currentCell, neighborCell, _side, rotation, inverted, socketDirectory, logIncompatibilities);
                    if (!compatibile) break;
                }

                if (compatibile)
                {
                    int[] rotation_isInverted = new int[2] { rotation, inverted ? 1 : 0 };
                    compatibleRotations.Add(rotation_isInverted);
                }
            }
            return compatibleRotations;
        }

        public static bool IsTileCompatible(
            Dictionary<HexagonTileSide, TileSocketProfile> neighborTileSocketsBySide,
            HexagonTileTemplate currentTile,
            HexagonCellPrototype currentCell,
            HexagonCellPrototype neighborCell,
            int _side,
            int rotation,
            bool inverted,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities
        )
        {
            HexagonTileSide side = (HexagonTileSide)_side;

            string tileName = currentTile.name;
            HexagonTileTemplate neighborTile = neighborCell?.currentTile_V2;
            // string neighborCellStats = (neighborCell != null) ? neighborCell.LogStats() : "NULL";
            // string neighborTileName = (neighborTile != null) ? neighborTile.name : "EMPTY";

            TileSocketProfile profile = currentTile.GetRotatedSideSockets(side, rotation);
            if (profile == null)
            {
                Debug.LogError("profile == null");
                return false;
            }

            TileSocketProfile otherProfile = neighborTileSocketsBySide[side];
            if (otherProfile == null)
            {
                Debug.LogError("otherProfile == null");
                return false;

            }
            // int topBottomRotation = 0;
            // if (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom) topBottomRotation = rotation;

            return TileSocketProfile.IsCompatible(profile, otherProfile, socketDirectory);
        }

        public TileSocketProfile GetRotatedSideSockets(HexagonTileSide side, int rotation, bool inverted = false)
        {
            // if (inverted) else
            if (socketProfileBySide == null)
            {
                Debug.LogError("socketProfileBySide == null, using EDGE");
                return new TileSocketProfile((int)GlobalSockets.Edge);
            }

            if (rotation == 0)
            {
                if (socketProfileBySide.ContainsKey(side))
                {
                    // Debug.Log("Default rotation");
                    return socketProfileBySide[side];
                }
                else
                {
                    Debug.LogError("socketProfileBySide does not have side: " + side + ",  rotation: " + rotation + ", using Empty_Space");
                    return new TileSocketProfile((int)GlobalSockets.Empty_Space);
                }
            }

            TileSocketProfile rotatedProfile = TileSocketProfile.GetRotatedSideSockets(socketProfileBySide, side, rotation, inverted);
            if (rotatedProfile == null)
            {
                Debug.LogError("rotatedProfile == null, using Empty_Space");
                return new TileSocketProfile((int)GlobalSockets.Empty_Space);
            }

            return rotatedProfile;
        }

        // private static Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> EvaluateRotatedSideSockets(Dictionary<HexagonTileSide, TileSocketProfile> socketProfileBySide)
        // {
        //     Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> socketsBySideByRotation = new Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>>() {
        //         { 0, socketProfileBySide }
        //     };
        //     int rotations = 6;
        //     for (int rot = 1; rot < rotations; rot++)
        //     {
        //         // if (rot == 0)
        //         // {
        //         //     socketsBySideByRotation.Add(0, socketProfileBySide);
        //         // }
        //         // else
        //         // {
        //         socketsBySideByRotation.Add(rot, new Dictionary<HexagonTileSide, TileSocketProfile>() {
        //                     { HexagonTileSide.Front, null },
        //                     { HexagonTileSide.FrontRight,  null },
        //                     { HexagonTileSide.BackRight,  null },
        //                     { HexagonTileSide.Back,  null },
        //                     { HexagonTileSide.BackLeft,  null },
        //                     { HexagonTileSide.FrontLeft,  null },
        //                     { HexagonTileSide.Top,  null },
        //                     { HexagonTileSide.Bottom, null }
        //                 }
        //         );
        //         // }
        //     }
        //     // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
        //     for (int rot = 1; rot < rotations; rot++)
        //     {
        //         int offset = (rot == 0) ? 0 : rot - 1;
        //         socketsBySideByRotation[rot][HexagonTileSide.Front] = socketsBySideByRotation[rot][HexagonTileSide.FrontRight];
        //         socketsBySideByRotation[rot][HexagonTileSide.FrontRight] = socketsBySideByRotation[rot][HexagonTileSide.BackRight];
        //         socketsBySideByRotation[rot][HexagonTileSide.BackRight] = socketsBySideByRotation[rot][HexagonTileSide.Back];
        //         socketsBySideByRotation[rot][HexagonTileSide.Back] = socketsBySideByRotation[rot][HexagonTileSide.BackLeft];
        //         socketsBySideByRotation[rot][HexagonTileSide.BackLeft] = socketsBySideByRotation[rot][HexagonTileSide.FrontLeft];
        //         socketsBySideByRotation[rot][HexagonTileSide.FrontLeft] = socketsBySideByRotation[rot][HexagonTileSide.Front];

        //         // socketsBySideByRotation[rot][HexagonTileSide.Top] = socketProfileBySide[HexagonTileSide.Top];
        //         // socketsBySideByRotation[rot][HexagonTileSide.Bottom] = socketProfileBySide[HexagonTileSide.Bottom];
        //     }
        //     return socketsBySideByRotation;
        // }
    }
}