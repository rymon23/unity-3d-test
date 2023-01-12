using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using UnityEditor;

[System.Serializable]
public class HexagonCell : MonoBehaviour
{
    public int id = -1;
    public int size = 12;
    public Vector3[] _cornerPoints; // Clear nonce calculations are done
    public List<HexagonCell> _neighbors;
    Dictionary<int, HexagonCell> neighborsBySide;

    public HexagonTile currentTile;
    public int currentRotation = 0;
    private Transform center;

    [Header("WFC Params")]
    public float highestProbability;




    // public int GetNeighborTileId(int side)
    // {
    //     if (_neighbors[side].currentTile != null)
    //         return _neighbors[side].currentTile.id;
    //     else
    //         return outOfBoundsSlotId;
    // }

    private void Awake()
    {
        center = transform;
    }

    void OnValidate()
    {
        center = transform;
        // Vector3[] corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
    }
    [Header("Debug Settings")]
    [SerializeField] private bool showCenter;
    [SerializeField] private bool showNeighbors;
    [SerializeField] private bool showCorners;
    [SerializeField] private bool showEdges;

    private void OnDrawGizmos()
    {
        if (showCenter)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);
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
            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_cornerPoints);
        }

        if (showNeighbors && _neighbors != null && _neighbors.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (HexagonCell neighbor in _neighbors)
            {
                Gizmos.DrawSphere(neighbor.center.position, 3f);
            }
        }

        // if (showPoints)
        // {
        //     Gizmos.color = Color.magenta;
        //     Gizmos.DrawSphere(center.position, 0.3f);
        //     // ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(UtilityHelpers.GetTransformPositions(_edgePoints), transform);
        // }
    }
    public void SetNeighborsBySide()
    {
        // Create a new dictionary to store neighbors by side
        Dictionary<int, HexagonCell> newNeighborsBySide = new Dictionary<int, HexagonCell>();
        for (int i = 0; i < 6; i++)
        {
            newNeighborsBySide.Add(i, null);
        }
        for (int k = 0; k < _neighbors.Count; k++)
        {
            // Assign the neighbor to the corresponding side in the dictionary
            newNeighborsBySide[k] = _neighbors[k];
        }
        neighborsBySide = newNeighborsBySide;
    }

    public static void PopulateNeighborsFromCornerPoints(List<HexagonCell> tiles, float offset = 0.3f)
    {
        foreach (HexagonCell tile1 in tiles)
        {
            //for each edgepoint on the current hexagontile
            for (int i = 0; i < tile1._cornerPoints.Length; i++)
            {
                //loop through all the hexagontile to check for neighbors
                for (int j = 0; j < tiles.Count; j++)
                {
                    //skip if the hexagontile is the current tile
                    if (tiles[j] == tile1)
                        continue;

                    //loop through the _cornerPoints of the neighboring tile
                    for (int k = 0; k < tiles[j]._cornerPoints.Length; k++)
                    {
                        if (Vector3.Distance(tiles[j]._cornerPoints[k], tile1._cornerPoints[i]) <= offset)
                        {
                            tile1._neighbors.Add(tiles[j]);
                            break;
                        }
                    }
                }
            }
            tile1.SetNeighborsBySide();
        }
    }


    // public static void PopulateNeighbors(List<HexagonCell> tiles, float offset = 0.2f)
    // {
    //     foreach (HexagonCell tile1 in tiles)
    //     {
    //         //for each edgepoint on the current hexagontile
    //         for (int i = 0; i < tile1._edgePoints.Length; i++)
    //         {
    //             //loop through all the hexagontile to check for neighbors
    //             for (int j = 0; j < tiles.Count; j++)
    //             {
    //                 //skip if the hexagontile is the current tile
    //                 if (tiles[j] == tile1)
    //                     continue;

    //                 //loop through the _edgePoints of the neighboring tile
    //                 for (int k = 0; k < tiles[j]._edgePoints.Length; k++)
    //                 {
    //                     if (Vector3.Distance(tiles[j]._edgePoints[k].position, tile1._edgePoints[i].position) <= offset)
    //                     {
    //                         tile1._neighbors.Add(tiles[j]);
    //                         break;
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }
}