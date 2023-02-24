using System;
using UnityEngine;
using ProceduralBase;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WFCSystem
{

    [System.Serializable]
    public class HexagonTileMicro : MonoBehaviour
    {
        public int id = -1;
        public int size = 12;

        [Header("Tile Settings")]
        [SerializeField] private TileCategory _tileCategory;
        public TileCategory GetTileCategory() => _tileCategory;
        [SerializeField] private TileType _tileType;
        public TileType GetTileType() => _tileType;
        public bool IsExteriorWall() => _tileCategory == TileCategory.Wall && (_tileType == TileType.ExteriorWallLarge || _tileType == TileType.ExteriorWallSmall);

        [SerializeField] private TileContext _tileContext;
        public TileContext GetTileContext() => _tileContext;

        [Header("Layer Settings")]
        public bool baseLayerOnly;
        public bool noBaseLayer;
        public bool noGroundLayer;
        public bool isLayerConnector;

        [Header("Tile Compatibility / Probability")]
        public bool isEdgeable; // can be placed on the edge / border or the grid
        public bool isEntrance;
        public bool isStairway;
        public bool isPath;
        public bool isLeveledTile;
        public bool isVerticalWFC;
        public bool isLeveledRamp;
        public bool isFragment; // Is incomplete by itself, needs neighbor tiles like itself
        public bool isInClusterSet; // Is part of a set of tiles that make a cluster
        public bool isClusterCenterTile; // Is the center part of a set of tiles that make a cluster
        [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

        [Header("Tile Sides / Mirroring")]
        [SerializeField] private MirroredSideState mirroredSideState = 0;
        public MirroredSideState GetMirroredSideState() => mirroredSideState;

        [Header("Tile Socket Configuration")]
        [SerializeField] private MicroTileSocketDirectory tileSocketDirectory;
        [SerializeField] private TileLabelGroup tileLabelGroup;
        [Range(-10f, 0f)][SerializeField] private float sideBottomLabelYOffset = -4f;
        [Range(0f, 10f)][SerializeField] private float sideTopLabelYOffset = 4f;
        [Range(-16f, -4f)][SerializeField] private float bottomLabelYOffset = -6f;
        [Range(4f, 16f)][SerializeField] private float topLabelYOffset = 6f;
        [Range(0f, 10f)][SerializeField] private float labelXZOffset = 1f;
        [Range(0f, 10f)][SerializeField] private float labelForwardOffset = 2f;

        #region Tile Sockets
        [Header("Tile Environment")]
        public TileSocketEnvironment tileEnvironment = TileSocketEnvironment.Any;

        [Header("Tile Sockets")]
        [Header("Controls")]
        [SerializeField] private MicroTileSocket resetToSocket;
        [SerializeField] private SocketResetState resetSockets = SocketResetState.Unset;
        [SerializeField] private SocketMirrorState useSocketMirroring = SocketMirrorState.Unset;
        private int[] sideTopCornerSocketIds = new int[12];
        private int[] sideBtmCornerSocketIds = new int[12];
        private int[] topCornerSocketIds = new int[12];
        private int[] bottomCornerSocketIds = new int[12];
        public void SetCornerSocketSetIds(CornerSocketSetType socketSetType, int[] _newCornerSocketIds) { }

        [Header("Side Bottom Sockets")]
        [SerializeField] private MicroTileSocket sideBtmFrontA;
        [SerializeField] private MicroTileSocket sideBtmFrontB;
        [SerializeField] private MicroTileSocket sideBtmFrontRightA;
        [SerializeField] private MicroTileSocket sideBtmFrontRightB;
        [SerializeField] private MicroTileSocket sideBtmBackRightA;
        [SerializeField] private MicroTileSocket sideBtmBackRightB;
        [SerializeField] private MicroTileSocket sideBtmBackA;
        [SerializeField] private MicroTileSocket sideBtmBackB;
        [SerializeField] private MicroTileSocket sideBtmBackLeftA;
        [SerializeField] private MicroTileSocket sideBtmBackLeftB;
        [SerializeField] private MicroTileSocket sideBtmFrontLeftA;
        [SerializeField] private MicroTileSocket sideBtmFrontLeftB;

        [Header("Side Top Sockets")]
        [SerializeField] private MicroTileSocket sideTopFrontA;
        [SerializeField] private MicroTileSocket sideTopFrontB;
        [SerializeField] private MicroTileSocket sideTopFrontRightA;
        [SerializeField] private MicroTileSocket sideTopFrontRightB;
        [SerializeField] private MicroTileSocket sideTopBackRightA;
        [SerializeField] private MicroTileSocket sideTopBackRightB;
        [SerializeField] private MicroTileSocket sideTopBackA;
        [SerializeField] private MicroTileSocket sideTopBackB;
        [SerializeField] private MicroTileSocket sideTopBackLeftA;
        [SerializeField] private MicroTileSocket sideTopBackLeftB;
        [SerializeField] private MicroTileSocket sideTopFrontLeftA;
        [SerializeField] private MicroTileSocket sideTopFrontLeftB;

        [Header("Bottom Edge Sockets")]
        [SerializeField] private MicroTileSocket bottomFrontA;
        [SerializeField] private MicroTileSocket bottomFrontB;
        [SerializeField] private MicroTileSocket bottomFrontRightA;
        [SerializeField] private MicroTileSocket bottomFrontRightB;
        [SerializeField] private MicroTileSocket bottomBackRightA;
        [SerializeField] private MicroTileSocket bottomBackRightB;
        [SerializeField] private MicroTileSocket bottomBackA;
        [SerializeField] private MicroTileSocket bottomBackB;
        [SerializeField] private MicroTileSocket bottomBackLeftA;
        [SerializeField] private MicroTileSocket bottomBackLeftB;
        [SerializeField] private MicroTileSocket bottomFrontLeftA;
        [SerializeField] private MicroTileSocket bottomFrontLeftB;

        [Header("Top Edge Sockets")]
        [SerializeField] private MicroTileSocket topFrontA;
        [SerializeField] private MicroTileSocket topFrontB;
        [SerializeField] private MicroTileSocket topFrontRightA;
        [SerializeField] private MicroTileSocket topFrontRightB;
        [SerializeField] private MicroTileSocket topBackRightA;
        [SerializeField] private MicroTileSocket topBackRightB;
        [SerializeField] private MicroTileSocket topBackA;
        [SerializeField] private MicroTileSocket topBackB;
        [SerializeField] private MicroTileSocket topBackLeftA;
        [SerializeField] private MicroTileSocket topBackLeftB;
        [SerializeField] private MicroTileSocket topFrontLeftA;
        [SerializeField] private MicroTileSocket topFrontLeftB;
        #endregion

        private void UpdateAllSocketIDs()
        {
            if (useSocketMirroring != SocketMirrorState.Unset)
            {

                if (useSocketMirroring == SocketMirrorState.MirrorSideBtmToTop)
                {
                    sideTopFrontA = sideBtmFrontA;
                    sideTopFrontB = sideBtmFrontB;
                    sideTopFrontRightA = sideBtmFrontRightA;
                    sideTopFrontRightB = sideBtmFrontRightB;
                    sideTopBackRightA = sideBtmBackRightA;
                    sideTopBackRightB = sideBtmBackRightB;
                    sideTopBackA = sideBtmBackA;
                    sideTopBackB = sideBtmBackB;
                    sideTopBackLeftA = sideBtmBackLeftA;
                    sideTopBackLeftB = sideBtmBackLeftB;
                    sideTopFrontLeftA = sideBtmFrontLeftA;
                    sideTopFrontLeftB = sideBtmFrontLeftB;
                }
                else if (useSocketMirroring == SocketMirrorState.MirrorSideTopToBtm)
                {
                    sideBtmFrontA = sideTopFrontA;
                    sideBtmFrontB = sideTopFrontB;
                    sideBtmFrontRightA = sideTopFrontRightA;
                    sideBtmFrontRightB = sideTopFrontRightB;
                    sideBtmBackRightA = sideTopBackRightA;
                    sideBtmBackRightB = sideTopBackRightB;
                    sideBtmBackA = sideTopBackA;
                    sideBtmBackB = sideTopBackB;
                    sideBtmBackLeftA = sideTopBackLeftA;
                    sideBtmBackLeftB = sideTopBackLeftB;
                    sideBtmFrontLeftA = sideTopFrontLeftA;
                    sideBtmFrontLeftB = sideTopFrontLeftB;
                }
                else if (useSocketMirroring == SocketMirrorState.MirrorBtmToTop)
                {
                    topFrontA = bottomFrontA;
                    topFrontB = bottomFrontB;
                    topFrontRightA = bottomFrontRightA;
                    topFrontRightB = bottomFrontRightB;
                    topBackRightA = bottomBackRightA;
                    topBackRightB = bottomBackRightB;
                    topBackA = bottomBackA;
                    topBackB = bottomBackB;
                    topBackLeftA = bottomBackLeftA;
                    topBackLeftB = bottomBackLeftB;
                    topFrontLeftA = bottomFrontLeftA;
                    topFrontLeftB = bottomFrontLeftB;
                }
                else if (useSocketMirroring == SocketMirrorState.MirrorTopToBtm)
                {
                    bottomFrontA = topFrontA;
                    bottomFrontB = topFrontB;
                    bottomFrontRightA = topFrontRightA;
                    bottomFrontRightB = topFrontRightB;
                    bottomBackRightA = topBackRightA;
                    bottomBackRightB = topBackRightB;
                    bottomBackA = topBackA;
                    bottomBackB = topBackB;
                    bottomBackLeftA = topBackLeftA;
                    bottomBackLeftB = topBackLeftB;
                    bottomFrontLeftA = topFrontLeftA;
                    bottomFrontLeftB = topFrontLeftB;
                }
                else if (useSocketMirroring == SocketMirrorState.MirrorLayersToSides)
                {
                    sideBtmFrontA = bottomFrontA;
                    sideBtmFrontB = bottomFrontB;
                    sideBtmFrontRightA = bottomFrontRightA;
                    sideBtmFrontRightB = bottomFrontRightB;
                    sideBtmBackRightA = bottomBackRightA;
                    sideBtmBackRightB = bottomBackRightB;
                    sideBtmBackA = bottomBackA;
                    sideBtmBackB = bottomBackB;
                    sideBtmBackLeftA = bottomBackLeftA;
                    sideBtmBackLeftB = bottomBackLeftB;
                    sideBtmFrontLeftA = bottomFrontLeftA;
                    sideBtmFrontLeftB = bottomFrontLeftB;

                    sideTopFrontA = topFrontA;
                    sideTopFrontB = topFrontB;
                    sideTopFrontRightA = topFrontRightA;
                    sideTopFrontRightB = topFrontRightB;
                    sideTopBackRightA = topBackRightA;
                    sideTopBackRightB = topBackRightB;
                    sideTopBackA = topBackA;
                    sideTopBackB = topBackB;
                    sideTopBackLeftA = topBackLeftA;
                    sideTopBackLeftB = topBackLeftB;
                    sideTopFrontLeftA = topFrontLeftA;
                    sideTopFrontLeftB = topFrontLeftB;
                }
                else if (useSocketMirroring == SocketMirrorState.MirrorSidesToLayers)
                {
                    bottomFrontA = sideBtmFrontA;
                    bottomFrontB = sideBtmFrontB;
                    bottomFrontRightA = sideBtmFrontRightA;
                    bottomFrontRightB = sideBtmFrontRightB;
                    bottomBackRightA = sideBtmBackRightA;
                    bottomBackRightB = sideBtmBackRightB;
                    bottomBackA = sideBtmBackA;
                    bottomBackB = sideBtmBackB;
                    bottomBackLeftA = sideBtmBackLeftA;
                    bottomBackLeftB = sideBtmBackLeftB;
                    bottomFrontLeftA = sideBtmFrontLeftA;
                    bottomFrontLeftB = sideBtmFrontLeftB;

                    topFrontA = sideTopFrontA;
                    topFrontB = sideTopFrontB;
                    topFrontRightA = sideTopFrontRightA;
                    topFrontRightB = sideTopFrontRightB;
                    topBackRightA = sideTopBackRightA;
                    topBackRightB = sideTopBackRightB;
                    topBackA = sideTopBackA;
                    topBackB = sideTopBackB;
                    topBackLeftA = sideTopBackLeftA;
                    topBackLeftB = sideTopBackLeftB;
                    topFrontLeftA = sideTopFrontLeftA;
                    topFrontLeftB = sideTopFrontLeftB;
                }
            }

            // Top & BTM
            bottomCornerSocketIds[0] = (int)bottomFrontA;
            bottomCornerSocketIds[1] = (int)bottomFrontB;
            bottomCornerSocketIds[2] = (int)bottomFrontRightA;
            bottomCornerSocketIds[3] = (int)bottomFrontRightB;
            bottomCornerSocketIds[4] = (int)bottomBackRightA;
            bottomCornerSocketIds[5] = (int)bottomBackRightB;
            bottomCornerSocketIds[6] = (int)bottomBackA;
            bottomCornerSocketIds[7] = (int)bottomBackB;
            bottomCornerSocketIds[8] = (int)bottomBackLeftA;
            bottomCornerSocketIds[9] = (int)bottomBackLeftB;
            bottomCornerSocketIds[10] = (int)bottomFrontLeftA;
            bottomCornerSocketIds[11] = (int)bottomFrontLeftB;

            topCornerSocketIds[0] = (int)topFrontA;
            topCornerSocketIds[1] = (int)topFrontB;
            topCornerSocketIds[2] = (int)topFrontRightA;
            topCornerSocketIds[3] = (int)topFrontRightB;
            topCornerSocketIds[4] = (int)topBackRightA;
            topCornerSocketIds[5] = (int)topBackRightB;
            topCornerSocketIds[6] = (int)topBackA;
            topCornerSocketIds[7] = (int)topBackB;
            topCornerSocketIds[8] = (int)topBackLeftA;
            topCornerSocketIds[9] = (int)topBackLeftB;
            topCornerSocketIds[10] = (int)topFrontLeftA;
            topCornerSocketIds[11] = (int)topFrontLeftB;

            // Side Top & BTM
            sideBtmCornerSocketIds[0] = (int)sideBtmFrontA;
            sideBtmCornerSocketIds[1] = (int)sideBtmFrontB;
            sideBtmCornerSocketIds[2] = (int)sideBtmFrontRightA;
            sideBtmCornerSocketIds[3] = (int)sideBtmFrontRightB;
            sideBtmCornerSocketIds[4] = (int)sideBtmBackRightA;
            sideBtmCornerSocketIds[5] = (int)sideBtmBackRightB;
            sideBtmCornerSocketIds[6] = (int)sideBtmBackA;
            sideBtmCornerSocketIds[7] = (int)sideBtmBackB;
            sideBtmCornerSocketIds[8] = (int)sideBtmBackLeftA;
            sideBtmCornerSocketIds[9] = (int)sideBtmBackLeftB;
            sideBtmCornerSocketIds[10] = (int)sideBtmFrontLeftA;
            sideBtmCornerSocketIds[11] = (int)sideBtmFrontLeftB;

            sideTopCornerSocketIds[0] = (int)sideTopFrontA;
            sideTopCornerSocketIds[1] = (int)sideTopFrontB;
            sideTopCornerSocketIds[2] = (int)sideTopFrontRightA;
            sideTopCornerSocketIds[3] = (int)sideTopFrontRightB;
            sideTopCornerSocketIds[4] = (int)sideTopBackRightA;
            sideTopCornerSocketIds[5] = (int)sideTopBackRightB;
            sideTopCornerSocketIds[6] = (int)sideTopBackA;
            sideTopCornerSocketIds[7] = (int)sideTopBackB;
            sideTopCornerSocketIds[8] = (int)sideTopBackLeftA;
            sideTopCornerSocketIds[9] = (int)sideTopBackLeftB;
            sideTopCornerSocketIds[10] = (int)sideTopFrontLeftA;
            sideTopCornerSocketIds[11] = (int)sideTopFrontLeftB;
        }


        private void ResetAllSockets(SocketResetState resetState)
        {
            bool resetAll = resetState == SocketResetState.All;
            if (resetAll || resetState == SocketResetState.Bottom)
            {
                bottomFrontA = resetToSocket;
                bottomFrontB = resetToSocket;
                bottomFrontRightA = resetToSocket;
                bottomFrontRightB = resetToSocket;
                bottomBackRightA = resetToSocket;
                bottomBackRightB = resetToSocket;
                bottomBackA = resetToSocket;
                bottomBackB = resetToSocket;
                bottomBackLeftA = resetToSocket;
                bottomBackLeftB = resetToSocket;
                bottomFrontLeftA = resetToSocket;
                bottomFrontLeftB = resetToSocket;
            }
            if (resetAll || resetState == SocketResetState.SideBottom)
            {
                sideBtmFrontA = resetToSocket;
                sideBtmFrontB = resetToSocket;
                sideBtmFrontRightA = resetToSocket;
                sideBtmFrontRightB = resetToSocket;
                sideBtmBackRightA = resetToSocket;
                sideBtmBackRightB = resetToSocket;
                sideBtmBackA = resetToSocket;
                sideBtmBackB = resetToSocket;
                sideBtmBackLeftA = resetToSocket;
                sideBtmBackLeftB = resetToSocket;
                sideBtmFrontLeftA = resetToSocket;
                sideBtmFrontLeftB = resetToSocket;
            }
            if (resetAll || resetState == SocketResetState.SideTop)
            {
                sideTopFrontA = resetToSocket;
                sideTopFrontB = resetToSocket;
                sideTopFrontRightA = resetToSocket;
                sideTopFrontRightB = resetToSocket;
                sideTopBackRightA = resetToSocket;
                sideTopBackRightB = resetToSocket;
                sideTopBackA = resetToSocket;
                sideTopBackB = resetToSocket;
                sideTopBackLeftA = resetToSocket;
                sideTopBackLeftB = resetToSocket;
                sideTopFrontLeftA = resetToSocket;
                sideTopFrontLeftB = resetToSocket;
            }
            if (resetAll || resetState == SocketResetState.Top)
            {
                topFrontA = resetToSocket;
                topFrontB = resetToSocket;
                topFrontRightA = resetToSocket;
                topFrontRightB = resetToSocket;
                topBackRightA = resetToSocket;
                topBackRightB = resetToSocket;
                topBackA = resetToSocket;
                topBackB = resetToSocket;
                topBackLeftA = resetToSocket;
                topBackLeftB = resetToSocket;
                topFrontLeftA = resetToSocket;
                topFrontLeftB = resetToSocket;
            }
        }

        public int GetCornerSocketId(HexagonCorner corner, bool top, bool layered)
        {
            if (layered)
            {
                return top ? topCornerSocketIds[(int)corner] : bottomCornerSocketIds[(int)corner];
            }
            else
            {
                return top ? sideTopCornerSocketIds[(int)corner] : sideBtmCornerSocketIds[(int)corner];
            }
        }

        private Transform center;
        [SerializeField] private Vector3[] _corners;
        [SerializeField] private Vector3[] _sides;
        [SerializeField] private HexagonSideEntry[] sideSockets;
        public GameObject[] socketTextDisplay;

        [Header("Rotation")]
        [Range(0, 5)][SerializeField] private int currentRotation = 0;
        void RotateTransform(int rotation, Transform transform)
        {
            float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };
            transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
        }

        #region Saved Values
        private Vector3 _currentCenterPosition;
        #endregion

        public int[][] rotatedTopCornerSockets { get; private set; }
        public int[][] rotatedBottomCornerSockets { get; private set; }
        public int[][] rotatedSideTopCornerSockets { get; private set; }
        public int[][] rotatedSideBtmCornerSockets { get; private set; }

        public int GetRotatedSideCornerSocketId(HexagonCorner corner, int rotation, bool top)
        {
            EvaluateRotatedCornerSockets();

            Debug.Log("GetRotatedSideCornerSocketId - rotation: " + rotation + ", corner: " + corner + ", top: " + top);
            int val = top ? rotatedSideTopCornerSockets[rotation][(int)corner] : rotatedSideBtmCornerSockets[rotation][(int)corner];
            Debug.Log("GetRotatedSideCornerSocketId - val: " + val);
            return val;
        }
        public int GetRotatedCornerSocketId(HexagonCorner corner, int rotation, bool top)
        {
            EvaluateRotatedCornerSockets();

            Debug.Log("GetRotatedCornerSocketId - rotation: " + rotation + ", corner: " + corner + ", top: " + top);
            int val = top ? rotatedTopCornerSockets[rotation][(int)corner] : rotatedBottomCornerSockets[rotation][(int)corner];
            Debug.Log("GetRotatedCornerSocketId - val: " + val);
            return val;
        }

        public int[] GetRotatedLayerCornerSockets(bool top, int rotation)
        {
            EvaluateRotatedCornerSockets();
            return top ? rotatedTopCornerSockets[rotation] : rotatedBottomCornerSockets[rotation];
        }

        public (int[], int[]) GetRotatedCornerSocketsBySide(HexagonSide side, int rotation)
        {
            EvaluateRotatedCornerSockets();

            (HexagonCorner cornerA, HexagonCorner cornerB) = HexagonCell.GetCornersFromSide(side);

            int[] top = new int[2];
            int[] bottom = new int[2];

            bottom[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true);
            bottom[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true);

            top[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true);
            top[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true);

            return (bottom, top);
        }

        private void EvaluateRotatedCornerSockets()
        {
            int sides = 6;
            int corners = 12;
            int[][] newRotatedSideTopCornerSocketIds = new int[sides][];
            int[][] newRotatedSideBtmCornerSocketIds = new int[sides][];
            int[][] newRotatedTopCornerSocketIds = new int[sides][];
            int[][] newRotatedBottomCornerSocketIds = new int[sides][];

            for (int i = 0; i < sides; i++)
            {
                newRotatedSideTopCornerSocketIds[i] = new int[corners];
                newRotatedSideBtmCornerSocketIds[i] = new int[corners];
                newRotatedTopCornerSocketIds[i] = new int[corners];
                newRotatedBottomCornerSocketIds[i] = new int[corners];
            }
            // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
            for (int i = 0; i < corners; i++)
            {
                newRotatedSideTopCornerSocketIds[0][i] = sideTopCornerSocketIds[i];
                newRotatedSideBtmCornerSocketIds[0][i] = sideBtmCornerSocketIds[i];

                newRotatedTopCornerSocketIds[0][i] = topCornerSocketIds[i];
                newRotatedBottomCornerSocketIds[0][i] = bottomCornerSocketIds[i];
            }

            // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
            for (int i = 1; i < sides; i++)
            {
                // Side Top
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightB];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontRightA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackRightA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontRightB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackRightB];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackRightA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackRightB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackB];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftB];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackLeftA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.BackLeftB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftB];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontLeftA] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontA];
                newRotatedSideTopCornerSocketIds[i][(int)HexagonCorner.FrontLeftB] = newRotatedSideTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontB];
                // Side Bottom
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightB];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontRightA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackRightA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontRightB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackRightB];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackRightA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackRightB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackB];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftB];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackLeftA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.BackLeftB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftB];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontLeftA] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontA];
                newRotatedSideBtmCornerSocketIds[i][(int)HexagonCorner.FrontLeftB] = newRotatedSideBtmCornerSocketIds[i - 1][(int)HexagonCorner.FrontB];

                // Top
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightB];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontRightA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackRightA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontRightB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackRightB];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackRightA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackRightB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackB];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftB];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackLeftA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.BackLeftB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftB];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontLeftA] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontA];
                newRotatedTopCornerSocketIds[i][(int)HexagonCorner.FrontLeftB] = newRotatedTopCornerSocketIds[i - 1][(int)HexagonCorner.FrontB];
                // Bottom
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontRightB];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontRightA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackRightA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontRightB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackRightB];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackRightA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackRightB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackB];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.BackLeftB];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackLeftA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.BackLeftB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontLeftB];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontLeftA] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontA];
                newRotatedBottomCornerSocketIds[i][(int)HexagonCorner.FrontLeftB] = newRotatedBottomCornerSocketIds[i - 1][(int)HexagonCorner.FrontB];
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
            center = transform;
            // TransferAllSocketIDValues();

            if (noBaseLayer) baseLayerOnly = false;

            if (resetPoints || _currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
            {
                resetPoints = false;
                _currentCenterPosition = center.position;
                RecalculateEdgePoints();
                EvaluateRotatedCornerSockets();
            }

            if (resetSockets != SocketResetState.Unset)
            {
                ResetAllSockets(resetSockets);

                resetSockets = SocketResetState.Unset;
            }

            UpdateAllSocketIDs();


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
        [SerializeField] private bool enableEditMode;
        // [SerializeField] private bool showSocketLabels;
        // private bool _showSocketLabels = false;
        [SerializeField] private SocketDisplayState showSocketLabelState;
        private SocketDisplayState _showSocketLabelState;
        [SerializeField] private bool showCorners;
        [SerializeField] private bool showSides;
        [SerializeField] private bool showEdges;
        [SerializeField] private bool resetPoints;

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
            center = transform;

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

            // if (showSides)
            // {
            //     for (int i = 0; i < _sides.Length; i++)
            //     {
            //         // // Fix if out of bounds
            //         // if (sideSocketIds[i] > tileSocketDirectory.sockets.Length - 1)
            //         // {
            //         //     sideSocketIds[i] = tileSocketDirectory.sockets.Length - 1;
            //         // }

            //         Gizmos.color = tileSocketDirectory.sockets[sideSocketIds[i]].color;
            //         Vector3 pos = _sides[i];
            //         pos = pos - UtilityHelpers.FaceAwayFromPoint(center.position, _sides[i]);
            //         // pos = pos - pos * 0.1f;
            //         Gizmos.DrawSphere(pos, 1f);
            //     }
            // }


            string[] cornerNames = Enum.GetNames(typeof(HexagonCorner));

            if (showEdges)
            {
                Gizmos.color = Color.magenta;
                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_corners);
            }

            if (socketTextDisplay == null || socketTextDisplay.Length == 0) return;

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

            Vector3 pos = (_sides[side] - center.position) * labelForwardOffset;
            Quaternion rot = Quaternion.LookRotation(center.position - _sides[side]);
            int[] cornerSocketIds;
            if (layered)
            {
                pos.y += top ? topLabelYOffset : bottomLabelYOffset;
                cornerSocketIds = top ? topCornerSocketIds : bottomCornerSocketIds;
            }
            else
            {
                pos.y += top ? sideTopLabelYOffset : sideBottomLabelYOffset;
                cornerSocketIds = top ? sideTopCornerSocketIds : sideBtmCornerSocketIds;
            }

            // Corner A
            pos.x += labelXZOffset;
            Gizmos.color = tileSocketDirectory ? tileSocketDirectory.compatibilityTable[cornerSocketIds[cornerA]].color : Color.white;
            Gizmos.DrawSphere(pos, 0.1f * transform.lossyScale.z);
            socketTextDisplay[labelAIX].GetComponent<RectTransform>().position = pos;
            socketTextDisplay[labelAIX].GetComponent<RectTransform>().rotation = rot;

            // Corner B
            pos.x -= labelXZOffset * 2;
            Gizmos.color = tileSocketDirectory ? tileSocketDirectory.compatibilityTable[cornerSocketIds[cornerB]].color : Color.white;
            Gizmos.DrawSphere(pos, 0.1f * transform.lossyScale.z);
            socketTextDisplay[labelBIX].GetComponent<RectTransform>().position = pos;
            socketTextDisplay[labelBIX].GetComponent<RectTransform>().rotation = rot;
        }

        private void UpdateSocketLabel(HexagonSide _side, bool top, bool layered, int rotationAmount)
        {
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
            // int labelAIX = top ? (12 + cornerA) : cornerA;
            // int labelBIX = top ? (12 + cornerB) : cornerB;

            TextMesh textMeshA = socketTextDisplay[labelAIX].GetComponent<TextMesh>();
            TextMesh textMeshB = socketTextDisplay[labelBIX].GetComponent<TextMesh>();
            string strA;
            string strB;

            if (top)
            {
                if (layered)
                {
                    textMeshA.color = tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerA]].color;
                    strA = "id_" + topCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerA]].name + "\n" + "Top " + _cornerA;
                    textMeshB.color = tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerB]].color;
                    strB = "id_" + topCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerB]].name + "\n" + "Top " + _cornerB;
                }
                else
                {
                    textMeshA.color = tileSocketDirectory.compatibilityTable[sideTopCornerSocketIds[cornerA]].color;
                    strA = "id_" + sideTopCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[sideTopCornerSocketIds[cornerA]].name + "\n" + "SideTop " + _cornerA;
                    textMeshB.color = tileSocketDirectory.compatibilityTable[sideTopCornerSocketIds[cornerB]].color;
                    strB = "id_" + sideTopCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[sideTopCornerSocketIds[cornerB]].name + "\n" + "SideTop " + _cornerB;
                }
            }
            else
            {

                if (layered)
                {
                    textMeshA.color = tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerA]].color;
                    strA = "id_" + bottomCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerA]].name + "\n" + "BTM " + _cornerA;
                    textMeshB.color = tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerB]].color;
                    strB = "id_" + bottomCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerB]].name + "\n" + "BTM " + _cornerB;
                }
                else
                {
                    textMeshA.color = tileSocketDirectory.compatibilityTable[sideBtmCornerSocketIds[cornerA]].color;
                    strA = "id_" + sideBtmCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[sideBtmCornerSocketIds[cornerA]].name + "\n" + "SideBTM " + _cornerA;
                    textMeshB.color = tileSocketDirectory.compatibilityTable[sideBtmCornerSocketIds[cornerB]].color;
                    strB = "id_" + sideBtmCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[sideBtmCornerSocketIds[cornerB]].name + "\n" + "SideBTM " + _cornerB;
                }
            }

            textMeshA.text = strA;
            textMeshA.fontSize = 11;

            textMeshB.text = strB;
            textMeshB.fontSize = 11;
        }

        #endregion


        public enum SocketDisplayState
        {
            ShowAll = 0,
            ShowNone = 1,
            ShowSides = 2,
            ShowLayered = 3,
        }

        public enum SocketMirrorState
        {
            Unset = 0,
            MirrorSideBtmToTop = 1,
            MirrorSideTopToBtm = 2,
            MirrorTopToBtm = 3,
            MirrorBtmToTop = 4,
            MirrorSidesToLayers = 5,
            MirrorLayersToSides = 6,
        }

        public enum SocketResetState
        {
            Unset = 0,
            All = 1,
            Bottom = 2,
            Top = 3,
            SideBottom = 4,
            SideTop = 5,
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