using System;
using UnityEngine;
using ProceduralBase;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TileCategory
{
    Unset = 0,
    Building,
    Road,
    Bridge,
    Wall,
    Gate,
    Interior,
    Misc,
}

public enum TileType
{
    Unset = 0,
    ExteriorWallSmall,
    ExteriorWallLarge,
    TowerSmall,
    TowerLarge,
    InteriorWall,
    BuildingSmall,
    BuildingMedium,
    BuildingLarge,
    RoadSmall,
    RoadLarge,
}

public enum HexagonSide
{
    Front = 0,
    FrontRight,
    BackRight,
    Back,
    BackLeft,
    FrontLeft,
}

public enum HexagonCorner
{
    FrontA = 0,
    FrontB,
    FrontRightA,
    FrontRightB,
    BackRightA,
    BackRightB,

    BackA,
    BackB,
    BackLeftA,
    BackLeftB,
    FrontLeftA,
    FrontLeftB,
}

public enum HexagonSideEdge
{
    Bottom = 0,
    Right,
    Top,
    Left,
}




[System.Serializable]
public class HexagonTile : MonoBehaviour
{
    public int id = -1;
    public int size = 12;

    [Header("Tile Settings")]
    [SerializeField] private TileCategory _tileCategory;
    public TileCategory GetTileCategory() => _tileCategory;
    [SerializeField] private TileType _tileType;
    public TileType GetTileType() => _tileType;
    public bool IsExteriorWall() => _tileCategory == TileCategory.Wall && (_tileType == TileType.ExteriorWallLarge || _tileType == TileType.ExteriorWallSmall);
    [Header("Layer Settings")]
    public bool baseLayerOnly;
    public bool isLayerConnector;

    [Header("Tile Compatibility / Probability")]
    public bool isInClusterSet; // Is part of a set of tiles that make a cluster
    public bool isClusterCenterTile; // Is the center part of a set of tiles that make a cluster
    public bool isEdgeable; // can be placed on the edge / border or the grid
    public bool isEntrance;
    public bool isPath;
    public bool isLeveledTile;
    public bool isLeveledRamp;
    public bool isVerticalWFC;
    public bool isFragment; // Is incomplete by itself, needs neighbor tiles like itself
    [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

    [Header("Tile Socket Configuration")]
    [SerializeField] private TileSocketDirectory tileSocketDirectory;
    [SerializeField] private TileLabelGroup tileLabelGroup;
    [Range(-10f, 0f)][SerializeField] private float bottomLabelYOffset = -3f;
    [Range(0f, 10f)][SerializeField] private float topLabelYOffset = 4f;
    [Range(0f, 10f)][SerializeField] private float labelXZOffset = 1f;
    [Range(0f, 10f)][SerializeField] private float labelForwardOffset = 2f;

    #region Tile Sockets
    [Header("Tile Sockets")]
    [SerializeField] private TileSocketPrimitive resetAllToSocket;
    [SerializeField] private bool resetAllSockets;
    [SerializeField] private SocketMirrorState useSocketMirroring = SocketMirrorState.Unset;

    private int[] topCornerSocketIds = new int[12];
    private int[] bottomCornerSocketIds = new int[12];

    [Header("Bottom Sockets")]
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

    [Header("Top Sockets")]
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
            if (useSocketMirroring == SocketMirrorState.MirrorBottom)
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
            else
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
    }
    // // TEMP 
    // private void TransferAllSocketIDValues()
    // {
    //     bottomFrontA = (TileSocketPrimitive)bottomCornerSocketIds[0];
    //     bottomFrontB = (TileSocketPrimitive)bottomCornerSocketIds[1];
    //     bottomFrontRightA = (TileSocketPrimitive)bottomCornerSocketIds[2];
    //     bottomFrontRightB = (TileSocketPrimitive)bottomCornerSocketIds[3];
    //     bottomBackRightA = (TileSocketPrimitive)bottomCornerSocketIds[4];
    //     bottomBackRightB = (TileSocketPrimitive)bottomCornerSocketIds[5];
    //     bottomBackA = (TileSocketPrimitive)bottomCornerSocketIds[6];
    //     bottomBackB = (TileSocketPrimitive)bottomCornerSocketIds[7];
    //     bottomBackLeftA = (TileSocketPrimitive)bottomCornerSocketIds[8];
    //     bottomBackLeftB = (TileSocketPrimitive)bottomCornerSocketIds[9];
    //     bottomFrontLeftA = (TileSocketPrimitive)bottomCornerSocketIds[10];
    //     bottomFrontLeftB = (TileSocketPrimitive)bottomCornerSocketIds[11];

    //     topFrontA = (TileSocketPrimitive)topCornerSocketIds[0];
    //     topFrontB = (TileSocketPrimitive)topCornerSocketIds[1];
    //     topFrontRightA = (TileSocketPrimitive)topCornerSocketIds[2];
    //     topFrontRightB = (TileSocketPrimitive)topCornerSocketIds[3];
    //     topBackRightA = (TileSocketPrimitive)topCornerSocketIds[4];
    //     topBackRightB = (TileSocketPrimitive)topCornerSocketIds[5];
    //     topBackA = (TileSocketPrimitive)topCornerSocketIds[6];
    //     topBackB = (TileSocketPrimitive)topCornerSocketIds[7];
    //     topBackLeftA = (TileSocketPrimitive)topCornerSocketIds[8];
    //     topBackLeftB = (TileSocketPrimitive)topCornerSocketIds[9];
    //     topFrontLeftA = (TileSocketPrimitive)topCornerSocketIds[10];
    //     topFrontLeftB = (TileSocketPrimitive)topCornerSocketIds[11];
    // }

    private void ResetAllSockets()
    {
        bottomFrontA = resetAllToSocket;
        bottomFrontB = resetAllToSocket;
        bottomFrontRightA = resetAllToSocket;
        bottomFrontRightB = resetAllToSocket;
        bottomBackRightA = resetAllToSocket;
        bottomBackRightB = resetAllToSocket;
        bottomBackA = resetAllToSocket;
        bottomBackB = resetAllToSocket;
        bottomBackLeftA = resetAllToSocket;
        bottomBackLeftB = resetAllToSocket;
        bottomFrontLeftA = resetAllToSocket;
        bottomFrontLeftB = resetAllToSocket;
        topFrontA = resetAllToSocket;
        topFrontB = resetAllToSocket;
        topFrontRightA = resetAllToSocket;
        topFrontRightB = resetAllToSocket;
        topBackRightA = resetAllToSocket;
        topBackRightB = resetAllToSocket;
        topBackA = resetAllToSocket;
        topBackB = resetAllToSocket;
        topBackLeftA = resetAllToSocket;
        topBackLeftB = resetAllToSocket;
        topFrontLeftA = resetAllToSocket;
        topFrontLeftB = resetAllToSocket;
    }


    public int GetCornerSocketId(HexagonCorner corner, bool top) => top ? topCornerSocketIds[(int)corner] : bottomCornerSocketIds[(int)corner];
    // public int GetInnerClusterSocketCount()
    // {
    //     int found = 0;
    //     foreach (int socket in cornerSocketIds)
    //     {
    //         if (socket == (int)TileSocketConstants.InnerCuster) found++;
    //     }
    //     return found;
    // }

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

    public int GetRotatedCornerSocketId(HexagonCorner corner, int rotation, bool top)
    {
        EvaluateRotatedCornerSockets();

        Debug.Log("GetRotatedCornerSocketId - rotation: " + rotation + ", corner: " + corner + ", top: " + top);
        int val = top ? rotatedTopCornerSockets[rotation][(int)corner] : rotatedBottomCornerSockets[rotation][(int)corner];
        Debug.Log("GetRotatedCornerSocketId - val: " + val);
        return val;
    }

    public int[] GetRotatedCornerSockets(bool top, int rotation)
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

        bottom[0] = GetRotatedCornerSocketId(cornerA, rotation, true);
        bottom[1] = GetRotatedCornerSocketId(cornerB, rotation, true);

        top[0] = GetRotatedCornerSocketId(cornerA, rotation, true);
        top[1] = GetRotatedCornerSocketId(cornerB, rotation, true);

        return (bottom, top);
    }

    private void EvaluateRotatedCornerSockets()
    {
        int sides = 6;
        int corners = 12;
        int[][] newRotatedTopCornerSocketIds = new int[sides][];
        int[][] newRotatedBottomCornerSocketIds = new int[sides][];

        for (int i = 0; i < sides; i++)
        {
            newRotatedTopCornerSocketIds[i] = new int[corners];
            newRotatedBottomCornerSocketIds[i] = new int[corners];
        }
        // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
        for (int i = 0; i < corners; i++)
        {
            newRotatedTopCornerSocketIds[0][i] = topCornerSocketIds[i];
            newRotatedBottomCornerSocketIds[0][i] = bottomCornerSocketIds[i];
        }

        // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
        for (int i = 1; i < sides; i++)
        {
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

        if (resetPoints || _currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
        {
            resetPoints = false;
            _currentCenterPosition = center.position;
            RecalculateEdgePoints();
            EvaluateRotatedCornerSockets();
        }

        if (resetAllSockets)
        {
            resetAllSockets = false;
            ResetAllSockets();
        }

        UpdateAllSocketIDs();


        if (!enableEditMode) return;

        if (!tileLabelGroup) tileLabelGroup = GetComponentInChildren<TileLabelGroup>();

        if (socketTextDisplay == null || socketTextDisplay.Length == 0) socketTextDisplay = TileLabelGroup.Reevaluate(this.gameObject);

        EvaluateTextDisplay();
        EvaluateSocketLabels(_showSocketLabels != showSocketLabels);

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
    [SerializeField] private bool showSocketLabels;
    private bool _showSocketLabels = false;
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

            string str = i < sideNames.Length ? "Bottom " : "Top ";
            socketTextDisplay[i].gameObject.name = str + sideNames[i % sideNames.Length];
            // RectTransform rectTransform = socketTextDisplay[i].GetComponent<RectTransform>();
            // rectTransform.rotation = new Quaternion(0, 180, 0, 0);
        }
    }

    private void EvaluateSocketLabels(bool force = false)
    {
        if (force || _showSocketLabels != showSocketLabels)
        {
            _showSocketLabels = showSocketLabels;
            if (socketTextDisplay != null && socketTextDisplay.Length > 0)
            {
                for (int i = 0; i < socketTextDisplay.Length; i++)
                {
                    socketTextDisplay[i].SetActive(showSocketLabels);
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
            // float xzOffset = (i % 2 == 0) ? labelXZOffset : -labelXZOffset;
            // bool useOffset = (i % 2 == 0) ? false : true;

            HexagonSide side = (HexagonSide)i;
            UpdateSocketLabelPlacement(side, true);
            UpdateSocketLabel(side, true, rotationAmount); // Top

            UpdateSocketLabelPlacement(side, false);
            UpdateSocketLabel(side, false, rotationAmount); // Bottom
            rotationAmount += 60;
        }
        // for (int i = 0; i < cornerNames.Length; i++)
        // {
        //     // float xzOffset = (i % 2 == 0) ? labelXZOffset : -labelXZOffset;
        //     bool useOffset = (i % 2 == 0) ? false : true;

        //     UpdateSocketLabelPlacement((HexagonCorner)i, true, useOffset);
        //     UpdateSocketLabel((HexagonCorner)i, true); // Top

        //     UpdateSocketLabelPlacement((HexagonCorner)i, false, useOffset);
        //     UpdateSocketLabel((HexagonCorner)i, false); // Bottom
        // }
    }

    private void UpdateSocketLabelPlacement(HexagonSide _side, bool top)
    {
        (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexagonCell.GetCornersFromSide(_side);

        int cornerA = (int)_cornerA;
        int cornerB = (int)_cornerB;

        int side = (int)_side;

        int labelAIX = top ? (12 + cornerA) : cornerA;
        int labelBIX = top ? (12 + cornerB) : cornerB;

        // Vector3 pos = _sides[side];

        Vector3 pos = (_sides[side] - center.position) * labelForwardOffset;
        Quaternion rot = Quaternion.LookRotation(center.position - _sides[side]);
        pos.y += top ? topLabelYOffset : bottomLabelYOffset;

        int[] cornerSocketIds = top ? topCornerSocketIds : bottomCornerSocketIds;

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

    private void UpdateSocketLabel(HexagonSide _side, bool top, int rotationAmount)
    {
        (HexagonCorner _cornerA, HexagonCorner _cornerB) = HexagonCell.GetCornersFromSide(_side);

        int cornerA = (int)_cornerA;
        int cornerB = (int)_cornerB;

        int side = (int)_side;

        int labelAIX = top ? (12 + cornerA) : cornerA;
        int labelBIX = top ? (12 + cornerB) : cornerB;

        TextMesh textMeshA = socketTextDisplay[labelAIX].GetComponent<TextMesh>();
        TextMesh textMeshB = socketTextDisplay[labelBIX].GetComponent<TextMesh>();

        // RectTransform rectTransform = socketTextDisplay[labelIX].GetComponent<RectTransform>();
        // rectTransform.rotation = new Quaternion(0, 90, -90, 0);

        // rectTransform.rotation = new Quaternion(0, 180, -90, 0);

        // Vector3 direction = center.position - rectTransform.position;
        // rectTransform.rotation = Quaternion.LookRotation(direction);

        string strA;
        string strB;

        if (top)
        {
            textMeshA.color = tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerA]].color;
            strA = "id_" + topCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerA]].name + "\n" + "Top " + _cornerA;

            textMeshB.color = tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerB]].color;
            strB = "id_" + topCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[topCornerSocketIds[cornerB]].name + "\n" + "Top " + _cornerB;
        }
        else
        {
            textMeshA.color = tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerA]].color;
            strA = "id_" + bottomCornerSocketIds[cornerA] + " - " + tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerA]].name + "\n" + "BTM " + _cornerA;

            textMeshB.color = tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerB]].color;
            strB = "id_" + bottomCornerSocketIds[cornerB] + " - " + tileSocketDirectory.compatibilityTable[bottomCornerSocketIds[cornerB]].name + "\n" + "BTM " + _cornerB;
        }

        textMeshA.text = strA;
        textMeshA.fontSize = 11;

        textMeshB.text = strB;
        textMeshB.fontSize = 11;
    }

    #endregion


    public enum SocketMirrorState
    {
        Unset = 0,
        MirrorBottom = 1,
        MirrorTop = 2,
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

[System.Serializable]
public struct HexagonTilePrototype
{
    public string id;
    public string topNeighborId;
    public string bottomNeighborId;
    public int size;
    public Vector3 center;
    public Vector3[] cornerPoints;
}

