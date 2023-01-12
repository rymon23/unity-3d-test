using System;
using UnityEngine;
using ProceduralBase;
using UnityEditor;

public enum HexagonSides
{
    Front,
    FrontRight,
    BackRight,
    Back,
    BackLeft,
    FrontLeft,
    Invalid
}

[System.Serializable]
public class HexagonTile : MonoBehaviour
{
    public int id = -1;
    public int size = 12;
    private Transform center;
    [SerializeField] private Vector3[] _corners;
    [SerializeField] private Vector3[] _sides;

    [Header("Tile Config")]
    [SerializeField] private TileSocketDirectory tileSocketDirectory;
    [SerializeField] public int[] sideSocketIds = new int[6];
    public int GetSideSocketId(HexagonSides side) => sideSocketIds[(int)side];
    public int[][] rotatedSideSocketIds { get; private set; }
    public bool isEdgeable; // can be placed on the edge / border or the grid
    [SerializeField] private float sideDisplayOffsetY = 6f;
    public GameObject[] socketTextDisplay;

    #region Saved Values
    private Vector3 _currentCenterPosition;
    #endregion


    private void EvaluateRotatedSideSockets()
    {
        rotatedSideSocketIds = new int[6][];
        for (int i = 0; i < 6; i++)
        {
            rotatedSideSocketIds[i] = new int[6];
        }

        // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
        for (int i = 0; i < 6; i++)
        {
            rotatedSideSocketIds[0][i] = sideSocketIds[i];
        }

        // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
        for (int i = 1; i < 6; i++)
        {
            rotatedSideSocketIds[i][(int)HexagonSides.Front] = rotatedSideSocketIds[i - 1][(int)HexagonSides.FrontRight];
            rotatedSideSocketIds[i][(int)HexagonSides.FrontRight] = rotatedSideSocketIds[i - 1][(int)HexagonSides.BackRight];
            rotatedSideSocketIds[i][(int)HexagonSides.BackRight] = rotatedSideSocketIds[i - 1][(int)HexagonSides.Back];
            rotatedSideSocketIds[i][(int)HexagonSides.Back] = rotatedSideSocketIds[i - 1][(int)HexagonSides.BackLeft];
            rotatedSideSocketIds[i][(int)HexagonSides.BackLeft] = rotatedSideSocketIds[i - 1][(int)HexagonSides.FrontLeft];
            rotatedSideSocketIds[i][(int)HexagonSides.FrontLeft] = rotatedSideSocketIds[i - 1][(int)HexagonSides.Front];
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

    private void RecalculateEdgePoints()
    {
        _corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
        _sides = HexagonGenerator.GenerateHexagonSidePoints(_corners);
    }

    private void Awake()
    {
        center = transform;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        RecalculateEdgePoints();
    }

    void OnValidate()
    {
        center = transform;

        if (_currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
        {
            _currentCenterPosition = center.position;
            RecalculateEdgePoints();
        }

        if (!enableEditMode) return;

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
    [SerializeField] private bool showCorners;
    [SerializeField] private bool showSides;
    [SerializeField] private bool showEdges;
    private bool _showSocketLabels;

    private void OnDrawGizmos()
    {
        EvaluateSocketLabels(_showSocketLabels != showSocketLabels);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(center.position, 0.3f);

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
}

[System.Serializable]
public struct HexagonTilePrototype
{
    public Vector3 center;
    public Vector3[] cornerPoints;
}