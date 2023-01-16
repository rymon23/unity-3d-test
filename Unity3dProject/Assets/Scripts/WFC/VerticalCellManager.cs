using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class VerticalCellManager : MonoBehaviour
{
    // public int id = -1;
    [Header("Manager Settings")]
    // StrucureType
    // StrucureType



    [Header("Cell Settings")]
    [Range(1, 72)] public int levels = 4;
    [Range(8, 24)] public int height = 8;
    [Range(12, 64)] public int width = 30;
    [Range(12, 64)] public int depth = 30;
    [Range(12, 72)] public int size = 30;

    [SerializeField] private List<VerticalCellPrototype> cellPrototypes;

    [Header("Debug Settings")]
    [SerializeField] private bool generatePrototypes;
    // [SerializeField] private bool resetPoints;

    #region Saved State
    int _levels;
    int _height;
    int _width;
    int _depth;
    int _size;
    #endregion

    void OnValidate()
    {
        if (levels != _levels || height != _height || width != _width
            || depth != _depth
            || size != _size)
        {
            if (cellPrototypes != null)
            {
                generatePrototypes = true;
            }
        }

        if (generatePrototypes)
        {
            generatePrototypes = false;

            cellPrototypes = ProceduralTerrainUtility.GenerateCubeStack(
                levels, transform.position, width, height, depth
            );
        }

    }

    void OnDrawGizmos()
    {
        if (cellPrototypes == null) return;
        foreach (VerticalCellPrototype cellPrototype in cellPrototypes)
        {
            Vector3[] _cornerPoints = cellPrototype._cornerPoints;
            ProceduralTerrainUtility.DrawRectangleInGizmos(_cornerPoints);

            // for (int i = 0; i < corners.Length; i++)
            // {
            //     Vector3 current = corners[i];
            //     Vector3 next = corners[(i + 1) % corners.Length];
            //     Gizmos.DrawLine(current, next);
            // }
        }
    }

}
