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

public enum HexagonSides
{
    Front = 0,
    FrontRight,
    BackRight,
    Back,
    BackLeft,
    FrontLeft,
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

    [Header("Tile Compatibility / Probability")]
    public bool isInClusterSet; // Is part of a set of tiles that make a cluster
    public bool isClusterCenterTile; // Is the center part of a set of tiles that make a cluster
    public bool isEdgeable; // can be placed on the edge / border or the grid
    public bool isFragment; // Is incomplete by itself, needs neighbor tiles like itself
    [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

    [Header("Tile Socket Configuration")]
    [SerializeField] private TileSocketDirectory tileSocketDirectory;
    [SerializeField] private TileLabelGroup tileLabelGroup;
    [SerializeField] public int[] sideSocketIds = new int[6];
    public int GetSideSocketId(HexagonSides side) => sideSocketIds[(int)side];
    public int GetInnerClusterSocketCount()
    {
        int found = 0;
        foreach (int socket in sideSocketIds)
        {
            if (socket == (int)TileSocketConstants.InnerCuster) found++;
        }
        return found;
    }
    [SerializeField] private float sideDisplayOffsetY = 6f;

    private Transform center;
    [SerializeField] private Vector3[] _corners;
    [SerializeField] private Vector3[] _sides;




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

    public int[][] rotatedSideSocketIds { get; private set; }
    public int GetRotatedSideSocketId(HexagonSides side, int rotation)
    {
        EvaluateRotatedSideSockets();

        Debug.Log("GetRotatedSideSocketId - rotation: " + rotation + ", side: " + side);
        int val = rotatedSideSocketIds[rotation][(int)side];
        Debug.Log("GetRotatedSideSocketId - val: " + val);
        return val;
    }
    private void EvaluateRotatedSideSockets()
    {
        int[][] newRotatedSideSocketIds = new int[6][];
        for (int i = 0; i < 6; i++)
        {
            newRotatedSideSocketIds[i] = new int[6];
        }
        // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
        for (int i = 0; i < 6; i++)
        {
            newRotatedSideSocketIds[0][i] = sideSocketIds[i];
        }

        // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
        for (int i = 1; i < 6; i++)
        {
            newRotatedSideSocketIds[i][(int)HexagonSides.Front] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.FrontRight];
            newRotatedSideSocketIds[i][(int)HexagonSides.FrontRight] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.BackRight];
            newRotatedSideSocketIds[i][(int)HexagonSides.BackRight] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.Back];
            newRotatedSideSocketIds[i][(int)HexagonSides.Back] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.BackLeft];
            newRotatedSideSocketIds[i][(int)HexagonSides.BackLeft] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.FrontLeft];
            newRotatedSideSocketIds[i][(int)HexagonSides.FrontLeft] = newRotatedSideSocketIds[i - 1][(int)HexagonSides.Front];
        }

        rotatedSideSocketIds = newRotatedSideSocketIds;

        // Debug.Log("EvaluateRotatedSideSockets - Updated");
    }



    private void RecalculateEdgePoints()
    {
        _corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
        _sides = HexagonGenerator.GenerateHexagonSidePoints(_corners);
        EvaluateRotatedSideSockets();
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

        if (!tileLabelGroup) tileLabelGroup = GetComponentInChildren<TileLabelGroup>();


        // RotateTransform(currentRotation, transform);
        // UpdateHexagonSideEntries();

        if (resetPoints || _currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
        {
            resetPoints = false;
            _currentCenterPosition = center.position;
            RecalculateEdgePoints();
            EvaluateRotatedSideSockets();
        }

        if (!enableEditMode) return;

        if (tileLabelGroup != null) tileLabelGroup.SetLabelsEnabled(_showSocketLabels != showSocketLabels);


        EvaluateSocketLabels(_showSocketLabels != showSocketLabels);
        EvaluateTextDisplay();

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
    // [SerializeField] private bool showSocketColorMap;
    [SerializeField] private bool showSocketLabels;
    private bool _showSocketLabels = false;
    [SerializeField] private bool showCorners;
    [SerializeField] private bool showSides;
    [SerializeField] private bool showEdges;
    [SerializeField] private bool resetPoints;

    private void EvaluateTextDisplay()
    {
        string[] sideNames = Enum.GetNames(typeof(HexagonSides));
        for (int i = 0; i < socketTextDisplay.Length; i++)
        {
            socketTextDisplay[i].gameObject.name = sideNames[i];
            RectTransform rectTransform = socketTextDisplay[i].GetComponent<RectTransform>();
            rectTransform.rotation = new Quaternion(0, 180, 0, 0);
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

        if (showSides)
        {
            for (int i = 0; i < _sides.Length; i++)
            {
                // Fix if out of bounds
                if (sideSocketIds[i] > tileSocketDirectory.sockets.Length - 1)
                {
                    sideSocketIds[i] = tileSocketDirectory.sockets.Length - 1;
                }

                Gizmos.color = tileSocketDirectory.sockets[sideSocketIds[i]].color;
                Vector3 pos = _sides[i];
                pos = pos - UtilityHelpers.FaceAwayFromPoint(center.position, _sides[i]);
                // pos = pos - pos * 0.1f;
                Gizmos.DrawSphere(pos, 1f);
            }
        }

        if (showEdges)
        {
            Gizmos.color = Color.magenta;
            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_corners);
        }

        // if (!showSocketColorMap) return;

        if (socketTextDisplay == null || socketTextDisplay.Length == 0) return;

        for (int i = 0; i < _sides.Length; i++)
        {
            Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[sideSocketIds[i]].color : Color.white;
            Gizmos.DrawSphere(_sides[i], 0.1f * transform.lossyScale.z);
            socketTextDisplay[i].GetComponent<RectTransform>().position = _sides[i] + new Vector3(0, sideDisplayOffsetY, 0);
        }

        if (showSocketLabels && socketTextDisplay != null && socketTextDisplay.Length == 6)
        {
            string[] sideNames = Enum.GetNames(typeof(HexagonSides));
            for (int i = 0; i < socketTextDisplay.Length; i++)
            {
                RectTransform rectTransform = socketTextDisplay[i].GetComponent<RectTransform>();
                rectTransform.rotation = new Quaternion(0, 180, 0, 0);
                socketTextDisplay[i].GetComponent<RectTransform>().rotation = new Quaternion(0, 180, 0, 0);
                TextMesh textMesh = socketTextDisplay[i].GetComponent<TextMesh>();
                textMesh.color = tileSocketDirectory.sockets[sideSocketIds[i]].color;
                string str = "id_" + sideSocketIds[i] + " - " + tileSocketDirectory.sockets[sideSocketIds[i]].name + "\n" + sideNames[i];
                textMesh.text = str;
                textMesh.fontSize = 12;
            }
        }
    }

    #endregion

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
        [Range(0, 128)] public int socketId;
    }
}

[System.Serializable]
public struct HexagonTilePrototype
{
    public string id;
    public int size;
    public Vector3 center;
    public Vector3[] cornerPoints;
}

