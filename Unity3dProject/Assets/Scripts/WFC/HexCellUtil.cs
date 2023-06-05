using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public static class HexCellUtil
    {
        public static int CalculateExpandedHexRadius(int cellSize, int radiusMult)
        {
            int radius = cellSize;
            for (int i = 0; i < radiusMult; i++)
            {
                radius *= 3;
            }
            return radius;
        }


        // public static void EvaluateSideNeighbors(HexagonCell cell, float offset = 0.33f)
        // {
        //     HexagonCell[] neighborsBySide = new HexagonCell[6];
        //     HashSet<string> added = new HashSet<string>();

        //     cell.RecalculateEdgePoints();

        //     for (int side = 0; side < 6; side++)
        //     {
        //         Vector3 sidePoint = cell._sides[side];

        //         for (int neighbor = 0; neighbor < cell._neighbors.Count; neighbor++)
        //         {
        //             if (cell._neighbors[neighbor].GetLayer() != cell.GetLayer() || added.Contains(cell._neighbors[neighbor].id)) continue;

        //             cell._neighbors[neighbor].RecalculateEdgePoints();

        //             for (int neighborSide = 0; neighborSide < 6; neighborSide++)
        //             {
        //                 Vector3 neighborSidePoint = cell._neighbors[neighbor]._sides[neighborSide];

        //                 if (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z)) <= offset)
        //                 {
        //                     cell._neighborsBySide[side] = cell._neighbors[neighbor];
        //                     added.Add(_neighbors[neighbor].id);
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        //     cell._neighborsBySide = _neighborsBySide;
        // }


        public static void SetTunnelCells(List<HexagonCellPrototype> cellsToAssign)
        {
            for (int i = 0; i < cellsToAssign.Count; i++)
            {
                HexagonCellPrototype tunnelCell = cellsToAssign[i];
                TunnelStatus status = TunnelStatus.Unset;

                if (tunnelCell.IsUnderGround())
                {
                    // HexagonCellPrototype topNeighbor = tunnelCell.layerNeighbors[1];
                    HexagonCellPrototype bottomNeighbor = tunnelCell.layerNeighbors[0];

                    if (cellsToAssign.Contains(bottomNeighbor))
                    {
                        status = TunnelStatus.AboveGround;
                    }
                    else
                    {
                        status = TunnelStatus.FlatGround;
                    }
                }
                tunnelCell.SetTunnel(true, status);
            }
        }

        // private static bool EvaluateEdgeCell(HexagonCellPrototype prototype, EdgeCellType edgeCellType, bool assignOriginalGridEdge)
        // {
        //     if (prototype == null) return false;

        //     List<HexagonCellPrototype> allSideNeighbors = prototype.neighbors.FindAll(c => c.IsSameLayer(prototype) && c.IsRemoved() == false);
        //     List<HexagonCellPrototype> allSiblingSideNeighbors = allSideNeighbors.FindAll(n => n.parentId == prototype.parentId);

        //     int sideNeighborCount = allSideNeighbors.Count;
        //     // int totalNeighborCount = prototype.neighbors.Count;
        //     bool isEdge = false;

        //     if (allSiblingSideNeighbors.Count < 6)
        //     {
        //         if (assignOriginalGridEdge == true) prototype.SetOriginalGridEdge(true);


        //         prototype.isEdgeOfParent = true;
        //     }

        //     bool isConnectorCell = allSideNeighbors.Any(n => n.GetParenCellId() != prototype.GetParenCellId());

        //     if (allSiblingSideNeighbors.Any(n => n.IsPath())) prototype.isEdgeTOPath = true;

        //     if (sideNeighborCount < 6)
        //     {
        //         prototype.SetEdgeCell(true, edgeCellType);
        //         isEdge = true;
        //     }
        //     return isEdge;
        // }

        public static bool IsEdgeCell(HexagonCell cell, bool isMultilayerCellGrid, bool assignToName, bool scopeToParentCell)
        {
            if (cell == null) return false;

            if (cell.GetEdgeCellType() > EdgeCellType.Default)
            {
                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += "_EDGE";
                return true;
            }

            List<HexagonCell> allSideNeighbors = cell._neighbors.FindAll(c => c.GetLayer() == cell.GetLayer());
            bool isConnectorCell = allSideNeighbors.Find(n => n.GetParenCellId() != cell.GetParenCellId());

            if (scopeToParentCell) allSideNeighbors = allSideNeighbors.FindAll(n => n.GetParenCellId() == cell.GetParenCellId());

            int sideNeighborCount = allSideNeighbors.Count; //cell.GetSideNeighborCount(scopeToParentCell);
            int totalNeighborCount = cell._neighbors.Count;
            bool isEdge = false;

            if (sideNeighborCount < 6 || (isMultilayerCellGrid && totalNeighborCount < 7) || (cell.GetLayer() > 0 && cell.layeredNeighbor[0] != null && cell.layeredNeighbor[0]._neighbors.Count < 7))
            {
                cell.SetEdgeCell(true, isConnectorCell ? EdgeCellType.Connector : EdgeCellType.Default);
                isEdge = true;

                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += isConnectorCell ? "_EDGE_CONNECTOR" : "_EDGE";
            }
            return isEdge;
        }


        public static List<HexagonCell> GetInnerEdges(List<HexagonCell> cells, EdgeCellType edgeCellType, bool assignToName = true, bool scopeToParentCell = false)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();
            foreach (HexagonCell prototype in cells)
            {
                if (IsEdge(prototype, edgeCellType, assignToName, scopeToParentCell)) edgeCells.Add(prototype);
            }
            return edgeCells;
        }

        public static List<HexagonCell> GetEdgeCells(List<HexagonCell> cells, bool assignToName = true, bool scopeToParentCell = false)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();
            bool hasMultilayerCells = cells.Any(c => c.GetLayer() > 0);

            foreach (HexagonCell cell in cells)
            {
                if (IsEdgeCell(cell, hasMultilayerCells, assignToName, scopeToParentCell))
                {
                    edgeCells.Add(cell);
                    // cell.SetEdgeCell(true);
                }
            }
            // Order edge cells by the fewest neighbors first
            return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
        }



        private static bool IsEdge(HexagonCell cell, EdgeCellType edgeCellType, bool assignToName, bool scopeToParentCell = false)
        {
            if (cell == null) return false;

            List<HexagonCell> allSideNeighbors = cell._neighbors.FindAll(c => c.GetLayer() == cell.GetLayer());
            // bool isConnectorCell = allSideNeighbors.Find(n => n.GetParenCellId() != cell.GetParenCellId());
            // if (scopeToParentCell) allSideNeighbors = allSideNeighbors.FindAll(n => n.GetParenCellId() == cell.GetParenCellId());
            int sideNeighborCount = allSideNeighbors.Count; //cell.GetSideNeighborCount(scopeToParentCell);

            if (sideNeighborCount < 6)
            {
                cell.SetEdgeCell(true, edgeCellType);
                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += "_EDGE";
                return true;
            }
            return false;
        }


        public static void PopulateNeighborsFromCornerPoints(List<HexagonCell> cells, float offset = 0.33f)
        {
            foreach (HexagonCell cell in cells)
            {
                //for each edgepoint on the current hexagontile
                for (int i = 0; i < cell._cornerPoints.Length; i++)
                {
                    //loop through all the hexagontile to check for neighbors
                    for (int j = 0; j < cells.Count; j++)
                    {
                        //skip if the hexagontile is the current tile
                        if (cells[j] == cell || cells[j].GetLayer() != cell.GetLayer())
                            continue;

                        Vector3 distA = cell.transform.TransformPoint(cell.transform.position);
                        Vector3 distB = cell.transform.TransformPoint(cells[j].transform.position);

                        float distance = Vector3.Distance(distA, distB);
                        if (distance > cell.GetSize() * HexagonCell.neighborSearchCenterDistMult) continue;

                        //loop through the _cornerPoints of the neighboring tile
                        for (int k = 0; k < cells[j]._cornerPoints.Length; k++)
                        {
                            // if (Vector3.Distance(cells[j]._cornerPoints[k], cell._cornerPoints[i]) <= offset)
                            if (Vector2.Distance(new Vector2(cells[j]._cornerPoints[k].x, cells[j]._cornerPoints[k].z), new Vector2(cell._cornerPoints[i].x, cell._cornerPoints[i].z)) <= offset)
                            {
                                if (cell._neighbors.Contains(cells[j]) == false) cell._neighbors.Add(cells[j]);
                                if (cells[j]._neighbors.Contains(cell) == false) cells[j]._neighbors.Add(cell);
                                break;
                            }
                        }
                    }
                }
                cell.SetNeighborsBySide(offset);
            }
        }

        public static (HexagonCell, float) GetClosestCell(List<HexagonCell> cells, Vector2 position)
        {
            HexagonCell nearestCell = cells[0];
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2 posXZ = new Vector2(cells[i].transform.position.x, cells[i].transform.position.z);
                float dist = Vector2.Distance(position, posXZ);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestCell = cells[i];
                }
            }
            return (nearestCell, nearestDistance);
        }


        public static void ShuffleCells(List<HexagonCell> cells)
        {
            int n = cells.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonCell temp = cells[r];
                cells[r] = cells[i];
                cells[i] = temp;
            }
        }
        public static Vector3 CalculateCenterPositionOfHexagonCells(HexagonCell[] cells)
        {
            // Calculate the center point
            Vector3 center = Vector3.zero;
            for (int i = 0; i < cells.Length; i++)
                center += cells[i].transform.position;
            center /= cells.Length;

            return center;
        }

        public static List<Vector3> GetCenterPositionsFromCells(List<HexagonCell> cells)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (var item in cells)
            {
                positions.Add(item.transform.position);
            }
            return positions;
        }

        public static List<Vector3> GetOrderedVertices(List<HexagonCell> cells)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (HexagonCell cell in cells)
            {
                Vector3[] cornerPoints = cell._cornerPoints;
                Vector3 transformPos = cell.transform.position;

                for (int i = 0; i < cornerPoints.Length; i++)
                {
                    vertices.Add(cell._cornerPoints[i]);
                }
            }
            return vertices;
        }


        public static List<HexagonCellPrototype> ExtractCellsByLayer(Dictionary<int, List<HexagonCellPrototype>> cellsByLayer)
        {
            List<HexagonCellPrototype> cells = new List<HexagonCellPrototype>();

            foreach (var kvp in cellsByLayer)
            {
                cells.AddRange(kvp.Value);
            }
            return cells;
        }
        public static List<HexagonCell> ExtractCellsByLayer(Dictionary<int, List<HexagonCell>> cellsByLayer)
        {
            List<HexagonCell> cells = new List<HexagonCell>();

            foreach (var kvp in cellsByLayer)
            {
                cells.AddRange(kvp.Value);
            }
            return cells;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> OrganizeByLayer(List<HexagonCellPrototype> cells)
        {
            Dictionary<int, List<HexagonCellPrototype>> cellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (HexagonCellPrototype cell in cells)
            {
                int layer = cell.GetLayer();

                if (cellsByLayer.ContainsKey(layer) == false)
                {
                    cellsByLayer.Add(layer, new List<HexagonCellPrototype>());
                }

                cellsByLayer[layer].Add(cell);
            }
            return cellsByLayer;
        }

        public static Dictionary<int, List<HexagonCell>> OrganizeByLayer(List<HexagonCell> cells)
        {
            Dictionary<int, List<HexagonCell>> cellsByLayer = new Dictionary<int, List<HexagonCell>>();

            foreach (HexagonCell cell in cells)
            {
                int layer = cell.GetLayer();

                if (cellsByLayer.ContainsKey(layer) == false)
                {
                    cellsByLayer.Add(layer, new List<HexagonCell>());
                }

                cellsByLayer[layer].Add(cell);
            }
            return cellsByLayer;
        }



        public static HexagonCell[,] CreateRectangularGrid(List<HexagonCell> cellGrid)
        {
            float hexagonSize = cellGrid[0].GetSize();

            //Find the minimum and maximum x and z coordinates of the hexagon cells in the grid
            float minX = cellGrid.Min(cell => cell.transform.position.x);
            float maxX = cellGrid.Max(cell => cell.transform.position.x);
            float minZ = cellGrid.Min(cell => cell.transform.position.z);
            float maxZ = cellGrid.Max(cell => cell.transform.position.z);
            //Determine the number of rows and columns needed for the rectangular grid
            int rows = (int)((maxZ - minZ) / hexagonSize) + 1;
            int cols = (int)((maxX - minX) / (hexagonSize * 0.75f)) + 1;

            //Initialize the rectangular grid with the determined number of rows and columns
            HexagonCell[,] rectGrid = new HexagonCell[rows, cols];

            //Iterate through each hexagon cell in the cell grid
            foreach (HexagonCell cell in cellGrid)
            {
                //Determine the row and column index of the current cell in the rectangular grid
                int row = (int)((cell.transform.position.z - minZ) / hexagonSize);
                int col = (int)((cell.transform.position.x - minX) / (hexagonSize * 0.75f));

                //Assign the current cell to the corresponding position in the rectangular grid
                rectGrid[row, col] = cell;
            }

            return rectGrid;
        }



        public static void CreateGridMesh(List<HexagonCell> cells, MeshFilter meshFilter)
        {
            int vertexIndex = 0;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            foreach (HexagonCell cell in cells)
            {
                for (int i = 0; i < 6; i++)
                {
                    vertices.Add(cell._cornerPoints[i]);
                    // vertices.Add(cell.transform.TransformPoint(cell._cornerPoints[i]));
                    triangles.Add(vertexIndex + (i + 2) % 6);
                    triangles.Add(vertexIndex + (i + 1) % 6);
                    triangles.Add(vertexIndex);
                }

                vertexIndex += 6;
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        public static void CreateGridMesh(List<Vector3> vertices, MeshFilter meshFilter)
        {
            Dictionary<int, List<int>> pointConnections = new Dictionary<int, List<int>>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < vertices.Count; i++)
            {
                pointConnections[i] = new List<int>();

                for (int j = 0; j < vertices.Count; j++)
                {
                    if (i == j) continue;

                    // Check if the distance between the two vertices is close enough
                    if (Vector3.Distance(vertices[i], vertices[j]) <= 100.0f)
                    {
                        // Check if the triangle between the two vertices is not already in the list
                        bool isDuplicate = false;
                        for (int k = 0; k < triangles.Count; k += 3)
                        {
                            if ((triangles[k] == i && triangles[k + 1] == j) || (triangles[k + 1] == i && triangles[k + 2] == j) || (triangles[k + 2] == i && triangles[k] == j))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (!isDuplicate)
                        {
                            triangles.Add(i);
                            triangles.Add(j);
                            triangles.Add(i);
                            pointConnections[i].Add(j);
                        }
                    }
                }
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        public static void UpdateMesh(List<HexagonCell> cells, MeshFilter meshFilter)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // Add the vertices to the vertices list
            foreach (var cell in cells)
            {
                // Add each of the cell's corner points as a vertex
                foreach (var cornerPoint in cell._cornerPoints)
                {
                    vertices.Add(cornerPoint + cell.transform.position);
                }
            }

            // Add the triangles to the triangles list
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                int vertexIndex = i * 6;

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex);

                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 4);
                triangles.Add(vertexIndex);

                triangles.Add(vertexIndex + 4);
                triangles.Add(vertexIndex + 5);
                triangles.Add(vertexIndex);
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }




    }
}