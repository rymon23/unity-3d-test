using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace WFCSystem
{

    [System.Serializable]
    public class TileSocketController : MonoBehaviour
    {
        [Header("Tile")]
        [SerializeField] private HexagonTileCore tile;
        [SerializeField] private HexagonSocketDirectory tileSocketDirectory;
        [SerializeField] private HexagonSocketDataManager tileSocketDataManager;
        public HexagonSocketDirectory GetTileSocketDirectory() => tileSocketDirectory;

        [Header("Inherit Sockets From Another Tile")]
        [SerializeField] private HexagonTileCore tileSocketParent;
        [SerializeField] private bool inheritOnSides;
        [SerializeField] private bool inheritOnLayers;

        public int swappableSideSocketA { get; set; }
        public int swappableSideSocketB { get; set; }
        private int _swappableSideSocketA;
        private int _swappableSideSocketB;
        public void UpdateSwappableSocketIDs()
        {
            if (_swappableSideSocketA != swappableSideSocketA)
            {
                _swappableSideSocketA = swappableSideSocketA;
            }
            if (_swappableSideSocketB != swappableSideSocketB)
            {
                _swappableSideSocketB = swappableSideSocketB;
            }
        }


        #region Tile Sockets
        [Header("Tile Sockets")]
        [SerializeField] private SocketResetState resetSockets = SocketResetState.Unset;
        [SerializeField] private SocketMirrorState useSocketMirroring = SocketMirrorState.Unset;
        [SerializeField] public int resetToSocket { get; set; }
        [SerializeField] private bool reevaluate;

        // [Header("Side Bottom Sockets")]
        [SerializeField] public int sideBtmFrontA { get; set; }
        [SerializeField] public int sideBtmFrontB { get; set; }
        [SerializeField] public int sideBtmFrontRightA { get; set; }
        [SerializeField] public int sideBtmFrontRightB { get; set; }
        [SerializeField] public int sideBtmBackRightA { get; set; }
        [SerializeField] public int sideBtmBackRightB { get; set; }
        [SerializeField] public int sideBtmBackA { get; set; }
        [SerializeField] public int sideBtmBackB { get; set; }
        [SerializeField] public int sideBtmBackLeftA { get; set; }
        [SerializeField] public int sideBtmBackLeftB { get; set; }
        [SerializeField] public int sideBtmFrontLeftA { get; set; }
        [SerializeField] public int sideBtmFrontLeftB { get; set; }

        // [Header("Side Top Sockets")]
        [SerializeField] public int sideTopFrontA { get; set; }
        [SerializeField] public int sideTopFrontB { get; set; }
        [SerializeField] public int sideTopFrontRightA { get; set; }
        [SerializeField] public int sideTopFrontRightB { get; set; }
        [SerializeField] public int sideTopBackRightA { get; set; }
        [SerializeField] public int sideTopBackRightB { get; set; }
        [SerializeField] public int sideTopBackA { get; set; }
        [SerializeField] public int sideTopBackB { get; set; }
        [SerializeField] public int sideTopBackLeftA { get; set; }
        [SerializeField] public int sideTopBackLeftB { get; set; }
        [SerializeField] public int sideTopFrontLeftA { get; set; }
        [SerializeField] public int sideTopFrontLeftB { get; set; }

        // [Header("Bottom Edge Sockets")]
        [SerializeField] public int bottomFrontA { get; set; }
        [SerializeField] public int bottomFrontB { get; set; }
        [SerializeField] public int bottomFrontRightA { get; set; }
        [SerializeField] public int bottomFrontRightB { get; set; }
        [SerializeField] public int bottomBackRightA { get; set; }
        [SerializeField] public int bottomBackRightB { get; set; }
        [SerializeField] public int bottomBackA { get; set; }
        [SerializeField] public int bottomBackB { get; set; }
        [SerializeField] public int bottomBackLeftA { get; set; }
        [SerializeField] public int bottomBackLeftB { get; set; }
        [SerializeField] public int bottomFrontLeftA { get; set; }
        [SerializeField] public int bottomFrontLeftB { get; set; }

        // [Header("Top Edge Sockets")]
        [SerializeField] public int topFrontA { get; set; }
        [SerializeField] public int topFrontB { get; set; }
        [SerializeField] public int topFrontRightA { get; set; }
        [SerializeField] public int topFrontRightB { get; set; }
        [SerializeField] public int topBackRightA { get; set; }
        [SerializeField] public int topBackRightB { get; set; }
        [SerializeField] public int topBackA { get; set; }
        [SerializeField] public int topBackB { get; set; }
        [SerializeField] public int topBackLeftA { get; set; }
        [SerializeField] public int topBackLeftB { get; set; }
        [SerializeField] public int topFrontLeftA { get; set; }
        [SerializeField] public int topFrontLeftB { get; set; }
        #endregion

        public void EvaluateAllSocketIDs()
        {
            // Debug.Log("EvaluateAllSocketIDs");
            swappableSideSocketA = _swappableSideSocketA;
            swappableSideSocketB = _swappableSideSocketB;

            // Top & BTM
            int[] _bottomCornerSocketIds = tile.bottomCornerSocketIds;
            bottomFrontA = _bottomCornerSocketIds[0];
            bottomFrontB = _bottomCornerSocketIds[1];
            bottomFrontRightA = _bottomCornerSocketIds[2];
            bottomFrontRightB = _bottomCornerSocketIds[3];
            bottomBackRightA = _bottomCornerSocketIds[4];
            bottomBackRightB = _bottomCornerSocketIds[5];
            bottomBackA = _bottomCornerSocketIds[6];
            bottomBackB = _bottomCornerSocketIds[7];
            bottomBackLeftA = _bottomCornerSocketIds[8];
            bottomBackLeftB = _bottomCornerSocketIds[9];
            bottomFrontLeftA = _bottomCornerSocketIds[10];
            bottomFrontLeftB = _bottomCornerSocketIds[11];

            int[] _topCornerSocketIds = tile.topCornerSocketIds;
            topFrontA = _topCornerSocketIds[0];
            topFrontB = _topCornerSocketIds[1];
            topFrontRightA = _topCornerSocketIds[2];
            topFrontRightB = _topCornerSocketIds[3];
            topBackRightA = _topCornerSocketIds[4];
            topBackRightB = _topCornerSocketIds[5];
            topBackA = _topCornerSocketIds[6];
            topBackB = _topCornerSocketIds[7];
            topBackLeftA = _topCornerSocketIds[8];
            topBackLeftB = _topCornerSocketIds[9];
            topFrontLeftA = _topCornerSocketIds[10];
            topFrontLeftB = _topCornerSocketIds[11];


            // Side Top & BTM
            int[] _sideBtmCornerSocketIds = tile.sideBtmCornerSocketIds;
            sideBtmFrontA = _sideBtmCornerSocketIds[0];
            sideBtmFrontB = _sideBtmCornerSocketIds[1];
            sideBtmFrontRightA = _sideBtmCornerSocketIds[2];
            sideBtmFrontRightB = _sideBtmCornerSocketIds[3];
            sideBtmBackRightA = _sideBtmCornerSocketIds[4];
            sideBtmBackRightB = _sideBtmCornerSocketIds[5];
            sideBtmBackA = _sideBtmCornerSocketIds[6];
            sideBtmBackB = _sideBtmCornerSocketIds[7];
            sideBtmBackLeftA = _sideBtmCornerSocketIds[8];
            sideBtmBackLeftB = _sideBtmCornerSocketIds[9];
            sideBtmFrontLeftA = _sideBtmCornerSocketIds[10];
            sideBtmFrontLeftB = _sideBtmCornerSocketIds[11];

            int[] _sideTopCornerSocketIds = tile.sideTopCornerSocketIds;
            sideTopFrontA = _sideTopCornerSocketIds[0];
            sideTopFrontB = _sideTopCornerSocketIds[1];
            sideTopFrontRightA = _sideTopCornerSocketIds[2];
            sideTopFrontRightB = _sideTopCornerSocketIds[3];
            sideTopBackRightA = _sideTopCornerSocketIds[4];
            sideTopBackRightB = _sideTopCornerSocketIds[5];
            sideTopBackA = _sideTopCornerSocketIds[6];
            sideTopBackB = _sideTopCornerSocketIds[7];
            sideTopBackLeftA = _sideTopCornerSocketIds[8];
            sideTopBackLeftB = _sideTopCornerSocketIds[9];
            sideTopFrontLeftA = _sideTopCornerSocketIds[10];
            sideTopFrontLeftB = _sideTopCornerSocketIds[11];
        }

        public void UpdateAllSocketIDs()
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

            Update_BottomCornerSocketIds();
            Update_TopCornerSocketIds();
            Update_SideBottomCornerSocketIds();
            Update_SideTopCornerSocketIds();

            UpdateSwappableSocketIDs();
        }

        private void Update_BottomCornerSocketIds()
        {
            int[] _bottomCornerSocketIds = new int[12];
            _bottomCornerSocketIds[0] = bottomFrontA;
            _bottomCornerSocketIds[1] = bottomFrontB;
            _bottomCornerSocketIds[2] = bottomFrontRightA;
            _bottomCornerSocketIds[3] = bottomFrontRightB;
            _bottomCornerSocketIds[4] = bottomBackRightA;
            _bottomCornerSocketIds[5] = bottomBackRightB;
            _bottomCornerSocketIds[6] = bottomBackA;
            _bottomCornerSocketIds[7] = bottomBackB;
            _bottomCornerSocketIds[8] = bottomBackLeftA;
            _bottomCornerSocketIds[9] = bottomBackLeftB;
            _bottomCornerSocketIds[10] = bottomFrontLeftA;
            _bottomCornerSocketIds[11] = bottomFrontLeftB;

            tile.SetCornerSocketSetIds(CornerSocketSetType.Bottom, _bottomCornerSocketIds);
        }


        private void Update_TopCornerSocketIds()
        {
            int[] _topCornerSocketIds = new int[12];
            _topCornerSocketIds[0] = topFrontA;
            _topCornerSocketIds[1] = topFrontB;
            _topCornerSocketIds[2] = topFrontRightA;
            _topCornerSocketIds[3] = topFrontRightB;
            _topCornerSocketIds[4] = topBackRightA;
            _topCornerSocketIds[5] = topBackRightB;
            _topCornerSocketIds[6] = topBackA;
            _topCornerSocketIds[7] = topBackB;
            _topCornerSocketIds[8] = topBackLeftA;
            _topCornerSocketIds[9] = topBackLeftB;
            _topCornerSocketIds[10] = topFrontLeftA;
            _topCornerSocketIds[11] = topFrontLeftB;

            tile.SetCornerSocketSetIds(CornerSocketSetType.Top, _topCornerSocketIds);
        }
        private void Update_SideBottomCornerSocketIds()
        {
            int[] _sideBtmCornerSocketIds = new int[12];
            _sideBtmCornerSocketIds[0] = sideBtmFrontA;
            _sideBtmCornerSocketIds[1] = sideBtmFrontB;
            _sideBtmCornerSocketIds[2] = sideBtmFrontRightA;
            _sideBtmCornerSocketIds[3] = sideBtmFrontRightB;
            _sideBtmCornerSocketIds[4] = sideBtmBackRightA;
            _sideBtmCornerSocketIds[5] = sideBtmBackRightB;
            _sideBtmCornerSocketIds[6] = sideBtmBackA;
            _sideBtmCornerSocketIds[7] = sideBtmBackB;
            _sideBtmCornerSocketIds[8] = sideBtmBackLeftA;
            _sideBtmCornerSocketIds[9] = sideBtmBackLeftB;
            _sideBtmCornerSocketIds[10] = sideBtmFrontLeftA;
            _sideBtmCornerSocketIds[11] = sideBtmFrontLeftB;

            tile.SetCornerSocketSetIds(CornerSocketSetType.SideBottom, _sideBtmCornerSocketIds);
        }
        private void Update_SideTopCornerSocketIds()
        {
            int[] _sideTopCornerSocketIds = new int[12];
            _sideTopCornerSocketIds[0] = sideTopFrontA;
            _sideTopCornerSocketIds[1] = sideTopFrontB;
            _sideTopCornerSocketIds[2] = sideTopFrontRightA;
            _sideTopCornerSocketIds[3] = sideTopFrontRightB;
            _sideTopCornerSocketIds[4] = sideTopBackRightA;
            _sideTopCornerSocketIds[5] = sideTopBackRightB;
            _sideTopCornerSocketIds[6] = sideTopBackA;
            _sideTopCornerSocketIds[7] = sideTopBackB;
            _sideTopCornerSocketIds[8] = sideTopBackLeftA;
            _sideTopCornerSocketIds[9] = sideTopBackLeftB;
            _sideTopCornerSocketIds[10] = sideTopFrontLeftA;
            _sideTopCornerSocketIds[11] = sideTopFrontLeftB;

            tile.SetCornerSocketSetIds(CornerSocketSetType.SideTop, _sideTopCornerSocketIds);
        }


        private void ResetAllSockets(SocketResetState state)
        {
            bool resetAll = state == SocketResetState.All;
            if (resetAll || state == SocketResetState.Bottom)
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

                Update_BottomCornerSocketIds();
            }
            if (resetAll || state == SocketResetState.SideBottom)
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

                Update_SideBottomCornerSocketIds();
            }
            if (resetAll || state == SocketResetState.SideTop)
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

                Update_SideTopCornerSocketIds();
            }
            if (resetAll || state == SocketResetState.Top)
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

                Update_TopCornerSocketIds();
            }
        }


        private void CopyTileSockets_Sides(HexagonTileCore tileToCopyFrom)
        {
            sideBtmFrontA = tileToCopyFrom.sideBtmCornerSocketIds[0];
            sideBtmFrontB = tileToCopyFrom.sideBtmCornerSocketIds[1];
            sideBtmFrontRightA = tileToCopyFrom.sideBtmCornerSocketIds[2];
            sideBtmFrontRightB = tileToCopyFrom.sideBtmCornerSocketIds[3];
            sideBtmBackRightA = tileToCopyFrom.sideBtmCornerSocketIds[4];
            sideBtmBackRightB = tileToCopyFrom.sideBtmCornerSocketIds[5];
            sideBtmBackA = tileToCopyFrom.sideBtmCornerSocketIds[6];
            sideBtmBackB = tileToCopyFrom.sideBtmCornerSocketIds[7];
            sideBtmBackLeftA = tileToCopyFrom.sideBtmCornerSocketIds[8];
            sideBtmBackLeftB = tileToCopyFrom.sideBtmCornerSocketIds[9];
            sideBtmFrontLeftA = tileToCopyFrom.sideBtmCornerSocketIds[10];
            sideBtmFrontLeftB = tileToCopyFrom.sideBtmCornerSocketIds[11];
            Update_SideBottomCornerSocketIds();

            sideTopFrontA = tileToCopyFrom.sideTopCornerSocketIds[0];
            sideTopFrontB = tileToCopyFrom.sideTopCornerSocketIds[1];
            sideTopFrontRightA = tileToCopyFrom.sideTopCornerSocketIds[2];
            sideTopFrontRightB = tileToCopyFrom.sideTopCornerSocketIds[3];
            sideTopBackRightA = tileToCopyFrom.sideTopCornerSocketIds[4];
            sideTopBackRightB = tileToCopyFrom.sideTopCornerSocketIds[5];
            sideTopBackA = tileToCopyFrom.sideTopCornerSocketIds[6];
            sideTopBackB = tileToCopyFrom.sideTopCornerSocketIds[7];
            sideTopBackLeftA = tileToCopyFrom.sideTopCornerSocketIds[8];
            sideTopBackLeftB = tileToCopyFrom.sideTopCornerSocketIds[9];
            sideTopFrontLeftA = tileToCopyFrom.sideTopCornerSocketIds[10];
            sideTopFrontLeftB = tileToCopyFrom.sideTopCornerSocketIds[11];
            Update_SideTopCornerSocketIds();
        }

        private void CopyTileSockets_Layers(HexagonTileCore tileToCopyFrom)
        {
            bottomFrontA = tileToCopyFrom.bottomCornerSocketIds[0];
            bottomFrontB = tileToCopyFrom.bottomCornerSocketIds[1];
            bottomFrontRightA = tileToCopyFrom.bottomCornerSocketIds[2];
            bottomFrontRightB = tileToCopyFrom.bottomCornerSocketIds[3];
            bottomBackRightA = tileToCopyFrom.bottomCornerSocketIds[4];
            bottomBackRightB = tileToCopyFrom.bottomCornerSocketIds[5];
            bottomBackA = tileToCopyFrom.bottomCornerSocketIds[6];
            bottomBackB = tileToCopyFrom.bottomCornerSocketIds[7];
            bottomBackLeftA = tileToCopyFrom.bottomCornerSocketIds[8];
            bottomBackLeftB = tileToCopyFrom.bottomCornerSocketIds[9];
            bottomFrontLeftA = tileToCopyFrom.bottomCornerSocketIds[10];
            bottomFrontLeftB = tileToCopyFrom.bottomCornerSocketIds[11];
            Update_BottomCornerSocketIds();

            topFrontA = tileToCopyFrom.topCornerSocketIds[0];
            topFrontB = tileToCopyFrom.topCornerSocketIds[1];
            topFrontRightA = tileToCopyFrom.topCornerSocketIds[2];
            topFrontRightB = tileToCopyFrom.topCornerSocketIds[3];
            topBackRightA = tileToCopyFrom.topCornerSocketIds[4];
            topBackRightB = tileToCopyFrom.topCornerSocketIds[5];
            topBackA = tileToCopyFrom.topCornerSocketIds[6];
            topBackB = tileToCopyFrom.topCornerSocketIds[7];
            topBackLeftA = tileToCopyFrom.topCornerSocketIds[8];
            topBackLeftB = tileToCopyFrom.topCornerSocketIds[9];
            topFrontLeftA = tileToCopyFrom.topCornerSocketIds[10];
            topFrontLeftB = tileToCopyFrom.topCornerSocketIds[11];
            Update_TopCornerSocketIds();
        }

        public void OnValidate()
        {
            if (tile == null) tile = GetComponent<HexagonTileCore>();
            if (tileSocketDataManager == null) TryAutoFillDataManager();

            tileSocketDirectory = tile.GetSocketDirectory();

            if (_hasSaved)
            {
                Load();
            }

            if (resetSockets != SocketResetState.Unset)
            {
                ResetAllSockets(resetSockets);
                resetSockets = SocketResetState.Unset;

                reevaluate = true;
            }

            if (tileSocketParent == null)
            {
                if (inheritOnSides || inheritOnLayers)
                {
                    inheritOnSides = false;
                    inheritOnLayers = false;
                    Debug.LogError("tileSocketParent is missing");
                }
            }
            else
            {
                if (tileSocketParent == tile || tileSocketParent.GetUid() == tile.GetUid()) tileSocketParent = null;

                if (inheritOnSides) CopyTileSockets_Sides(tileSocketParent);
                if (inheritOnLayers) CopyTileSockets_Layers(tileSocketParent);
            }

            if (reevaluate)
            {
                reevaluate = false;
                EvaluateAllSocketIDs();
            }

            UpdateAllSocketIDs();
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


        public void Save()
        {
            if (tile.HasUid())
            {
                UpdateAllSocketIDs();
                Debug.Log("Save Tile Socket data for uid: " + tile.GetUid());
                bool saved = SaveData(tile.BundleSocketIdData(), savedfilePath, savefileName);
                if (!_hasSaved) _hasSaved = saved;
            }
            else
            {
                Debug.LogError("Tile has no uid set!");
            }
        }

        public void Load()
        {
            Dictionary<string, List<int[]>> data = LoadData(savedfilePath, savefileName);

            tile.LoadSocketIdData(data);

            EvaluateAllSocketIDs();
        }

        [Header("Save / Load Settings")]
        [SerializeField] private bool _hasSaved;
        [SerializeField] private string savedfilePath = "Assets/WFC/";
        [SerializeField] private string savefileName = "tile_socket_data";


        public static bool SaveData(Dictionary<string, List<int[]>> data, string directoryPath, string fileName)
        {
            string filePath = Path.Combine(directoryPath, fileName + ".json");

            // Load existing data from file
            Dictionary<string, List<int[]>> existingData = new Dictionary<string, List<int[]>>();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                existingData = JsonConvert.DeserializeObject<Dictionary<string, List<int[]>>>(json);
            }

            // Merge new data with existing data
            foreach (var kvp in data)
            {
                if (existingData.ContainsKey(kvp.Key))
                {
                    existingData.Remove(kvp.Key);
                    existingData.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    existingData.Add(kvp.Key, kvp.Value);
                }
            }

            // Save merged data to file
            string mergedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(filePath, mergedJson);
                Debug.Log("SaveData!: \n" + filePath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while saving data: " + ex.Message);
                return false;
            }
        }
        public static Dictionary<string, List<int[]>> LoadData(string directoryPath, string fileName)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, fileName + ".json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Dictionary<string, List<int[]>> data = JsonConvert.DeserializeObject<Dictionary<string, List<int[]>>>(json);

                    // Debug.Log("Loaded Socket Data!");
                    return data;
                }
                else
                {
                    Debug.LogError("Error while loading data: file not found");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while loading data: " + ex.Message);
                return null;
            }
        }
        // public static bool SaveData(Dictionary<string, List<int[]>> data, string directoryPath, string fileName)
        // {
        //     string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        //     bool suceess = false;
        //     try
        //     {
        //         if (!Directory.Exists(directoryPath))
        //         {
        //             Directory.CreateDirectory(directoryPath);
        //         }

        //         string filePath = Path.Combine(directoryPath, fileName + ".json");
        //         File.WriteAllText(filePath, json);

        //         Debug.Log("SaveData!: \n" + filePath);

        //         suceess = true;
        //     }
        //     catch (System.Exception ex)
        //     {
        //         Debug.LogError("Error while saving data: " + ex.Message);
        //     }
        //     return suceess;
        // }



        private void TryAutoFillDataManager()
        {
            string[] items = AssetDatabase.FindAssets("t:HexagonSocketDataManager", new string[1] { savedfilePath });
            foreach (string item in items)
            {
                string path = AssetDatabase.GUIDToAssetPath(item);
                HexagonSocketDataManager dataManager = AssetDatabase.LoadAssetAtPath<HexagonSocketDataManager>(path);
                if (dataManager != null)
                {
                    tileSocketDataManager = dataManager;
                    Debug.Log("Suceeded in auto-filling the missing tile socket data manager: " + gameObject.name);
                    break;
                }
            }
        }

    }
}