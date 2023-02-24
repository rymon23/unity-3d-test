using UnityEngine;

namespace WFCSystem
{
    [System.Serializable]
    public class HexagonTileSocketSystemMicro : MonoBehaviour, IHexagonTileSocketSystem
    {
        [Header("Tile")]
        [SerializeField] private IHexagonTile tile;

        #region Tile Sockets
        [Header("Tile Sockets")]
        [SerializeField] private MicroTileSocket resetSidesTo;
        [SerializeField] private MicroTileSocket_Vertical resetLayersTo;
        [SerializeField] private SocketResetState resetSockets = SocketResetState.Unset;
        [SerializeField] private SocketMirrorState useSocketMirroring = SocketMirrorState.Unset;

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
        [SerializeField] private MicroTileSocket_Vertical bottomFrontA;
        [SerializeField] private MicroTileSocket_Vertical bottomFrontB;
        [SerializeField] private MicroTileSocket_Vertical bottomFrontRightA;
        [SerializeField] private MicroTileSocket_Vertical bottomFrontRightB;
        [SerializeField] private MicroTileSocket_Vertical bottomBackRightA;
        [SerializeField] private MicroTileSocket_Vertical bottomBackRightB;
        [SerializeField] private MicroTileSocket_Vertical bottomBackA;
        [SerializeField] private MicroTileSocket_Vertical bottomBackB;
        [SerializeField] private MicroTileSocket_Vertical bottomBackLeftA;
        [SerializeField] private MicroTileSocket_Vertical bottomBackLeftB;
        [SerializeField] private MicroTileSocket_Vertical bottomFrontLeftA;
        [SerializeField] private MicroTileSocket_Vertical bottomFrontLeftB;

        [Header("Top Edge Sockets")]
        [SerializeField] private MicroTileSocket_Vertical topFrontA;
        [SerializeField] private MicroTileSocket_Vertical topFrontB;
        [SerializeField] private MicroTileSocket_Vertical topFrontRightA;
        [SerializeField] private MicroTileSocket_Vertical topFrontRightB;
        [SerializeField] private MicroTileSocket_Vertical topBackRightA;
        [SerializeField] private MicroTileSocket_Vertical topBackRightB;
        [SerializeField] private MicroTileSocket_Vertical topBackA;
        [SerializeField] private MicroTileSocket_Vertical topBackB;
        [SerializeField] private MicroTileSocket_Vertical topBackLeftA;
        [SerializeField] private MicroTileSocket_Vertical topBackLeftB;
        [SerializeField] private MicroTileSocket_Vertical topFrontLeftA;
        [SerializeField] private MicroTileSocket_Vertical topFrontLeftB;
        #endregion

        private void UpdateAllSocketIDs()
        {
            if (useSocketMirroring != SocketMirrorState.Unset)
            {
                // Sides
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

                // Layered
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
                bottomFrontA = resetLayersTo;
                bottomFrontB = resetLayersTo;
                bottomFrontRightA = resetLayersTo;
                bottomFrontRightB = resetLayersTo;
                bottomBackRightA = resetLayersTo;
                bottomBackRightB = resetLayersTo;
                bottomBackA = resetLayersTo;
                bottomBackB = resetLayersTo;
                bottomBackLeftA = resetLayersTo;
                bottomBackLeftB = resetLayersTo;
                bottomFrontLeftA = resetLayersTo;
                bottomFrontLeftB = resetLayersTo;
            }
            if (resetAll || resetState == SocketResetState.SideBottom)
            {
                sideBtmFrontA = resetSidesTo;
                sideBtmFrontB = resetSidesTo;
                sideBtmFrontRightA = resetSidesTo;
                sideBtmFrontRightB = resetSidesTo;
                sideBtmBackRightA = resetSidesTo;
                sideBtmBackRightB = resetSidesTo;
                sideBtmBackA = resetSidesTo;
                sideBtmBackB = resetSidesTo;
                sideBtmBackLeftA = resetSidesTo;
                sideBtmBackLeftB = resetSidesTo;
                sideBtmFrontLeftA = resetSidesTo;
                sideBtmFrontLeftB = resetSidesTo;
            }
            if (resetAll || resetState == SocketResetState.SideTop)
            {
                sideTopFrontA = resetSidesTo;
                sideTopFrontB = resetSidesTo;
                sideTopFrontRightA = resetSidesTo;
                sideTopFrontRightB = resetSidesTo;
                sideTopBackRightA = resetSidesTo;
                sideTopBackRightB = resetSidesTo;
                sideTopBackA = resetSidesTo;
                sideTopBackB = resetSidesTo;
                sideTopBackLeftA = resetSidesTo;
                sideTopBackLeftB = resetSidesTo;
                sideTopFrontLeftA = resetSidesTo;
                sideTopFrontLeftB = resetSidesTo;
            }
            if (resetAll || resetState == SocketResetState.Top)
            {
                topFrontA = resetLayersTo;
                topFrontB = resetLayersTo;
                topFrontRightA = resetLayersTo;
                topFrontRightB = resetLayersTo;
                topBackRightA = resetLayersTo;
                topBackRightB = resetLayersTo;
                topBackA = resetLayersTo;
                topBackB = resetLayersTo;
                topBackLeftA = resetLayersTo;
                topBackLeftB = resetLayersTo;
                topFrontLeftA = resetLayersTo;
                topFrontLeftB = resetLayersTo;
            }
        }

        void OnValidate()
        {
            if (tile == null) tile = GetComponent<IHexagonTile>();
            if (tile == null) Debug.LogError("IHexagonTile component missing");

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
            // MirrorSidesToLayers = 5,
            // MirrorLayersToSides = 6,
        }
    }
}