using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace WFCSystem
{
    public enum CellStatus
    { Unset = 0, Ground = 1, UnderGround = 2, Remove = 3, AboveGround }

    // [System.Serializable]
    public class HexagonCellPrototype : IHexCell
    {
        #region Static Vars
        static float neighborSearchCenterDistMult = 2.4f;
        public static float GetCornerNeighborSearchDist(int cellSize) => 1.2f * ((float)cellSize / 12f);
        static float vertexCenterPointDistance = 0.36f;
        #endregion

        #region Interface Methods
        public string GetId() => id;
        public string Get_Uid() => uid;
        public int GetSize() => size;
        public int GetLayer() => layer;
        public bool IsEdge() => isEdge;
        public bool IsEntry() => isEntry;
        public bool IsPath() => isPath;
        public bool IsGround() => cellStatus == CellStatus.Ground;
        public CellStatus GetCellStatus() => cellStatus;
        public EdgeCellType GetEdgeCellType() => _edgeCellType;
        public Vector3 GetPosition() => center;
        public Vector3[] GetCorners() => cornerPoints;
        public Vector3[] GetSides() => sidePoints;
        #endregion
        public string id { get; private set; }
        public string uid { get; private set; }
        public string topNeighborId;
        public string bottomNeighborId;
        public string parentId;
        public string name;
        public int size;
        public int layer { get; private set; }
        public Vector3 center;
        public Vector3[] cornerPoints;
        public Vector3[] sidePoints;
        public List<HexagonCellPrototype> neighbors = new List<HexagonCellPrototype>();
        public HexagonCellPrototype[] neighborsBySide = new HexagonCellPrototype[6];
        public HexagonCellPrototype[] layerNeighbors = new HexagonCellPrototype[2];
        public List<int> _vertexIndices = new List<int>();
        public List<int>[] _vertexIndicesBySide = new List<int>[6] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        public List<int> rampSlopeSides;
        public CellStatus cellStatus;
        public bool IsDisposable() => (cellStatus == CellStatus.Remove || cellStatus == CellStatus.UnderGround);

        public EdgeCellType _edgeCellType;
        public bool isEdge;
        public bool isEntry;
        public bool isPath;
        public bool isGroundRamp;

        public HexagonCellPrototype(string id, string topNeighborId, string bottomNeighborId, string parentId, string name, int size, int layer, Vector3 center, Vector3[] cornerPoints)
        {
            this.id = id;
            this.topNeighborId = topNeighborId;
            this.bottomNeighborId = bottomNeighborId;
            this.parentId = parentId;
            this.name = name;
            this.size = size;
            this.layer = layer;
            this.center = center;
            this.cornerPoints = cornerPoints;
        }

        public HexagonCellPrototype(Vector3 center, int size)
        {
            this.center = center;
            this.size = size;
            RecalculateEdgePoints();
        }

        public HexagonCellPrototype(Vector3 center, int size, string parentId = null, string appendToId = "")
        {
            this.center = center;
            this.size = size;
            RecalculateEdgePoints();

            if (parentId != null)
            {
                this.parentId = parentId;
                this.id = "H" + parentId + "_";
            }
            int idFragment = Mathf.Abs((int)(center.z + center.x + center.y));
            this.id += "X" + size + "-(" + idFragment + appendToId + ")";
            this.name = "Cell_Prototype-" + id;
        }

        public HexagonCellPrototype(Vector3 center, int size, IHexCell parentCell, string appendToId = "", int layer = -1)
        {
            this.center = center;
            this.size = size;
            RecalculateEdgePoints();

            bool hasParent = parentCell != null;

            if (layer < 0) layer = hasParent ? parentCell.GetLayer() : 0;
            this.layer = layer;

            string baseID = "x" + size + "-" + center;
            string parentHeader = "";

            if (hasParent)
            {
                this.parentId = parentCell.GetId();
                parentHeader = "[" + this.parentId + "]/";

                this.isPath = parentCell.IsPath();
                this.cellStatus = parentCell.GetCellStatus();
            }
            this.id = parentHeader + baseID;
            this.name = "prototype-" + this.id;
            if (layer > 0) this.name += "[L_" + layer + "]";
            // int idFragment = Mathf.Abs((int)(center.z + center.x + center.y));
            // this.id += "X" + size + "-(" + idFragment + appendToId + ")";
            this.uid = UtilityHelpers.GenerateUniqueID(baseID);

            // this.uid = UtilityHelpers.GenerateUniqueID(idFragment + "");
        }
        private void RecalculateEdgePoints()
        {
            cornerPoints = ProceduralTerrainUtility.GenerateHexagonPoints(center, size);
            sidePoints = HexagonGenerator.GenerateHexagonSidePoints(cornerPoints);
        }
        public List<Vector3> GetEdgePoints()
        {
            List<Vector3> allEdgePoints = new List<Vector3>();
            allEdgePoints.AddRange(cornerPoints);
            allEdgePoints.AddRange(sidePoints);
            return allEdgePoints;
        }

        public void SetNeighborsBySide(float offset = 0.33f)
        {
            HexagonCellPrototype[] newNeighborsBySide = new HexagonCellPrototype[6];
            HashSet<string> added = new HashSet<string>();

            RecalculateEdgePoints();

            for (int side = 0; side < 6; side++)
            {
                Vector3 sidePoint = sidePoints[side];

                for (int neighbor = 0; neighbor < neighbors.Count; neighbor++)
                {
                    if (neighbors[neighbor].layer != layer || added.Contains(neighbors[neighbor].id)) continue;

                    neighbors[neighbor].RecalculateEdgePoints();

                    for (int neighborSide = 0; neighborSide < 6; neighborSide++)
                    {
                        Vector3 neighborSidePoint = neighbors[neighbor].sidePoints[neighborSide];

                        if (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z)) <= offset)
                        {
                            newNeighborsBySide[side] = neighbors[neighbor];
                            added.Add(neighbors[neighbor].id);
                            break;
                        }
                    }
                }
            }
            neighborsBySide = newNeighborsBySide;
        }
        public int GetNeighborsRelativeSide(HexagonSide side)
        {
            if (neighborsBySide[(int)side] == null) return -1;

            for (int neighborSide = 0; neighborSide < 6; neighborSide++)
            {
                if (neighborsBySide[(int)side].neighborsBySide[neighborSide] == this) return neighborSide;
            }
            return -1;
        }
        public void SetVertices(VertexType vertexType, TerrainVertex[,] vertexGrid)
        {

            if (_vertexIndices != null && _vertexIndices.Count > 0)
            {
                int vertexGridLength = vertexGrid.GetLength(0);

                foreach (int ix in _vertexIndices)
                {
                    vertexGrid[ix / vertexGridLength, ix % vertexGridLength].type = vertexType;
                }
            }
        }

        #region Static Methods

        public static HexagonCellPrototype GetClosestPrototype(List<HexagonCellPrototype> prototypes, Vector3 position)
        {
            HexagonCellPrototype nearest = prototypes[0];
            float nearestDist = float.MaxValue;
            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(prototypes[i].center.x, prototypes[i].center.z));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = prototypes[i];
                }
            }
            return nearest;
        }

        public static HexagonCellPrototype GetGroundLayerNeighbor(HexagonCellPrototype prototypeCell)
        {
            int verticalDirection = prototypeCell.layer == 0 ? 1 : prototypeCell.cellStatus == CellStatus.AboveGround ? 0 : 1;
            HexagonCellPrototype currentPrototype = prototypeCell;
            bool groundFound = false;

            while (groundFound == false && currentPrototype != null)
            {
                if (currentPrototype.cellStatus != CellStatus.Ground)
                {
                    currentPrototype = currentPrototype.layerNeighbors[verticalDirection];
                }
                else
                {
                    groundFound = true;
                    return currentPrototype;
                }
            }
            return null;
        }

        #region Pathing 

        public static List<int> GetPathNeighborSides(HexagonCellPrototype prototype)
        {
            List<int> sidesWithPathNeighbor = new List<int>();
            // HexagonCellPrototype[] allSideNeighbors = new HexagonCellPrototype[6];
            for (int i = 0; i < 6; i++)
            {
                HexagonCellPrototype sideNeighbor = prototype.neighborsBySide[i];

                if (sideNeighbor == null)
                {
                    foreach (var item in prototype.layerNeighbors)
                    {
                        if (item != null && item.neighborsBySide[i] != null)
                        {
                            if (item.neighborsBySide[i].isPath) sidesWithPathNeighbor.Add(i);
                        }
                    }
                }
                else
                {
                    if (sideNeighbor.isPath) sidesWithPathNeighbor.Add(i);
                }
            }
            return sidesWithPathNeighbor;
        }

        public static void AssignPathCenterVertices(List<HexagonCellPrototype> prototypePath, TerrainVertex[,] vertexGrid)
        {
            foreach (HexagonCellPrototype prototype in prototypePath)
            {
                if (prototype._vertexIndices == null) continue;

                List<int> sidesWithPathNeighbor = GetPathNeighborSides(prototype);
                foreach (int side in sidesWithPathNeighbor)
                {
                    Vector2 sidePTPosXZ = new Vector2(prototype.sidePoints[side].x, prototype.sidePoints[side].z);

                    foreach (int vertexIndex in prototype._vertexIndices)
                    {
                        Vector3 vertPos = vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].position;
                        Vector2 vertPosXZ = new Vector2(vertPos.x, vertPos.z);
                        if (Vector2.Distance(vertPosXZ, sidePTPosXZ) <= 6f)
                        {
                            vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].type = VertexType.Road;
                            vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].isCellCenterPoint = true;
                        }
                    }
                }
            }
        }

        public static void SmoothVertexElevationAlongPath(List<HexagonCellPrototype> prototypePath, TerrainVertex[,] vertexGrid)
        {
            List<TerrainVertex> vertexList = new List<TerrainVertex>();
            foreach (HexagonCellPrototype prototype in prototypePath)
            {
                if (prototype._vertexIndices == null) continue;

                List<float> neighborElevations = new List<float>();
                foreach (HexagonCellPrototype item in prototype.neighbors)
                {
                    if (item != null && item.cellStatus == CellStatus.Ground) neighborElevations.Add(item.center.y);
                }
                neighborElevations.Add(prototype.center.y);
                // foreach (HexagonCellPrototype item in prototype.neighborsBySide)
                // {
                //     if (item != null) neighborElevations.Add(item.center.y);
                // }
                float elevationAvg = CalculateAverage(neighborElevations.ToArray());

                foreach (int vertexIndex in prototype._vertexIndices)
                {
                    vertexList.Add(vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)]);
                    vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].type = VertexType.Road;

                    // if (vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].isCellCenterPoint)
                    // {
                    vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].position.y = elevationAvg;
                    // }
                }
            }
            WorldArea.SmoothVertexList(vertexList, vertexGrid);
        }

        public static float CalculateAverage(float[] elevations)
        {
            float sum = 0.0f;

            for (int i = 0; i < elevations.Length; i++)
            {
                sum += elevations[i];
            }

            float average = sum / elevations.Length;
            return average;
        }

        static float DistanceXZ(Vector3 a, Vector3 b)
        {
            Vector2 aXZ = new Vector2(a.x, a.z);
            Vector2 bXZ = new Vector2(b.x, b.z);
            return Vector2.Distance(a, b);
        }
        public static void SmoothElevationAlongPathNeighbors(List<HexagonCellPrototype> prototypePath, TerrainVertex[,] vertexGrid)
        {
            // Get the side vertexGrid and neighbor relative side vertexGrid 
            foreach (HexagonCellPrototype prototype in prototypePath)
            {
                for (int i = 0; i < prototype.neighborsBySide.Length; i++)
                {
                    HexagonCellPrototype neighbor = prototype.neighborsBySide[i];
                    if (neighbor == null) continue;
                    if (neighbor.isPath) continue;

                    int side = prototype.GetNeighborsRelativeSide((HexagonSide)i);
                    Vector3 neighborSidePoint = neighbor.sidePoints[side];

                    List<TerrainVertex> vertexList = new List<TerrainVertex>();
                    foreach (int vertexIndex in prototype._vertexIndicesBySide[i])
                    {
                        vertexList.Add(vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)]);
                    }

                    vertexList.Sort((a, b) => DistanceXZ(a.position, neighborSidePoint).CompareTo(DistanceXZ(b.position, neighborSidePoint)));

                    for (int j = 0; j < vertexList.Count - 1; j++)
                    {
                        TerrainVertex currVertex = vertexList[i];
                        Vector2 currentPosXZ = new Vector2(currVertex.position.x, currVertex.position.z);
                        float slopeY;

                        if (i == 0)
                        {
                            TerrainVertex nextVertex = vertexList[i + 1];
                            slopeY = Mathf.Lerp(neighborSidePoint.y, nextVertex.position.y, 0.03f);
                        }
                        else
                        {
                            TerrainVertex prevVertex = vertexList[i - 1];
                            TerrainVertex nextVertex = vertexList[i + 1];
                            slopeY = Mathf.Lerp(prevVertex.position.y, nextVertex.position.y, 0.03f);
                        }

                        currVertex.position.y = slopeY;

                        vertexList[i] = currVertex;
                        vertexGrid[vertexList[i].index / vertexGrid.GetLength(0), vertexList[i].index % vertexGrid.GetLength(0)].position = currVertex.position;
                    }
                }
            }
        }

        public static List<HexagonCellPrototype> GenerateRandomPath(List<HexagonCellPrototype> entryPrototypes, List<HexagonCellPrototype> allPrototypes, Vector3 position)
        {
            HexagonCellPrototype centerPrototype = GetClosestPrototype(allPrototypes, position);
            List<HexagonCellPrototype> result = FindPath(entryPrototypes[0], centerPrototype, true, false);
            List<HexagonCellPrototype> islandOnRamps = allPrototypes.FindAll(c => c.isGroundRamp);

            // int paths = 0;
            for (int i = 1; i < entryPrototypes.Count; i++)
            {
                List<HexagonCellPrototype> newPathA = FindPath(entryPrototypes[i], centerPrototype, true);
                if (newPathA != null) result.AddRange(newPathA);

                List<HexagonCellPrototype> newPathB = FindPath(entryPrototypes[i], entryPrototypes[i - 1], true);
                if (newPathB != null) result.AddRange(newPathB);
            }

            // Debug.Log("GenerateRandomPath - allPrototypes: " + allPrototypes.Count + ", centerPrototype: " + centerPrototype.id);
            foreach (HexagonCellPrototype ramp in islandOnRamps)
            {
                result.AddRange(HexagonCellPrototype.FindPath(ramp, entryPrototypes[UnityEngine.Random.Range(0, entryPrototypes.Count)], true));
                ramp.isPath = true;
                if (result.Contains(ramp) == false) result.Add(ramp);
            }

            List<HexagonCellPrototype> finalPath = new List<HexagonCellPrototype>();

            List<HexagonCellPrototype> invalids = result.FindAll(r => r.cellStatus != CellStatus.Ground);

            // result = result.FindAll(r => r.cellStatus == CellStatus.Ground);

            Debug.Log("GenerateRandomPath - invalids: " + invalids.Count + ", results: " + result.Count);


            foreach (HexagonCellPrototype item in result)
            {
                if (item.IsDisposable() || item.cellStatus != CellStatus.Ground)
                {
                    HexagonCellPrototype groundLayerNeighbor = GetGroundLayerNeighbor(item);
                    if (groundLayerNeighbor != null && groundLayerNeighbor.cellStatus == CellStatus.Ground)
                    {
                        finalPath.Add(groundLayerNeighbor);
                        groundLayerNeighbor.isPath = true;
                    }
                    else
                    {
                        finalPath.Add(item);
                        item.isPath = true;
                    }
                }
                else
                {
                    finalPath.Add(item);
                    item.isPath = true;
                }
            }

            // return finalPath;
            return ClearPathCellClumps(finalPath);
        }

        public static List<HexagonCellPrototype> ClearPathCellClumps(List<HexagonCellPrototype> pathCells)
        {
            List<HexagonCellPrototype> result = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> cleared = new List<HexagonCellPrototype>();

            foreach (HexagonCellPrototype cell in pathCells)
            {
                if (cell.cellStatus != CellStatus.Ground && cell.layerNeighbors[0] != null && cell.layerNeighbors[0].isPath)
                {
                    cell.isPath = false;
                    cleared.Add(cell);
                    continue;
                }

                List<HexagonCellPrototype> pathNeighbors = cell.neighbors.FindAll(n => cleared.Contains(n) == false && n.layer == cell.layer && (n.isPath || pathCells.Contains(n)));
                if (pathNeighbors.Count >= 4)
                {
                    // bool neighborHasMultipleConnections = pathNeighbors.Any(n => n.neighbors.FindAll(n => pathNeighbors.Contains(n)).Count > 1);
                    // if (neighborHasMultipleConnections)
                    // {
                    cell.isPath = false;
                    cleared.Add(cell);
                    // }
                    // else
                    // {
                    //     result.Add(cell);
                    // }
                }
                else
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        public static List<HexagonCellPrototype> FindPath(HexagonCellPrototype startCell, HexagonCellPrototype endCell, bool ignoreEdgeCells, bool startCellIgnoresLayeredNeighbors = true, bool terminateAtFirstPathCell = false)
        {
            // Create a queue to store the cells to be visited
            Queue<HexagonCellPrototype> queue = new Queue<HexagonCellPrototype>();

            // Create a dictionary to store the parent of each cell
            Dictionary<string, HexagonCellPrototype> parent = new Dictionary<string, HexagonCellPrototype>();

            // Create a set to store the visited cells
            HashSet<string> visited = new HashSet<string>();

            // Enqueue the start cell and mark it as visited
            queue.Enqueue(startCell);
            visited.Add(startCell.id);

            // Get an inner neighbor of endCell if it is on the edge 
            if (ignoreEdgeCells && (endCell.isEdge || endCell.isEntry))
            {
                HexagonCellPrototype newEndCell = endCell.neighbors.Find(n => n.layer == endCell.layer && !n.isEdge && !n.isEntry);
                if (newEndCell != null) endCell = newEndCell;
            }

            // Run the BFS loop
            while (queue.Count > 0)
            {
                HexagonCellPrototype currentCell = queue.Dequeue();

                // Check if the current cell is the end cell
                if (currentCell.id == endCell.id || (terminateAtFirstPathCell && currentCell.isPath))
                {
                    // Create a list to store the path
                    List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();

                    // Trace back the path from the end cell to the start cell
                    HexagonCellPrototype current = currentCell.isPath ? currentCell : endCell;
                    while (current.id != startCell.id)
                    {
                        path.Add(current);
                        current = parent[current.id];
                    }
                    path.Reverse();
                    return path;
                }

                // List<HexagonCellPrototype> viableNeighbors = currentCell.neighbors.FindAll(n => n.cellStatus == CellStatus.Ground || n.cellStatus == CellStatus.AboveGround);
                // List<HexagonCellPrototype> viableNeighbors = currentCell.neighbors.FindAll(n => n.IsDisposable() == false);
                // Debug.Log("viableNeighbors: " + viableNeighbors.Count + " of: " + currentCell.neighbors.Count);

                // Enqueue the unvisited neighbors
                foreach (HexagonCellPrototype neighbor in currentCell.neighbors)
                {
                    if (!visited.Contains(neighbor.id))
                    {
                        visited.Add(neighbor.id);

                        if (neighbor.cellStatus == CellStatus.Remove) continue;

                        if (ignoreEdgeCells && neighbor.isEdge) continue;

                        // If entry cell, dont use layered neighbors
                        // if (((currentCell == startCell && startCellIgnoresLayeredNeighbors) || currentCell.isEntry) && currentCell.layerNeighbors.Contains(neighbor)) continue;

                        //  Dont use layered neighbors if not ground
                        // if (currentCell.layerNeighbors[1] == neighbor && neighbor.cellStatus != CellStatus.Ground) continue;

                        queue.Enqueue(neighbor);
                        parent[neighbor.id] = currentCell;
                    }
                }
            }

            // If there is no path between the start and end cells
            return null;
        }

        #endregion



        public static List<HexagonCellPrototype> GetRandomEntryPrototypes(List<HexagonCellPrototype> edgePrototypes, int num, bool assign, int gridLayer = 0, bool excludeAdjacentNeighbors = true)
        {
            List<HexagonCellPrototype> entrances = new List<HexagonCellPrototype>();
            Shuffle(edgePrototypes);

            foreach (HexagonCellPrototype edgePrototype in edgePrototypes)
            {
                if (entrances.Count >= num) break;

                bool isNeighbor = false;
                foreach (HexagonCellPrototype item in entrances)
                {
                    if ((item.neighbors.Contains(edgePrototype) && !excludeAdjacentNeighbors) || (excludeAdjacentNeighbors && item.neighbors.Any(nb => nb.neighbors.Contains(edgePrototype))))
                    {
                        isNeighbor = true;
                        break;
                    }
                }
                if (!isNeighbor)
                {
                    entrances.Add(edgePrototype);
                    if (assign) edgePrototype.isEntry = true;
                }

            }
            return entrances;
        }

        public static List<HexagonCellPrototype> PickRandomEntryFromGridEdges(List<HexagonCellPrototype> allGridEdges, int num, bool assign, bool excludeAdjacentNeighbors = true)
        {
            List<HexagonCellPrototype> possibles = new List<HexagonCellPrototype>();
            foreach (HexagonCellPrototype edgePrototype in allGridEdges)
            {
                if (edgePrototype.cellStatus != CellStatus.Ground) continue;
                int groundNeighborCount = edgePrototype.neighbors.FindAll(
                        n => n.cellStatus == CellStatus.Ground && n.layer == edgePrototype.layer).Count;
                if (groundNeighborCount >= 3) possibles.Add(edgePrototype);
            }
            return GetRandomEntryPrototypes(possibles, num, assign, -1, excludeAdjacentNeighbors);
        }


        public static List<HexagonCellPrototype> GetEdgePrototypes(List<HexagonCellPrototype> prototypes, EdgeCellType edgeCellType, bool assignToName = true, bool scopeToParentCell = false)
        {
            List<HexagonCellPrototype> edgePrototypes = new List<HexagonCellPrototype>();
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                if (IsEdge(prototype, edgeCellType, assignToName, scopeToParentCell)) edgePrototypes.Add(prototype);
            }
            return edgePrototypes;
        }

        private static bool IsEdge(HexagonCellPrototype prototype, EdgeCellType edgeCellType, bool assignToName, bool scopeToParentCell = false)
        {
            if (prototype == null) return false;

            List<HexagonCellPrototype> allSideNeighbors = prototype.neighbors.FindAll(c => c.layer == prototype.layer);
            // bool isConnectorCell = allSideNeighbors.Find(n => n.GetParenCellId() != prototype.GetParenCellId());
            // if (scopeToParentCell) allSideNeighbors = allSideNeighbors.FindAll(n => n.GetParenCellId() == cell.GetParenCellId());

            int sideNeighborCount = allSideNeighbors.Count; //cell.GetSideNeighborCount(scopeToParentCell);
            int totalNeighborCount = prototype.neighbors.Count;
            bool isEdge = false;

            if (sideNeighborCount < 6)
            {
                prototype.isEdge = true;
                prototype._edgeCellType = edgeCellType;
                isEdge = true;
            }
            return isEdge;
        }

        public static void SmoothRampVertices(HexagonCellPrototype prototypeRamp, TerrainVertex[,] vertexGrid, int layerElevation = 4, float elevationStep = 0.45f)
        {
            if (prototypeRamp.rampSlopeSides == null || prototypeRamp.rampSlopeSides.Count == 0) return;

            int vertexGridLength = vertexGrid.GetLength(0);
            Vector2 centerPosXZ = new Vector2(prototypeRamp.center.x, prototypeRamp.center.z);

            float centerY = prototypeRamp.center.y - (elevationStep + 0.2f);
            float baseY = (prototypeRamp.center.y - layerElevation) + elevationStep;

            foreach (int index in prototypeRamp._vertexIndices)
            {
                vertexGrid[index / vertexGridLength, index % vertexGridLength].type = VertexType.Road;

                TerrainVertex currentVertex = vertexGrid[index / vertexGridLength, index % vertexGridLength];
                if (currentVertex.isCellCenterPoint)
                {
                    vertexGrid[index / vertexGridLength, index % vertexGridLength].position.y = centerY;
                }
            }
            foreach (int side in prototypeRamp.rampSlopeSides)
            {
                Vector2 sidePointPosXZ = new Vector2(prototypeRamp.sidePoints[side].x, prototypeRamp.sidePoints[side].z);

                // Debug.Log("prototypeRamp.rampSlopeSides - Side: " + side);

                prototypeRamp._vertexIndicesBySide[side] = prototypeRamp._vertexIndicesBySide[side].OrderByDescending(ix => Vector3.Distance(
                    vertexGrid[ix / vertexGridLength, ix % vertexGridLength].position,
                    prototypeRamp.center)
                ).ToList();

                List<int> vertexIndexes = prototypeRamp._vertexIndicesBySide[side];
                // List<TerrainVertex> sideVertices = new List<TerrainVertex>();

                int stepchange = 0;

                for (int i = 0; i < prototypeRamp._vertexIndicesBySide[side].Count; i++)
                {
                    int currentVertexIX = prototypeRamp._vertexIndicesBySide[side][i];
                    float slopeY;

                    if (i == 0)
                    {
                        slopeY = baseY;
                    }
                    else
                    {
                        int prevVertexIX = prototypeRamp._vertexIndicesBySide[side][i - 1];
                        TerrainVertex prevVertex = vertexGrid[prevVertexIX / vertexGridLength, prevVertexIX % vertexGridLength];
                        // Vector2 prevPosXZ = new Vector2(prevVertex.position.x, prevVertex.position.z);

                        // TerrainVertex currentVertex = vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength];
                        // Vector2 currentPosXZ = new Vector2(currentVertex.position.x, prevVertex.position.z);
                        if (stepchange > 2)
                        {
                            slopeY = prevVertex.position.y + elevationStep;
                            stepchange = 0;
                        }
                        else
                        {
                            slopeY = prevVertex.position.y;
                            stepchange++;
                        }
                        if (slopeY > centerY) slopeY = centerY;
                        // float slopeY = Mathf.Lerp(prevVertex.position.y, nextVertex.position.y, 0.03f);
                    }

                    vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength].position.y = slopeY;
                    vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength].type = VertexType.Road;
                }


                // foreach (int index in vertexIndexes)
                // {
                //     sideVertices.Add(vertexGrid[index / vertexGridLength, index % vertexGridLength]);
                //     TerrainVertex currentVertex = vertexGrid[index / vertexGridLength, index % vertexGridLength];

                //     Vector2 vertPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
                //     float edgeDistance = Vector2.Distance(vertPosXZ, sidePointPosXZ);
                //     float centerDistance = Vector2.Distance(vertPosXZ, centerPosXZ);
                //     float slopeY = baseY;

                //     float distanceMult = edgeDistance / centerDistance;
                //     // float distanceMult = edgeDistance / centerDistance;
                //     Debug.Log("edgeDistance: " + edgeDistance + ", centerDistance: " + centerDistance + ", distanceMult: " + distanceMult);

                //     if (edgeDistance < centerDistance)
                //     {
                //         slopeY = Mathf.Lerp(baseY, centerY, 0.5f);
                //     }
                //     else
                //     {
                //         slopeY = Mathf.Lerp(baseY, centerY - elevationStep, distanceMult);
                //     }

                //     vertexGrid[index / vertexGridLength, index % vertexGridLength].position.y = slopeY;
                //     vertexGrid[index / vertexGridLength, index % vertexGridLength].type = VertexType.Road;
                // }

                // Debug.Log("vertexGridLength: " + vertexGridLength + ",vertexGrid.GetLength(0) : " + vertexGrid.GetLength(0));
                // Debug.Log("vertexIndexes: " + prototypeRamp._vertexIndicesBySide[side].Count);


                // foreach (int index in prototypeRamp._vertexIndicesBySide[side])
                // {
                //     TerrainVertex currentVertex = vertexGrid[index / vertexGridLength, index % vertexGridLength];

                //     Debug.Log("vertexIndexes index: " + index + ", currentVertex.index" + currentVertex.index);

                //     sideVertices.Add(currentVertex);
                // }

                // sideVertices = sideVertices.OrderBy(v => Vector3.Distance(v.position, prototypeRamp.center)).ToList();

                // Debug.Log("vertexGridLength: " + vertexGridLength + ",vertexGrid.GetLength(0) : " + vertexGrid.GetLength(0) + ", sideVertices[0].index: " + sideVertices[0].index);

                // // vertexGrid[sideVertices[0].index / vertexGridLength, sideVertices[0].index % vertexGridLength].position.y = baseY;
                // // vertexGrid[sideVertices[0].index / vertexGridLength, sideVertices[0].index % vertexGridLength].type = VertexType.Road;

                // for (int i = 0; i < sideVertices.Count - 1; i++)
                // {
                //     int currentVertexIX = sideVertices[i].index;
                //     float slopeY;

                //     if (i == 0)
                //     {
                //         slopeY = baseY;
                //     }
                //     else
                //     {
                //         int prevVertexIX = sideVertices[i - 1].index;
                //         TerrainVertex prevVertex = vertexGrid[prevVertexIX / vertexGridLength, prevVertexIX % vertexGridLength];
                //         // Vector2 prevPosXZ = new Vector2(prevVertex.position.x, prevVertex.position.z);

                //         // TerrainVertex currentVertex = vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength];
                //         // Vector2 currentPosXZ = new Vector2(currentVertex.position.x, prevVertex.position.z);

                //         slopeY = prevVertex.position.y + elevationStep;
                //         // float slopeY = Mathf.Lerp(prevVertex.position.y, nextVertex.position.y, 0.03f);
                //     }

                //     vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength].position.y = slopeY;
                //     vertexGrid[currentVertexIX / vertexGridLength, currentVertexIX % vertexGridLength].type = VertexType.Road;
                // }
            }

        }

        public static void CleanupCellIslandLayerPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, int islandMemberMin = 3)
        {
            int totalLayers = prototypesByLayer.Keys.Count;
            int currentLayer = totalLayers - 1;
            do
            {
                List<HexagonCellPrototype> prototypesForLayer = prototypesByLayer[currentLayer];
                Dictionary<int, List<HexagonCellPrototype>> prototypesByIsland = GetIslandsFromLayerPrototypes(prototypesForLayer.FindAll(p => p.cellStatus == CellStatus.Ground));

                // Debug.Log("CleanupCellIslandLayerPrototypes - Layer: " + currentLayer + ", islands: " + prototypesByIsland.Keys.Count);

                bool isBottomLayer = currentLayer == 0;
                int layerTarget = isBottomLayer ? 1 : 0;

                foreach (var kvpB in prototypesByIsland)
                {
                    // Restore Ground assignment to layer above if island on base layer has only too few cells
                    if (kvpB.Value.Count >= islandMemberMin) continue;

                    // Debug.Log("CleanupCellIslandLayerPrototypes - Layer: " + currentLayer + ", islandMembers: " + kvpB.Value.Count);

                    for (var i = 0; i < kvpB.Value.Count; i++)
                    {
                        HexagonCellPrototype targetNeighbor = kvpB.Value[i].layerNeighbors[layerTarget];

                        if (targetNeighbor != null && kvpB.Value[i].cellStatus == CellStatus.Ground && targetNeighbor.neighbors.FindAll(n => n.layer == targetNeighbor.layer && n.cellStatus == CellStatus.Ground)?.Count > 0)
                        {
                            targetNeighbor.cellStatus = CellStatus.Ground;
                            kvpB.Value[i].cellStatus = isBottomLayer ? CellStatus.UnderGround : CellStatus.AboveGround;
                        }
                    }
                }
                currentLayer--;

            } while (currentLayer > -1);
        }

        public static void AssignRampsForIslandLayerPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, int layerElevation = 4, float elevationStep = 0.45f)
        {
            int rampsAssigned = 0;
            int totalLayers = prototypesByLayer.Keys.Count;

            foreach (var kvpA in prototypesByLayer)
            {
                if (kvpA.Key == 0) continue;

                List<HexagonCellPrototype> prototypesForLayer = kvpA.Value;
                Dictionary<int, List<HexagonCellPrototype>> prototypesByIsland = GetIslandsFromLayerPrototypes(prototypesForLayer.FindAll(p => p.cellStatus == CellStatus.Ground));
                // Debug.Log("AssignRampsForIslandLayerPrototypes - Layer: " + kvpA.Key + ", prototypeIslands: " + prototypesByIsland.Keys.Count);

                foreach (var kvpB in prototypesByIsland)
                {
                    bool isBottomLayer = (kvpA.Key == 0);
                    int layerTarget = isBottomLayer ? 1 : 0;

                    List<HexagonCellPrototype> possibleIslandRamps = kvpB.Value.FindAll(
                                p => p.layerNeighbors[layerTarget] != null && p.layerNeighbors[layerTarget].neighbors.Find(n => n.cellStatus == CellStatus.Ground && n.layer == p.layerNeighbors[layerTarget].layer) != null)
                                .OrderByDescending(c => c.neighbors.Count).ToList();

                    // Debug.Log("AssignRampsForIslandLayerPrototypes - Layer: " + kvpA.Key + ", island: " + kvpB.Key + ", possibleIslandRamps: " + possibleIslandRamps.Count);

                    if (possibleIslandRamps.Count > 0)
                    {
                        possibleIslandRamps[0].isGroundRamp = true;

                        if (isBottomLayer)
                        {
                            possibleIslandRamps[0].SetVertices(VertexType.Road, vertexGrid);
                        }
                        else
                        {
                            possibleIslandRamps[0].rampSlopeSides = new List<int>();
                            for (var side = 0; side < possibleIslandRamps[0].neighborsBySide.Length; side++)
                            {
                                HexagonCellPrototype sideNeighbor = possibleIslandRamps[0].neighborsBySide[side];

                                if (sideNeighbor != null && sideNeighbor.cellStatus == CellStatus.AboveGround)
                                {
                                    possibleIslandRamps[0].rampSlopeSides.Add(side);

                                }
                            }

                            SmoothRampVertices(possibleIslandRamps[0], vertexGrid, layerElevation, elevationStep);
                        }

                        rampsAssigned++;
                    }
                }

            }
            // Debug.Log("AssignRampsForIslandLayerPrototypes - rampsAssigned: " + rampsAssigned);
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GetIslandsFromLayerPrototypes(List<HexagonCellPrototype> prototypesForLayer)
        {
            Dictionary<int, List<HexagonCellPrototype>> clusters = new Dictionary<int, List<HexagonCellPrototype>>();
            HashSet<string> visited = new HashSet<string>();

            prototypesForLayer = prototypesForLayer.OrderBy(p => p.center.x).ThenBy(p => p.center.z).ToList();

            int clusterIndex = 0;
            // Debug.Log("GetClustersWithinDistance - prototypesForLayer: " + prototypesForLayer.Count);

            for (int i = 0; i < prototypesForLayer.Count; i++)
            {
                HexagonCellPrototype prototype = prototypesForLayer[i];

                if (!visited.Contains(prototype.id))
                {
                    List<HexagonCellPrototype> cluster = new List<HexagonCellPrototype>();

                    VisitNeighbors(prototype, cluster, visited, prototypesForLayer);

                    clusters.Add(clusterIndex, cluster);

                    clusterIndex++;
                }
            }
            // Debug.Log("GetClustersWithinDistance - clusterIndex: " + clusterIndex);
            return clusters;
        }

        private static void VisitNeighbors(HexagonCellPrototype prototype, List<HexagonCellPrototype> cluster, HashSet<string> visited, List<HexagonCellPrototype> prototypesForLayer)
        {
            cluster.Add(prototype);
            visited.Add(prototype.id);
            // Debug.Log("VisitNeighbors - prototype.neighbors: " + prototype.neighbors.Count);

            for (int i = 0; i < prototype.neighbors.Count; i++)
            {
                HexagonCellPrototype neighbor = prototype.neighbors[i];

                if (neighbor.layer != prototype.layer) continue;

                if (!visited.Contains(neighbor.id))
                {
                    if (prototypesForLayer.Contains(neighbor))
                    {
                        VisitNeighbors(neighbor, cluster, visited, prototypesForLayer);
                    }

                }
            }
        }

        public static void PopulateNeighborsFromCornerPoints(List<HexagonCellPrototype> cells, Transform transform, float offset = 0.33f)
        {
            for (int ixA = 0; ixA < cells.Count; ixA++)
            {
                HexagonCellPrototype cellA = cells[ixA];
                if (cellA.cellStatus == CellStatus.Remove) continue;

                for (int ixB = 0; ixB < cells.Count; ixB++)
                {
                    HexagonCellPrototype cellB = cells[ixB];
                    if (ixB == ixA || cellB.cellStatus == CellStatus.Remove || cellA.layer != cellB.layer) continue;

                    Vector3 cellPosA = transform.TransformVector(cellA.center);
                    Vector3 cellPosB = transform.TransformVector(cellB.center);

                    float distance = Vector3.Distance(cellPosA, cellPosB);
                    if (distance > cellA.size * neighborSearchCenterDistMult) continue;

                    if (distance < 1f)
                    {
                        cellB.cellStatus = CellStatus.Remove;
                        Debug.LogError("Duplicate Cells: " + cellA.id + ", uid: " + cellA.uid + ", and " + cellB.id + ", uid: " + cellB.uid + "\n total cells: " + cells.Count);
                        continue;
                    }

                    bool found = false;

                    for (int crIXA = 0; crIXA < cellA.cornerPoints.Length; crIXA++)
                    {
                        if (found) break;

                        Vector3 cornerA = transform.TransformVector(cellA.cornerPoints[crIXA]);

                        for (int crIXB = 0; crIXB < cellB.cornerPoints.Length; crIXB++)
                        {
                            Vector3 cornerB = transform.TransformVector(cellB.cornerPoints[crIXB]);

                            Vector2 posA = new Vector2(cornerA.x, cornerA.z);
                            Vector2 posB = new Vector2(cornerB.x, cornerB.z);

                            if (Vector2.Distance(posA, posB) <= offset)
                            {
                                if (cellA.neighbors.Contains(cellB) == false) cellA.neighbors.Add(cellB);
                                if (cellB.neighbors.Contains(cellA) == false) cellB.neighbors.Add(cellA);
                                found = true;
                                break;
                            }

                        }
                    }
                }
                // cellA.SetNeighborsBySide(offset);
            }
        }

        // public static void PopulateNeighborsFromCornerPoints(List<HexagonCellPrototype> cells, Transform transform, float offset = 0.33f)
        // {
        //     foreach (HexagonCellPrototype cell in cells)
        //     {
        //         if (cell.cellStatus == CellStatus.Remove) continue;

        //         //for each edgepoint on the current hexagontile
        //         for (int i = 0; i < cell.cornerPoints.Length; i++)
        //         {
        //             //loop through all the hexagontile to check for neighbors
        //             for (int j = 0; j < cells.Count; j++)
        //             {
        //                 //skip if the hexagontile is the current tile
        //                 if (cells[j] == cell || cells[j].layer != cell.layer)
        //                     continue;

        //                 Vector3 distA = transform.TransformVector(cell.center);
        //                 Vector3 distB = transform.TransformVector(cells[j].center);

        //                 float distance = Vector3.Distance(distA, distB);
        //                 if (distance > cell.size * neighborSearchCenterDistMult) continue;

        //                 if (distance < 1f)
        //                 {
        //                     cells[j].cellStatus = CellStatus.Remove;
        //                     Debug.LogError("Possible Duplicate Cells: " + cell.id + " and " + cells[j].id + ", removing the latter");
        //                     continue;
        //                 }


        //                 //loop through the cornerPoints of the neighboring tile
        //                 for (int k = 0; k < cells[j].cornerPoints.Length; k++)
        //                 {
        //                     // if (Vector3.Distance(cells[j].cornerPoints[k], cell.cornerPoints[i]) <= offset)
        //                     if (Vector2.Distance(new Vector2(cells[j].cornerPoints[k].x, cells[j].cornerPoints[k].z), new Vector2(cell.cornerPoints[i].x, cell.cornerPoints[i].z)) <= offset)
        //                     {
        //                         if (cell.neighbors.Contains(cells[j]) == false) cell.neighbors.Add(cells[j]);
        //                         if (cells[j].neighbors.Contains(cell) == false) cells[j].neighbors.Add(cell);
        //                         break;
        //                     }
        //                 }
        //             }
        //         }
        //         cell.SetNeighborsBySide(offset);
        //     }
        // }

        public static List<HexagonCellPrototype> PopulateNeighborsFromCornerPoints(List<HexagonCellPrototype> prototypes, float offset = 0.33f)
        {
            // Debug.Log("PopulateNeighborsFromCornerPoints: prototypes count: " + prototypes.Count);
            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            int neighborsAssigned = 0;
            int removed = 0;

            // foreach (HexagonCellPrototype prototype in prototypes)
            // {

            for (int ix = 0; ix < prototypes.Count; ix++)
            {
                HexagonCellPrototype prototype = prototypes[ix];

                if (prototype.cellStatus == CellStatus.Remove) continue;
                // if (prototype.neighbors.Count > 2)
                // {
                //     Debug.LogError("PopulateNeighborsFromCornerPoints - " + prototype.id + ", Pre check Neighbors " + prototype.neighbors.Count + "");
                //     foreach (var item in prototype.neighbors)
                //     {
                //         if (prototype.layerNeighbors.Contains(item) == false)
                //         {
                //             if (item == null)
                //             {
                //                 Debug.LogError("ERROR in prototype: " + prototype.id + " ,  neighbor is Null ");
                //             }
                //             else if (item.id == prototype.id) Debug.LogError("ERROR in prototype: " + prototype.id + " ,  neighbor has Same ID");
                //             else Debug.LogError("ERROR in prototype: " + prototype.id + " ,  neighbor: " + item.id + ", Layer: " + item.layer);
                //         }
                //     }
                // }
                results.Add(prototype);
                prototype.RecalculateEdgePoints();

                //for each edgepoint on the current hexagontile
                for (int i = 0; i < prototype.cornerPoints.Length; i++)
                {
                    //loop through all the hexagontile to check for neighbors
                    for (int j = 0; j < prototypes.Count; j++)
                    {
                        //skip if the hexagontile is the current tile
                        if ((prototypes[j].center == prototype.center) || (prototypes[j] == prototype) || (prototypes[j].layer != prototype.layer) || (prototypes[j].cellStatus == CellStatus.Remove))
                        {
                            if (prototype.neighbors.Contains(prototypes[j]))
                            {
                                prototype.neighbors.Remove(prototypes[j]);
                                Debug.LogError("prototype removed: " + prototypes[j].id + ", from " + prototype + " neighbors");
                            }
                            continue;
                        }

                        float distance = Vector3.Distance(prototype.center, prototypes[j].center);
                        if (distance > prototype.size * neighborSearchCenterDistMult) continue;

                        if (distance < 1f)
                        {
                            prototypes[j].cellStatus = CellStatus.Remove;
                            removed++;
                            Debug.LogError("Possible Duplicate Cells: " + prototype.id + " and " + prototypes[j].id + ", removing the latter");
                            continue;
                        }

                        //loop through the cornerPoints of the neighboring tile
                        for (int k = 0; k < prototypes[j].cornerPoints.Length; k++)
                        {
                            if (Vector2.Distance(new Vector2(prototypes[j].cornerPoints[k].x, prototypes[j].cornerPoints[k].z), new Vector2(prototype.cornerPoints[i].x, prototype.cornerPoints[i].z)) <= offset)
                            {
                                if (prototype.neighbors.Contains(prototypes[j]) == false) prototype.neighbors.Add(prototypes[j]);
                                if (prototypes[j].neighbors.Contains(prototype) == false) prototypes[j].neighbors.Add(prototype);

                                neighborsAssigned++;

                                break;
                            }
                        }
                    }
                }

                List<HexagonCellPrototype> allSideNeighbors = prototype.neighbors.FindAll(c => c.layer == prototype.layer);
                if (allSideNeighbors.Count > 7)
                {
                    foreach (var item in allSideNeighbors)
                    {
                        Debug.LogError(prototype.id + " - neighbor: " + item.id + ", distance: " + Vector3.Distance(prototype.center, item.center));
                    }
                }

                // prototype.SetNeighborsBySide(offset);
            }
            Debug.Log("PopulateNeighborsFromCornerPoints: neighborsAssigned: " + neighborsAssigned + ", Removed: " + removed);
            if (removed > 0) Debug.LogError("Removed " + removed + " Duplicate Cells");
            return results;
        }

        public static void AssignTerrainVerticesToGroundPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, float unassignedYOffset = -1f)
        {
            List<HexagonCellPrototype> allGroundPrototypes = new List<HexagonCellPrototype>();

            // Consolidate Ground Cells
            foreach (var kvp in prototypesByLayer)
            {
                List<HexagonCellPrototype> prototypes = kvp.Value;

                List<HexagonCellPrototype> groundPrototypes = prototypes.FindAll(p => p.cellStatus == CellStatus.Ground);
                foreach (HexagonCellPrototype prototype in groundPrototypes)
                {
                    prototype._vertexIndices = new List<int>();
                    prototype._vertexIndicesBySide = new List<int>[prototype.cornerPoints.Length];
                    for (int i = 0; i < prototype._vertexIndicesBySide.Length; i++)
                    {
                        prototype._vertexIndicesBySide[i] = new List<int>();
                    }
                }
                allGroundPrototypes.AddRange(groundPrototypes);
            }

            int verticeIndex = 0;
            foreach (TerrainVertex vertice in vertexGrid)
            {
                float closestDistance = float.MaxValue;
                HexagonCellPrototype closestCell = null;
                Vector2 vertexPosXZ = new Vector2(vertice.position.x, vertice.position.z);

                foreach (HexagonCellPrototype prototype in allGroundPrototypes)
                {
                    Vector2 currentPosXZ = new Vector2(prototype.center.x, prototype.center.z);

                    float distance = Vector2.Distance(vertexPosXZ, currentPosXZ);
                    if (distance < closestDistance)
                    {
                        Vector3[] prototypeCorners = prototype.cornerPoints;
                        float xMin = prototypeCorners[0].x;
                        float xMax = prototypeCorners[0].x;
                        float zMin = prototypeCorners[0].z;
                        float zMax = prototypeCorners[0].z;
                        for (int i = 1; i < prototypeCorners.Length; i++)
                        {
                            if (prototypeCorners[i].x < xMin)
                                xMin = prototypeCorners[i].x;
                            if (prototypeCorners[i].x > xMax)
                                xMax = prototypeCorners[i].x;
                            if (prototypeCorners[i].z < zMin)
                                zMin = prototypeCorners[i].z;
                            if (prototypeCorners[i].z > zMax)
                                zMax = prototypeCorners[i].z;
                        }

                        if (vertice.position.x >= xMin && vertice.position.x <= xMax && vertice.position.z >= zMin && vertice.position.z <= zMax)
                        {
                            closestDistance = distance;
                            closestCell = prototype;
                        }
                    }
                }

                int indexX = verticeIndex / vertexGrid.GetLength(0);
                int indexZ = verticeIndex % vertexGrid.GetLength(0);

                if (closestCell != null)
                {
                    closestCell._vertexIndices.Add(verticeIndex);

                    bool isCellCenterVertex = false;
                    if (closestDistance < closestCell.size * 0.33f)
                    {
                        isCellCenterVertex = true;
                    }
                    else
                    {
                        // Get Closest Corner if not within center radius   
                        (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = ProceduralTerrainUtility.
                                                            GetClosestPoint(closestCell.cornerPoints, vertexPosXZ);

                        if (nearestDistance != float.MaxValue)
                        {
                            HexagonSide side = HexagonCell.GetSideFromCorner((HexagonCorner)nearestIndex);
                            closestCell._vertexIndicesBySide[(int)side].Add(verticeIndex);

                            vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].corner = nearestIndex;
                            vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].isCellCornerPoint = true;
                        }
                    }

                    float pathCellYOffset = 0f;
                    // if (closestCell.isLeveledRampCell)
                    // {
                    //     pathCellYOffset = 2f;
                    // }
                    // else if (closestCell.isPathCell || closestCell.isEntryCell)
                    // {
                    //     pathCellYOffset = closestCell.GetGridLayer() == 0 ? 0.3f : -0.8f;
                    // }
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].position = new Vector3(vertice.position.x, closestCell.center.y + pathCellYOffset, vertice.position.z);
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].isCellCenterPoint = isCellCenterVertex;
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].type = VertexType.Cell;
                }
                else
                {
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].position = new Vector3(vertice.position.x, vertice.position.y + unassignedYOffset, vertice.position.z);
                }
                verticeIndex++;
            }
        }
        public static void AssignTerrainVerticesToPrototypes(List<HexagonCellPrototype> prototypes, TerrainVertex[,] vertexGrid, bool updateElevation = true, float unassignedYOffset = -1f)
        {
            int verticeIndex = 0;
            foreach (TerrainVertex vertice in vertexGrid)
            {
                float closestDistance = float.MaxValue;
                HexagonCellPrototype closestCell = null;
                Vector2 vertexPosXZ = new Vector2(vertice.position.x, vertice.position.z);

                foreach (HexagonCellPrototype prototype in prototypes)
                {
                    Vector2 currentPosXZ = new Vector2(prototype.center.x, prototype.center.z);

                    float distance = Vector2.Distance(vertexPosXZ, currentPosXZ);
                    if (distance < closestDistance)
                    {
                        Vector3[] prototypeCorners = prototype.cornerPoints;
                        float xMin = prototypeCorners[0].x;
                        float xMax = prototypeCorners[0].x;
                        float zMin = prototypeCorners[0].z;
                        float zMax = prototypeCorners[0].z;
                        for (int i = 1; i < prototypeCorners.Length; i++)
                        {
                            if (prototypeCorners[i].x < xMin)
                                xMin = prototypeCorners[i].x;
                            if (prototypeCorners[i].x > xMax)
                                xMax = prototypeCorners[i].x;
                            if (prototypeCorners[i].z < zMin)
                                zMin = prototypeCorners[i].z;
                            if (prototypeCorners[i].z > zMax)
                                zMax = prototypeCorners[i].z;
                        }

                        if (vertice.position.x >= xMin && vertice.position.x <= xMax && vertice.position.z >= zMin && vertice.position.z <= zMax)
                        {
                            closestDistance = distance;
                            closestCell = prototype;
                        }
                    }
                }

                int indexX = verticeIndex / vertexGrid.GetLength(0);
                int indexZ = verticeIndex % vertexGrid.GetLength(0);

                if (closestCell != null)
                {
                    closestCell._vertexIndices.Add(verticeIndex);

                    bool isCellCenterVertex = false;
                    if (closestDistance < closestCell.size * vertexCenterPointDistance)
                    {
                        isCellCenterVertex = true;
                    }
                    else
                    {
                        // Get Closest Corner if not within center radius   
                        (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = ProceduralTerrainUtility.
                                                            GetClosestPoint(closestCell.cornerPoints, vertexPosXZ);

                        if (nearestDistance != float.MaxValue)
                        {
                            HexagonSide side = HexagonCell.GetSideFromCorner((HexagonCorner)nearestIndex);

                            if (closestCell._vertexIndicesBySide == null) closestCell._vertexIndicesBySide = new List<int>[6] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };

                            // if (closestCell._vertexIndicesBySide[(int)side] == null)
                            // {
                            //     for (int i = 0; i < closestCell._vertexIndicesBySide.Length; i++)
                            //     {
                            //         closestCell._vertexIndicesBySide[i] = new List<int>();
                            //     }
                            // }
                            closestCell._vertexIndicesBySide[(int)side].Add(verticeIndex);

                            vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].corner = nearestIndex;
                            vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].isCellCornerPoint = true;
                        }
                    }

                    if (updateElevation) vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].position = new Vector3(vertice.position.x, closestCell.center.y, vertice.position.z);

                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].isCellCenterPoint = isCellCenterVertex;
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].type = VertexType.Cell;
                }
                else
                {
                    vertexGrid[verticeIndex / vertexGrid.GetLength(0), verticeIndex % vertexGrid.GetLength(0)].position = new Vector3(vertice.position.x, vertice.position.y + unassignedYOffset, vertice.position.z);
                }
                verticeIndex++;
            }
        }



        public static void GroundPrototypesToTerrainVertexElevation(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, float distanceYOffset = 0.6f, bool fallbackOnBottomCell = false)
        {
            List<HexagonCellPrototype> topLayerPrototypes = prototypesByLayer[prototypesByLayer.Count - 1];

            foreach (HexagonCellPrototype prototypeCell in topLayerPrototypes)
            {
                if (prototypeCell._vertexIndices != null && prototypeCell._vertexIndices.Count > 0)
                {
                    Debug.Log("GroundPrototypesToTerrainVertexElevation - A");
                    ClearLayersAboveVertexElevationsAndSetGround(prototypeCell, prototypeCell._vertexIndices, prototypeCell._vertexIndicesBySide, vertexGrid, distanceYOffset, fallbackOnBottomCell);
                    continue;
                }

                Debug.Log("GroundPrototypesToTerrainVertexElevation - B");

                Vector2 currentPosXZ = new Vector2(prototypeCell.center.x, prototypeCell.center.z);

                // Get closest vertex
                float closestDistance = float.MaxValue;
                TerrainVertex closestVertex = vertexGrid[0, 0];

                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        TerrainVertex currentVertex = vertexGrid[x, z];

                        Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);

                        float dist = Vector2.Distance(currentPosXZ, vertexPosXZ);
                        if (dist < closestDistance)
                        {
                            // Debug.Log("currentVertex - elevationY:" + currentVertex.position.y);
                            closestDistance = dist;
                            closestVertex = currentVertex;
                        }

                    }
                }

                if (closestDistance != float.MaxValue) ClearLayersAboveElevationAndSetGround(prototypeCell, closestVertex.position.y, distanceYOffset, fallbackOnBottomCell);
            }
        }

        public static bool IsPrototypeCenterBelowAllVertices(HexagonCellPrototype prototype, List<int> vertexIndices, TerrainVertex[,] vertexGrid, float distanceYOffset = 1.8f)
        {
            if (vertexIndices == null || vertexIndices.Count == 0) return false;
            Debug.Log("IsPrototypeCenterBelowAllVertices - A");
            bool passed = true;
            foreach (int ix in vertexIndices)
            {
                if ((prototype.center.y - distanceYOffset) <= vertexGrid[ix / vertexGrid.GetLength(0), ix % vertexGrid.GetLength(0)].position.y)
                {
                }
                else
                {
                    return false;
                }

            }
            Debug.Log("IsPrototypeCenterBelowAllVertices - passed");

            return passed;
            // return vertexIndices.All(ix => (prototype.center.y - distanceYOffset) <= vertexGrid[ix / vertexGrid.GetLength(0), ix % vertexGrid.GetLength(0)].position.y);
        }

        public static void ClearLayersAboveVertexElevationsAndSetGround(HexagonCellPrototype prototypeCell, List<int> vertexIndices, List<int>[] vertexIndicesBySide, TerrainVertex[,] vertexGrid, float distanceYOffset = 1.8f, bool fallbackOnBottomCell = false)
        {
            // Set every cell below as underground
            HexagonCellPrototype currentPrototype = prototypeCell;
            bool groundFound = false;
            while (groundFound == false && currentPrototype != null)
            {
                if (IsPrototypeCenterBelowAllVertices(currentPrototype, vertexIndices, vertexGrid, distanceYOffset) == false)
                {
                    Debug.Log("ClearLayersAboveVertexElevationsAndSetGround - A");

                    if (fallbackOnBottomCell && currentPrototype.layerNeighbors[0] == null)
                    {
                        currentPrototype.cellStatus = CellStatus.Ground;
                        currentPrototype._vertexIndices = vertexIndices;
                        currentPrototype._vertexIndicesBySide = vertexIndicesBySide;
                    }
                    else
                    {
                        currentPrototype.cellStatus = CellStatus.AboveGround;

                        currentPrototype = currentPrototype.layerNeighbors[0];
                    }
                }
                else
                {
                    Debug.Log("ClearLayersAboveVertexElevationsAndSetGround - B");

                    groundFound = true;
                    currentPrototype._vertexIndices = vertexIndices;
                    currentPrototype._vertexIndicesBySide = vertexIndicesBySide;
                    SetToGroundLevel(currentPrototype);
                }
            }
        }

        public static void ClearLayersAboveElevationAndSetGround(HexagonCellPrototype prototypeCell, float elevationY, float distanceYOffset = 1.8f, bool fallbackOnBottomCell = false)
        {
            // Debug.Log("ClearLayersAboveElevationAndSetGround - elevationY:" + elevationY);
            // Set every cell below as underground
            HexagonCellPrototype currentPrototype = prototypeCell;
            bool groundFound = false;

            while (groundFound == false && currentPrototype != null)
            {
                if ((currentPrototype.center.y - distanceYOffset) > elevationY)
                {
                    currentPrototype.cellStatus = CellStatus.AboveGround;

                    if (fallbackOnBottomCell && currentPrototype.layerNeighbors[0] == null)
                    {
                        currentPrototype.cellStatus = CellStatus.Ground;
                    }
                    else
                    {
                        currentPrototype = currentPrototype.layerNeighbors[0];
                    }
                }
                else
                {
                    groundFound = true;
                    SetToGroundLevel(currentPrototype);
                }
            }
        }

        public static void SetToGroundLevel(HexagonCellPrototype prototypeCell)
        {
            // Set as ground cell
            prototypeCell.cellStatus = CellStatus.Ground;
            // Set every cell below as underground
            HexagonCellPrototype bottomNeighbor = prototypeCell.layerNeighbors[0];
            while (bottomNeighbor != null)
            {
                bottomNeighbor.cellStatus = CellStatus.UnderGround;
                bottomNeighbor = bottomNeighbor.layerNeighbors[0];
            }
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateGridsByLayer(Vector3 centerPos, float radius, int cellSize, int cellLayers, int cellLayerElevation, IHexCell parentCell, int baseLayerOffset = 0, Transform transform = null, bool useCorners = false)
        {
            int startingLayer = 0 + baseLayerOffset;
            List<HexagonCellPrototype> newCellPrototypes = GenerateHexGrid(centerPos, cellSize, (int)radius, parentCell, transform);
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            // // TEMP
            // if (useCorners && transform != null || parentCell != null)
            // {
            //     Transform tran = transform ? transform : parentCell.gameObject.transform;
            //     Vector3[] corners = HexagonGenerator.GenerateHexagonPoints(tran.position, 12);

            //     List<HexagonCellPrototype> cornerPrototypesByLayer = GeneratePrototypesAtPoints(corners, cellSize, parentCell);
            //     newPrototypesByLayer[startingLayer].AddRange(cornerPrototypesByLayer);
            // }

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonCellPrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateGridToNewLayerAbove(newPrototypesByLayer[startingLayer], cellLayerElevation, i, parentCell);
                    }
                    else
                    {
                        List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateGridToNewLayerAbove(previousLayer, cellLayerElevation, i, parentCell);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                }
            }
            return newPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateGridsByLayer(Vector3 centerPos, float radius, int cellSize, int cellLayers, int cellLayerElevation, Vector2 gridGenerationCenterPosXZOffeset, int baseLayerOffset, string parentId = "", Transform transform = null, bool useCorners = true)
        {
            List<HexagonCellPrototype> newCellPrototypes = GenerateHexGrid(centerPos, cellSize, (int)radius, null);
            // List<HexagonCellPrototype> newCellPrototypes = GetPrototypesWithinXZRadius(
            //                             GenerateGridWithinRadius(
            //                                     centerPos,
            //                                     radius,
            //                                     cellSize, parentCell),
            //                                     centerPos,
            //                                     radius);

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            int startingLayer = 0 + baseLayerOffset;
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            // // TEMP
            // if (useCorners && transform != null || parentCell != null)
            // {
            //     Transform tran = transform ? transform : parentCell.gameObject.transform;
            //     Vector3[] corners = HexagonGenerator.GenerateHexagonPoints(tran.position, 12);

            //     List<HexagonCellPrototype> cornerPrototypesByLayer = GeneratePrototypesAtPoints(corners, cellSize, parentCell);
            //     newPrototypesByLayer[startingLayer].AddRange(cornerPrototypesByLayer);
            // }

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonCellPrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateGridToNewLayerAbove(newPrototypesByLayer[startingLayer], cellLayerElevation, i, null);
                    }
                    else
                    {
                        List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateGridToNewLayerAbove(previousLayer, cellLayerElevation, i, null);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                }
            }

            return newPrototypesByLayer;
        }

        public static List<HexagonCellPrototype> GeneratePrototypesAtPoints(Vector3[] centerPoints, int cellSize, HexagonCell parentCell = null)
        {
            List<HexagonCellPrototype> prototypes = new List<HexagonCellPrototype>();
            for (var i = 0; i < centerPoints.Length; i++)
            {
                HexagonCellPrototype prototype = new HexagonCellPrototype(
                    centerPoints[i],
                    cellSize
                );

                int idFragment = Mathf.Abs((int)(centerPoints[i].z + centerPoints[i].x));

                if (parentCell != null)
                {
                    prototype.parentId = parentCell.id;
                    prototype.id = "p_" + parentCell.id + "-";
                }
                prototype.id += "X" + cellSize + "-" + idFragment + "-" + i;
                prototype.name = "Cell_Prototype-" + prototype.id;

                prototypes.Add(prototype);
            }
            return prototypes;
        }
        public static List<HexagonCellPrototype> GenerateHexGrid(Vector3 center, int size, int radius, IHexCell parentCell = null, Transform transform = null, bool filterOutCorners = true)
        {
            Dictionary<HexagonCellPrototype, List<Vector3>> quadrantCenterPoints = new Dictionary<HexagonCellPrototype, List<Vector3>>();
            List<Vector3> spawnCenters = new List<Vector3>();
            List<Vector3> quatCenterPoints = new List<Vector3>();
            List<int> quadrantSizes = new List<int>();

            if (transform != null) center = transform.TransformVector(center);

            int prevStepSize = radius;
            int currentStepSize = radius;
            while (size < currentStepSize)
            {
                currentStepSize = (prevStepSize / 3);

                List<Vector3> newCenterPoints = new List<Vector3>();
                if (quatCenterPoints.Count == 0)
                {
                    newCenterPoints = GenerateHexagonCenterPoints(center, currentStepSize, true, currentStepSize > size);
                }
                else
                {
                    foreach (Vector3 centerPoint in quatCenterPoints)
                    {
                        HexagonCellPrototype quadrantPrototype = new HexagonCellPrototype(centerPoint, prevStepSize);
                        List<Vector3> points = GenerateHexagonCenterPoints(centerPoint, currentStepSize, true, true);
                        newCenterPoints.AddRange(points);
                        // Debug.Log("newCenterPoints: " + newCenterPoints.Count + ", size" + currentStepSize);

                        quadrantCenterPoints.Add(quadrantPrototype, points);
                    }
                }
                Debug.Log("newCenterPoints: " + newCenterPoints.Count + ", currentStepSize" + currentStepSize + ", desired size: " + size);

                prevStepSize = currentStepSize;

                if (currentStepSize <= size)
                {
                    spawnCenters.AddRange(newCenterPoints);
                    break;
                }
                else
                {
                    quatCenterPoints.Clear();
                    quatCenterPoints.AddRange(newCenterPoints);
                    Debug.Log("Quadrants of size " + currentStepSize + ": " + quatCenterPoints.Count);
                }
            }

            // int targetSize = radius;
            // int prevTargetSize;
            // bool done = false;

            // while (!done)
            // {
            //     int quadrantSize = (targetSize / 3);
            //     if (size > quadrantSize)
            //     {
            //         done = true;
            //     }
            //     else
            //     {
            //         prevTargetSize = targetSize;
            //         targetSize = quadrantSize;
            //         quadrantSizes.Add(quadrantSize);

            //         List<Vector3> newCenterPoints = new List<Vector3>();
            //         if (quatCenterPoints.Count == 0)
            //         {
            //             newCenterPoints = GenerateHexagonCenterPoints(center, quadrantSize);
            //             quatCenterPoints.AddRange(newCenterPoints);
            //         }
            //         else
            //         {
            //             Debug.Log("Quadrants of size " + prevTargetSize + ": " + quatCenterPoints.Count);

            //             foreach (Vector3 centerPoint in quatCenterPoints)
            //             {
            //                 HexagonCellPrototype quadrantPrototype = new HexagonCellPrototype(centerPoint, prevTargetSize);

            //                 List<Vector3> points = GenerateHexagonCenterPoints(centerPoint, quadrantSize);
            //                 quadrantCenterPoints.Add(quadrantPrototype, points);
            //                 newCenterPoints.AddRange(points);
            //             }
            //             quatCenterPoints = newCenterPoints;
            //         }
            //     }
            // }

            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            Vector2 baseCenterPosXZ = new Vector2(center.x, center.z);

            int skipped = 0;
            for (int i = 0; i < spawnCenters.Count; i++)
            {
                Vector3 centerPoint = transform != null ? transform.TransformVector(spawnCenters[i]) : spawnCenters[i];

                // Filter out duplicate points & out of bounds
                float distance = Vector2.Distance(new Vector2(centerPoint.x, centerPoint.z), baseCenterPosXZ);
                if (distance < radius)
                {
                    bool skip = false;
                    foreach (HexagonCellPrototype item in results)
                    {
                        // if (Vector2.Distance(new Vector2(centerPoint.x, centerPoint.z), new Vector2(item.center.x, item.center.z)) < 1f)
                        if (Vector3.Distance(centerPoint, item.center) < 1f)
                        {
                            skip = true;
                            skipped++;
                            break;
                        }
                    }
                    if (!skip) results.Add(new HexagonCellPrototype(centerPoint, size, parentCell, "-" + i));
                }
            }

            HexagonCellPrototype parentHex = new HexagonCellPrototype(center, radius);
            results = GetPrototypesWithinHexagon(results, center, radius, parentHex.GetEdgePoints());
            if (parentCell != null)
            {
                Debug.Log("GenerateHexGrid - results: " + results.Count + ", size: " + size + ", parentCell: " + parentCell.GetId());
                if (skipped > 0) Debug.LogError("Skipped: " + skipped + ", parentCell: " + parentCell.GetId() + ",  size: " + size);

                if (transform != null)
                {
                    Debug.Log("GenerateHexGrid - parent: " + parentCell.GetId() + ", position: " + parentCell.GetPosition() + ",  center: " + center + ", transformed Pos: " + transform.InverseTransformVector(parentCell.GetPosition()));
                }
                if (size == 4)
                {
                    foreach (var item in results)
                    {
                        Debug.Log("GenerateHexGrid - parent: " + parentCell.GetId() + ", result: " + item.id);
                    }
                }
            }
            else
            {
                Debug.Log("GenerateHexGrid - results: " + results.Count + ", size: " + size);
                if (skipped > 0) Debug.LogError("Skipped: " + skipped + ", size: " + size);

                if (size == 12)
                {
                    foreach (var item in results)
                    {
                        Debug.Log("GenerateHexGrid - X12, result - id: " + item.id + ", uid: " + item.uid);
                    }
                }
            }

            return results;
        }

        public static List<Vector3> GenerateHexagonCenterPoints(Vector3 center, int size, bool addStartingCenter = true, bool useCorners = false)
        {
            HexagonCellPrototype centerHex = new HexagonCellPrototype(center, size);
            List<Vector3> results = new List<Vector3>();
            if (addStartingCenter) results.Add(center);

            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(centerHex.cornerPoints[i], centerHex.cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                int currentSide = (i + 5) % 6;

                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add(sidePoint);

                if (useCorners)   // Get Corner
                {
                    float angle = 60f * i;
                    float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                    float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                    Vector3 cornerPoint = new Vector3(x, center.y, z);
                    direction = (cornerPoint - center).normalized;
                    edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                    cornerPoint = center + direction * (edgeDistance * 3f);
                    results.Add(cornerPoint);
                }
            }

            return results;
        }


        public static List<HexagonCellPrototype> GenerateGrid(int hexagonSize, int numHexagons, int numRows, Vector3 startPos, float adjusterMult = 1.734f, string appendToId = "", HexagonCell parentCell = null)
        {
            List<HexagonCellPrototype> newPrototypes = new List<HexagonCellPrototype>();
            float angle = 60 * Mathf.Deg2Rad;
            float currentX = startPos.x;
            float currentZ = startPos.z;
            float lastZ = startPos.z;

            int idFragment = Mathf.Abs((int)(startPos.z + startPos.x));

            for (int k = 0; k < numRows; k++)
            {
                currentX = startPos.x;
                for (int i = 0; i < numHexagons; i++)
                {
                    float adjusterMultB = 0.88f;

                    Vector3[] hexagonPoints = new Vector3[6];
                    if (i % 2 == 1)
                    {
                        currentZ -= hexagonSize * adjusterMultB;
                    }
                    else
                    {
                        currentZ += hexagonSize * adjusterMultB;
                    }

                    for (int j = 0; j < 6; j++)
                    {
                        float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                        float z = currentZ + hexagonSize * Mathf.Sin(angle * j);

                        hexagonPoints[j] = (new Vector3(x, startPos.y, z));

                        if (j == 2) lastZ = z;
                    }

                    HexagonCellPrototype prototype = new HexagonCellPrototype(
                        HexagonGenerator.GetPolygonCenter(hexagonPoints),
                        hexagonSize
                    );
                    // HexagonCellPrototype prototype = new HexagonCellPrototype(
                    //     HexagonGenerator.GetPolygonCenter(hexagonPoints),
                    //     hexagonPoints,
                    //     hexagonSize
                    // );

                    if (parentCell != null)
                    {
                        prototype.parentId = parentCell.id;
                        prototype.id = "p_" + parentCell.id + "-";
                    }

                    prototype.id += "X" + hexagonSize + "-" + idFragment + "-" + k + i;
                    prototype.name = "Cell_Prototype-" + appendToId + prototype.id;

                    newPrototypes.Add(prototype);

                    currentX += hexagonSize * 1.5f;
                }
                currentZ += hexagonSize * adjusterMult;
            }
            return newPrototypes;
        }

        public static void RemoveExcessPrototypesByDistance(List<HexagonCellPrototype> prototypes, float distanceThreshold = 0.5f)
        {
            prototypes.Sort((a, b) => Vector3.Distance(a.center, Vector3.zero).CompareTo(Vector3.Distance(b.center, Vector3.zero)));
            int removed = 0;

            for (int i = prototypes.Count - 1; i >= 0; i--)
            {
                HexagonCellPrototype current = prototypes[i];

                for (int j = i - 1; j >= 0; j--)
                {
                    HexagonCellPrototype other = prototypes[j];

                    if (Vector3.Distance(current.center, other.center) < distanceThreshold)
                    {
                        prototypes.RemoveAt(j);
                        removed++;
                    }
                }
            }
            Debug.Log("RemoveExcessPrototypesByDistance: removed: " + removed);
        }

        public static List<HexagonCellPrototype> DuplicateGridToNewLayerAbove(List<HexagonCellPrototype> prototypes, int layerElevation, int layer, IHexCell parentCell)
        {
            List<HexagonCellPrototype> newPrototypes = new List<HexagonCellPrototype>();
            foreach (var prototype in prototypes)
            {


                Vector3 newCenterPos = new Vector3(prototype.center.x, prototype.center.y + layerElevation, prototype.center.z);
                HexagonCellPrototype newPrototype = new HexagonCellPrototype(newCenterPos, prototype.size, parentCell, "", layer);

                // HexagonCellPrototype newPrototype = new HexagonCellPrototype(
                //     new Vector3(prototype.center.x, prototype.center.y + layerElevation, prototype.center.z),
                //     prototype.cornerPoints.Select(c => new Vector3(c.x, c.y + layerElevation, c.z)).ToArray(),
                //     prototype.size
                // );
                // newPrototype.id = prototype.id + layer;
                // newPrototype.parentId = prototype.parentId;
                // newPrototype.name = "Cell_Prototype-" + prototype.id + "-L" + layer;

                // newPrototype.layer = layer;
                newPrototype.bottomNeighborId = prototype.id;

                // Set layer neighbors
                newPrototype.layerNeighbors[0] = prototype;
                if (newPrototype.neighbors.Contains(prototype) == false) newPrototype.neighbors.Add(prototype);

                prototype.layerNeighbors[1] = newPrototype;
                if (prototype.neighbors.Contains(newPrototype) == false) prototype.neighbors.Add(newPrototype);

                // Debug.Log("newPrototype - size: " + newPrototype.size + ", neighbors: " + newPrototype.neighbors.Count);

                newPrototypes.Add(newPrototype);
            }
            return newPrototypes;
        }

        public static List<HexagonCellPrototype> GenerateGridWithinRadius(Vector3 position, float radius, int hexagonSize, float adjusterMult = 1.734f, string appendToId = "")
        {
            float bottomCornerX = position.x - (radius * 0.9f);
            float bottomCornerZ = position.z - (radius * 0.9f);
            Vector3 bottomCorner = new Vector3(bottomCornerX, position.y, bottomCornerZ);

            int numHexagons = (int)((radius * 2.2f) / (hexagonSize * 1.5f));
            float numRowsf = ((radius * 2f) / (hexagonSize * adjusterMult));
            int numRows = Mathf.CeilToInt(numRowsf);
            List<HexagonCellPrototype> newPrototypes = GenerateGrid(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId);
            // List<HexagonCellPrototype> newPrototypes = GenerateGrid_MT(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId);
            return newPrototypes;
        }

        public static List<HexagonCellPrototype> GenerateGridWithinRadius(Vector3 position, float radius, int hexagonSize, HexagonCell parentCell, float adjusterMult = 1.734f, string appendToId = "")
        {
            float bottomCornerX = position.x - (radius * 0.9f);
            float bottomCornerZ = position.z - (radius * 0.9f);
            Vector3 bottomCorner = new Vector3(bottomCornerX, position.y, bottomCornerZ);

            int numHexagons = (int)((radius * 2.2f) / (hexagonSize * 1.5f));
            float numRowsf = ((radius * 2f) / (hexagonSize * adjusterMult));
            int numRows = Mathf.CeilToInt(numRowsf);
            List<HexagonCellPrototype> newPrototypes = GenerateGrid(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId, parentCell);
            // List<HexagonCellPrototype> newPrototypes = GenerateGrid_MT(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId, parentCell);
            return newPrototypes;
        }



        public static List<HexagonCellPrototype> GetPrototypesWithinHexagon(List<HexagonCellPrototype> prototypes, Vector3 position, float radius, List<Vector3> hexEdgePoints)
        {
            List<HexagonCellPrototype> prototypesWithinRadius = new List<HexagonCellPrototype>();
            Vector2 posXZ = new Vector2(position.x, position.z);
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in prototype.cornerPoints)
                {
                    float distance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), posXZ);
                    if (distance < radius * 0.95f) continue;

                    isWithinRadius = IsPositionWithinPolygon(hexEdgePoints, cornerPoint);
                    if (!isWithinRadius) break;
                }
                if (isWithinRadius) prototypesWithinRadius.Add(prototype);
            }
            return prototypesWithinRadius;
        }

        public static bool IsPositionWithinPolygon(List<Vector3> points, Vector3 position)
        {
            int numVertices = points.Count;
            bool isInside = false;

            for (int i = 0, j = numVertices - 1; i < numVertices; j = i++)
            {
                if (((points[i].z <= position.z && position.z < points[j].z) ||
                     (points[j].z <= position.z && position.z < points[i].z)) &&
                    (position.x < (points[j].x - points[i].x) * (position.z - points[i].z) / (points[j].z - points[i].z) + points[i].x))
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        public static List<HexagonCellPrototype> GetPrototypesWithinXZRadius(List<HexagonCellPrototype> prototypes, Vector3 position, float radius)
        {
            List<HexagonCellPrototype> prototypesWithinRadius = new List<HexagonCellPrototype>();
            Vector2 posXZ = new Vector2(position.x, position.z);
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in prototype.cornerPoints)
                {
                    float distance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), posXZ);
                    if (distance > radius)
                    {
                        isWithinRadius = false;
                        break;
                    }
                }
                if (isWithinRadius) prototypesWithinRadius.Add(prototype);
            }
            return prototypesWithinRadius;
        }

        public static List<HexagonCellPrototype> GetPrototypesWithinRadius(List<HexagonCellPrototype> prototypes, Vector3 position, float radius)
        {
            List<HexagonCellPrototype> prototypesWithinRadius = new List<HexagonCellPrototype>();
            foreach (HexagonCellPrototype tile in prototypes)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in tile.cornerPoints)
                {
                    float distance = Vector3.Distance(cornerPoint, position);
                    if (distance > radius)
                    {
                        isWithinRadius = false;
                        break;
                    }
                }
                if (isWithinRadius)
                {
                    prototypesWithinRadius.Add(tile);
                }
            }
            return prototypesWithinRadius;
        }

        public static void DrawHexagonCellPrototype(HexagonCellPrototype prototype, float centerRadius, float cornerRadius = 0.25f)
        {
            Gizmos.DrawSphere(prototype.center, centerRadius);
            for (int j = 0; j < prototype.cornerPoints.Length; j++)
            {
                Gizmos.DrawSphere(prototype.cornerPoints[j], 0.25f);
            }
            Gizmos.color = Color.gray;
            for (int j = 0; j < prototype.sidePoints.Length; j++)
            {
                Gizmos.DrawSphere(prototype.sidePoints[j], 0.3f);
            }
            Gizmos.color = Color.black;
            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(prototype.cornerPoints);
        }

        public static void DrawHexagonCellPrototypes(List<HexagonCellPrototype> prototypes, Transform transform, GridFilter_Type filterType)
        {
            Color brown = new Color(0.4f, 0.2f, 0f);
            Color orange = new Color(1f, 0.5f, 0f);
            Color purple = new Color(0.8f, 0.2f, 1f);

            bool showAll = filterType == GridFilter_Type.All;

            foreach (HexagonCellPrototype item in prototypes)
            {
                Vector3 pointPos = item.center;

                if (filterType != GridFilter_Type.None)
                {
                    bool show = false;
                    // float rad = 4f;
                    // Gizmos.color = Color.black;

                    if ((showAll || filterType == GridFilter_Type.Entrance) && item.isEntry)
                    {
                        Gizmos.color = purple;
                        show = true;
                    }
                    else if ((showAll || filterType == GridFilter_Type.Edge) && item.isEdge) // item._edgeCellType > 0)
                    {
                        Gizmos.color = Color.red;
                        show = true;
                    }
                    else if ((showAll || filterType == GridFilter_Type.Path) && item.isPath)
                    {
                        Gizmos.color = (item.cellStatus != CellStatus.Ground) ? orange : Color.cyan;
                        show = true;
                    }
                    else if ((showAll || filterType == GridFilter_Type.Ground) && item.cellStatus == CellStatus.Ground)
                    {
                        Gizmos.color = brown;
                        show = true;
                    }
                    if (showAll || show) Gizmos.DrawSphere(item.center, item.size == 4 ? 2f : (item.size / 3f));

                    // if (item.neighbors.Count > 0)
                    // {
                    //     Gizmos.color = Color.green;
                    //     Gizmos.DrawSphere(item.center, 4);
                    // }
                    // if (item.neighbors.Count > 5)
                    // {
                    //     List<HexagonCellPrototype> allSideNeighbors = item.neighbors.FindAll(c => c.layer == item.layer && Vector3.Distance(item.center, c.center) < item.size * 2f);
                    //     List<HexagonCellPrototype> allNonSideNeighbors = item.neighbors.FindAll(c => c.layer != item.layer);
                    //     if (allSideNeighbors.Count > 9)
                    //     {
                    //         Gizmos.color = Color.yellow;
                    //         Gizmos.DrawWireSphere(item.center, item.size * 2f);
                    //     }
                    //     else
                    //     {
                    //         if (allNonSideNeighbors.Count > 0) Gizmos.color = Color.magenta;
                    //     }

                    //     Gizmos.DrawSphere(item.center, item.size == 4 ? 3f : (item.size / 3f));
                    // }


                    Gizmos.color = Color.magenta;

                    float size = 0.3f;
                    switch (item.cellStatus)
                    {
                        // case CellStatus.Ground:

                        //     if (item.isGroundRamp && item.rampSlopeSides != null)
                        //     {
                        //         foreach (int side in item.rampSlopeSides)
                        //         {
                        //             Gizmos.color = Color.blue;
                        //             Gizmos.DrawSphere(item.sidePoints[side], 1f);
                        //         }
                        //     }
                        //     Gizmos.color = brown;
                        //     size = 4f;
                        //     Gizmos.DrawSphere(pointPos, size);
                        //     if (item.isGroundRamp)
                        //     {
                        //         Gizmos.color = Color.yellow;
                        //         size = 5f;
                        //         Gizmos.DrawWireSphere(pointPos, size);

                        //     }
                        //     break;
                        case CellStatus.UnderGround:
                            Gizmos.color = Color.white;
                            size = 4f;
                            Gizmos.DrawSphere(pointPos, size);
                            break;
                        // // case CellStatus.Remove:
                        // //     Gizmos.color = Color.gray;
                        // //     Gizmos.DrawSphere(pointPos, 6);
                        //     break;
                        case CellStatus.AboveGround:
                            Gizmos.color = Color.grey;
                            size = 0.6f;
                            Gizmos.DrawSphere(pointPos, size);

                            break;
                        default:
                            Gizmos.color = Color.magenta;
                            size = 0.33f;
                            Gizmos.DrawWireSphere(pointPos, size);
                            break;
                    }

                    // if (item.isEdge)
                    // {
                    //     if (item.isEntry)
                    //     {
                    //         Gizmos.color = purple;
                    //     }
                    //     else Gizmos.color = Color.red;
                    //     Gizmos.DrawWireSphere(pointPos, item.size);
                    // }
                    // else if (item.isPath)
                    // {
                    //     Gizmos.color = orange;

                    //     if (item.cellStatus != CellStatus.Ground)
                    //     {
                    //         Gizmos.DrawSphere(pointPos, 5);
                    //     }
                    //     else
                    //     {
                    //         Gizmos.DrawWireSphere(pointPos, 5.5f);
                    //     }
                    // }
                }
                Gizmos.color = Color.magenta;
                for (int j = 0; j < item.cornerPoints.Length; j++)
                {
                    pointPos = item.cornerPoints[j];
                    Gizmos.DrawSphere(pointPos, 0.25f);
                }

                Gizmos.color = Color.black;
                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(item.cornerPoints);
            }
        }

        public static void DrawHexagonCellPrototypeGrid(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, Transform transform, GridFilter_Type filterType)
        {
            if (prototypesByLayer == null) return;

            foreach (var kvp in prototypesByLayer)
            {
                int key = kvp.Key;
                // if (key != prototypesByLayer.Keys.Count - 1) continue;
                DrawHexagonCellPrototypes(kvp.Value, transform, filterType);
            }
        }

        public static void Shuffle(List<HexagonCellPrototype> prototypes)
        {
            int n = prototypes.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonCellPrototype temp = prototypes[r];
                prototypes[r] = prototypes[i];
                prototypes[i] = temp;
            }
        }

        #endregion


        // #region Multithreading


        // #endregion
    }
}