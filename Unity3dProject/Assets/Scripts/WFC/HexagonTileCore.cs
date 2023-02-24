using System;
using UnityEngine;
using ProceduralBase;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WFCSystem
{
    public interface IHexagonTile
    {
        public TileContext GetTileContext();
        public int[] GetRotatedLayerCornerSockets(bool top, int rotation, bool inverted = false);
        public int GetRotatedSideCornerSocketId(HexagonCorner corner, int rotation, bool top, bool inverted = false);
        public void SetCornerSocketSetIds(CornerSocketSetType socketSetType, int[] _newCornerSocketIds);
        public MirroredSideState GetMirroredSideState();

    }
    public enum CornerSocketSetType { SideBottom, SideTop, Bottom, Top }

    public enum TileContext
    {
        Default = 0,
        Micro = 1,
        Meta,
    }


    public enum TileVariant
    {
        Unset = 0,
        WalledBuilding = 1,
        InnerCityBuilding_A = 2,
    }



    [System.Serializable]
    public class HexagonTileCore : MonoBehaviour, IHexagonTile
    {
        [SerializeField] public string _uid;
        private void OnEnable()
        {
            if (_uid == null || _uid == "") _uid = UtilityHelpers.GenerateUniqueID(this.gameObject);
        }

        [SerializeField] private int id = -1;
        public int GetId() => id;
        public void SetId(int _id)
        {
            id = _id;
        }
        [SerializeField] private int size = 12;
        public int GetSize() => size;


        [Header("Settings")]
        [SerializeField] private GameObject model;
        [SerializeField] private bool isInvertable;
        public bool IsInvertable() => isInvertable;
        [SerializeField] private bool isModelInverted;
        public void InvertModel()
        {
            WFCUtilities.InvertTile(model);
            isModelInverted = true;
        }


        [Header("Tile Context")]
        [SerializeField] private TileContext _tileContext;
        public TileContext GetTileContext() => _tileContext;
        [Header("Category & Variant")]
        [SerializeField] private TileCategory _tileCategory;
        public TileCategory GetTileCategory() => _tileCategory;
        [SerializeField] private TileVariant _tileVariant;
        public TileVariant GetTileVariant() => _tileVariant;

        [SerializeField] private TileType _tileType;
        public TileType GetTileType() => _tileType;
        public bool IsExteriorWall() => _tileCategory == TileCategory.Wall && (_tileType == TileType.ExteriorWallLarge || _tileType == TileType.ExteriorWallSmall);


        [Header("Layer Settings")]
        [SerializeField] private ExcludeLayerState excludeLayerState;
        public enum ExcludeLayerState { Unset = 0, BaseLayerOnly, TopLayerOnly, NoBaseLayer }
        public bool baseLayerOnly;
        public bool noBaseLayer;
        public bool noGroundLayer;
        public bool isLayerConnector;


        [Header("Tile Compatibility / Probability")]
        public bool isInClusterSet; // Is part of a set of tiles that make a cluster
        public bool isClusterCenterTile; // Is the center part of a set of tiles that make a cluster
        public bool isEdgeable; // can be placed on the edge / border or the grid
        public bool isEntrance;
        public bool isPath;
        public enum PathType { Unset = 0, Road, Stairway, Elevator }

        public bool isLeveledTile;
        public bool isLeveledRamp;
        public bool isVerticalWFC;
        public bool isFragment; // Is incomplete by itself, needs neighbor tiles like itself
        [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

        [Header("Tile Sides / Mirroring")]



        [SerializeField] private MirroredSideState mirroredSideState = 0;
        public MirroredSideState GetMirroredSideState() => mirroredSideState;

        [Header("Tile Socket Configuration")]
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        public HexagonSocketDirectory GetSocketDirectory() => socketDirectory;
        [SerializeField] private TileLabelGroup tileLabelGroup;

        [Header("Side Label Offsets")]
        [Range(-10f, 0f)][SerializeField] private float sideBottomLabelYOffset = -1.5f;
        [Range(0f, 10f)][SerializeField] private float sideTopLabelYOffset = 1.5f;

        [Header("Layered Label Offsets")]
        [Range(-12f, -2f)][SerializeField] private float bottomLabelYOffset = -4f;
        [Range(2f, 12f)][SerializeField] private float topLabelYOffset = 4f;

        [Header("  ")]
        [Range(0f, 3f)][SerializeField] private float labelXZOffset = 1.7f;
        [Range(0f, 6f)][SerializeField] private float labelForwardOffset = 1.7f;
        [Header("  ")]

        #region Tile Sockets
        public int[] sideBtmCornerSocketIds = new int[12];
        public int[] sideTopCornerSocketIds = new int[12];
        public int[] bottomCornerSocketIds = new int[12];
        public int[] topCornerSocketIds = new int[12];
        // public int[] sideBtmCornerSocketIds { get; private set; } = new int[12];
        // public int[] sideTopCornerSocketIds { get; private set; } = new int[12];
        // public int[] bottomCornerSocketIds { get; private set; } = new int[12];
        // public int[] topCornerSocketIds { get; private set; } = new int[12];


        public int[] GetInvertedSocketIds(int[] cornerSocketIds)
        {
            int[] flippedIds = new int[12];
            flippedIds[(int)HexagonCorner.FrontA] = cornerSocketIds[(int)HexagonCorner.BackA];
            flippedIds[(int)HexagonCorner.FrontB] = cornerSocketIds[(int)HexagonCorner.BackB];

            flippedIds[(int)HexagonCorner.BackRightA] = cornerSocketIds[(int)HexagonCorner.FrontRightA];
            flippedIds[(int)HexagonCorner.BackRightB] = cornerSocketIds[(int)HexagonCorner.FrontRightB];

            flippedIds[(int)HexagonCorner.FrontLeftA] = cornerSocketIds[(int)HexagonCorner.BackLeftA];
            flippedIds[(int)HexagonCorner.FrontLeftB] = cornerSocketIds[(int)HexagonCorner.BackLeftB];

            flippedIds[(int)HexagonCorner.BackA] = cornerSocketIds[(int)HexagonCorner.FrontA];
            flippedIds[(int)HexagonCorner.BackB] = cornerSocketIds[(int)HexagonCorner.FrontB];

            flippedIds[(int)HexagonCorner.BackLeftA] = cornerSocketIds[(int)HexagonCorner.FrontLeftA];
            flippedIds[(int)HexagonCorner.BackLeftB] = cornerSocketIds[(int)HexagonCorner.FrontLeftB];

            flippedIds[(int)HexagonCorner.FrontRightA] = cornerSocketIds[(int)HexagonCorner.BackRightA];
            flippedIds[(int)HexagonCorner.FrontRightB] = cornerSocketIds[(int)HexagonCorner.BackRightB];
            return flippedIds;
        }

        private void EvaluateInvertedRotatedCornerSockets()
        {
            int rotations = 6;
            int corners = 12;
            int[][] newRotatedInvertedCornerSockets_sideTop = new int[rotations][];
            int[][] newRotatedInvertedCornerSockets_sideBottom = new int[rotations][];

            int[][] newRotatedInvertedCornerSockets_top = new int[rotations][];
            int[][] newRotatedInvertedCornerSockets_bottom = new int[rotations][];


            int[] invertedCornerSockets_sideTop = GetInvertedSocketIds(sideTopCornerSocketIds);
            int[] invertedCornerSockets_sideBottom = GetInvertedSocketIds(sideBtmCornerSocketIds);

            int[] invertedCornerSockets_top = GetInvertedSocketIds(topCornerSocketIds);
            int[] invertedCornerSockets_bottom = GetInvertedSocketIds(bottomCornerSocketIds);


            for (int rot = 0; rot < rotations; rot++)
            {
                newRotatedInvertedCornerSockets_sideTop[rot] = new int[corners];
                newRotatedInvertedCornerSockets_sideBottom[rot] = new int[corners];

                newRotatedInvertedCornerSockets_top[rot] = new int[corners];
                newRotatedInvertedCornerSockets_bottom[rot] = new int[corners];
            }
            // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
            for (int corner = 0; corner < corners; corner++)
            {
                newRotatedInvertedCornerSockets_sideTop[0][corner] = invertedCornerSockets_sideTop[corner];
                newRotatedInvertedCornerSockets_sideBottom[0][corner] = invertedCornerSockets_sideBottom[corner];

                newRotatedInvertedCornerSockets_top[0][corner] = invertedCornerSockets_top[corner];
                newRotatedInvertedCornerSockets_bottom[0][corner] = invertedCornerSockets_bottom[corner];
            }

            // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
            for (int rot = 1; rot < rotations; rot++)
            {
                int offset = (rot == 0) ? 0 : rot - 1;
                // Side TOP
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontRightA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontRightB];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontRightA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackRightA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontRightB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackRightB];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackRightA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackRightB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackB];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackLeftA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.BackLeftB];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackLeftA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.BackLeftB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontLeftA] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontA];
                newRotatedInvertedCornerSockets_sideTop[rot][(int)HexagonCorner.FrontLeftB] = newRotatedInvertedCornerSockets_sideTop[offset][(int)HexagonCorner.FrontB];

                // Side BTM
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontRightA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontRightB];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontRightA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackRightA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontRightB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackRightB];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackRightA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackRightB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackB];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackLeftA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.BackLeftB];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackLeftA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.BackLeftB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontLeftA] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontA];
                newRotatedInvertedCornerSockets_sideBottom[rot][(int)HexagonCorner.FrontLeftB] = newRotatedInvertedCornerSockets_sideBottom[offset][(int)HexagonCorner.FrontB];

                // TOP
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontRightA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontRightB];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontRightA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackRightA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontRightB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackRightB];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackRightA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackRightB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackB];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackLeftA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.BackLeftB];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackLeftA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.BackLeftB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontLeftA] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontA];
                newRotatedInvertedCornerSockets_top[rot][(int)HexagonCorner.FrontLeftB] = newRotatedInvertedCornerSockets_top[offset][(int)HexagonCorner.FrontB];

                // BTM
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontRightA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontRightB];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontRightA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackRightA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontRightB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackRightB];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackRightA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackRightB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackB];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackLeftA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.BackLeftB];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackLeftA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.BackLeftB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontLeftA] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontA];
                newRotatedInvertedCornerSockets_bottom[rot][(int)HexagonCorner.FrontLeftB] = newRotatedInvertedCornerSockets_bottom[offset][(int)HexagonCorner.FrontB];
            }

            invertedRotatedSideTopCornerSockets = newRotatedInvertedCornerSockets_sideTop;
            invertedRotatedSideBtmCornerSockets = newRotatedInvertedCornerSockets_sideBottom;
            invertedRotatedTopCornerSockets = newRotatedInvertedCornerSockets_top;
            invertedRotatedBottomCornerSockets = newRotatedInvertedCornerSockets_bottom;
        }

        public Dictionary<string, List<int[]>> BundleSocketIdData()
        {
            Dictionary<string, List<int[]>> data = new Dictionary<string, List<int[]>>();
            List<int[]> socketIds = new List<int[]>();
            socketIds.Add(sideBtmCornerSocketIds);
            socketIds.Add(sideTopCornerSocketIds);
            socketIds.Add(bottomCornerSocketIds);
            socketIds.Add(topCornerSocketIds);
            data.Add(_uid, socketIds);
            return data;
        }

        public void LoadSocketIdData(Dictionary<string, List<int[]>> data)
        {
            if (data.TryGetValue(_uid, out List<int[]> socketIds))
            {
                if (socketIds.Count == 4)
                {
                    sideBtmCornerSocketIds = socketIds[0];
                    sideTopCornerSocketIds = socketIds[1];
                    bottomCornerSocketIds = socketIds[2];
                    topCornerSocketIds = socketIds[3];
                }
                else
                {
                    Debug.LogError("Invalid number of socket id sets");
                }
            }
            else
            {
                Debug.LogError("Socket id data not found for uid: " + _uid);
            }
        }

        public void SetCornerSocketSetIds(CornerSocketSetType socketSetType, int[] _newCornerSocketIds)
        {
            for (int i = 0; i < 12; i++)
            {
                if (socketSetType == CornerSocketSetType.SideBottom)
                {
                    sideBtmCornerSocketIds[i] = _newCornerSocketIds[i];
                }
                else if (socketSetType == CornerSocketSetType.SideTop)
                {
                    sideTopCornerSocketIds[i] = _newCornerSocketIds[i];
                }
                else if (socketSetType == CornerSocketSetType.Bottom)
                {
                    bottomCornerSocketIds[i] = _newCornerSocketIds[i];
                }
                else if (socketSetType == CornerSocketSetType.Top)
                {
                    topCornerSocketIds[i] = _newCornerSocketIds[i];
                }
            }
        }

        public int GetCornerSocketId(HexagonCorner corner, bool top, bool layered)
        {
            if (layered) return top ? topCornerSocketIds[(int)corner] : bottomCornerSocketIds[(int)corner];
            return top ? sideTopCornerSocketIds[(int)corner] : sideBtmCornerSocketIds[(int)corner];
        }

        public (int[], int[]) GetCornerSocketsBySide(HexagonSide side)
        {
            (HexagonCorner cornerA, HexagonCorner cornerB) = HexagonCell.GetCornersFromSide(side);

            int[] top = new int[2];
            int[] bottom = new int[2];

            bottom[0] = GetCornerSocketId(cornerA, false, false);
            bottom[1] = GetCornerSocketId(cornerB, false, false);

            top[0] = GetCornerSocketId(cornerA, true, false);
            top[1] = GetCornerSocketId(cornerB, true, false);

            return (bottom, top);
        }

        #endregion

        private Transform center;
        [SerializeField] private Vector3[] _corners;
        [SerializeField] private Vector3[] _sides;
        public GameObject[] socketTextDisplay;


        #region Saved Values
        private Vector3 _currentCenterPosition;
        private int _changeRotation;
        #endregion

        #region Rotated Corner Sockets
        public int[][] rotatedTopCornerSockets { get; private set; }
        public int[][] rotatedBottomCornerSockets { get; private set; }
        public int[][] rotatedSideTopCornerSockets { get; private set; }
        public int[][] rotatedSideBtmCornerSockets { get; private set; }

        //INVERTED
        public int[][] invertedRotatedTopCornerSockets { get; private set; }
        public int[][] invertedRotatedBottomCornerSockets { get; private set; }
        public int[][] invertedRotatedSideTopCornerSockets { get; private set; }
        public int[][] invertedRotatedSideBtmCornerSockets { get; private set; }
        #endregion




        public int GetRotatedSideCornerSocketId(HexagonCorner corner, int rotation, bool top, bool inverted)
        {
            if (inverted)
            {
                EvaluateInvertedRotatedCornerSockets();
                return top ? invertedRotatedSideTopCornerSockets[rotation][(int)corner] : invertedRotatedSideBtmCornerSockets[rotation][(int)corner];
            }
            else
            {
                EvaluateRotatedCornerSockets();
                int val = top ? rotatedSideTopCornerSockets[rotation][(int)corner] : rotatedSideBtmCornerSockets[rotation][(int)corner];
                return val;
            }
        }

        public int[] GetRotatedLayerCornerSockets(bool top, int rotation, bool inverted = false)
        {
            if (inverted)
            {
                EvaluateInvertedRotatedCornerSockets();
                return top ? invertedRotatedTopCornerSockets[rotation] : invertedRotatedBottomCornerSockets[rotation];

            }
            else
            {
                EvaluateRotatedCornerSockets();
                return top ? rotatedTopCornerSockets[rotation] : rotatedBottomCornerSockets[rotation];
            }
        }

        public int[] GetRotatedSideCornerSockets(bool top, int rotation, bool inverted = false)
        {
            EvaluateRotatedCornerSockets();
            if (inverted)
            {
                int[] invertedRotatedsockets = GetInvertedSocketIds(top ? rotatedSideTopCornerSockets[rotation] : rotatedSideBtmCornerSockets[rotation]);
                return invertedRotatedsockets;
            }
            else
            {
                return top ? rotatedSideTopCornerSockets[rotation] : rotatedSideBtmCornerSockets[rotation];
            }
        }

        public (int[], int[]) GetRotatedCornerSocketsBySide(HexagonSide side, int rotation, bool inverted)
        {
            EvaluateRotatedCornerSockets();

            // if (inverted) side = HexagonCell.GetInvertedSide(side);
            (HexagonCorner cornerA, HexagonCorner cornerB) = HexagonCell.GetCornersFromSide(side);

            int[] top = new int[2];
            int[] bottom = new int[2];

            bottom[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true, inverted);
            bottom[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true, inverted);

            top[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true, inverted);
            top[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true, inverted);

            return (bottom, top);
        }

        private void EvaluateRotatedCornerSockets()
        {
            int rotations = 6;
            int corners = 12;
            int[][] newRotatedSideTopCornerSocketIds = new int[rotations][];
            int[][] newRotatedSideBtmCornerSocketIds = new int[rotations][];
            int[][] newRotatedTopCornerSocketIds = new int[rotations][];
            int[][] newRotatedBottomCornerSocketIds = new int[rotations][];

            for (int rot = 0; rot < rotations; rot++)
            {
                newRotatedSideTopCornerSocketIds[rot] = new int[corners];
                newRotatedSideBtmCornerSocketIds[rot] = new int[corners];
                newRotatedTopCornerSocketIds[rot] = new int[corners];
                newRotatedBottomCornerSocketIds[rot] = new int[corners];
            }
            // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
            for (int corner = 0; corner < corners; corner++)
            {
                newRotatedSideTopCornerSocketIds[0][corner] = sideTopCornerSocketIds[corner];
                newRotatedSideBtmCornerSocketIds[0][corner] = sideBtmCornerSocketIds[corner];

                newRotatedTopCornerSocketIds[0][corner] = topCornerSocketIds[corner];
                newRotatedBottomCornerSocketIds[0][corner] = bottomCornerSocketIds[corner];
            }

            // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
            for (int rot = 1; rot < rotations; rot++)
            {
                int offset = (rot == 0) ? 0 : rot - 1;
                // Side Top
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontRightA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontRightB];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontRightA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackRightA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontRightB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackRightB];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackRightA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackRightB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackB];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackLeftA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.BackLeftB];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackLeftA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.BackLeftB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontLeftA] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontA];
                newRotatedSideTopCornerSocketIds[rot][(int)HexagonCorner.FrontLeftB] = newRotatedSideTopCornerSocketIds[offset][(int)HexagonCorner.FrontB];
                // Side Bottom
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontRightA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontRightB];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontRightA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackRightA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontRightB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackRightB];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackRightA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackRightB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackB];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackLeftA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.BackLeftB];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackLeftA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.BackLeftB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontLeftA] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontA];
                newRotatedSideBtmCornerSocketIds[rot][(int)HexagonCorner.FrontLeftB] = newRotatedSideBtmCornerSocketIds[offset][(int)HexagonCorner.FrontB];
                // Top
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontRightA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontRightB];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontRightA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackRightA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontRightB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackRightB];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackRightA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackRightB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackB];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackLeftA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.BackLeftB];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackLeftA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.BackLeftB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontLeftA] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontA];
                newRotatedTopCornerSocketIds[rot][(int)HexagonCorner.FrontLeftB] = newRotatedTopCornerSocketIds[offset][(int)HexagonCorner.FrontB];
                // Bottom
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontRightA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontRightB];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontRightA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackRightA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontRightB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackRightB];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackRightA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackRightB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackB];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackLeftA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.BackLeftB];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackLeftA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontLeftA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.BackLeftB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontLeftB];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontLeftA] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontA];
                newRotatedBottomCornerSocketIds[rot][(int)HexagonCorner.FrontLeftB] = newRotatedBottomCornerSocketIds[offset][(int)HexagonCorner.FrontB];
            }

            rotatedSideTopCornerSockets = newRotatedSideTopCornerSocketIds;
            rotatedSideBtmCornerSockets = newRotatedSideBtmCornerSocketIds;
            rotatedTopCornerSockets = newRotatedTopCornerSocketIds;
            rotatedBottomCornerSockets = newRotatedBottomCornerSocketIds;
        }

        private void RecalculateEdgePoints()
        {
            _corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
            _sides = HexagonGenerator.GenerateHexagonSidePoints(_corners);
            EvaluateRotatedCornerSockets();
        }

        public static List<HexagonTile> ExtractClusterSetTiles(List<HexagonTile> tiles)
        {
            return tiles.FindAll(t => t.isInClusterSet);
        }
        public static List<HexagonTileCore> ExtractClusterSetTiles(List<HexagonTileCore> tiles)
        {
            return tiles.FindAll(t => t.isInClusterSet);
        }

        private void Awake()
        {
            center = transform;
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            RecalculateEdgePoints();
        }

        private void Start()
        {
            RecalculateEdgePoints();
        }


        void OnValidate()
        {
            if (isInvertable && model == null)
            {
                Debug.LogError("Invertable tile: " + gameObject.name + " has no model set!");
            }

            if (_uid == null || _uid == "") _uid = UtilityHelpers.GenerateUniqueID(this.gameObject);

            center = transform;
            // TransferAllSocketIDValues();

            if (changeRotation != _changeRotation)
            {
                _changeRotation = changeRotation;

                RotateTile(gameObject, changeRotation);
                // gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[changeRotation], 0f);
            }

            // if (tileSocketDirectory == null) tileSocketDirectory = (ITileSocketDirectory)_socketDirectory;

            if (noBaseLayer) baseLayerOnly = false;

            if (resetPoints || _currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
            {
                resetPoints = false;
                _currentCenterPosition = center.position;
                RecalculateEdgePoints();
                EvaluateRotatedCornerSockets();
            }

            // if (resetSockets != SocketResetState.Unset)
            // {
            //     ResetAllSockets(resetSockets);

            //     resetSockets = SocketResetState.Unset;
            // }

            // UpdateAllSocketIDs();


            if (!enableEditMode) return;

            if (!tileLabelGroup) tileLabelGroup = GetComponentInChildren<TileLabelGroup>();

            if (socketTextDisplay == null || socketTextDisplay.Length == 0) socketTextDisplay = TileLabelGroup.Reevaluate(this.gameObject);

            EvaluateTextDisplay();
            EvaluateSocketLabels(_showSocketLabelState != showSocketLabelState);

            if (generateMesh)
            {
                generateMesh = false;

                lastGeneratedMesh = HexagonGenerator.CreateHexagonMesh(_corners);
                // lastGeneratedMesh = HexagonGenerator.CreateHexagonMesh(UtilityHelpers.GetTransformPositions(_corners));
                if (meshFilter.sharedMesh == null)
                {
                    meshFilter.mesh = lastGeneratedMesh;
                    meshFilter.mesh.RecalculateNormals();
                }
            }
            if (saveMesh)
            {
                saveMesh = false;

                if (!lastGeneratedMesh) return;
                SaveMeshAsset(lastGeneratedMesh, "New Tile Mesh");
            }
        }

        #region Debug

        [Header("Debug Settings")]

        [Range(0, 5)][SerializeField] private int changeRotation = 0;

        float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };
        [SerializeField] private bool enableEditMode;
        [SerializeField] private SocketDisplayState showSocketLabelState;
        private SocketDisplayState _showSocketLabelState;
        [SerializeField] private bool showCorners;
        [SerializeField] private bool showSides;
        [SerializeField] private bool showEdges;
        [SerializeField] private bool resetPoints;
        [SerializeField] private bool ignoreSocketLabelUpdates;

        public void SetIgnoreSocketLabelUpdates(bool enable)
        {
            ignoreSocketLabelUpdates = enable;
        }
        public void ShowSocketLabels(bool enable)
        {
            showSocketLabelState = enable ? SocketDisplayState.ShowAll : SocketDisplayState.ShowNone;
            EvaluateSocketLabels(true);
        }

        private void EvaluateTextDisplay()
        {
            if (socketTextDisplay == null) return;

            string[] sideNames = Enum.GetNames(typeof(HexagonCorner));
            for (int i = 0; i < socketTextDisplay.Length; i++)
            {
                if (socketTextDisplay[i] == null) break;

                int sideDisplayRange = 24;
                string str;
                if (i < sideDisplayRange)
                {
                    str = i < sideNames.Length ? "SB " : "ST ";
                }
                else
                {
                    str = i < sideDisplayRange + 12 ? "BTM " : "TOP ";
                }
                socketTextDisplay[i].gameObject.name = str + sideNames[i % sideNames.Length];
            }
        }

        private void EvaluateSocketLabels(bool force = false)
        {
            if (force || _showSocketLabelState != showSocketLabelState)
            {
                _showSocketLabelState = showSocketLabelState;

                bool showAll = showSocketLabelState == SocketDisplayState.ShowAll;
                bool hideAll = showSocketLabelState == SocketDisplayState.ShowNone;
                int sideDisplayRange = 24;

                if (socketTextDisplay != null && socketTextDisplay.Length > 0)
                {
                    for (int i = 0; i < socketTextDisplay.Length; i++)
                    {
                        if (showAll || hideAll)
                        {
                            socketTextDisplay[i].SetActive(showAll);

                        }
                        else
                        {
                            if (showSocketLabelState == SocketDisplayState.ShowSides)
                            {
                                socketTextDisplay[i].SetActive(i < sideDisplayRange);
                            }
                            else if (showSocketLabelState == SocketDisplayState.ShowLayered)
                            {
                                socketTextDisplay[i].SetActive(i >= sideDisplayRange);
                            }
                        }
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            center = gameObject.transform;

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);


            if (!enableEditMode) return;

            if (showCorners)
            {
                Gizmos.color = Color.magenta;

                foreach (Vector3 item in _corners)
                {
                    Gizmos.DrawSphere(item, 0.3f);
                }
            }

            if (showSides)
            {
                for (int i = 0; i < _sides.Length; i++)
                {
                    Gizmos.color = Color.white;// tileSocketDirectory.sockets[sideSocketIds[i]].color;
                    Vector3 pos = _sides[i];
                    pos = pos - UtilityHelpers.FaceAwayFromPoint(center.position, _sides[i]);
                    // pos = pos - pos * 0.1f;
                    Gizmos.DrawSphere(pos, 1f);
                }
            }


            string[] cornerNames = Enum.GetNames(typeof(HexagonCorner));

            if (showEdges)
            {
                Gizmos.color = Color.magenta;
                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_corners);
            }


            if (ignoreSocketLabelUpdates || socketTextDisplay == null || socketTextDisplay.Length == 0) return;

            if (changeRotation > 0) EvaluateRotatedCornerSockets();

            int rotationAmount = 180;

            for (int i = 0; i < _sides.Length; i++)
            {
                HexagonSide side = (HexagonSide)i;
                // Top
                UpdateSocketLabelPlacement(side, true, false);
                UpdateSocketLabelPlacement(side, true, true);
                UpdateSocketLabel(side, true, false, rotationAmount);
                UpdateSocketLabel(side, true, true, rotationAmount);
                // Bottom
                UpdateSocketLabelPlacement(side, false, false);
                UpdateSocketLabelPlacement(side, false, true);
                UpdateSocketLabel(side, false, false, rotationAmount);
                UpdateSocketLabel(side, false, true, rotationAmount);
                rotationAmount += 60;
            }
        }

        private void UpdateSocketLabelPlacement(HexagonSide _side, bool top, bool layered)
        {

            // bool inverted = false;
            // if (gameObject.transform.localScale.z < 0f)
            // {
            //     inverted = true;
            //     _side = HexagonCell.GetInvertedSide(_side);
            // }

            (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexagonCell.GetCornersFromSide(_side);

            int cornerA = (int)_cornerA;
            int cornerB = (int)_cornerB;

            int side = (int)_side;
            int labelAIX;
            int labelBIX;
            if (layered)
            {
                labelAIX = top ? (36 + cornerA) : (24 + cornerA);
                labelBIX = top ? (36 + cornerB) : (24 + cornerB);
            }
            else
            {
                labelAIX = top ? (12 + cornerA) : cornerA;
                labelBIX = top ? (12 + cornerB) : cornerB;
            }

            Vector3 pos = transform.TransformPoint((_sides[side] - center.position) * labelForwardOffset);
            Quaternion rot = Quaternion.LookRotation(transform.TransformPoint(center.position - _sides[side]));

            int[] cornerSocketIds;
            if (layered)
            {
                rot = Quaternion.Euler(rot.x + 90f, rot.y, rot.z + 90);


                pos.y += top ? topLabelYOffset : bottomLabelYOffset;

                if (changeRotation > 0)
                {
                    cornerSocketIds = top ? rotatedTopCornerSockets[changeRotation] : rotatedBottomCornerSockets[changeRotation];
                }
                else
                {
                    cornerSocketIds = top ? topCornerSocketIds : bottomCornerSocketIds;
                }
            }
            else
            {
                pos.y += top ? sideTopLabelYOffset : sideBottomLabelYOffset;

                if (changeRotation > 0)
                {
                    cornerSocketIds = top ? rotatedSideTopCornerSockets[changeRotation] : rotatedSideBtmCornerSockets[changeRotation];
                }
                else
                {
                    cornerSocketIds = top ? sideTopCornerSocketIds : sideBtmCornerSocketIds;
                }
            }


            Color colorA = Color.white;
            Color colorB = Color.white;

            if (socketDirectory.colors != null)
            {
                colorA = socketDirectory.colors[cornerSocketIds[cornerA]];
                colorB = socketDirectory.colors[cornerSocketIds[cornerB]];
            }

            RectTransform rectA = socketTextDisplay[labelAIX].GetComponent<RectTransform>();
            RectTransform rectB = socketTextDisplay[labelBIX].GetComponent<RectTransform>();

            Vector3 leftPoint, rightPoint;
            GetLeftAndRightPoints(pos, labelXZOffset, out leftPoint, out rightPoint);

            leftPoint.y += 0.4f * side;
            rightPoint.y += 0.4f * side;

            UpdateTextRectTransform(rectA, pos, leftPoint, rot, colorA);
            UpdateTextRectTransform(rectB, pos, rightPoint, rot, colorB);
        }

        private void UpdateTextRectTransform(RectTransform rect, Vector3 pos, Vector3 point, Quaternion rotation, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(pos, 0.1f * transform.lossyScale.z);
            rect.position = point;
            rect.rotation = rotation;
            rect.localScale = new Vector3(0.05f, 0.05f, 1);
            rect.sizeDelta = new Vector2(48, 48);
        }

        public static void GetLeftAndRightPoints(Vector3 position, float distance, out Vector3 leftPoint, out Vector3 rightPoint)
        {
            // Find a perpendicular vector to the forward direction of the position
            Vector3 forward = position.normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

            // Calculate the left and right points at the specified distance
            leftPoint = position - (distance * right);
            rightPoint = position + (distance * right);
        }

        private void UpdateSocketLabel(HexagonSide _side, bool top, bool layered, int rotationAmount)
        {
            // TEMP check for inverted
            bool inverted = false;
            HexagonSide _displaySide = _side;
            HexagonCorner[] _displayCorners = new HexagonCorner[2];

            // if (gameObject.transform.localScale.z < 0f)
            // {
            //     inverted = true;
            //     _displaySide = HexagonCell.GetInvertedSide(_side);
            //     // Debug.Log("TRUE SIDE: " + _side + ", Inverted: " + _displaySide);
            // }

            (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexagonCell.GetCornersFromSide(_side);
            int cornerA = (int)_cornerA;
            int cornerB = (int)_cornerB;

            if (inverted)
            {
                (HexagonCorner _displayCornerA, HexagonCorner _displayCornerB) = HexagonCell.GetCornersFromSide(_displaySide);
                _displayCorners[0] = _displayCornerA;
                _displayCorners[1] = _displayCornerB;
            }
            else
            {
                _displayCorners[0] = _cornerA;
                _displayCorners[1] = _cornerB;
            }

            int side = (int)_side;

            int labelAIX;
            int labelBIX;
            int[] cornerSocketIds;

            if (layered)
            {
                labelAIX = top ? (36 + cornerA) : (24 + cornerA);
                labelBIX = top ? (36 + cornerB) : (24 + cornerB);
                if (changeRotation > 0)
                {
                    cornerSocketIds = top ? rotatedTopCornerSockets[changeRotation] : rotatedBottomCornerSockets[changeRotation];
                }
                else
                {
                    cornerSocketIds = top ? topCornerSocketIds : bottomCornerSocketIds;
                }
            }
            else
            {
                labelAIX = top ? (12 + cornerA) : cornerA;
                labelBIX = top ? (12 + cornerB) : cornerB;
                if (changeRotation > 0)
                {
                    cornerSocketIds = top ? rotatedSideTopCornerSockets[changeRotation] : rotatedSideBtmCornerSockets[changeRotation];
                }
                else
                {
                    if (isModelInverted)
                    {
                        cornerSocketIds = top ? sideTopCornerSocketIds : sideBtmCornerSocketIds;
                        cornerSocketIds = GetInvertedSocketIds(cornerSocketIds);
                    }
                    else
                    {
                        cornerSocketIds = top ? sideTopCornerSocketIds : sideBtmCornerSocketIds;
                    }

                }
            }

            TextMesh textMeshA = socketTextDisplay[labelAIX].GetComponent<TextMesh>();
            TextMesh textMeshB = socketTextDisplay[labelBIX].GetComponent<TextMesh>();
            string strA = inverted ? "INV_" + _displaySide + "_" : "";
            string strB = inverted ? "INV_" + _displaySide + "_" : "";

            string name_A = "";
            string name_B = "";

            textMeshA.color = Color.black;
            textMeshB.color = Color.black;

            if (socketDirectory != null)
            {

                textMeshA.color = socketDirectory.colors[cornerSocketIds[cornerA]];
                textMeshB.color = socketDirectory.colors[cornerSocketIds[cornerB]];
                name_A = socketDirectory.sockets[cornerSocketIds[cornerA]];
                name_B = socketDirectory.sockets[cornerSocketIds[cornerB]];
            }
            else
            {
                Debug.LogError(" socketDirectory is missing: " + gameObject.name);
            }

            if (top)
            {
                if (layered)
                {
                    strA += "id_" + cornerSocketIds[cornerA] + "\n" + name_A + "\n" + "Top " + _displayCorners[0];
                    strB += "id_" + cornerSocketIds[cornerB] + "\n" + name_B + "\n" + "Top " + _displayCorners[1];
                }
                else
                {
                    strA += "id_" + cornerSocketIds[cornerA] + "\n" + name_A + "\n" + "SideTop " + _displayCorners[0];
                    strB += "id_" + cornerSocketIds[cornerB] + "\n" + name_B + "\n" + "SideTop " + _displayCorners[1];
                }
            }
            else
            {
                if (layered)
                {
                    strA += "id_" + cornerSocketIds[cornerA] + "\n" + name_A + "\n" + "BTM " + _displayCorners[0];
                    strB += "id_" + cornerSocketIds[cornerB] + "\n" + name_B + "\n" + "BTM " + _displayCorners[1];
                }
                else
                {
                    strA += "id_" + cornerSocketIds[cornerA] + "\n" + name_A + "\n" + "SideBTM " + _displayCorners[0];
                    strB += "id_" + cornerSocketIds[cornerB] + "\n" + name_B + "\n" + "SideBTM " + _displayCorners[1];
                }
            }

            textMeshA.text = strA;
            textMeshA.fontSize = 56;
            textMeshA.fontStyle = FontStyle.Bold;

            textMeshB.text = strB;
            textMeshB.fontSize = 56;
            textMeshB.fontStyle = FontStyle.Bold;
        }

        #endregion


        public static void RotateTile(GameObject tile, int rotation)
        {
            float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };
            tile.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
        }


        public enum SocketDisplayState
        {
            ShowAll = 0,
            ShowNone = 1,
            ShowSides = 2,
            ShowLayered = 3,
        }

        [Header("Mesh Generation")]
        [SerializeField] private bool generateMesh;
        [SerializeField] private bool saveMesh;
        private Mesh lastGeneratedMesh;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;


        private void SaveMeshAsset(Mesh mesh, string assetName)
        {
            // Create a new mesh asset
            lastGeneratedMesh = Instantiate(mesh) as Mesh;
            lastGeneratedMesh.name = assetName;
            // Save the mesh asset to the project
            AssetDatabase.CreateAsset(lastGeneratedMesh, "Assets/Meshes/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }

        [System.Serializable]
        public struct HexagonSideEntry
        {
            public string name;
            public string socketName;
            [Range(0, 128)] public int TopA;
            [Range(0, 128)] public int TopB;
            [Range(0, 128)] public int BottomA;
            [Range(0, 128)] public int BottomB;
        }
    }
}