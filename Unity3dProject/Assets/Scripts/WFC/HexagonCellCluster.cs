using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    [System.Serializable]
    public class HexagonCellCluster
    {
        public List<HexagonCell> cells;
        [SerializeField] private HexagonCell parent;
        public HexagonCell GetParentCell() => cells.Count == 0 ? null : parent;
        public HexagonCellCluster(int id, List<HexagonCell> cells)
        {
            this.id = id;
            this.cells = cells;

            Reevaluate();
        }
        public int id = -1;
        public float probability;
        public Vector3 center;
        [SerializeField] private Vector3 foundationCenter;
        public Vector3 GetFoundationCenter() => foundationCenter;

        // public Vector3 center { private set; get; }
        public Vector3[] _sides;
        public List<HexagonCell> _neighbors;
        public HexagonCell[] neighborsBySide;
        public bool isEdgeCuster { private set; get; }
        public void SetEdgeCluster(bool enable)
        {
            isEdgeCuster = enable;
        }

        [Header("Tile")]
        [SerializeField] public GameObject mainTile;
        [SerializeField] private List<GameObject> currentTiles;
        // [SerializeField] private int currentTileRotation;
        public bool IsAssigned() => currentTiles.Count == cells.Count;
        public List<GameObject> GetTiles() => currentTiles;
        // public int GetTileRotation() => currentTileRotation;
        public void SetTiles(List<GameObject> newTiles)
        {
            currentTiles = newTiles;
            // currentTileRotation = rotation;
        }
        public bool selectIgnore;

        public void AssignCells()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].SetClusterID(id);
            }
        }

        public void Reevaluate()
        {
            CalculateClusterCenter();
            EvaluateParentCell();
            CalculateFoundationCenter();
        }

        public void EvaluateParentCell()
        {
            if (cells.Count == 0) return;

            int[] internalNeighborsArray = new int[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                int internalNeighbors = 0;
                foreach (HexagonCell item in cells[i]._neighbors)
                {
                    if (cells.Contains(item)) internalNeighbors++;
                }
                internalNeighborsArray[i] = internalNeighbors;
                cells[i].SetNumberofNeighborsInCluster(internalNeighbors);
            }

            int hightestIX = 0;
            int hightestValue = 0;
            for (int i = 0; i < internalNeighborsArray.Length; i++)
            {
                if (internalNeighborsArray[i] > hightestValue)
                {
                    hightestValue = internalNeighborsArray[i];
                    hightestIX = i;
                }
            }

            parent = cells[hightestIX];
            // Debug.Log("parent Cell: " + parent.id);
        }

        public void CalculateFoundationCenter()
        {
            // foundationCenter = center;
            // return

            HexagonCell parentCell = GetParentCell();
            if (!parentCell || cells.Count != 5)
            {
                foundationCenter = center;
                return;
            }

            List<Vector3> points = new List<Vector3>();

            // points.AddRange(parentCell._sides);
            // points.AddRange(parentCell._cornerPoints);
            points.Add(parentCell.transform.position);
            points.Add(center);
            points.Add(center);

            Vector3 sum = Vector3.zero;
            foreach (Vector3 point in points)
            {
                sum += point;
            }
            foundationCenter = sum / points.Count;
        }

        public void CalculateClusterCenter()
        {
            List<Vector3> cellPoints = new List<Vector3>();
            foreach (HexagonCell cell in cells)
            {
                cellPoints.AddRange(cell._sides);
                cellPoints.AddRange(cell._cornerPoints);
                cellPoints.Add(cell.transform.position);
            }
            if (cellPoints.Count == 0) return;
            center = FindClosestPoint(cellPoints.ToArray());
        }

        public int GetHexagonClusterSideCount(int cells)
        {
            if (cells < 1)
            {
                return 0;
            }
            int sides = cells * 6;
            int overlappingSides = (cells - 1) * 2;
            return sides - overlappingSides;
        }

        public void GetHexagonClusterNeighbors()
        {
            List<HexagonCell> neighbors = new List<HexagonCell>();
            for (int i = 0; i < cells.Count; i++)
            {
                for (int j = 0; j < cells[i]._neighbors.Count; j++)
                {
                    if (!cells.Contains(cells[i]._neighbors[j]))
                    {
                        neighbors.Add(cells[i]._neighbors[j]);
                    }
                }
            }
            _neighbors = neighbors;
        }

        public bool AreAnyPotentialCellsAssigned()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].IsInCluster() || cells[i].IsAssigned())
                {
                    return true;
                }
            }
            return false;
        }
        public bool AreAnyInnerNeighborCellsAssigned()
        {
            if (_neighbors == null) GetHexagonClusterNeighbors();

            if (_neighbors.Count == 0) return false;

            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (!_neighbors[i].isEdgeCell && !_neighbors[i].IsInCluster() && _neighbors[i].IsAssigned())
                {
                    return true;
                }
            }
            return false;
        }

        public static void SortClustersByCellCount(List<HexagonCellCluster> clusters)
        {
            clusters.Sort((a, b) => a.cells.Count.CompareTo(b.cells.Count));
        }

        public static bool AnyNeighborsInACluster(List<HexagonCell> cellNeighbors)
        {
            foreach (HexagonCell neighbor in cellNeighbors)
            {
                if (neighbor.isClusterPrototype)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<HexagonCellCluster> GetHexagonCellClusters(List<HexagonCell> cells, Vector3 positionCenter, WFCCollapseOrder_General collapseOrder = 0, bool ignoreEdges = false)
        {
            List<HexagonCell> edgeCells = HexagonCell.GetEdgeCells(cells); // get the edge cells first
            HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(cells, positionCenter);

            List<HexagonCell> allCells = new List<HexagonCell>();

            if (!ignoreEdges)
            {
                allCells.AddRange(edgeCells);
            }
            // if (collapseOrder == WFCCollapseOrder.Default)
            // {
            //     allCells.Add(centerCell);
            // }
            if (ignoreEdges)
            {
                allCells.AddRange(cells.Except(edgeCells).Except(allCells));
                allCells = allCells.OrderByDescending(x => x._neighbors.Count).ToList();
            }
            else
            {
                allCells.AddRange(cells.Except(allCells));
            }

            HashSet<string> addedToCluster = new HashSet<string>();
            List<HexagonCellCluster> clusters = new List<HexagonCellCluster>();
            int clusterId = -1;

            if (ignoreEdges == false)
            {
                foreach (HexagonCell cell in edgeCells)
                {
                    HexagonCell nei = cell._neighbors.Find(c => c._neighbors.Count == 6);
                    if (nei != null && !addedToCluster.Contains(nei.id) && !AnyNeighborsInACluster(nei._neighbors))
                    {
                        List<HexagonCell> toAdd = new List<HexagonCell>();

                        foreach (HexagonCell neighborCell in nei._neighbors)
                        {
                            toAdd.Add(neighborCell);
                            addedToCluster.Add(neighborCell.id);
                            neighborCell.isClusterPrototype = true;
                        }
                        if (toAdd.Count > 0)
                        {
                            clusterId++;
                            HexagonCellCluster cluster = new HexagonCellCluster(clusterId, new List<HexagonCell>());

                            toAdd.Add(nei);
                            toAdd.Add(cell);
                            cluster.cells.AddRange(toAdd.OrderByDescending(x => x._neighbors.Count));

                            clusters.Add(cluster);
                            addedToCluster.Add(cell.id);
                            cluster.Reevaluate();

                            // Set cluster to edge cluster if any cell is an edge cell
                            if (cluster.cells.Any(x => edgeCells.Contains(x)))
                            {
                                cluster.SetEdgeCluster(true);
                            }
                        }
                    }
                }
            }

            clusterId = -1;

            int ix = 0;

            while (ix < allCells.Count)
            {
                allCells = allCells.OrderByDescending(x => x.GetNumberOfNeighborsUnclustered()).ToList();
                HexagonCell cell = allCells[ix];

                if (addedToCluster.Contains(cell.id))
                {
                    allCells.Remove(cell);
                    List<HexagonCell> updatedList = new List<HexagonCell>();
                    updatedList = allCells;

                    ix = 0;
                    continue;
                }


                if (cell._neighbors.Count > 0 && !ignoreEdges || !cell._neighbors.Any(x => x.isEdgeCell || x.isEntryCell))
                {
                    List<HexagonCell> toAdd = new List<HexagonCell>();

                    foreach (HexagonCell neighborCell in cell._neighbors)
                    {
                        if (!addedToCluster.Contains(neighborCell.id))
                        {
                            toAdd.Add(neighborCell);
                            addedToCluster.Add(neighborCell.id);
                            neighborCell.isClusterPrototype = true;
                        }
                    }

                    if (toAdd.Count > 0)
                    {
                        clusterId++;
                        HexagonCellCluster cluster = new HexagonCellCluster(clusterId, new List<HexagonCell>());
                        toAdd.Add(cell);
                        cluster.cells.AddRange(toAdd.OrderByDescending(x => x._neighbors.Count));

                        clusters.Add(cluster);
                        addedToCluster.Add(cell.id);
                        cluster.Reevaluate();

                        List<HexagonCell> updatedList = new List<HexagonCell>();
                        updatedList.AddRange(allCells.Except(toAdd));
                        allCells = updatedList;

                        ix = 0;

                        // Set cluster to edge cluster if any cell is an edge cell
                        if (cluster.cells.Any(x => edgeCells.Contains(x)))
                        {
                            cluster.SetEdgeCluster(true);
                        }
                    }
                    else
                    {
                        ix++;
                    }
                }
                else
                {
                    ix++;
                }
            }

            return clusters.OrderByDescending(x => x.cells.Count).ToList();
        }


        public static List<HexagonCell> GetCellsFromConsecutiveNeighboringClusters(List<HexagonCellCluster> cellClusters, float maxPercentOfCells)
        {
            List<HexagonCell> result = new List<HexagonCell>();
            int totalCells = cellClusters.Sum(cluster => cluster.cells.Count);
            int maxCells = (int)(totalCells * maxPercentOfCells);

            // Create a list of visited clusters
            List<HexagonCellCluster> visitedClusters = new List<HexagonCellCluster>();

            // Iterate through each cluster
            foreach (HexagonCellCluster cluster in cellClusters)
            {
                // Skip visited clusters
                if (visitedClusters.Contains(cluster))
                    continue;

                // Create a queue of clusters to visit
                Queue<HexagonCellCluster> clustersToVisit = new Queue<HexagonCellCluster>();
                clustersToVisit.Enqueue(cluster);

                // Add the current cluster to the visited clusters
                visitedClusters.Add(cluster);

                // Iterate through the clusters in the queue
                while (clustersToVisit.Count > 0)
                {
                    HexagonCellCluster currentCluster = clustersToVisit.Dequeue();

                    // Add the cells of the current cluster to the result
                    result.AddRange(currentCluster.cells);

                    // Check if adding the cells of the current cluster exceeds the maxCells
                    if (result.Count > maxCells)
                        return result.Take(maxCells).ToList();

                    // Iterate through the cells of the current cluster
                    foreach (HexagonCell cell in currentCluster.cells)
                    {
                        // Iterate through the neighbors of the current cell
                        foreach (HexagonCell neighbor in cell._neighbors)
                        {
                            // Get the cluster of the neighbor
                            HexagonCellCluster neighborCluster = cellClusters.Find(c => c.cells.Contains(neighbor));

                            // Check if the cluster of the neighbor has not been visited yet
                            if (!visitedClusters.Contains(neighborCluster))
                            {
                                // Add the cluster of the neighbor to the visited clusters
                                visitedClusters.Add(neighborCluster);

                                // Add the cluster of the neighbor to the queue
                                clustersToVisit.Enqueue(neighborCluster);
                            }
                        }
                    }
                }
            }

            return result;
        }



        // public static List<HexagonCellCluster> GetHexagonCellClusters(List<HexagonCell> cells, Vector3 positionCenter, WFCCollapseOrder collapseOrder = 0, bool ignoreEdges = false)
        // {
        //     List<HexagonCell> edgeCells = HexagonCell.GetEdgeCells(cells); // get the edge cells first
        //     HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(cells, positionCenter);

        //     List<HexagonCell> allCells = new List<HexagonCell>();

        //     if (!ignoreEdges)
        //     {
        //         allCells.AddRange(edgeCells);
        //     }
        //     if (collapseOrder == WFCCollapseOrder.Default)
        //     {
        //         allCells.Add(centerCell);
        //     }
        //     if (ignoreEdges)
        //     {
        //         allCells.AddRange(cells.Except(edgeCells).Except(allCells));
        //         allCells = allCells.OrderByDescending(x => x._neighbors.Count).ToList();
        //     }
        //     else
        //     {
        //         allCells.AddRange(cells.Except(allCells));
        //     }

        //     HashSet<int> addedToCluster = new HashSet<int>();
        //     List<HexagonCellCluster> clusters = new List<HexagonCellCluster>();
        //     int clusterId = -1;

        //     if (ignoreEdges == false)
        //     {
        //         foreach (HexagonCell cell in edgeCells)
        //         {
        //             HexagonCell nei = cell._neighbors.Find(c => c._neighbors.Count == 6);
        //             if (nei != null && !addedToCluster.Contains(nei.id) && !AnyNeighborsInACluster(nei._neighbors))
        //             {
        //                 List<HexagonCell> toAdd = new List<HexagonCell>();

        //                 foreach (HexagonCell neighborCell in nei._neighbors)
        //                 {
        //                     toAdd.Add(neighborCell);
        //                     addedToCluster.Add(neighborCell.id);
        //                     neighborCell.isClusterPrototype = true;
        //                 }
        //                 if (toAdd.Count > 0)
        //                 {
        //                     clusterId++;
        //                     HexagonCellCluster cluster = new HexagonCellCluster(clusterId, new List<HexagonCell>());

        //                     toAdd.Add(nei);
        //                     toAdd.Add(cell);
        //                     cluster.cells.AddRange(toAdd.OrderByDescending(x => x._neighbors.Count));

        //                     clusters.Add(cluster);
        //                     addedToCluster.Add(cell.id);
        //                     cluster.Reevaluate();

        //                     // Set cluster to edge cluster if any cell is an edge cell
        //                     if (cluster.cells.Any(x => edgeCells.Contains(x)))
        //                     {
        //                         cluster.SetEdgeCluster(true);
        //                     }
        //                 }
        //             }
        //         }
        //     }

        //     clusterId = -1;
        //     foreach (HexagonCell cell in allCells)
        //     {
        //         if (!addedToCluster.Contains(cell.id))
        //         {
        //             if (cell._neighbors.Count > 0 && !ignoreEdges || !cell._neighbors.Any(x => x.isEdgeCell || x.isEntryCell))
        //             {
        //                 List<HexagonCell> toAdd = new List<HexagonCell>();

        //                 foreach (HexagonCell neighborCell in cell._neighbors)
        //                 {
        //                     if (!addedToCluster.Contains(neighborCell.id))
        //                     {
        //                         toAdd.Add(neighborCell);
        //                         addedToCluster.Add(neighborCell.id);
        //                         neighborCell.isClusterPrototype = true;
        //                     }
        //                 }

        //                 if (toAdd.Count > 0)
        //                 {
        //                     clusterId++;
        //                     HexagonCellCluster cluster = new HexagonCellCluster(clusterId, new List<HexagonCell>());
        //                     toAdd.Add(cell);
        //                     cluster.cells.AddRange(toAdd.OrderByDescending(x => x._neighbors.Count));

        //                     clusters.Add(cluster);
        //                     addedToCluster.Add(cell.id);
        //                     cluster.Reevaluate();

        //                     // Set cluster to edge cluster if any cell is an edge cell
        //                     if (cluster.cells.Any(x => edgeCells.Contains(x)))
        //                     {
        //                         cluster.SetEdgeCluster(true);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //     return clusters.OrderByDescending(x => x.cells.Count).ToList();
        // }

        public static Vector3 FindClosestPoint(Vector3[] points)
        {
            // Calculate the center point
            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
                center += points[i];
            center /= points.Length;

            // Initialize the closest point and its distance
            Vector3 closestPoint = points[0];
            float closestDistance = Vector3.Distance(center, closestPoint);

            // Iterate through the points and check if any point is closer to the center
            for (int i = 1; i < points.Length; i++)
            {
                float distance = Vector3.Distance(center, points[i]);
                if (distance < closestDistance)
                {
                    closestPoint = points[i];
                    closestDistance = distance;
                }
            }

            return closestPoint;
        }
    }
}