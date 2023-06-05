using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using UnityEditor;
namespace WFCSystem
{
    [System.Serializable]
    public class VerticalCellManager : MonoBehaviour
    {
        // public int id = -1;
        [Header("Manager Settings")]
        // StrucureType
        // StrucureType


        [Header("Cell Settings")]
        [Range(1, 72)] public int levels = 4;
        [Range(6, 24)] public int height = 8;
        [Range(12, 64)] public int width = 30;
        [Range(12, 64)] public int depth = 30;
        [Range(12, 72)] public int size = 30;

        [SerializeField] private List<VerticalCellPrototype> cellPrototypes;
        public List<VerticalCell> cells;
        [SerializeField] private VerticalCell cellPrefab;

        [Header("Debug Settings")]
        [SerializeField] private bool generatePrototypes;
        [SerializeField] private bool generateCells;
        // [SerializeField] private bool resetPoints;

        #region Saved State
        Vector3 _lastPosition;
        int _levels;
        int _height;
        int _width;
        int _depth;
        int _size;
        #endregion

        void OnValidate()
        {
            if (_lastPosition != transform.position || levels != _levels || height != _height || width != _width
                || depth != _depth
                || size != _size)
            {
                _lastPosition = transform.position;
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

                // for (int i = 0; i < cells.Count; i++)
                // {
                //     cells[i].gameObject.SetActive(false);
                //     Destroy(cells[i].transform);
                // }
            }

            bool hasTilePrototypes = cellPrototypes != null && cellPrototypes.Count > 0;

            if (generateCells && hasTilePrototypes)
            {
                generateCells = false;

                cells = GenerateVerticalTileCellObjects(cellPrototypes);
            }
        }

        public void EvaluateCells()
        {
            cellPrototypes = ProceduralTerrainUtility.GenerateCubeStack(
                levels, transform.position, width, height, depth
            );

            if (cells == null || cells.Count == 0)
            {
                cells = GenerateVerticalTileCellObjects(cellPrototypes);
            }
        }


        private List<VerticalCell> GenerateVerticalTileCellObjects(List<VerticalCellPrototype> cellPrototypes)
        {
            List<VerticalCell> newCells = new List<VerticalCell>();
            int cellId = 0;

            foreach (VerticalCellPrototype cellPrototype in cellPrototypes)
            {
                Vector3 pointPos = cellPrototype.position;

                GameObject newObject = Instantiate(cellPrefab.gameObject, pointPos, Quaternion.identity);
                VerticalCell newCell = newObject.GetComponent<VerticalCell>();
                newCell._cornerPoints = cellPrototype._cornerPoints;
                newCell.size = cellPrototype.width;
                newCell.width = cellPrototype.width;
                newCell.height = cellPrototype.height;
                newCell.depth = cellPrototype.depth;
                newCell.id = cellId;

                newCells.Add(newCell);
                newObject.transform.SetParent(gameObject.transform);

                cellId++;
            }
            return newCells;
        }


        void OnDrawGizmos()
        {
            if (cellPrototypes == null) return;
            foreach (VerticalCellPrototype cellPrototype in cellPrototypes)
            {
                Vector3[] _cornerPoints = cellPrototype._cornerPoints;
                VectorUtil.DrawRectangleInGizmos(_cornerPoints);

                // for (int i = 0; i < corners.Length; i++)
                // {
                //     Vector3 current = corners[i];
                //     Vector3 next = corners[(i + 1) % corners.Length];
                //     Gizmos.DrawLine(current, next);
                // }
            }
        }

    }
}