using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;


namespace WFCSystem
{
    public enum VerticalSide
    {
        Top = 0,
        Bottom
    }

    public enum VerticalEdges
    {
        BottomFront = 0,
        BottomRight,
        BottomBack,
        BottomLeft,

        TopFront = 4,
        TopRight,
        TopBack,
        TopLeft,
    }

    [System.Serializable]
    public class VerticalTile : MonoBehaviour
    {
        [SerializeField] private int _id = -1;
        public int GetID() => _id;
        public void SetID(int id)
        {
            _id = id;
        }
        [Range(12, 64)][SerializeField] private int _size = 24;
        public int GetSize() => _size;

        [Header("Tile Settings")]
        [SerializeField] private bool isBaseFloor;
        public bool IsBaseFloor() => isBaseFloor;
        [SerializeField] private TileCategory _tileCategory;
        public TileCategory GetTileCategory() => _tileCategory;
        [SerializeField] private TileType _tileType;
        public TileType GetTileType() => _tileType;


        [Header("Tile Compatibility / Probability")]
        [Range(0.05f, 1f)] public float probabilityWeight = 0.33f;


        [Header("Tile Socket Configuration")]
        // [SerializeField] private TileSocketDirectory tileSocketDirectory;
        [SerializeField] private TileLabelGroup tileLabelGroup;
        [SerializeField] public int[] edgeSockets = new int[8];
        public int GetEdgeSocketId(VerticalEdges edge) => edgeSockets[(int)edge];
        public int[] GetEdgeSockets() => edgeSockets;
        public int[] GetTopEdgeSockets() => edgeSockets.Skip(4).ToArray();
        public int[] GetBottomEdgeSockets(VerticalEdges edge) => edgeSockets.Take(4).ToArray();

        private Transform center;
        [SerializeField] private Vector3[] _corners;
        [SerializeField] private Vector3[] _sides;
        [SerializeField] private Vector3[] _edges = new Vector3[8];

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

        public int[][] rotatedEdgeSockets { get; private set; }
        public int GetRotatedEdgeSocketId(VerticalEdges side, int rotation)
        {
            EvaluateRotatedVerticalEdgeSockets();
            return rotatedEdgeSockets[rotation][(int)side];
        }


        private void EvaluateRotatedVerticalEdgeSockets()
        {
            int edges = 8;

            int[][] newRotatedVerticalEdges = new int[edges][];
            for (int i = 0; i < edges; i++)
            {
                newRotatedVerticalEdges[i] = new int[edges];
            }
            // Initialize rotatedVerticalEdges with the edgeIds of the unrotated cube
            for (int i = 0; i < edges; i++)
            {
                newRotatedVerticalEdges[0][i] = edgeSockets[i];
            }

            // Update rotatedVerticalEdges with the edgeIds of the rotated cubes
            for (int i = 1; i < edges; i++)
            {
                newRotatedVerticalEdges[i][(int)VerticalEdges.BottomFront] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.BottomRight];
                newRotatedVerticalEdges[i][(int)VerticalEdges.BottomRight] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.BottomBack];
                newRotatedVerticalEdges[i][(int)VerticalEdges.BottomBack] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.BottomLeft];
                newRotatedVerticalEdges[i][(int)VerticalEdges.BottomLeft] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.BottomFront];

                newRotatedVerticalEdges[i][(int)VerticalEdges.TopFront] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.TopRight];
                newRotatedVerticalEdges[i][(int)VerticalEdges.TopRight] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.TopBack];
                newRotatedVerticalEdges[i][(int)VerticalEdges.TopBack] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.TopLeft];
                newRotatedVerticalEdges[i][(int)VerticalEdges.TopLeft] = newRotatedVerticalEdges[i - 1][(int)VerticalEdges.TopFront];
            }

            rotatedEdgeSockets = newRotatedVerticalEdges;
        }

        private void RecalculateEdgePoints()
        {
            _corners = ProceduralTerrainUtility.GenerateCubePoints(transform.position, _size);
            _sides = HexagonGenerator.GetTopAndBottomEdgePointsOfRectangle(_corners).ToArray();
            EvaluateRotatedVerticalEdgeSockets();
        }

        private void Awake()
        {
            center = transform;
            // meshFilter = GetComponent<MeshFilter>();
            // meshRenderer = GetComponent<MeshRenderer>();

            tileLabelGroup = GetComponent<TileLabelGroup>();

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


            if (resetPoints || _currentCenterPosition != center.position || _corners == null || _corners.Length == 0 || _sides == null || _sides.Length == 0)
            {
                resetPoints = false;
                _currentCenterPosition = center.position;
                RecalculateEdgePoints();
                EvaluateRotatedVerticalEdgeSockets();
            }

            if (tileLabelGroup != null) tileLabelGroup.SetLabelsEnabled(_showSocketLabels != showSocketLabels);

            EvaluateTextDisplay();
        }

        #region Debug

        [Header("Debug Settings")]
        [SerializeField] private bool enableEditMode;
        // [SerializeField] private bool showSocketColorMap;
        [SerializeField] private bool showSocketLabels;
        private bool _showSocketLabels = false;
        // [SerializeField] private bool showCorners;
        // [SerializeField] private bool showSides;
        [SerializeField] private bool showEdges;
        [SerializeField] private bool resetPoints;

        private void EvaluateTextDisplay()
        {
            string[] sideNames = Enum.GetNames(typeof(VerticalEdges));
            GameObject[] labels = tileLabelGroup.labels;

            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].gameObject.name = sideNames[i];
                RectTransform rectTransform = labels[i].GetComponent<RectTransform>();
                rectTransform.rotation = new Quaternion(0, 180, 0, 0);
            }
        }

        private void OnDrawGizmos()
        {
            center = transform;

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);

            // if (showCorners)
            // {
            //     Gizmos.color = Color.magenta;

            //     foreach (Vector3 item in _corners)
            //     {
            //         Gizmos.DrawSphere(item, 0.3f);
            //     }
            // }

            // if (showSides)
            // {
            //     for (int i = 0; i < _sides.Length; i++)
            //     {
            //         // Fix if out of bounds
            //         if (edgeSockets[i] > tileSocketDirectory.sockets.Length - 1)
            //         {
            //             edgeSockets[i] = tileSocketDirectory.sockets.Length - 1;
            //         }

            //         Gizmos.color = tileSocketDirectory.sockets[edgeSockets[i]].color;
            //         Vector3 pos = _sides[i];
            //         pos = pos - UtilityHelpers.FaceAwayFromPoint(center.position, _sides[i]);
            //         // pos = pos - pos * 0.1f;
            //         Gizmos.DrawSphere(pos, 1f);
            //     }
            // }

            if (showEdges)
            {
                Gizmos.color = Color.magenta;
                VectorUtil.DrawHexagonPointLinesInGizmos(_corners);
            }

            // if (tileSocketDirectory != null && tileLabelGroup != null)
            // {
            //     for (int i = 0; i < _edges.Length; i++)
            //     {
            //         Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[edgeSockets[i]].color : Color.white;
            //         Gizmos.DrawSphere(_edges[i], 0.1f * transform.lossyScale.z);
            //         tileLabelGroup.labels[i].GetComponent<RectTransform>().position = _edges[i] + new Vector3(0, sideDisplayOffsetY, 0);
            //     }
            // }

            // if (showSocketLabels && tileLabelGroup.labels != null && tileLabelGroup.labels.Length == 6)
            // {
            //     string[] sideNames = Enum.GetNames(typeof(VerticalEdges));
            //     for (int i = 0; i < tileLabelGroup.labels.Length; i++)
            //     {
            //         RectTransform rectTransform = tileLabelGroup.labels[i].GetComponent<RectTransform>();
            //         rectTransform.rotation = new Quaternion(0, 180, 0, 0);
            //         tileLabelGroup.labels[i].GetComponent<RectTransform>().rotation = new Quaternion(0, 180, 0, 0);
            //         TextMesh textMesh = tileLabelGroup.labels[i].GetComponent<TextMesh>();
            //         textMesh.color = tileSocketDirectory.sockets[edgeSockets[i]].color;
            //         string str = "id_" + edgeSockets[i] + " - " + tileSocketDirectory.sockets[edgeSockets[i]].name + "\n" + sideNames[i];
            //         textMesh.text = str;
            //         textMesh.fontSize = 12;
            //     }
            // }
        }

        #endregion

        // [Header("Mesh Generation")]
        // [SerializeField] private bool generateMesh;
        // [SerializeField] private bool saveMesh;
        // private Mesh lastGeneratedMesh;
        // [SerializeField] private MeshFilter meshFilter;
        // [SerializeField] private MeshRenderer meshRenderer;

        // private void SaveMeshAsset(Mesh mesh, string assetName)
        // {
        //     // Create a new mesh asset
        //     lastGeneratedMesh = Instantiate(mesh) as Mesh;
        //     lastGeneratedMesh.name = assetName;
        //     // Save the mesh asset to the project
        //     AssetDatabase.CreateAsset(lastGeneratedMesh, "Assets/Meshes/" + assetName + ".asset");
        //     AssetDatabase.SaveAssets();
        // }

        [System.Serializable]
        public struct HexagonSideEntry
        {
            public string name;
            public string socketName;
            [Range(0, 128)] public int socketId;
        }
    }
}