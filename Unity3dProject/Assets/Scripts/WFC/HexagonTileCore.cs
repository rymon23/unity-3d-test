using System;
using UnityEngine;
using ProceduralBase;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace WFCSystem
{
    public enum TileSeries
    {
        Unset = 0,
        TestBuilding_A,
        TestBuilding_B,
        TestBuilding_C,
    }

    public enum TileVariant
    {
        Unset = 0,
        WalledBuilding = 1,
        InnerCityBuilding_A = 2,
    }

    public enum TileLayeredGroup
    {
        Unset = 0,
        Group_A,
        Group_B,
        Group_C,
        Group_D,
    }

    [System.Serializable]
    public class HexagonTileCore : MonoBehaviour, IHexagonTile
    {
        [SerializeField] private string _uid;
        public string GetUid() => _uid;
        public bool HasUid() => (_uid != null && _uid != "");

        private void OnEnable()
        {
            if (HasUid() == false) _uid = UtilityHelpers.GenerateUniqueID(this.gameObject);
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

        #region Model Manipulation
        [SerializeField] private GameObject model;
        [SerializeField] private GameObject modelRoof;
        [SerializeField] private Vector3 modelPosition;
        public void SetModel(GameObject _model)
        {
            model = _model;
        }

        [Header("Inversion Settings")]
        [SerializeField] private bool isInvertable;
        [SerializeField] private float invertedPosition = 0.01f;
        public bool IsInvertable() => isInvertable;
        [SerializeField] private bool isModelInverted;
        public void InvertModel()
        {
            if (invertedPosition != 0.01f)
            {
                WFCUtilities.InvertTile(model, invertedPosition);
            }
            else
            {
                WFCUtilities.InvertTile(model);
            }
            isModelInverted = true;
        }

        [SerializeField] private bool isRoofable;
        public bool IsRoofable() => isRoofable;
        [SerializeField] private bool isModelRoofed;
        public void SetModelRoofActive(bool enable)
        {
            modelRoof.SetActive(enable);
            isModelRoofed = enable;
        }
        #endregion

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
        [SerializeField] private TileLayeredGroup _layeredGroup;
        public TileLayeredGroup GetLayeredGrouping() => _layeredGroup;
        [SerializeField] private ExcludeLayerState _excludeLayerState;
        public ExcludeLayerState GetExcludeLayerState() => _excludeLayerState;
        public enum ExcludeLayerState { Unset = 0, BaseLayerOnly, TopLayerOnly, NoBaseLayer, NoTopLayer }
        public bool baseLayerOnly;
        public bool noBaseLayer;
        public bool noGroundLayer;
        public bool isLayerConnector;


        [Header("Tile Compatibility / Probability")]
        [SerializeField] private CellStatus[] _excludeCellStatusList;
        [SerializeField] private GridExclusionRule _gridExclusionRule;
        public GridExclusionRule GetGridExclusionRule() => _gridExclusionRule;
        public bool IsGridEdgeCompatible() => isEdgeable && (_gridExclusionRule == GridExclusionRule.GridEdgesOnly || _gridExclusionRule == GridExclusionRule.EdgeOnly || _gridExclusionRule == GridExclusionRule.Unset);
        public bool isEdgeable = true; // can be placed on the edge / border or the grid
        public bool allowPathPlacement;
        public bool isPath;
        public enum PathType { Unset = 0, Road, Stairway, Elevator }
        public bool isEntrance;
        public bool isFragment; // Is incomplete by itself, needs neighbor tiles like itself
        public bool isInClusterSet; // Is part of a set of tiles that make a cluster
        public bool isClusterCenterTile; // Is the center part of a set of tiles that make a cluster
        public bool isLeveledTile;
        public bool isLeveledRamp;

        [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

        [Header("Tile Socket Configuration")]
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        public HexagonSocketDirectory GetSocketDirectory() => socketDirectory;
        [SerializeField] private TileLabelGroup tileLabelGroup;
        [SerializeField] private bool useSwappableSockets;
        public int[] _swappableSideSocketPair = new int[2];

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
        public int[] layerCenterSocketIds = new int[2];
        // public int[] sideBtmCornerSocketIds { get; private set; } = new int[12];
        // public int[] sideTopCornerSocketIds { get; private set; } = new int[12];
        // public int[] bottomCornerSocketIds { get; private set; } = new int[12];
        // public int[] topCornerSocketIds { get; private set; } = new int[12];

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
                Debug.LogError("Socket id data not found for uid: " + _uid + ", Tile: " + gameObject.name);
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
            (HexagonCorner cornerA, HexagonCorner cornerB) = HexCoreUtil.GetCornersFromSide(side);

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
        Vector3 _position;
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

        //SWAPPED
        public int[][] swapped_rotatedSideBtmCornerSockets { get; private set; }
        public int[][] swapped_rotatedSideTopCornerSockets { get; private set; }
        public int[][] swapped_rotatedBtmCornerSockets { get; private set; }
        public int[][] swapped_rotatedTopCornerSockets { get; private set; }

        public int[][] swapped_rotatedSideBtmCornerSockets_inverted { get; private set; }
        public int[][] swapped_rotatedSideTopCornerSockets_inverted { get; private set; }
        public int[][] swapped_rotatedBtmCornerSockets_inverted { get; private set; }
        public int[][] swapped_rotatedTopCornerSockets_inverted { get; private set; }

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

        public (int[], int[]) GetRotatedCornerSocketsBySide(HexagonSide side, int rotation, bool inverted)
        {
            (HexagonCorner cornerA, HexagonCorner cornerB) = HexCoreUtil.GetCornersFromSide(side);

            int[] top = new int[2];
            int[] bottom = new int[2];

            bottom[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true, inverted);
            bottom[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true, inverted);

            top[0] = GetRotatedSideCornerSocketId(cornerA, rotation, true, inverted);
            top[1] = GetRotatedSideCornerSocketId(cornerB, rotation, true, inverted);

            return (bottom, top);
        }

        private void EvaluateInvertedRotatedCornerSockets()
        {
            int[] invertedCornerSockets_sideBottom = GetInvertedSocketIds(sideBtmCornerSocketIds);
            int[] invertedCornerSockets_sideTop = GetInvertedSocketIds(sideTopCornerSocketIds);

            int[] invertedCornerSockets_bottom = GetInvertedSocketIds(bottomCornerSocketIds);
            int[] invertedCornerSockets_top = GetInvertedSocketIds(topCornerSocketIds);

            (
            int[][] newRotatedInvertedCornerSockets_sideBottom,
             int[][] newRotatedInvertedCornerSockets_sideTop,

             int[][] newRotatedInvertedCornerSockets_bottom,
             int[][] newRotatedInvertedCornerSockets_top

            ) = GetRotatedCornerSockets(
                invertedCornerSockets_sideBottom,
                invertedCornerSockets_sideTop,

                invertedCornerSockets_bottom,
                invertedCornerSockets_top
            );

            invertedRotatedSideTopCornerSockets = newRotatedInvertedCornerSockets_sideTop;
            invertedRotatedSideBtmCornerSockets = newRotatedInvertedCornerSockets_sideBottom;
            invertedRotatedTopCornerSockets = newRotatedInvertedCornerSockets_top;
            invertedRotatedBottomCornerSockets = newRotatedInvertedCornerSockets_bottom;
        }

        private void EvaluateRotatedCornerSockets()
        {
            (
             int[][] newRotatedSideBtmCornerSocketIds,
             int[][] newRotatedSideTopCornerSocketIds,

             int[][] newRotatedBottomCornerSocketIds,
             int[][] newRotatedTopCornerSocketIds

            ) = GetRotatedCornerSockets(
                sideBtmCornerSocketIds,
                sideTopCornerSocketIds,

                bottomCornerSocketIds,
                topCornerSocketIds
            );

            rotatedSideTopCornerSockets = newRotatedSideTopCornerSocketIds;
            rotatedSideBtmCornerSockets = newRotatedSideBtmCornerSocketIds;
            rotatedTopCornerSockets = newRotatedTopCornerSocketIds;
            rotatedBottomCornerSockets = newRotatedBottomCornerSocketIds;
        }


        private void EvaluateRotatedCornerSockets_Swapped()
        {
            int[] swappedSockets_sideBottom = GetSwappedSocketIds(sideBtmCornerSocketIds, _swappableSideSocketPair);
            int[] swappedSockets_sideTop = GetSwappedSocketIds(sideTopCornerSocketIds, _swappableSideSocketPair);
            (
            int[][] newRotatedSwappedSockets_sideBottom,
             int[][] newRotatedSwappedSockets_sideTop,

             int[][] newRotatedSwappedSockets_bottom,
             int[][] newRotatedSwappedSockets_top

            ) = GetRotatedCornerSockets(
                swappedSockets_sideBottom,
                swappedSockets_sideTop,

                bottomCornerSocketIds,
                topCornerSocketIds
            );

            swapped_rotatedSideTopCornerSockets = newRotatedSwappedSockets_sideTop;
            swapped_rotatedSideBtmCornerSockets = newRotatedSwappedSockets_sideBottom;
            swapped_rotatedTopCornerSockets = newRotatedSwappedSockets_top;
            swapped_rotatedBtmCornerSockets = newRotatedSwappedSockets_bottom;
        }

        private void EvaluateInvertedRotatedCornerSockets_Swapped()
        {
            int[] invertedSockets_sideBottom_swapped = GetInvertedSocketIds(GetSwappedSocketIds(sideBtmCornerSocketIds, _swappableSideSocketPair));
            int[] invertedSockets_sideTop_swapped = GetInvertedSocketIds(GetSwappedSocketIds(sideTopCornerSocketIds, _swappableSideSocketPair));

            int[] invertedSockets_bottom_swapped = GetInvertedSocketIds(bottomCornerSocketIds);
            int[] invertedSockets_top_swapped = GetInvertedSocketIds(topCornerSocketIds);

            (
            int[][] newRotatedInvertedCornerSockets_sideBottom,
             int[][] newRotatedInvertedCornerSockets_sideTop,

             int[][] newRotatedInvertedCornerSockets_bottom,
             int[][] newRotatedInvertedCornerSockets_top

            ) = GetRotatedCornerSockets(
                invertedSockets_sideBottom_swapped,
                invertedSockets_sideTop_swapped,

                invertedSockets_bottom_swapped,
                invertedSockets_top_swapped
            );

            swapped_rotatedSideTopCornerSockets = newRotatedInvertedCornerSockets_sideTop;
            swapped_rotatedSideBtmCornerSockets = newRotatedInvertedCornerSockets_sideBottom;
            swapped_rotatedTopCornerSockets = newRotatedInvertedCornerSockets_top;
            swapped_rotatedBtmCornerSockets = newRotatedInvertedCornerSockets_bottom;
        }

        private void RecalculateEdgePoints()
        {
            _corners = HexCoreUtil.GenerateHexagonPoints(transform.position, size);
            _sides = HexagonGenerator.GenerateHexagonSidePoints(_corners);
            EvaluateRotatedCornerSockets();
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
            if (model == null)
            {
                if (isInvertable)
                {
                    Debug.LogError("Invertable tile: " + gameObject.name + " has no model set!");
                }
                else if (isRoofable)
                {

                    Debug.LogError("Roofable tile: " + gameObject.name + " has no model set!");
                }
            }
            else
            {
                if (isModelInverted == false && model.transform.localScale.z > -1)
                {
                    modelPosition = model.transform.position;
                }
            }

            if (isRoofable && modelRoof == null)
            {
                Debug.LogError("Roofable tile: " + gameObject.name + " has no roof model set!");
            }


            if (changeRotation != _changeRotation)
            {
                _changeRotation = changeRotation;
                RotateTile(gameObject, changeRotation);
            }

            if (baseLayerOnly) _excludeLayerState = ExcludeLayerState.BaseLayerOnly;
            if (noBaseLayer) _excludeLayerState = ExcludeLayerState.NoBaseLayer;
            if (noBaseLayer) baseLayerOnly = false;

            if (resetPoints || _position != transform.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
            {
                resetPoints = false;
                _position = transform.position;
                center = transform;

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

            if (HasUid() == false) _uid = UtilityHelpers.GenerateUniqueID(this.gameObject);

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


        #region Editor Tools
        [SerializeField] private bool enableEditMode;
        [SerializeField] private SocketDisplayState showSocketLabelState;
        private SocketDisplayState _showSocketLabelState;
        [SerializeField] private bool showCorners;
        [SerializeField] private bool showSides;
        [SerializeField] private bool showEdges;
        [SerializeField] private bool resetPoints;
        [SerializeField] private bool ignoreSocketLabelUpdates;

        public void SetEditorTools(bool enable)
        {
            if (enable == false) ClearEditorVisuals();

            SetIgnoreSocketLabelUpdates(enable);
            ShowSocketLabels(enable);

            enableEditMode = enable;
        }

        public void ClearEditorVisuals()
        {
            showCorners = false;
            showSides = false;
            showEdges = false;
        }

        public void SetIgnoreSocketLabelUpdates(bool enable)
        {
            ignoreSocketLabelUpdates = enable;
        }

        public void ShowSocketLabels(bool enable)
        {
            showSocketLabelState = enable ? SocketDisplayState.ShowAll : SocketDisplayState.ShowNone;
            EvaluateSocketLabels(true);
        }
        #endregion

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

            if (!enableEditMode) return;

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);


            if (_position != transform.position)
            {
                RecalculateEdgePoints();
                _position = transform.position;
                center = transform;
            }


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
                VectorUtil.DrawHexagonPointLinesInGizmos(_corners);
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
            (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexCoreUtil.GetCornersFromSide(_side);

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

            (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexCoreUtil.GetCornersFromSide(_side);
            int cornerA = (int)_cornerA;
            int cornerB = (int)_cornerB;

            if (inverted)
            {
                (HexagonCorner _displayCornerA, HexagonCorner _displayCornerB) = HexCoreUtil.GetCornersFromSide(_displaySide);
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

        public static int[] GetSwappedSocketIds(int[] cornerSocketIds, int[] swappablePair)
        {
            int[] swappedIds = new int[12];
            for (var i = 0; i < cornerSocketIds.Length; i++)
            {
                if (cornerSocketIds[i] == swappablePair[0])
                {
                    swappedIds[i] = swappablePair[1];
                }
                else
                {
                    swappedIds[i] = cornerSocketIds[i];
                }
            }
            return swappedIds;
        }

        public static int[] GetInvertedSocketIds(int[] cornerSocketIds)
        {
            int[] invertedIds = new int[12];
            invertedIds[(int)HexagonCorner.FrontA] = cornerSocketIds[(int)HexagonCorner.BackA];
            invertedIds[(int)HexagonCorner.FrontB] = cornerSocketIds[(int)HexagonCorner.BackB];

            invertedIds[(int)HexagonCorner.BackRightA] = cornerSocketIds[(int)HexagonCorner.FrontRightA];
            invertedIds[(int)HexagonCorner.BackRightB] = cornerSocketIds[(int)HexagonCorner.FrontRightB];

            invertedIds[(int)HexagonCorner.FrontLeftA] = cornerSocketIds[(int)HexagonCorner.BackLeftA];
            invertedIds[(int)HexagonCorner.FrontLeftB] = cornerSocketIds[(int)HexagonCorner.BackLeftB];

            invertedIds[(int)HexagonCorner.BackA] = cornerSocketIds[(int)HexagonCorner.FrontA];
            invertedIds[(int)HexagonCorner.BackB] = cornerSocketIds[(int)HexagonCorner.FrontB];

            invertedIds[(int)HexagonCorner.BackLeftA] = cornerSocketIds[(int)HexagonCorner.FrontLeftA];
            invertedIds[(int)HexagonCorner.BackLeftB] = cornerSocketIds[(int)HexagonCorner.FrontLeftB];

            invertedIds[(int)HexagonCorner.FrontRightA] = cornerSocketIds[(int)HexagonCorner.BackRightA];
            invertedIds[(int)HexagonCorner.FrontRightB] = cornerSocketIds[(int)HexagonCorner.BackRightB];
            return invertedIds;
        }

        private static (int[][], int[][], int[][], int[][]) GetRotatedCornerSockets(int[] sideBtmCornerSockets, int[] sideTopCornerSockets, int[] bottomCornerSockets, int[] topCornerSockets)
        {
            int rotations = 6;
            int corners = 12;
            int[][] newRotatedSideBtmCornerSocketIds = new int[rotations][];
            int[][] newRotatedSideTopCornerSocketIds = new int[rotations][];
            int[][] newRotatedBottomCornerSocketIds = new int[rotations][];
            int[][] newRotatedTopCornerSocketIds = new int[rotations][];

            for (int rot = 0; rot < rotations; rot++)
            {
                newRotatedSideBtmCornerSocketIds[rot] = new int[corners];
                newRotatedSideTopCornerSocketIds[rot] = new int[corners];
                newRotatedBottomCornerSocketIds[rot] = new int[corners];
                newRotatedTopCornerSocketIds[rot] = new int[corners];
            }
            // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
            for (int corner = 0; corner < corners; corner++)
            {
                newRotatedSideBtmCornerSocketIds[0][corner] = sideBtmCornerSockets[corner];
                newRotatedSideTopCornerSocketIds[0][corner] = sideTopCornerSockets[corner];
                newRotatedBottomCornerSocketIds[0][corner] = bottomCornerSockets[corner];
                newRotatedTopCornerSocketIds[0][corner] = topCornerSockets[corner];
            }
            // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
            for (int rot = 1; rot < rotations; rot++)
            {
                int offset = (rot == 0) ? 0 : rot - 1;
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
            }

            return (
             newRotatedSideBtmCornerSocketIds,
             newRotatedSideTopCornerSocketIds,

             newRotatedBottomCornerSocketIds,
             newRotatedTopCornerSocketIds
            );
        }

        public enum SocketDisplayState { ShowAll = 0, ShowNone = 1, ShowSides = 2, ShowLayered = 3 }

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