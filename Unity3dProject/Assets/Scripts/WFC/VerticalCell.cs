using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class VerticalCell : MonoBehaviour
{
    public int id = -1;
    [Range(8, 72)] public int height = 8;
    [Range(12, 64)] public int width = 30;
    [Range(12, 64)] public int depth = 30;
    [Range(12, 72)] public int size = 30;

    public Vector3[] _cornerPoints;
    public Vector3[] _sides;
    public Vector3[] _centerPoints;
    public List<VerticalCell> _neighbors;
    // public VerticalCell[] neighborsBySide = new VerticalCell[6];
    // public int GetNeighborsRelativeSide(HexagonSides side)
    // {
    //     if (neighborsBySide[(int)side] == null) return -1;

    //     for (int neighborSide = 0; neighborSide < 6; neighborSide++)
    //     {
    //         if (neighborsBySide[(int)side].neighborsBySide[neighborSide] == this)
    //         {
    //             return neighborSide;
    //         }
    //     }
    //     return -1;
    // }
    // public int[] GetNeighborTileSockets()
    // {
    //     int[] neighborSocketsBySide = new int[6];

    //     for (int side = 0; side < 6; side++)
    //     {
    //         // If no neighbor socket is Edge socket value
    //         if (neighborsBySide[side] == null)
    //         {
    //             neighborSocketsBySide[side] = 1;
    //         }
    //         else
    //         {
    //             // If neighbor has no tile set -1
    //             if (neighborsBySide[side].currentTile == null)
    //             {
    //                 neighborSocketsBySide[side] = -1;
    //             }
    //             else
    //             {
    //                 int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSides)side);
    //                 int facingSocket = neighborsBySide[side].currentTile.GetRotatedSideSocketId((HexagonSides)neighborRelativeSide, neighborsBySide[side].currentRotation);
    //                 neighborSocketsBySide[side] = facingSocket;
    //             }
    //         }
    //     }
    //     return neighborSocketsBySide;
    // }

    public VerticalTile currentTile;
    public int currentRotation = 0;
    private Transform center;

    private void RecalculateEdgePoints()
    {

        _cornerPoints = ProceduralTerrainUtility.GenerateCubePoints(transform.position, width, height, depth);
        _sides = HexagonGenerator.GetTopAndBottomEdgePointsOfRectangle(_cornerPoints).ToArray();

        _centerPoints = new Vector3[2];
        _centerPoints[0] = new Vector3(transform.position.x, transform.position.y - height * 0.5f, transform.position.z);
        _centerPoints[1] = new Vector3(transform.position.x, transform.position.y + height * 0.5f, transform.position.z);
    }

    private void Awake()
    {
        center = transform;
        RecalculateEdgePoints();
    }
    private void Start()
    {
        RecalculateEdgePoints();
    }

    void OnValidate()
    {
        center = transform;

        if (resetPoints || _currentCenterPosition != center.position || _currentSize != size || _cornerPoints == null || _cornerPoints.Length == 0)
        {
            resetPoints = false;
            _currentCenterPosition = center.position;
            // _currentSize = size;
            RecalculateEdgePoints();
        }

        if (showNeighbors)
        {
            if (_neighbors == null || _neighbors.Count == 0)
            {
                showNeighbors = false;
                return;
            }
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool showCenter;
    [SerializeField] private bool showNeighbors;
    [SerializeField] private bool showSides;
    [SerializeField] private bool showCorners;
    [SerializeField] private bool showEdges;
    [SerializeField] private bool resetPoints;
    public bool highlight;

    #region Saved State
    private Vector3 _currentCenterPosition;
    private int _currentSize;
    #endregion

    private void OnDrawGizmos()
    {
        if (!enableDebugMode) return;

        center = transform;

        if (_currentCenterPosition != center.position)
        {
            OnValidate();
        }

        if (showCenter)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);
        }
        if (showCenter)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);
            Gizmos.DrawSphere(_centerPoints[0], 0.5f);
            Gizmos.DrawSphere(_centerPoints[1], 0.5f);
        }

        // if (highlight)
        // {
        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(center.position, 6f);
        // }

        if (showSides && _sides != null)
        {
            for (int i = 0; i < _sides.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Vector3 pos = _sides[i];
                Gizmos.DrawSphere(pos, 1f);
            }
        }

        if (showCorners)
        {
            Gizmos.color = Color.magenta;


            foreach (Vector3 item in _cornerPoints)
            {
                Gizmos.DrawSphere(item, 0.3f);
            }
        }

        if (showEdges)
        {
            Gizmos.color = Color.magenta;
            ProceduralTerrainUtility.DrawRectangleInGizmos(_cornerPoints);
        }

        // if (showNeighbors)
        // {
        //     Gizmos.color = Color.green;
        //     foreach (VerticalCell neighbor in _neighbors)
        //     {
        //         Gizmos.DrawSphere(neighbor.center.position, 3f);
        //     }
        // }
    }
}

[System.Serializable]
public struct VerticalCellPrototype
{
    public Vector3[] _cornerPoints;
    public int height;
    public int width;
    public int depth;
}
