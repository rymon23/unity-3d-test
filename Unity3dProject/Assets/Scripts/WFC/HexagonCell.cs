using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using UnityEditor;

[System.Serializable]
public class HexagonCell : MonoBehaviour
{
    public int size = 12;
    public Vector3[] _cornerPoints; // Clear nonce calculations are done
    [SerializeField] private List<HexagonCell> _neighbors;
    private Transform center;

    [Header("Debug Settings")]
    [SerializeField] private bool showNeighbors;
    [SerializeField] private bool showPoints;

    private void Awake()
    {
        center = transform;
    }

    void OnValidate()
    {
        center = transform;
        // Vector3[] corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
    }
    private void OnDrawGizmos()
    {

        if (showNeighbors && _neighbors != null && _neighbors.Count > 0)
        {
            foreach (HexagonCell neighbor in _neighbors)
            {
                Gizmos.DrawSphere(neighbor.center.position, 3f);
            }
        }

        if (showPoints)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(center.position, 0.3f);
            // ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(UtilityHelpers.GetTransformPositions(_edgePoints), transform);
        }
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