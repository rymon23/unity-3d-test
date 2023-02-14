
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WFCSystem
{
    public interface IHexagonTileSocketSystem
    {
        // bool[,] GetCompatibilityMatrix();
    }


    [System.Serializable]
    public class HexagonTileSocketSystemDefault : MonoBehaviour, IHexagonTileSocketSystem
    {
        [Header("Tile")]
        [SerializeField] private IHexagonTile tile;

        #region Tile Sockets
        [Header("Tile Sockets")]
        [SerializeField] private TileSocketPrimitive resetToSocket;
        [SerializeField] private SocketResetState resetSockets = SocketResetState.Unset;
        [SerializeField] private SocketMirrorState useSocketMirroring = SocketMirrorState.Unset;


        [Header("Side Bottom Sockets")]
        [SerializeField] private TileSocketPrimitive sideBtmFrontA;
        [SerializeField] private TileSocketPrimitive sideBtmFrontB;
        [SerializeField] private TileSocketPrimitive sideBtmFrontRightA;
        [SerializeField] private TileSocketPrimitive sideBtmFrontRightB;
        [SerializeField] private TileSocketPrimitive sideBtmBackRightA;
        [SerializeField] private TileSocketPrimitive sideBtmBackRightB;
        [SerializeField] private TileSocketPrimitive sideBtmBackA;
        [SerializeField] private TileSocketPrimitive sideBtmBackB;
        [SerializeField] private TileSocketPrimitive sideBtmBackLeftA;
        [SerializeField] private TileSocketPrimitive sideBtmBackLeftB;
        [SerializeField] private TileSocketPrimitive sideBtmFrontLeftA;
        [SerializeField] private TileSocketPrimitive sideBtmFrontLeftB;

        [Header("Side Top Sockets")]
        [SerializeField] private TileSocketPrimitive sideTopFrontA;
        [SerializeField] private TileSocketPrimitive sideTopFrontB;
        [SerializeField] private TileSocketPrimitive sideTopFrontRightA;
        [SerializeField] private TileSocketPrimitive sideTopFrontRightB;
        [SerializeField] private TileSocketPrimitive sideTopBackRightA;
        [SerializeField] private TileSocketPrimitive sideTopBackRightB;
        [SerializeField] private TileSocketPrimitive sideTopBackA;
        [SerializeField] private TileSocketPrimitive sideTopBackB;
        [SerializeField] private TileSocketPrimitive sideTopBackLeftA;
        [SerializeField] private TileSocketPrimitive sideTopBackLeftB;
        [SerializeField] private TileSocketPrimitive sideTopFrontLeftA;
        [SerializeField] private TileSocketPrimitive sideTopFrontLeftB;

        [Header("Bottom Edge Sockets")]
        [SerializeField] private TileSocketPrimitive bottomFrontA;
        [SerializeField] private TileSocketPrimitive bottomFrontB;
        [SerializeField] private TileSocketPrimitive bottomFrontRightA;
        [SerializeField] private TileSocketPrimitive bottomFrontRightB;
        [SerializeField] private TileSocketPrimitive bottomBackRightA;
        [SerializeField] private TileSocketPrimitive bottomBackRightB;
        [SerializeField] private TileSocketPrimitive bottomBackA;
        [SerializeField] private TileSocketPrimitive bottomBackB;
        [SerializeField] private TileSocketPrimitive bottomBackLeftA;
        [SerializeField] private TileSocketPrimitive bottomBackLeftB;
        [SerializeField] private TileSocketPrimitive bottomFrontLeftA;
        [SerializeField] private TileSocketPrimitive bottomFrontLeftB;

        [Header("Top Edge Sockets")]
        [SerializeField] private TileSocketPrimitive topFrontA;
        [SerializeField] private TileSocketPrimitive topFrontB;
        [SerializeField] private TileSocketPrimitive topFrontRightA;
        [SerializeField] private TileSocketPrimitive topFrontRightB;
        [SerializeField] private TileSocketPrimitive topBackRightA;
        [SerializeField] private TileSocketPrimitive topBackRightB;
        [SerializeField] private TileSocketPrimitive topBackA;
        [SerializeField] private TileSocketPrimitive topBackB;
        [SerializeField] private TileSocketPrimitive topBackLeftA;
        [SerializeField] private TileSocketPrimitive topBackLeftB;
        [SerializeField] private TileSocketPrimitive topFrontLeftA;
        [SerializeField] private TileSocketPrimitive topFrontLeftB;
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
            int[] _bottomCornerSocketIds = new int[12];
            _bottomCornerSocketIds[0] = (int)bottomFrontA;
            _bottomCornerSocketIds[1] = (int)bottomFrontB;
            _bottomCornerSocketIds[2] = (int)bottomFrontRightA;
            _bottomCornerSocketIds[3] = (int)bottomFrontRightB;
            _bottomCornerSocketIds[4] = (int)bottomBackRightA;
            _bottomCornerSocketIds[5] = (int)bottomBackRightB;
            _bottomCornerSocketIds[6] = (int)bottomBackA;
            _bottomCornerSocketIds[7] = (int)bottomBackB;
            _bottomCornerSocketIds[8] = (int)bottomBackLeftA;
            _bottomCornerSocketIds[9] = (int)bottomBackLeftB;
            _bottomCornerSocketIds[10] = (int)bottomFrontLeftA;
            _bottomCornerSocketIds[11] = (int)bottomFrontLeftB;
            tile.SetCornerSocketSetIds(CornerSocketSetType.Bottom, _bottomCornerSocketIds);

            int[] _topCornerSocketIds = new int[12];
            _topCornerSocketIds[0] = (int)topFrontA;
            _topCornerSocketIds[1] = (int)topFrontB;
            _topCornerSocketIds[2] = (int)topFrontRightA;
            _topCornerSocketIds[3] = (int)topFrontRightB;
            _topCornerSocketIds[4] = (int)topBackRightA;
            _topCornerSocketIds[5] = (int)topBackRightB;
            _topCornerSocketIds[6] = (int)topBackA;
            _topCornerSocketIds[7] = (int)topBackB;
            _topCornerSocketIds[8] = (int)topBackLeftA;
            _topCornerSocketIds[9] = (int)topBackLeftB;
            _topCornerSocketIds[10] = (int)topFrontLeftA;
            _topCornerSocketIds[11] = (int)topFrontLeftB;
            tile.SetCornerSocketSetIds(CornerSocketSetType.Top, _topCornerSocketIds);

            // Side Top & BTM
            int[] _sideBtmCornerSocketIds = new int[12];
            _sideBtmCornerSocketIds[0] = (int)sideBtmFrontA;
            _sideBtmCornerSocketIds[1] = (int)sideBtmFrontB;
            _sideBtmCornerSocketIds[2] = (int)sideBtmFrontRightA;
            _sideBtmCornerSocketIds[3] = (int)sideBtmFrontRightB;
            _sideBtmCornerSocketIds[4] = (int)sideBtmBackRightA;
            _sideBtmCornerSocketIds[5] = (int)sideBtmBackRightB;
            _sideBtmCornerSocketIds[6] = (int)sideBtmBackA;
            _sideBtmCornerSocketIds[7] = (int)sideBtmBackB;
            _sideBtmCornerSocketIds[8] = (int)sideBtmBackLeftA;
            _sideBtmCornerSocketIds[9] = (int)sideBtmBackLeftB;
            _sideBtmCornerSocketIds[10] = (int)sideBtmFrontLeftA;
            _sideBtmCornerSocketIds[11] = (int)sideBtmFrontLeftB;
            tile.SetCornerSocketSetIds(CornerSocketSetType.SideBottom, _sideBtmCornerSocketIds);

            int[] _sideTopCornerSocketIds = new int[12];
            _sideTopCornerSocketIds[0] = (int)sideTopFrontA;
            _sideTopCornerSocketIds[1] = (int)sideTopFrontB;
            _sideTopCornerSocketIds[2] = (int)sideTopFrontRightA;
            _sideTopCornerSocketIds[3] = (int)sideTopFrontRightB;
            _sideTopCornerSocketIds[4] = (int)sideTopBackRightA;
            _sideTopCornerSocketIds[5] = (int)sideTopBackRightB;
            _sideTopCornerSocketIds[6] = (int)sideTopBackA;
            _sideTopCornerSocketIds[7] = (int)sideTopBackB;
            _sideTopCornerSocketIds[8] = (int)sideTopBackLeftA;
            _sideTopCornerSocketIds[9] = (int)sideTopBackLeftB;
            _sideTopCornerSocketIds[10] = (int)sideTopFrontLeftA;
            _sideTopCornerSocketIds[11] = (int)sideTopFrontLeftB;
            tile.SetCornerSocketSetIds(CornerSocketSetType.SideTop, _sideTopCornerSocketIds);
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

        void OnValidate()
        {
            if (resetSockets != SocketResetState.Unset)
            {
                ResetAllSockets(resetSockets);

                resetSockets = SocketResetState.Unset;
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
    }
}