using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public enum Filter_CellType { Any = 0, Edge, NoEdge, NullValue, Path }

    public enum EdgeCellType { None = 0, Default = 1, Connector, Inner }
    public enum PathCellType { Unset = 0, Default = 1, Start, End }
    public enum CellStatus { Unset = 0, GenericGround = 1, FlatGround, UnderGround, Remove, AboveGround, Underwater }
    public enum CellMaterialContext { Unset = 0, Land, Water, Air, Space, Fire }
    public enum TunnelEntryType { Basement = 0, Cave = 1 }
    public enum TunnelStatus { Unset = 0, GenericGround = 1, FlatGround, UnderGround, AboveGround, Underwater }
    public enum WorldCellStatus { Unset = 0, Land, Ocean, Coast, Water }

    [System.Serializable]
    public struct CellWorldData
    {
        public int layer;
        public Vector2 lookup;
        public Vector2 worldspaceLookup;
        public Vector2 parentLookup;
    }


    public class HexagonCellPrototype : IHexCell
    {
        #region Static Vars
        public static float neighborSearchCenterDistMult = 2.4f;
        public static float GetCornerNeighborSearchDist(int cellSize) => 1.2f * ((float)cellSize / 12f);
        public static float vertexCenterPointDistance = 0.36f;
        #endregion

        public HexagonCellPrototype(Vector3 center, int size, bool addUid)
        {
            this.center = center;
            this.size = size;
            this.worldCoordinate = new Vector2(center.x, center.z);
            RecalculateEdgePoints();
            if (addUid) this.uid = UtilityHelpers.GenerateUniqueID("" + worldCoordinate);
        }

        public HexagonCellPrototype(
            Vector3 center,
            int size,
            IHexCell parentCell,
            float layerOffset,
            string appendToId = "",
            bool preAssignGround = false
        )
        {
            this.center = center;
            this.worldCoordinate = new Vector2(center.x, center.z);
            this.size = size;
            RecalculateEdgePoints();


            string baseID = "x" + size + "-" + center;
            string parentHeader = "";

            bool hasHostParent = (parentCell != null && parentCell.GetSize() < (int)HexCellSizes.X_108);
            bool isSubCell = (size < (int)HexCellSizes.X_108);

            this.layer = HexCoreUtil.Calculate_CellSnapLayer(layerOffset, center.y);
            this.layerOffset = (int)layerOffset;

            if (hasHostParent)
            {
                this.parentId = parentCell.GetId();
                parentHeader = "[" + this.parentId + "]-";

                this.parentLookup = parentCell.GetLookup();
                this.worldspaceLookup = parentCell.GetWorldSpaceLookup();

                this.SetPathCell(parentCell.IsPath());

                int parentLayer = parentCell.GetGridLayer();
                if (parentLayer != this.layer) Debug.LogError("parent/child layer mismatch detected - parentlayer: " + parentLayer + ", this cell's layer: " + this.layer);

                if (parentCell.IsGround())
                {
                    if (layer == parentLayer) this.cellStatus = parentCell.GetCellStatus();
                }
                else this.cellStatus = parentCell.GetCellStatus();
            }
            else
            {
                this.parentLookup = Vector2.positiveInfinity;
                this.worldspaceLookup = Vector2.positiveInfinity;
            }

            this.id = parentHeader + baseID;
            this.uid = UtilityHelpers.GenerateUniqueID(baseID);

            this.name = "prototype-" + this.id;
            if (layer > 0) this.name += "[L_" + layer + "]";
            if (preAssignGround) SetToGround(true);
        }

        public static HexagonCellPrototype Generate_NearestHexCell(Vector3 position, int hexSize, HexCellSizes snapCellSize, int layerHeight)
        {
            Vector3 hexCenter = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, (int)snapCellSize);
            hexCenter.y = (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerHeight);
            return new HexagonCellPrototype(hexCenter, (int)hexSize, false);
        }

        public static HexagonCellPrototype Generate_HexCellAtPosition(Vector3 position, HexCellSizes hexSize, int layerHeight)
        {
            Vector3 hexCenter = position;
            hexCenter.y = (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerHeight);
            return new HexagonCellPrototype(hexCenter, (int)hexSize, false);
        }

        public static HexagonCellPrototype Generate_NearestHexCell(Vector3 position, HexCellSizes hexSize, int layerOffset)
        {
            Vector3 hexCenter = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, (int)hexSize);
            hexCenter.y = (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerOffset);
            return new HexagonCellPrototype(hexCenter, (int)hexSize, false);
        }

        public static HexagonCellPrototype New_WorldCell(Vector3 centerPoint, int size, Vector2 parentLookup)
        {
            int layerBaseOffset = 2;
            HexagonCellPrototype new_cell = new HexagonCellPrototype(centerPoint, size, null, layerBaseOffset);
            new_cell.SetParentLookup(parentLookup);
            new_cell.SetWorldCoordinate(new Vector2(centerPoint.x, centerPoint.z));
            return new_cell;
        }
        public static HexagonCellPrototype New_WorldCell(Vector3 centerPoint, int size)
        {
            int layerBaseOffset = 2;
            HexagonCellPrototype new_cell = new HexagonCellPrototype(centerPoint, size, null, layerBaseOffset);
            new_cell.SetWorldCoordinate(new Vector2(centerPoint.x, centerPoint.z));
            return new_cell;
        }

        public string LogStats()
        {
            string str = "";
            str += "Size: " + this.size;
            str += ", Layer: " + this.layer;
            str += ", Status: " + this.cellStatus;
            str += ", IsEdge: " + this.IsEdge();

            return str;
        }


        public Dictionary<HexagonTileSide, TileSocketProfile> tileSocketProfileBySide = null;
        public Dictionary<HexagonTileSide, TileSocketProfile> GetNeighborTileSocketProfilesOnSides() =>
            TileSocketProfile.GetNeighborTileSocketProfilesOnSides_X8(this);


        // temp
        // public Dictionary<HexagonTileSide, Dictionary<Vector3, Vector3>> sideEdgeSocketLookups = null;
        // public Dictionary<HexagonTileSide, List<HexSideEdgeSocket>> sideEdgeSocket = null;
        // public Dictionary<HexagonSide, HexSideStructure> sideEdgeStructures = null;
        // public List<Vector3> bottomFloorPoints = null;
        public List<Vector3> rawStructureBorderPoints = new List<Vector3>();
        public List<HexSideStructure> borderSurfaceStructures = new List<HexSideStructure>();
        public Dictionary<Vector3, Vector3> borderPoints = new Dictionary<Vector3, Vector3>();


        #region Interface Methods
        public string GetId() => id;
        public string Get_Uid() => uid;
        public string GetParentId() => parentId;

        public int GetSize() => size;


        public bool IsEdge() => isEdgeCell;
        public bool IsEdgeOfParent() => isEdgeOfParent;
        public bool IsOriginalGridEdge() => isOGGridEdge;
        public void SetOriginalGridEdge(bool enable)
        {
            isOGGridEdge = enable;
        }

        public bool IsEdgeTOPath() => isEdgeTOPath;
        public bool IsInnerEdge() => isEdgeCell && _edgeCellType == EdgeCellType.Inner;
        public bool IsGridEdge() => isEdgeCell && _edgeCellType == EdgeCellType.Default;
        public void SetEdgeCell(bool enable, EdgeCellType type = EdgeCellType.Default)
        {
            isEdgeCell = enable;
            SetEdgeCellType(enable ? type : EdgeCellType.None);
        }

        public void SetEdgeCellType(EdgeCellType type)
        {
            _edgeCellType = type;
        }

        public bool IsEntry() => isEntryCell;
        public bool IsPath() => isPathCell;

        public bool IsGround() => cellStatus == CellStatus.GenericGround || cellStatus == CellStatus.FlatGround;
        public bool IsGroundCell() => IsGround() || isLeveledGroundCell;
        public bool IsGenericGround() => cellStatus == CellStatus.GenericGround;

        public bool IsFlatGround() => cellStatus == CellStatus.FlatGround;
        public bool IsUnderGround() => cellStatus == CellStatus.UnderGround;
        public bool IsUnderWater() => materialContext == CellMaterialContext.Water || cellStatus == CellStatus.Underwater;
        public void SetToGround(bool isFlatGround)
        {
            if (isFlatGround)
            {
                SetCellStatus(CellStatus.FlatGround);
            }
            else SetCellStatus(CellStatus.GenericGround);
            // SetMaterialContext(CellMaterialContext.Land);
        }

        public bool isHighlighted { get; private set; }
        public List<HexagonTileSide> HighlightedSides { get; private set; } = null;
        public bool IsSideHighlighted() => HighlightedSides != null;
        public void Highlight(bool enable)
        {
            isHighlighted = enable;
        }
        public void HighlightSide(bool enable, HexagonTileSide tileSide)
        {
            isHighlighted = enable;
            if (enable)
            {
                if (HighlightedSides == null)
                {
                    HighlightedSides = new List<HexagonTileSide>() { tileSide };
                }
                else if (HighlightedSides.Contains(tileSide) == false) HighlightedSides.Add(tileSide);
            }
            else if (HighlightedSides != null && HighlightedSides.Contains(tileSide)) HighlightedSides.Remove(tileSide);
        }
        public void ClearSideHighlights()
        {
            HighlightedSides = null;
        }
        public void Draw_Highlights(float drawSize = 0.5f)
        {
            if (HighlightedSides == null) return;

            foreach (var side in HighlightedSides)
            {
                if (side == HexagonTileSide.Top)
                {
                    Gizmos.DrawSphere(center + new Vector3(0, layerOffset, 0), drawSize);
                    continue;
                }
                if (side == HexagonTileSide.Bottom)
                {
                    Gizmos.DrawSphere(center, drawSize);
                    continue;
                }
                Gizmos.DrawSphere(sidePoints[(int)side], drawSize);
            }
        }


        public bool isIgnored;
        public void SetIgnore(bool ignore)
        {
            isIgnored = ignore;
        }

        public bool IsInCluster() => (clusterId != null);

        public bool IsWFC_Assigned() => (isPathCell || isGridHost || currentTile != null);
        public bool IsPreAssigned() => (isPathCell || isGridHost || isWorldSpaceEdgePathConnector || IsInCluster());
        public bool IsAssigned() => HasTileAssigned() || IsGridHost() || isIgnored || IsInClusterSystem() || IsDisposable() || (isLeveledCell && !isLeveledGroundCell);
        public bool IsDisposable() => cellStatus == CellStatus.Remove;
        public bool IsRemoved() => (cellStatus == CellStatus.Remove);

        public bool HasTileAssigned() => (currentTile != null || currentTile_V2 != null);
        public HexagonTileTemplate currentTile_V2;
        private IHexagonTile currentTile;
        public IHexagonTile GetTile() => currentTile;
        public void ClearTile()
        {
            currentTile = null;
            currentTileRotation = 0;
            SetIgnore(false);
        }

        public bool IsGridHost() => isGridHost;
        public void SetGridHost(bool enable)
        {
            isGridHost = enable;
        }
        public CellStatus GetCellStatus() => cellStatus;
        public void SetCellStatus(CellStatus status)
        {
            cellStatus = status;
        }

        public EdgeCellType GetEdgeCellType() => _edgeCellType;
        public Vector3 GetPosition() => center;
        public Vector3[] GetCorners() => cornerPoints;
        public Vector3[] GetSides() => sidePoints;
        #endregion
        public string id { get; private set; }
        public string uid { get; private set; }
        public string parentId { get; private set; }
        public string topNeighborId;
        public string bottomNeighborId;
        public string name;
        public int size;
        public int layerOffset { get; private set; }
        public int layer { get; private set; } = -1;
        public int GetGridLayer() => layer;
        public bool IsSameLayer(HexagonCellPrototype cell) => (int)cell.center.y == (int)center.y;

        public Vector3 center { get; private set; }
        public void UpdateLayer(int elevation)
        {
            Vector3 temp = center;
            temp.y = elevation;
            this.center = temp;
            this.layer = HexCoreUtil.Calculate_CellSnapLayer(layerOffset, center.y);
            this.layerOffset = (int)layerOffset;
        }

        public Vector3[] cornerPoints { get; private set; }
        public Vector3[] sidePoints { get; private set; }

        public List<BoundsShapeBlock> buildingNodeClearBounds = null;
        public Vector3[] buildingNodeEdgePoints = null;
        public List<int> buildingNode_SlicedCorners = null;

        public void Draw_NodeEdges()
        {
            if (buildingNodeEdgePoints == null) return;

            for (int i = 0; i < buildingNodeEdgePoints.Length; i++)
            {
                Vector3 pointA = buildingNodeEdgePoints[i];
                Vector3 pointB = buildingNodeEdgePoints[(i + 1) % buildingNodeEdgePoints.Length];
                Gizmos.DrawLine(pointA, pointB);
                Gizmos.DrawSphere(pointA, 0.6f);
            }
        }


        public List<HexagonCellPrototype> neighbors = new List<HexagonCellPrototype>();
        public HexagonCellPrototype[] neighborsBySide { get; private set; } = new HexagonCellPrototype[6];
        public HexagonCellPrototype[] layerNeighbors = new HexagonCellPrototype[2];
        public void ClearNeighborLists()
        {
            this.neighborsBySide = new HexagonCellPrototype[6];
            this.neighbors = new List<HexagonCellPrototype>();
            this.neighbors.AddRange(this.layerNeighbors);
        }

        public void AssignSideNeighbor(HexagonCellPrototype new_neighbor, HexagonSide side)
        {
            if (this.neighbors.Contains(new_neighbor) == false) this.neighbors.Add(new_neighbor);
            neighborsBySide[(int)side] = new_neighbor;

            if (new_neighbor.neighbors.Contains(this) == false) new_neighbor.neighbors.Add(this);
            new_neighbor.neighborsBySide[(int)HexCoreUtil.GetRelativeHexagonSide(side)] = this;
        }

        public bool HasSideNeighbor() => neighbors.Any(n => IsSameLayer(n));
        public bool HasGroundSideNeighbor() => neighbors.Any(n => IsSameLayer(n) && n.IsGround());
        public bool HasPreassignedNeighbor() => neighbors.Any(n => n.IsPreAssigned());
        public int GetEdgeSideNeighborCount() => neighbors.FindAll(n => IsSameLayer(n) && n.IsEdge()).Count;
        public List<HexagonCellPrototype> GetEdgeSideNeighbors() => neighbors.FindAll(n => IsSameLayer(n) && n.IsEdge());


        public List<HexagonSide> GetNeighborSides(Filter_CellType filter_CellType)
        {
            List<HexagonSide> sides = new List<HexagonSide>();
            for (int side = 0; side < neighborsBySide.Length; side++)
            {
                if (neighborsBySide[side] == null)
                {
                    if (filter_CellType == Filter_CellType.NullValue) sides.Add((HexagonSide)side);

                    continue;
                }

                if (filter_CellType != Filter_CellType.Any)
                {
                    if (neighborsBySide[side].IsEdge() != (filter_CellType == Filter_CellType.Edge)) continue;
                    if ((neighborsBySide[side].IsEdge() == false) != (filter_CellType == Filter_CellType.NoEdge)) continue;
                }
                sides.Add((HexagonSide)side);
            }
            return sides;
        }

        public HexagonCellPrototype[] GetNeighborTileSides()
        {
            HexagonCellPrototype[] allNeighborsByTileSide = new HexagonCellPrototype[8];
            // Sides 
            allNeighborsByTileSide[0] = neighborsBySide[0];
            allNeighborsByTileSide[1] = neighborsBySide[1];
            allNeighborsByTileSide[2] = neighborsBySide[2];
            allNeighborsByTileSide[3] = neighborsBySide[3];
            allNeighborsByTileSide[4] = neighborsBySide[4];
            allNeighborsByTileSide[5] = neighborsBySide[5];
            // Top & Bottom
            allNeighborsByTileSide[6] = layerNeighbors[1]; //top
            allNeighborsByTileSide[7] = layerNeighbors[0]; //btm

            return allNeighborsByTileSide;
        }

        public List<HexagonTileSide> GetNeighborSidesX8(Filter_CellType filter_CellType)
        {
            List<HexagonTileSide> sides = new List<HexagonTileSide>();
            HexagonCellPrototype[] allNeighborsByTileSide = GetNeighborTileSides();

            for (int side = 0; side < allNeighborsByTileSide.Length; side++)
            {
                if (allNeighborsByTileSide[side] == null)
                {
                    if (filter_CellType == Filter_CellType.NullValue) sides.Add((HexagonTileSide)side);
                    continue;
                }

                if (filter_CellType != Filter_CellType.Any)
                {
                    if (allNeighborsByTileSide[side].IsEdge() != (filter_CellType == Filter_CellType.Edge)) continue;
                    if ((allNeighborsByTileSide[side].IsEdge() == false) != (filter_CellType == Filter_CellType.NoEdge)) continue;
                }
                sides.Add((HexagonTileSide)side);
            }
            return sides;
        }

        public HexagonCellPrototype GetNeighborOnSide(int side) => neighborsBySide[side];
        public HexagonCellPrototype GetNeighborOnSide(HexagonSide side) => neighborsBySide[(int)side];
        public List<HexagonCellPrototype> GetNeighborsWithStatus(CellStatus status) => neighbors.FindAll(n => n.GetCellStatus() == status);


        public CellWorldData[] neighborWorldData;
        public Vector3[] GetTerrainChunkCoordinates()
        {
            Vector3[] chunkLookups = new Vector3[4];
            chunkLookups[0] = sidePoints[1];
            chunkLookups[1] = sidePoints[2];
            chunkLookups[2] = sidePoints[4];
            chunkLookups[3] = sidePoints[5];
            return chunkLookups;
        }
        public Vector2[] CalculateTerrainChunkLookups()
        {
            Vector2[] chunkLookups = new Vector2[4];
            chunkLookups[0] = TerrainChunkData.CalculateTerrainChunkLookup(sidePoints[1]);
            chunkLookups[1] = TerrainChunkData.CalculateTerrainChunkLookup(sidePoints[2]);
            chunkLookups[2] = TerrainChunkData.CalculateTerrainChunkLookup(sidePoints[4]);
            chunkLookups[3] = TerrainChunkData.CalculateTerrainChunkLookup(sidePoints[5]);
            return chunkLookups;
        }
        public bool HasTerrainChunkLookup(Vector2 chunkLookup)
        {
            Vector2[] chunkLookups = CalculateTerrainChunkLookups();
            foreach (var lookup in chunkLookups)
            {
                if (chunkLookup == lookup) return true;
                // Debug.Log("chunkLookup: " + chunkLookup + ", current check: " + lookup);
            }
            return false;
        }

        public List<Vector2> vertexList_V2 = new List<Vector2>();

        private CellStatus cellStatus = CellStatus.Unset;

        public void SetParentId(string _parentCellId)
        {
            parentId = _parentCellId;
        }

        private CellMaterialContext materialContext;
        public void SetMaterialContext(CellMaterialContext context)
        {
            materialContext = context;
        }

        public EdgeCellType _edgeCellType;
        private bool isOGGridEdge;
        public bool isEdgeCell { private set; get; }
        public bool isEdgeOfParent;
        public bool isEdgeGridConnector; //{ private set; get; }
        public bool isWorldSpaceEdgePathConnector;
        public bool isEdgeTOPath;

        public bool isEntryCell { private set; get; }

        public bool isLocationMarker;

        public void SetEntryCell(bool enable)
        {
            isEntryCell = enable;
        }

        public PathCellType _pathCellType;
        public bool IsPathCellEnd() => _pathCellType == PathCellType.End;
        public bool IsPathCellStart() => _pathCellType == PathCellType.Start;
        public bool isPathCell { private set; get; }
        public void SetPathCell(bool enable, PathCellType type = PathCellType.Default)
        {
            isPathCell = enable;
            if (enable == false) type = PathCellType.Unset;
            _pathCellType = type;
        }


        public bool isLeveledGroundCell;//{ private set; get; }
        public void SetLeveledGroundCell(bool enable, bool isFlatGround)
        {
            isLeveledGroundCell = enable;
            SetToGround(isFlatGround);
        }


        #region World Data / Coordinates
        public int objectIndex = -1;
        public WorldCellStatus worldCellStatus;
        public Vector2 worldspaceLookup { get; private set; } = Vector2.positiveInfinity;
        public Vector2 parentLookup { get; private set; } = Vector2.positiveInfinity;
        public Vector2 worldCoordinate { get; private set; } = Vector2.positiveInfinity;
        public Vector2 GetLookup() => HexCoreUtil.Calculate_CenterLookup(center, size);
        // public Vector2 GetLookup() => HexCoreUtil.Calculate_CenterLookup(worldCoordinate, size);

        public Vector2 GetWorldSpaceLookup() => worldspaceLookup;
        public void SetWorldCoordinate(Vector2 coordinate) { worldCoordinate = coordinate; }

        public Vector2 GetParentLookup() => parentLookup;
        public void SetParentLookup(Vector2 lookupCoord) { parentLookup = lookupCoord; }
        public void SetWorldSpaceLookup(Vector2 lookupCoord) { worldspaceLookup = lookupCoord; }

        public bool HasWorldCoordinate() => worldCoordinate != Vector2.positiveInfinity;
        public bool HasParentLookup() => parentLookup != Vector2.positiveInfinity;
        public bool HasWorldSpaceLookup() => worldspaceLookup != Vector2.positiveInfinity;
        public bool isWorldSpaceCellInitialized;
        #endregion


        public int CalculateNextSize_Up() => (size * 3);
        public int CalculateNextSize_Down() => (size / 3);

        #region Tunnels

        public void SetTunnel(bool enable, TunnelStatus status)
        {
            isTunnel = enable;
            tunnelStatus = enable ? status : TunnelStatus.Unset;
        }
        public void SetTunnelGroundEntry(TunnelEntryType entryType)
        {
            isTunnel = true;
            isTunnelGroundEntry = true;
            tunnelEntryType = TunnelEntryType.Basement;
        }

        public bool isTunnel { get; private set; }
        public bool isTunnelGroundEntry { get; private set; }
        public bool isTunnelStart;
        public TunnelEntryType tunnelEntryType = TunnelEntryType.Basement;
        public TunnelStatus tunnelStatus;
        public List<int> tunnelStartOpenSides;

        #endregion

        public bool isGroundRamp;


        #region Host Cell
        public bool isGridHost { get; private set; }
        public string clusterId = null;
        public HexagonCellCluster clusterParent;
        public List<HexagonCellPrototype> children { get; private set; }
        public bool HasChildren() => children != null && children.Count > 0;
        public bool AreAnyChildrenPreAssigned() => HasChildren() && children.Any(c => c.IsPreAssigned());
        public void SetChild(HexagonCellPrototype newChild)
        {
            if (children == null) children = new List<HexagonCellPrototype>();
            if (children.Contains(newChild) == false)
            {
                newChild.SetParentId(GetId());
                newChild.SetParentLookup(GetLookup());
                children.Add(newChild);
            }
        }
        public Dictionary<int, List<HexagonCellPrototype>> cellPrototypes_X4_ByLayer;
        #endregion

        public float MaxBoundsRadius() => (size * 1.12f);


        public void RecalculateEdgePoints()
        {
            cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
            sidePoints = HexagonGenerator.GenerateHexagonSidePoints(cornerPoints);
        }

        public List<Vector3> GetEdgePoints()
        {
            List<Vector3> allEdgePoints = new List<Vector3>();
            allEdgePoints.AddRange(cornerPoints);
            allEdgePoints.AddRange(sidePoints);
            return allEdgePoints;
        }


        public List<HexagonCellPrototype> GetChildCellsWithCellStatus(CellStatus status)
        {
            if (!HasChildren()) return null;
            return children.FindAll(c => c.cellStatus == status);
        }

        public List<HexagonCellPrototype> GetChildGroundCells() => GetChildCellsWithCellStatus(CellStatus.GenericGround);

        public List<Vector3> GetDottedEdgeLine() => VectorUtil.GenerateDottedLine(this.cornerPoints.ToList(), 8);


        #region Neighbors
        public void SetTopNeighbor(HexagonCellPrototype cell)
        {
            layerNeighbors[1] = cell;
        }
        public void SetBottomNeighbor(HexagonCellPrototype cell)
        {
            layerNeighbors[0] = cell;
        }
        public HexagonCellPrototype GetTopNeighbor() => layerNeighbors[1];
        public HexagonCellPrototype GetBottomNeighbor() => layerNeighbors[0];
        public bool HasTopNeighbor() => layerNeighbors[1] != null;
        public bool HasBottomNeighbor() => layerNeighbors[0] != null;

        public int GetSideNeighborCount(bool scopeToParentCell)
        {
            int count = 0;
            foreach (HexagonCellPrototype item in neighborsBySide)
            {
                if (item != null && (scopeToParentCell == false || item.GetParentId() == GetParentId()))
                {
                    count++;
                }
            }
            return count;
        }

        public List<HexagonCellPrototype> GetLayerNeighbors() => neighbors.FindAll(n => n.GetGridLayer() == GetGridLayer());
        public bool HasEntryNeighbor() => neighbors.Any(n => n.isEntryCell);
        public int GetUnassignedNeighborCount(bool includeLayers) => neighbors.FindAll(n => n.IsAssigned() == false && n.GetGridLayer() == GetGridLayer()).Count;

        public List<HexagonCellPrototype> GetPathNeighbors()
        {
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();
            found.AddRange(neighborsBySide.ToList().FindAll(n => n.isPathCell));
            foreach (var layerNeighbor in layerNeighbors)
            {
                if (layerNeighbor != null)
                {
                    found.AddRange(layerNeighbor.neighbors.FindAll(n => n.isPathCell && !found.Contains(n)));
                }
            }
            return found;
        }

        public int GetNeighborsRelativeSide(HexagonSide side)
        {
            return (int)HexCoreUtil.GetRelativeHexagonSide(side);
            // if (neighborsBySide[(int)side] == null) return -1;

            // for (int neighborSide = 0; neighborSide < 6; neighborSide++)
            // {
            //     if (neighborsBySide[(int)side].neighborsBySide[neighborSide] == this) return neighborSide;
            // }
            // return -1;
        }

        #endregion


        private Dictionary<TileCategory, float> categoyBias;

        public float highestProbability;

        public bool isLeveledCell;//{ private set; get; }
        public void SetLeveledCell(bool enable)
        {
            isLeveledCell = enable;
        }
        public bool isLeveledEdge;//{ private set; get; }
        public void SetLeveledEdge(bool enable)
        {
            isLeveledEdge = enable;
        }
        public bool isLeveledRampCell;//{ private set; get; }
        public void SetLeveledRampCell(bool enable)
        {
            isLeveledRampCell = enable;
        }



        [Header("Micro Cluster System")]
        private bool _isClusterCellParent;
        public bool IsClusterCellParent() => _isClusterCellParent;
        public bool IsInClusterSystem() => IsInCluster() || IsClusterCellParent() || (_clusterCellParentId != "" && _clusterCellParentId != null);

        [SerializeField] private string _clusterCellParentId;
        public string GetClusterCellParentId() => _clusterCellParentId;
        public void SetClusterCellParentId(string _id)
        {
            _clusterCellParentId = _id;
        }

        [Header("Cluster")]
        [SerializeField] public bool isClusterPrototype;
        public string GetClusterID() => clusterId;
        public void SetClusterID(string _id)
        {
            clusterId = _id;
        }
        [SerializeField] private int _numberofNeighborsInCluster = 0;
        public int GetNumberofNeighborsInCluster() => _numberofNeighborsInCluster;
        public void SetNumberofNeighborsInCluster(int num)
        {
            _numberofNeighborsInCluster = num;
        }
        public int GetNumberOfNeighborsUnclustered()
        {
            if (neighbors.Count == 0) return 0;
            int num = 0;
            foreach (HexagonCellPrototype item in neighbors)
            {
                if (item.isClusterPrototype == false) num++;
            }
            return num;
        }

        [Header("Tile")]
        [SerializeField] private int currentTileRotation = 0;
        [SerializeField] private bool isCurrentTileInverted;
        public int GetTileRotation() => currentTileRotation;
        public bool IsTileInverted() => isCurrentTileInverted;
        public HexagonTileCore GetCurrentTile() => (HexagonTileCore)currentTile;
        public void SetTile(HexagonTileCore newTile, int rotation, bool inverted = false)
        {
            currentTile = (IHexagonTile)newTile;
            currentTileRotation = rotation;
            isCurrentTileInverted = inverted;
        }
        public void SetTile(HexagonTileTemplate newTile, int rotation, bool inverted = false)
        {
            currentTile_V2 = newTile;
            currentTileRotation = rotation;
            isCurrentTileInverted = inverted;
        }

        public NeighborSideCornerSockets[] GetSideNeighborTileSockets(bool useWalledEdgePreference = false, bool microTile = true)
        {
            NeighborSideCornerSockets[] neighborCornerSocketsBySide = new NeighborSideCornerSockets[6];

            for (int side = 0; side < 6; side++)
            {
                NeighborSideCornerSockets neighborCornerSockets = new NeighborSideCornerSockets();
                HexagonCellPrototype sideNeighbor = neighborsBySide[side];

                // If no neighbor, socket is Edge socket value
                if (sideNeighbor == null)
                {
                    neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
                    neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
                }
                else
                {

                    if (sideNeighbor.currentTile != null)
                    {
                        neighborCornerSockets = GetSideNeighborRelativeTileSockets(side);
                    }
                    else // Neighbor Has No Tile
                    {
                        GlobalSockets defaultSocketId;

                        // Debug.LogError("sideNeighbor has no tile. Current cell - Edge: " + IsEdge());

                        if (IsGridEdge())
                        {
                            // defaultSocketId = sideNeighbor.isEdgeCell ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                            defaultSocketId = (useWalledEdgePreference && sideNeighbor.isEdgeCell) ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                        }
                        else
                        {
                            defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                        }

                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(defaultSocketId);
                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(defaultSocketId);
                    }
                }
                neighborCornerSocketsBySide[side] = neighborCornerSockets;
            }
            return neighborCornerSocketsBySide;
        }

        public void SetCategoryBias(TileCategory category, float value)
        {
            if (categoyBias == null) categoyBias = new Dictionary<TileCategory, float>();

            if (!categoyBias.ContainsKey(category))
            {
                categoyBias.Add(category, value);
            }
            else
            {
                categoyBias[category] = value;
            }
        }

        public int[] GetBottomNeighborTileSockets(bool top)
        {
            if (GetGridLayer() == 0) return null;

            if (layerNeighbors[0] == null) return null;

            if (layerNeighbors[0].currentTile == null) return null;

            return layerNeighbors[0].currentTile.GetRotatedLayerCornerSockets(top, layerNeighbors[0].currentTileRotation, layerNeighbors[0].isCurrentTileInverted);
        }

        public int[] GetDefaultLayeredSocketSet(GlobalSockets tileSocketConstant)
        {
            int[] sockets = new int[6];
            sockets[0] = (int)tileSocketConstant;
            sockets[1] = (int)tileSocketConstant;
            sockets[2] = (int)tileSocketConstant;
            sockets[3] = (int)tileSocketConstant;
            sockets[4] = (int)tileSocketConstant;
            sockets[5] = (int)tileSocketConstant;
            return sockets;
        }

        public int[] GetDefaultSideSocketSet(GlobalSockets tileSocketConstant)
        {
            int[] sockets = new int[2];
            sockets[0] = (int)tileSocketConstant;
            sockets[1] = (int)tileSocketConstant;
            return sockets;
        }

        public NeighborLayerCornerSockets[] GetLayeredNeighborTileSockets(TileContext tileContext)
        {
            NeighborLayerCornerSockets[] layeredNeighborCornerSockets = new NeighborLayerCornerSockets[2];
            NeighborLayerCornerSockets bottomNeighborTopCornerSockets = EvaluateLayeredNeighborTileSockets(layerNeighbors[0], true, tileContext);
            NeighborLayerCornerSockets topNeighborBtmCornerSockets = EvaluateLayeredNeighborTileSockets(layerNeighbors[1], false, tileContext);

            layeredNeighborCornerSockets[0] = bottomNeighborTopCornerSockets;
            layeredNeighborCornerSockets[1] = topNeighborBtmCornerSockets;
            return layeredNeighborCornerSockets;
        }


        public NeighborLayerCornerSockets EvaluateLayeredNeighborTileSockets(HexagonCellPrototype layerNeighbor, bool top, TileContext tileContext)
        {
            NeighborLayerCornerSockets layeredNeighborCornerSockets = new NeighborLayerCornerSockets();

            // If no neighbor, socket is Edge socket value
            if (layerNeighbor == null)
            {
                layeredNeighborCornerSockets.corners = GetDefaultLayeredSocketSet(GlobalSockets.Edge);
            }
            else
            {

                if (layerNeighbor.currentTile != null)
                {
                    layeredNeighborCornerSockets = GetLayeredNeighborRelativeTileSockets(layerNeighbor, top);
                }
                else
                {
                    // Debug.Log("layerNeighbor id: " + layerNeighbor.id);
                    if (layerNeighbor.GetCurrentTile() == null)
                    {
                        // Debug.Log("layerNeighbor id: " + layerNeighbor.id + ", NO Tile");
                        if (layerNeighbor.isEdgeCell)
                        {
                            layeredNeighborCornerSockets.corners = GetDefaultLayeredSocketSet(GlobalSockets.Unassigned_EdgeCell);
                        }
                        else
                        {
                            layeredNeighborCornerSockets.corners = GetDefaultLayeredSocketSet(GlobalSockets.Unassigned_InnerCell);
                        }
                    }

                }
            }
            return layeredNeighborCornerSockets;
        }

        public NeighborLayerCornerSockets GetLayeredNeighborRelativeTileSockets(HexagonCellPrototype layerNeighbor, bool top)
        {
            NeighborLayerCornerSockets neighborCornerSockets = new NeighborLayerCornerSockets();
            neighborCornerSockets.corners = new int[6];

            bool isNeighborInverted = (layerNeighbor.isCurrentTileInverted);

            neighborCornerSockets.corners = layerNeighbor.currentTile.GetRotatedLayerCornerSockets(top, layerNeighbor.currentTileRotation, isNeighborInverted);
            return neighborCornerSockets;
        }

        public NeighborSideCornerSockets GetSideNeighborRelativeTileSockets(int side)
        {
            NeighborSideCornerSockets neighborCornerSockets = new NeighborSideCornerSockets();
            HexagonCellPrototype sideNeighbor = neighborsBySide[side];

            int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSide)side);
            bool isNeighborInverted = (sideNeighbor.isCurrentTileInverted);

            neighborCornerSockets.bottomCorners = new int[2];
            neighborCornerSockets.topCorners = new int[2];

            (HexagonCorner cornerA, HexagonCorner cornerB) = HexCoreUtil.GetCornersFromSide((HexagonSide)neighborRelativeSide);
            neighborCornerSockets.bottomCorners[0] = sideNeighbor.currentTile.GetRotatedSideCornerSocketId(cornerA, sideNeighbor.currentTileRotation, false, isNeighborInverted);
            neighborCornerSockets.bottomCorners[1] = sideNeighbor.currentTile.GetRotatedSideCornerSocketId(cornerB, sideNeighbor.currentTileRotation, false, isNeighborInverted);
            neighborCornerSockets.topCorners[0] = sideNeighbor.currentTile.GetRotatedSideCornerSocketId(cornerA, sideNeighbor.currentTileRotation, true, isNeighborInverted);
            neighborCornerSockets.topCorners[1] = sideNeighbor.currentTile.GetRotatedSideCornerSocketId(cornerB, sideNeighbor.currentTileRotation, true, isNeighborInverted);

            return neighborCornerSockets;
        }

        public NeighborSideCornerSockets[] GetSideNeighborTileSockets(bool useWalledEdgePreference = false)
        {
            // int[] neighborSocketsBySide = new int[6];
            NeighborSideCornerSockets[] neighborCornerSocketsBySide = new NeighborSideCornerSockets[6];

            for (int side = 0; side < 6; side++)
            {
                NeighborSideCornerSockets neighborCornerSockets = new NeighborSideCornerSockets();
                HexagonCellPrototype sideNeighbor = neighborsBySide[side];

                // If no neighbor, socket is Edge socket value
                if (sideNeighbor == null)
                {
                    if (IsInnerEdge())
                    {
                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.InnerCell_Generic);
                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.InnerCell_Generic);
                    }
                    else
                    {
                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
                    }
                }
                else
                {

                    if (sideNeighbor.currentTile != null)
                    {
                        neighborCornerSockets = GetSideNeighborRelativeTileSockets(side);
                    }

                    else
                    {

                        // EDGED
                        if (useWalledEdgePreference && IsGridEdge() && !IsInCluster())
                        {
                            GlobalSockets defaultSocketId;

                            if (!sideNeighbor.isEdgeCell && !sideNeighbor.isEntryCell)
                            {
                                defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                            }
                            else
                            {
                                if (sideNeighbor.isEntryCell || (isEntryCell && sideNeighbor.isEntryCell == false))
                                {
                                    defaultSocketId = GlobalSockets.Entrance_Generic;
                                }
                                else
                                {
                                    defaultSocketId = IsGroundCell() ? GlobalSockets.Unassigned_EdgeCell : defaultSocketId = GlobalSockets.Empty_Space;
                                }
                            }
                            neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(defaultSocketId);
                            neighborCornerSockets.topCorners = GetDefaultSideSocketSet(defaultSocketId);
                        }
                        else
                        {
                            if (isPathCell && isLeveledGroundCell && sideNeighbor.isPathCell)
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Path_Generic);
                                neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Path_Generic);
                            }
                            else
                            {
                                GlobalSockets defaultSocketId;

                                if (IsGridEdge())
                                {
                                    // Edge Connectors
                                    // if (GetEdgeCellType() == EdgeCellType.Connector && (sideNeighbor.GetParentId() != GetParentId()))
                                    // {
                                    //     defaultSocketId = GlobalSockets.Unset_Edge_Connector;
                                    // }
                                    // else
                                    // {
                                    defaultSocketId = (useWalledEdgePreference && sideNeighbor.isEdgeCell) ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                                    // }
                                }
                                else
                                {
                                    if (useWalledEdgePreference && (sideNeighbor.isEntryCell || (isEntryCell && sideNeighbor.isEntryCell == false)))
                                    {
                                        defaultSocketId = GlobalSockets.Entrance_Generic;
                                    }
                                    else
                                    {
                                        defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                                    }
                                }
                                neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(defaultSocketId);
                                neighborCornerSockets.topCorners = GetDefaultSideSocketSet(defaultSocketId);
                            }
                        }

                    }
                }

                neighborCornerSocketsBySide[side] = neighborCornerSockets;
            }

            return neighborCornerSocketsBySide;
        }
        public List<Vector2Int> vertexList = new List<Vector2Int>();



        public TileSocketProfile GetSideNeighborRelativeTileSockets_V2(int side)
        {
            HexagonTileSide relativeSide = HexCoreUtil.GetRelativeHexagonSide((HexagonTileSide)side);

            // int relativeSide = GetNeighborsRelativeSide((HexagonSide)side);
            // Debug.Log("side: " + side + ", " + (HexagonTileSide)side + ",  relativeSide: " + relativeSide + ", " + (HexagonTileSide)relativeSide);
            if (currentTile_V2.socketProfileBySide.ContainsKey(relativeSide) == false)
            {
                Debug.Log("currentTile_V2.socketProfileBySide.ContainsKey(relativeSide) == false, side: " + side + ", relativeSide." + relativeSide + ". Using EDGE");
                return new TileSocketProfile((int)GlobalSockets.Edge);
            }
            return currentTile_V2.socketProfileBySide[relativeSide];
        }



        #region Static Methods

        public static List<Vector2> CalculateNeighborLookupCoordinates(HexagonCellPrototype cell)
        {
            Vector2[] estimatedNeighborCenters = HexCoreUtil.Generate_NeighborLookups_X6(cell.center, cell.size);
            List<Vector2> neighborLookups = new List<Vector2>();
            foreach (Vector3 centerPos in estimatedNeighborCenters)
            {
                neighborLookups.Add(HexCoreUtil.Calculate_CenterLookup(centerPos, cell.size));
            }
            return neighborLookups;
        }
        public static List<Vector2> CalculateChildrenLookupCoordinates(HexagonCellPrototype cell)
        {
            return GenerateChildrenLookupCoordinates_X11(cell.center, cell.CalculateNextSize_Down());
        }


        public static (bool, float) IsPointWithinEdgeBounds_WithDistance(Vector3 point, HexagonCellPrototype prototype, float maxEdgeDistance = 0.66f)
        {
            float distance = VectorUtil.DistanceXZ(point, prototype.center);

            if (distance < (prototype.size * 0.93f)) return (true, distance);

            List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(prototype.cornerPoints.ToList(), 8);

            (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);

            if (closestPoint == Vector3.positiveInfinity || edgeDistance > maxEdgeDistance) return (false, distance);
            return (true, distance);
        }

        public static bool IsPointWithinEdgeBounds(Vector3 point, HexagonCellPrototype prototype, float maxEdgeDistance = 0.66f)
        {
            if (VectorUtil.DistanceXZ(point, prototype.center) < (prototype.size * 0.92f)) return true;

            List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(prototype.cornerPoints.ToList(), 8);
            (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);
            if (closestPoint == Vector3.positiveInfinity || edgeDistance > maxEdgeDistance) return false;
            return true;
        }
        public static (bool, bool) IsPointWithinEdgeBounds_WithEdgeCheck(Vector3 point, HexagonCellPrototype prototype, float maxEdgeDistance = 0.66f)
        {
            if (VectorUtil.DistanceXZ(point, prototype.center) < (prototype.size * 0.92f)) return (true, false);

            List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(prototype.cornerPoints.ToList(), 8);
            (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);
            if (closestPoint == Vector3.positiveInfinity || edgeDistance > maxEdgeDistance) return (false, false);
            return (true, true);
        }
        public static (bool, Vector3, float) IsPointWithinEdgeBounds_WithEdgePoint(Vector3 point, HexagonCellPrototype prototype, float maxEdgeDistance = 0.66f)
        {
            float centerDistance = VectorUtil.DistanceXZ(point, prototype.center);
            if (centerDistance > prototype.MaxBoundsRadius()) return (false, Vector3.positiveInfinity, -1);
            List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(prototype.cornerPoints.ToList(), 8);

            (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);
            if (closestPoint == Vector3.positiveInfinity) return (false, Vector3.positiveInfinity, -1);
            return (true, closestPoint, edgeDistance);
        }


        public static Dictionary<string, List<HexagonCellPrototype>> GetRandomCellClusters(List<HexagonCellPrototype> allPrototypesOfBaseLayer,
            Vector2 searchRadius_MinMax,
            int maxClusters,
            Vector2 minMaxMembers,
            bool allowClusterNeighbors = false,
            bool maximizeSpacing = true,
            bool excludeEdgeOfParentGrid = true
        )
        {
            Dictionary<string, List<HexagonCellPrototype>> clusters = new Dictionary<string, List<HexagonCellPrototype>>();
            List<HexagonCellPrototype> available = allPrototypesOfBaseLayer.FindAll(p => p.IsPreAssigned() == false && p.IsGround() && p.HasSideNeighbor());

            if (excludeEdgeOfParentGrid)
            {
                available = available.FindAll(p => p.IsOriginalGridEdge() == false);
            }

            int minClusterSize = 2;

            if (available.Count < minClusterSize)
            {
                Debug.LogError("Not enough available cells for clusters");
                return null;
            }

            int clustersFound = 0;
            int totalPossibleClusters = available.Count / minClusterSize;
            int remainingAvailable = totalPossibleClusters;
            HexagonCellPrototype centerCell = available[0];

            List<Vector3> clusterCenterPoints = new List<Vector3>();

            int attempts = 10;
            for (int i = 0; i < totalPossibleClusters; i++)
            {
                if (remainingAvailable < minClusterSize || clustersFound >= maxClusters) break;

                bool newClusterAdded = false;

                do
                {
                    int searchRadius = UnityEngine.Random.Range((int)searchRadius_MinMax.x, (int)searchRadius_MinMax.y + 1);
                    int remainder = (searchRadius % 4);
                    if (searchRadius + remainder > (int)searchRadius_MinMax.y)
                    {
                        searchRadius = (int)searchRadius_MinMax.y;
                    }
                    else searchRadius += remainder;



                    List<HexagonCellPrototype> newCluster;
                    HexagonCellPrototype startCell;

                    if (!maximizeSpacing || (maximizeSpacing && i == 0))
                    {
                        int memberCount = (int)UnityEngine.Random.Range(minMaxMembers.x, minMaxMembers.y);
                        startCell = available[UnityEngine.Random.Range(0, available.Count)];
                        List<HexagonCellPrototype> _newClusterGroup = HexGridPathingUtil.GetConsecutiveNeighborsCluster(startCell, memberCount, CellSearchPriority.SideNeighbors, null, excludeEdgeOfParentGrid);

                        // (List<HexagonCellPrototype> _newClusterGroup, HexagonCellPrototype startCell) = SelectCellsInRadiusOfRandomCell(available, searchRadius, maxMembers);
                        newCluster = _newClusterGroup;
                        centerCell = startCell;
                    }
                    else
                    {
                        int memberCount = (int)UnityEngine.Random.Range(minMaxMembers.x, minMaxMembers.y);
                        startCell = available[0];
                        newCluster = HexGridPathingUtil.GetConsecutiveNeighborsCluster(startCell, memberCount, CellSearchPriority.SideNeighbors, null, excludeEdgeOfParentGrid);
                        // newCluster = SelectCellsInRadiusOfCell(available, available[0], searchRadius, maxMembers);
                    }

                    if (newCluster.Count < minMaxMembers.x)
                    {
                        attempts--;
                        continue;
                    }

                    string newClusterId = "CSTR-" + startCell.GetId();
                    foreach (HexagonCellPrototype item in newCluster)
                    {
                        item.clusterId = newClusterId;
                    }

                    clusters.Add(newClusterId, newCluster);
                    clustersFound++;
                    newClusterAdded = true;

                    clusterCenterPoints.Add(CalculateCenterPositionFromGroup(newCluster));

                    if (allowClusterNeighbors == false || maximizeSpacing)
                    {

                        available = available.FindAll(c => !c.IsPreAssigned() && !c.HasPreassignedNeighbor() && !c.neighbors.Any(n => n.IsPreAssigned() || n.HasPreassignedNeighbor())).Except(newCluster)
                            .OrderByDescending(x => VectorUtil.AverageDistanceFromPointsXZ(x.center, clusterCenterPoints)).ToList();
                        // .OrderByDescending(x => Vector3.Distance(x.center, centerCell.center)).ToList();
                    }
                    else
                    {
                        available = available.Except(newCluster).ToList();
                    }

                    remainingAvailable = available.Count / minClusterSize;


                } while (!newClusterAdded && attempts > 0);

            }

            return clusters;
        }


        #region Grid Erosion Methods

        public static List<HexagonCellPrototype> GetRandomGridErosion(List<HexagonCellPrototype> allPrototypesOfBaseLayer, float maxRadius, int maxCells, int clumpSets = 5, int clusters = 1)
        {
            // List<HexagonCellPrototype> result = SelectCellsInRadiusOfRandomCell(allPrototypesOfBaseLayer, maxRadius);
            // for (int i = 0; i < clumpSets; i++)
            // {
            //     List<HexagonCellPrototype> newClump = SelectCellsInRadiusOfCell(allPrototypesOfBaseLayer, result[UnityEngine.Random.Range(0, result.Count)], maxRadius * 0.9f);
            //     result.AddRange(newClump.FindAll(c => result.Contains(c) == false));
            // }

            // if (clusters > 1) maxRadius = ((maxRadius * 1.5f) / (float)clusters);

            List<HexagonCellPrototype> result = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> available = new List<HexagonCellPrototype>();

            available.AddRange(allPrototypesOfBaseLayer);

            for (int i = 0; i < clusters; i++)
            {
                (List<HexagonCellPrototype> newSet, HexagonCellPrototype centerCell) = SelectCellsInRadiusOfRandomCell(available, maxRadius);
                if (newSet.Count > 0)
                {
                    result.AddRange(newSet.FindAll(c => result.Contains(c) == false));

                    for (int j = 0; j < clumpSets; j++)
                    {
                        List<HexagonCellPrototype> newClump = SelectCellsInRadiusOfCell(available, result[UnityEngine.Random.Range(0, result.Count)], maxRadius * 0.9f);
                        result.AddRange(newClump.FindAll(c => result.Contains(c) == false));
                    }
                    available = available.Except(result).ToList();
                }
            }

            foreach (HexagonCellPrototype cell in result)
            {
                cell.cellStatus = CellStatus.Remove;
            }

            return result;
        }

        public static List<HexagonCellPrototype> SelectCellsInRadiusOfPosition(List<HexagonCellPrototype> cells, Vector3 centerPosition, float radius)
        {
            List<HexagonCellPrototype> selectedCells = new List<HexagonCellPrototype>();
            int found = 0;

            //Iterate through each cell in the input list
            foreach (HexagonCellPrototype cell in cells)
            {
                // Check if the distance between the cell and the center cell is within the given radius
                if (Vector3.Distance(centerPosition, cell.center) <= radius)
                {
                    //If the distance is within the radius, add the current cell to the list of selected cells
                    selectedCells.Add(cell);
                    found++;
                }
            }
            return selectedCells;
        }

        public static List<HexagonCellPrototype> SelectCellsInRadiusOfCell(List<HexagonCellPrototype> cells, HexagonCellPrototype centerCell, float radius, int maximum = -1)
        {
            Vector2 centerPos = new Vector2(centerCell.center.x, centerCell.center.z);
            //Initialize a list to store cells within the radius distance
            List<HexagonCellPrototype> selectedCells = new List<HexagonCellPrototype>();
            bool limitToMax = (maximum > 0);
            int found = 0;

            //Iterate through each cell in the input list
            foreach (HexagonCellPrototype cell in cells)
            {
                Vector2 cellPos = new Vector2(cell.center.x, cell.center.z);

                // Check if the distance between the cell and the center cell is within the given radius
                if (Vector2.Distance(centerPos, cellPos) <= radius)
                {
                    //If the distance is within the radius, add the current cell to the list of selected cells
                    selectedCells.Add(cell);
                    found++;
                    if (limitToMax && found >= maximum) break;
                }
            }
            //Return the list of selected cells
            return selectedCells;
        }

        public static (List<HexagonCellPrototype>, HexagonCellPrototype) SelectCellsInRadiusOfRandomCell(List<HexagonCellPrototype> cells, float radius, int maximum = -1)
        {
            //Select a random center cell
            HexagonCellPrototype centerCell = cells[UnityEngine.Random.Range(0, cells.Count)];
            return (SelectCellsInRadiusOfCell(cells, centerCell, radius, maximum), centerCell);
        }

        #endregion


        public static HexagonCellPrototype GetClosestPrototypeXZ(List<HexagonCellPrototype> prototypes, Vector3 position)
        {
            HexagonCellPrototype nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(prototypes[i].center.x, prototypes[i].center.z));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = prototypes[i];
                }
            }
            return nearest;
        }

        public static (HexagonCellPrototype, int) GetClosestPrototypeXZ_WithIndex(List<HexagonCellPrototype> prototypes, Vector3 position)
        {
            HexagonCellPrototype nearest = null;
            float nearestDist = float.MaxValue;
            int nearestIX = -1;

            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(prototypes[i].center.x, prototypes[i].center.z));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = prototypes[i];
                    nearestIX = i;
                }
            }
            return (nearest, nearestIX);
        }

        public static HexagonCellPrototype GetClosestPrototypeXYZ(List<HexagonCellPrototype> prototypes, Vector3 position)
        {
            HexagonCellPrototype nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = Vector3.Distance(position, prototypes[i].center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = prototypes[i];
                }
            }
            return nearest;
        }

        public static float GetAverageElevationOfClosestPrototypes(List<HexagonCellPrototype> prototypes, Vector3 position, float maxDistance)
        {
            HexagonCellPrototype nearest = prototypes[0];
            float nearestDist = float.MaxValue;
            float sum = 0;
            int found = 0;
            // float sum = position.y;
            // int found = 1;

            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = Vector3.Distance(position, prototypes[i].center);
                if (dist < maxDistance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = prototypes[i];
                    found++;
                    sum += prototypes[i].center.y;
                }
            }
            if (found > 0)
            {
                float average = sum / found;
                return average;
            }
            return position.y;
        }

        public static int GetLowestLayerInList(List<HexagonCellPrototype> prototypes)
        {
            int lowestLayer = -1;
            foreach (HexagonCellPrototype item in prototypes)
            {
                if (lowestLayer == -1 || lowestLayer > item.GetGridLayer()) lowestLayer = item.GetGridLayer();
            }
            return lowestLayer;
        }
        public static int GetHighestLayerInList(List<HexagonCellPrototype> prototypes)
        {
            int highestLayer = -1;
            foreach (HexagonCellPrototype item in prototypes)
            {
                if (highestLayer == -1 || highestLayer < item.GetGridLayer()) highestLayer = item.GetGridLayer();
            }
            return highestLayer;
        }

        public static Vector2Int GetLayerBoundsOfList(List<HexagonCellPrototype> prototypes)
        {
            Vector2Int lowest_HighestLayers = new Vector2Int(
                GetLowestLayerInList(prototypes),
                GetHighestLayerInList(prototypes)
            );
            return lowest_HighestLayers;
        }

        public static HexagonCellPrototype ChangeGroundCellOnLayerStack(HexagonCellPrototype currentGroundPrototype, int desiredLayer)
        {
            HexagonCellPrototype desiredCell = GetCellInLayerStack(currentGroundPrototype, desiredLayer);
            if (desiredCell == null) return currentGroundPrototype;

            SetToGroundLevel(desiredCell);

            return desiredCell;
        }

        public static HexagonCellPrototype GetCellInLayerStack(HexagonCellPrototype prototype, int desiredLayer)
        {
            if (desiredLayer == prototype.layer) return prototype;

            int verticalDirection = prototype.layer < desiredLayer ? 1 : 0;

            HexagonCellPrototype currentPrototype = prototype;
            bool found = false;

            while (found == false && currentPrototype != null)
            {
                if (currentPrototype.layer != desiredLayer)
                {
                    currentPrototype = currentPrototype.layerNeighbors[verticalDirection];
                }
                else
                {
                    found = true;
                    return currentPrototype;
                }
            }
            return null;
        }

        public static HexagonCellPrototype FindBoundsCellInLayerStack(Vector2 elevationBottom_Top, HexagonCellPrototype baseCell)
        {
            int verticalDirection = (elevationBottom_Top.x >= baseCell.center.y) ? 1 : 0;

            HexagonCellPrototype currentCell = baseCell;
            bool found = false;

            while (found == false && currentCell != null)
            {
                if (HexCellUtil.IsBlockWithinCellVerticalBounds(elevationBottom_Top, currentCell))
                {
                    found = true;
                    return currentCell;
                }
                else currentCell = currentCell.layerNeighbors[verticalDirection];
            }
            return null;
        }

        public static HexagonCellPrototype GetTopLayerInStack(HexagonCellPrototype prototypeCell)
        {
            HexagonCellPrototype currentPrototype = prototypeCell;
            bool found = false;
            while (found == false && currentPrototype != null)
            {
                if (currentPrototype.HasTopNeighbor())
                {
                    currentPrototype = currentPrototype.layerNeighbors[1];
                }
                else
                {
                    found = true;
                    return currentPrototype;
                }
            }
            return null;
        }

        public static HexagonCellPrototype GetGroundLayerNeighbor(HexagonCellPrototype prototypeCell)
        {
            int verticalDirection = prototypeCell.layer == 0 ? 1 : prototypeCell.cellStatus == CellStatus.AboveGround ? 0 : 1;
            HexagonCellPrototype currentPrototype = prototypeCell;
            bool groundFound = false;

            while (groundFound == false && currentPrototype != null)
            {
                if (currentPrototype.IsGround() == false)
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

        public static HexagonCellPrototype GetGroundCellInLayerStack(HexagonCellPrototype prototype)
        {
            HexagonCellPrototype currentPrototype = prototype;
            bool groundFound = false;

            // Get to Top layer
            while (!groundFound && currentPrototype != null)
            {
                if (currentPrototype.IsGround())
                {
                    groundFound = true;
                    return currentPrototype;
                }
                currentPrototype = currentPrototype.layerNeighbors[1];
            }

            // Dig down to find ground layer
            currentPrototype = prototype.layerNeighbors[0];

            while (!groundFound && currentPrototype != null)
            {
                if (currentPrototype.IsGround())
                {
                    groundFound = true;
                    return currentPrototype;
                }
                currentPrototype = currentPrototype.layerNeighbors[0];
            }

            return null;
        }

        public static List<HexagonCellPrototype> GetAllCellsInLayerStack(HexagonCellPrototype prototype)
        {
            List<HexagonCellPrototype> stackCells = new List<HexagonCellPrototype>();
            HashSet<string> added = new HashSet<string>();
            // string  stackId = prototype.GetLayerStackId();

            HexagonCellPrototype currentPrototype = prototype;

            // Get to Top layer
            while (currentPrototype != null)
            {
                if (currentPrototype != null && added.Contains(currentPrototype.GetId()) == false)
                {
                    stackCells.Add(currentPrototype);
                    added.Add(currentPrototype.GetId());
                }
                currentPrototype = currentPrototype.layerNeighbors[1];
            }

            // Search down to base
            currentPrototype = prototype.layerNeighbors[0];

            while (currentPrototype != null)
            {
                if (currentPrototype != null && added.Contains(currentPrototype.GetId()) == false)
                {
                    stackCells.Add(currentPrototype);
                    added.Add(currentPrototype.GetId());
                }
                currentPrototype = currentPrototype.layerNeighbors[0];
            }
            return stackCells;
        }


        public static bool HasClusterCellInLayerStack(HexagonCellPrototype prototype)
        {
            return FindClusterCellInLayerStack(prototype) != null;
        }

        public static HexagonCellPrototype FindClusterCellInLayerStack(HexagonCellPrototype prototype)
        {
            HexagonCellPrototype currentPrototype = prototype;
            bool found = false;

            // Get to Top layer
            while (!found && currentPrototype != null)
            {
                if (currentPrototype.IsInCluster())
                {
                    found = true;
                    return currentPrototype;
                }
                currentPrototype = currentPrototype.layerNeighbors[1];
            }

            // Search down to base
            currentPrototype = prototype.layerNeighbors[0];

            while (!found && currentPrototype != null)
            {
                if (currentPrototype.IsInCluster())
                {
                    found = true;
                    return currentPrototype;
                }
                currentPrototype = currentPrototype.layerNeighbors[0];
            }
            return null;
        }

        public static HexagonCellPrototype GetTopCellInLayerStack(HexagonCellPrototype prototype)
        {
            HexagonCellPrototype currentPrototype = prototype;

            while (currentPrototype.layerNeighbors[1] != null)
            {
                currentPrototype = currentPrototype.layerNeighbors[1];
            }
            return currentPrototype;
        }

        public static List<HexagonCellPrototype> GetAllUpperCellsInLayerStack(HexagonCellPrototype prototype)
        {
            List<HexagonCellPrototype> upperPrototypes = new List<HexagonCellPrototype>();
            HexagonCellPrototype currentPrototype = prototype;
            while (currentPrototype.layerNeighbors[1] != null)
            {
                currentPrototype = currentPrototype.layerNeighbors[1];
                upperPrototypes.Add(currentPrototype);
            }
            return upperPrototypes;
        }


        #region Pathing 


        public static bool HasGridHostCellsOnSideNeighborStack(HexagonCellPrototype prototype, int side)
        {
            if (prototype.neighborsBySide == null)
            {
                Debug.LogError("neighborsBySide not initiated.");
                return false;
            }

            HexagonCellPrototype currentPrototype = prototype.neighborsBySide[side];

            if (currentPrototype != null && currentPrototype.IsGridHost())
            {
                return true;
            }
            else
            {
                if (currentPrototype != null && currentPrototype.IsGround() == false)
                {
                    currentPrototype = GetGroundLayerNeighbor(currentPrototype);

                    while (currentPrototype != null)
                    {
                        if (currentPrototype.IsGridHost()) return true;

                        currentPrototype = currentPrototype.layerNeighbors[1];
                    }
                    return false;
                }
                else
                {
                    HexagonCellPrototype nextLayerUp = prototype;
                    while (currentPrototype == null && nextLayerUp != null)
                    {
                        nextLayerUp = nextLayerUp.layerNeighbors[1];
                        if (nextLayerUp != null) currentPrototype = nextLayerUp.neighborsBySide[side];
                    }

                    if (currentPrototype == null) return false;

                    List<HexagonCellPrototype> upperPrototypesOfSide;
                    upperPrototypesOfSide = GetAllUpperCellsInLayerStack(currentPrototype);
                    foreach (HexagonCellPrototype item in upperPrototypesOfSide)
                    {
                        if (item.IsGridHost()) return true;
                    }
                    return false;
                }
            }
        }

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
                            if (item.neighborsBySide[i].IsPath()) sidesWithPathNeighbor.Add(i);
                        }
                    }
                }
                else
                {
                    if (sideNeighbor.IsPath()) sidesWithPathNeighbor.Add(i);
                }
            }
            return sidesWithPathNeighbor;
        }


        public static List<HexagonCellPrototype> GenerateRandomPathBetweenCells(List<HexagonCellPrototype> pathFocusCells, bool ignoreEdgeCells = false, bool allowNonGroundCells = false)
        {
            List<HexagonCellPrototype> initialPath = new List<HexagonCellPrototype>();

            HashSet<string> pathEnds = new HashSet<string>();

            for (int i = 0; i < pathFocusCells.Count; i++)
            {
                for (int j = 1; j < pathFocusCells.Count; j++)
                {
                    HexagonCellPrototype focusA = pathFocusCells[i];
                    HexagonCellPrototype focusB = pathFocusCells[j];

                    // List<HexagonCellPrototype> newPath = HexGridPathingUtil.FindPathToCell(focusA.neighbors.Find(n => !n.IsInCluster()), focusB, ignoreEdgeCells);
                    List<HexagonCellPrototype> newPath = HexGridPathingUtil.FindPath(focusA.neighbors.Find(n => !n.IsInCluster()), focusB, ignoreEdgeCells);

                    if (newPath != null && newPath.Count > 0)
                    {
                        newPath = newPath.FindAll(p => HasClusterCellInLayerStack(p) == false);
                        // newPath = newPath.FindAll(p => p.IsInCluster() == false && !p.layerNeighbors.ToList().Any(n => n != null && n.IsInCluster()));

                        int IX = 0;
                        while (IX < newPath.Count)
                        {
                            for (int l = IX + 1; l < newPath.Count; l++)
                            {
                                if (newPath[IX].GetLookup() == newPath[l].GetLookup())
                                {
                                    newPath.Remove(newPath[l]);
                                    IX = 0;

                                    Debug.LogError("GenerateRandomPathBetweenCells - Removed cell with shared layer stack");

                                    continue;
                                }
                            }
                            IX++;
                        }

                        bool validEndFoundA = false;
                        bool validEndFoundB = false;
                        int attempts = 999;
                        while ((!validEndFoundA || !validEndFoundB) && newPath.Count > 0 && attempts > 0)
                        {
                            attempts--;

                            if (!validEndFoundA)
                            {
                                (HexagonCellPrototype pathEnd_A, int ix_A) = GetClosestPrototypeXZ_WithIndex(newPath, focusA.center);
                                // if (pathEnd_A.layerNeighbors.ToList().Any(n => n != null && n.IsInCluster()))
                                if (HasClusterCellInLayerStack(pathEnd_A))
                                {
                                    newPath.Remove(pathEnd_A);
                                    Debug.LogError("GenerateRandomPathBetweenCells - validEndFoundA - Removed cell with shared layer stack");
                                }
                                else
                                {
                                    if (pathEnd_A.layer != focusA.layer) newPath[ix_A] = ChangeGroundCellOnLayerStack(pathEnd_A, focusA.layer);
                                    if (ix_A != -1)
                                    {
                                        pathEnds.Add(newPath[ix_A].GetId());
                                        validEndFoundA = true;
                                    }
                                }
                            }
                            if (!validEndFoundB)
                            {
                                (HexagonCellPrototype pathEnd_B, int ix_B) = GetClosestPrototypeXZ_WithIndex(newPath, focusB.center);
                                // if ( pathEnd_B.layerNeighbors.ToList().Any(n => n != null && n.IsInCluster()))
                                if (HasClusterCellInLayerStack(pathEnd_B))
                                {
                                    newPath.Remove(pathEnd_B);
                                    Debug.LogError("GenerateRandomPathBetweenCells - validEndFoundB - Removed cell with shared layer stack");
                                }
                                else
                                {
                                    if (pathEnd_B.layer != focusB.layer) newPath[ix_B] = ChangeGroundCellOnLayerStack(pathEnd_B, focusB.layer);
                                    if (ix_B != -1)
                                    {
                                        pathEnds.Add(newPath[ix_B].GetId());
                                        validEndFoundB = true;
                                    }
                                }
                            }
                            // HexagonCellPrototype sharedStack =  newPath[ixA].layerNeighbors.ToList().Find(n => newPath.Contains(n));
                        }

                        // (HexagonCellPrototype pathEndA, int ixA) = GetClosestPrototypeXZ_WithIndex(newPath, focusA.center);

                        // if (pathEndA.layer != focusA.layer)
                        // {
                        //     Debug.Log("GenerateRandomPathBetweenCells - pathEndA.layer: " + pathEndA.layer + ", focusA layer: " + focusA.layer);

                        //     newPath[ixA] = ChangeGroundCellOnLayerStack(pathEndA, focusA.layer);
                        // }
                        // if (ixA != -1)
                        // {
                        //     pathEnds.Add(newPath[ixA].GetId());
                        // }
                        // else
                        // {
                        //     Debug.LogError("GenerateRandomPathBetweenCells - NO ixA");
                        // }

                        // (HexagonCellPrototype pathEndB, int ixB) = GetClosestPrototypeXZ_WithIndex(newPath, focusB.center);
                        // if (pathEndB.layer != focusB.layer)
                        // {
                        //     Debug.Log("GenerateRandomPathBetweenCells - pathEndB.layer: " + pathEndB.layer + ", focusB layer: " + focusB.layer);

                        //     newPath[ixB] = ChangeGroundCellOnLayerStack(pathEndB, focusB.layer);
                        // }
                        // if (ixB != -1)
                        // {
                        //     pathEnds.Add(newPath[ixB].GetId());
                        // }
                        // else
                        // {
                        //     Debug.LogError("GenerateRandomPathBetweenCells - NO ixB");
                        // }

                        // if (ixA != -1 && ixB != -1)
                        // {
                        //     newPath = FindPath(newPath[ixA], newPath[ixB], ignoreEdgeCells);
                        // }

                        newPath = newPath.FindAll(p => pathEnds.Contains(p.GetId()) || (!pathEnds.Contains(p.GetId()) && !p.layerNeighbors.ToList().Any(n => n != null && pathEnds.Contains(p.GetId()))));

                        initialPath.AddRange(newPath);
                    }
                }
            }

            // Exclude pathFocusCells from final path
            initialPath = initialPath.FindAll(c => c.IsPreAssigned() == false);// && !c.layerNeighbors.Any(n => n != null && pathEnds.Contains(n.GetId())));

            if (initialPath.Count == 0)
            {
                Debug.LogError("initialPath is empty");
                return null;
            }


            List<HexagonCellPrototype> finalPath = new List<HexagonCellPrototype>();
            // Debug.Log("GenerateRandomPath - invalids: " + invalids.Count + ", results: " + result.Count);

            int endLayerSum = 0;
            int ends = 0;

            for (int i = 0; i < initialPath.Count; i++)
            {
                if (pathEnds.Contains(initialPath[i].GetId()))
                {
                    (HexagonCellPrototype closestFocusCell, int ix) = GetClosestPrototypeXZ_WithIndex(pathFocusCells, initialPath[i].center);
                    if (closestFocusCell != null)
                    {
                        if (initialPath[i].layer != closestFocusCell.layer)
                        {
                            initialPath[i] = ChangeGroundCellOnLayerStack(initialPath[i], closestFocusCell.layer);
                        }

                        HexagonCellPrototype currentCell = initialPath[i];

                        endLayerSum += currentCell.GetGridLayer();
                        ends++;

                        currentCell.SetPathCell(true, PathCellType.End);

                        SetToGroundLevel(currentCell);

                        finalPath.Add(currentCell);
                    }

                }
            }

            initialPath = initialPath.Except(finalPath).ToList();

            int avgEndLayer = endLayerSum / ends;
            Debug.Log("GenerateRandomPathBetweenCells - avgEndLayer: " + avgEndLayer + ", ends: " + ends + ", endLayerSum: " + endLayerSum);

            for (int i = 0; i < initialPath.Count; i++)
            {
                HexagonCellPrototype currentCell = initialPath[i];

                if (pathEnds.Contains(currentCell.GetId()))
                {
                    currentCell.SetPathCell(true);
                    // currentCell.SetPathCell(true);
                    // currentCell.isPathEnd = true;
                    // currentCell.isPathStart = i == 0;
                    SetToGroundLevel(currentCell);

                    finalPath.Add(currentCell);

                    continue;
                }

                bool reject = false;
                foreach (var item in finalPath)
                {
                    if (item.GetLookup() == currentCell.GetLookup())
                    {
                        Debug.Log("GenerateRandomPathBetweenCells - cell in finalPath has current as layer neighbor ");

                        if (pathEnds.Contains(currentCell.GetId()))
                        {
                            Debug.Log("GenerateRandomPathBetweenCells - currentCell is pathEnd");
                        }
                        else
                        {
                            reject = true;
                            break;
                        }
                    }
                }

                HexagonCellPrototype prevCell = GetClosestPrototypeXZ(finalPath, currentCell.center);

                if (reject) continue;

                int compareLayer = (prevCell != null) ? prevCell.GetGridLayer() : avgEndLayer;
                int layerDiff = Mathf.Abs(compareLayer - currentCell.GetGridLayer());

                if (layerDiff > 1)
                {
                    int desiredLayer = compareLayer > currentCell.GetGridLayer() ? compareLayer - 1 : compareLayer + 1;
                    HexagonCellPrototype updatedCell = ChangeGroundCellOnLayerStack(currentCell, desiredLayer);
                    updatedCell.SetPathCell(true);
                    finalPath.Add(updatedCell);
                    continue;
                }

                if (currentCell.IsDisposable() || (allowNonGroundCells == false && !currentCell.IsGround()) || (allowNonGroundCells && !currentCell.IsGround() && !currentCell.HasGroundSideNeighbor()))
                {
                    int desiredLayer = compareLayer > currentCell.GetGridLayer() ? compareLayer - 1 : compareLayer + 1;
                    HexagonCellPrototype updatedCell = ChangeGroundCellOnLayerStack(currentCell, desiredLayer);
                    updatedCell.SetPathCell(true);
                    finalPath.Add(updatedCell);
                }
                else
                {
                    currentCell.SetPathCell(true);
                    finalPath.Add(currentCell);
                }
            }

            return finalPath;
            // return ClearPathCellClumps(finalPath);
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
                    if (assign) edgePrototype.isEntryCell = true;
                }

            }
            return entrances;
        }

        public static List<HexagonCellPrototype> PickRandomEntryFromGridEdges(List<HexagonCellPrototype> allGridEdges, int num, bool assign, bool excludeAdjacentNeighbors = true)
        {
            List<HexagonCellPrototype> possibles = new List<HexagonCellPrototype>();
            foreach (HexagonCellPrototype edgePrototype in allGridEdges)
            {
                if (edgePrototype.IsGround() == false) continue;
                int groundNeighborCount = edgePrototype.neighbors.FindAll(
                        n => n.IsGround() && n.layer == edgePrototype.layer).Count;
                if (groundNeighborCount >= 3) possibles.Add(edgePrototype);
            }
            return GetRandomEntryPrototypes(possibles, num, assign, -1, excludeAdjacentNeighbors);
        }

        public static void EvaluateCellNeighborsAndEdgesInLayerList(List<HexagonCellPrototype> allLayerPrototypes, EdgeCellType edgeCellType, Transform transform, bool assignOriginalGridEdge)
        {
            HexCellUtil.PopulateNeighborsFromCornerPoints(allLayerPrototypes, transform);
            HexagonCellPrototype.GetEdgePrototypes(allLayerPrototypes, EdgeCellType.Default, assignOriginalGridEdge);
        }
        public static void EvaluateCellNeighborsAndEdgesInLayerList(List<HexagonCellPrototype> allLayerPrototypes, EdgeCellType edgeCellType, bool assignOriginalGridEdge)
        {
            HexCellUtil.PopulateNeighborsFromCornerPoints(allLayerPrototypes);
            HexagonCellPrototype.GetEdgePrototypes(allLayerPrototypes, EdgeCellType.Default, assignOriginalGridEdge);
        }


        public static void ClearNeighborLists(List<HexagonCellPrototype> cells)
        {
            foreach (HexagonCellPrototype currentCell in cells)
            {
                currentCell.ClearNeighborLists();
            }
        }

        public static void ResetNeighbors(List<HexagonCellPrototype> cells, EdgeCellType edgeCellType)
        {
            ClearNeighborLists(cells);
            EvaluateCellNeighborsAndEdgesInLayerList(cells, edgeCellType, false);
        }

        public static List<HexagonCellPrototype> GetEdgePrototypes(List<HexagonCellPrototype> prototypes, EdgeCellType edgeCellType, bool assignOriginalGridEdge = false)
        {
            List<HexagonCellPrototype> edgePrototypes = new List<HexagonCellPrototype>();
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                if (EvaluateForEdge(prototype, edgeCellType, assignOriginalGridEdge)) edgePrototypes.Add(prototype);
            }
            return edgePrototypes;
        }

        public static bool EvaluateForEdge(HexagonCellPrototype prototype, EdgeCellType edgeCellType, bool assignOriginalGridEdge)
        {
            if (prototype == null) return false;

            List<HexagonCellPrototype> allSideNeighbors = prototype.neighbors.FindAll(c => c.IsSameLayer(prototype) && c.IsRemoved() == false);
            List<HexagonCellPrototype> allSiblingSideNeighbors = allSideNeighbors.FindAll(n => n.parentId == prototype.parentId);

            int sideNeighborCount = allSideNeighbors.Count;
            bool isEdge = false;

            if (allSiblingSideNeighbors.Count < 6)
            {
                if (assignOriginalGridEdge == true) prototype.SetOriginalGridEdge(true);

                prototype.isEdgeOfParent = true;
            }

            if (allSiblingSideNeighbors.Any(n => n.IsPath())) prototype.isEdgeTOPath = true;

            if (sideNeighborCount < 6)
            {
                prototype.SetEdgeCell(true, edgeCellType);
                isEdge = true;
            }
            else
            {
                prototype.SetEdgeCell(false);
            }
            return isEdge;
        }

        public static void CleanupCellIslandLayerPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, int islandMemberMin = 3)
        {
            int totalLayers = prototypesByLayer.Keys.Count;
            int currentLayer = totalLayers - 1;
            do
            {
                List<HexagonCellPrototype> prototypesForLayer = prototypesByLayer[currentLayer];
                Dictionary<int, List<HexagonCellPrototype>> prototypesByIsland = GetIslandsFromLayerPrototypes(prototypesForLayer.FindAll(p => p.IsGround()));

                // Debug.Log("CleanupCellIslandLayerPrototypes - Layer: " + currentLayer + ", islands: " + prototypesByIsland.Keys.Count);

                bool isTopLayer = currentLayer == totalLayers;
                bool isBottomLayer = currentLayer == 0;
                int layerTarget = isBottomLayer ? 1 : 0;

                foreach (var kvpB in prototypesByIsland)
                {
                    // Restore Ground assignment to layer above if island on base layer has only too few cells
                    if (kvpB.Value.Count >= islandMemberMin) continue;

                    // Debug.Log("CleanupCellIslandLayerPrototypes - Layer: " + currentLayer + ", islandMembers: " + kvpB.Value.Count);

                    for (var i = 0; i < kvpB.Value.Count; i++)
                    {
                        // if (!isBottomLayer && currentLayer < totalLayers ) {
                        // }
                        HexagonCellPrototype targetNeighbor = kvpB.Value[i].layerNeighbors[layerTarget];

                        if (targetNeighbor != null && kvpB.Value[i].IsGround() && targetNeighbor.neighbors.FindAll(n => n.layer == targetNeighbor.layer && n.IsGround())?.Count > 0)
                        {
                            targetNeighbor.SetToGround(false);
                            kvpB.Value[i].cellStatus = isBottomLayer ? CellStatus.UnderGround : CellStatus.AboveGround;
                        }
                    }
                }
                currentLayer--;

            } while (currentLayer > -1);
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

        #region Consecutive Spread



        public static List<HexagonCellPrototype> GetConsecutiveNeighborsCluster(HexagonCellPrototype prototype, CellStatus status, int maxMembers = 6)
        {
            HashSet<string> visited = new HashSet<string>();
            List<HexagonCellPrototype> cluster = new List<HexagonCellPrototype>();

            VisitConsecutiveNeighbors(prototype, cluster, visited, status, maxMembers);

            return cluster;
        }

        private static void VisitConsecutiveNeighbors(HexagonCellPrototype prototype, List<HexagonCellPrototype> cluster, HashSet<string> visited, CellStatus status, int maxMembers = 3)
        {
            cluster.Add(prototype);
            visited.Add(prototype.id);

            // if (cluster.Count >= maxMembers) return;

            // Debug.Log("VisitNeighbors - prototype.neighbors: " + prototype.neighbors.Count);

            for (int i = 0; i < prototype.neighbors.Count; i++)
            {
                if (cluster.Count >= maxMembers) break;

                HexagonCellPrototype neighbor = prototype.neighbors[i];

                if (neighbor.GetCellStatus() != status) continue;

                if (!visited.Contains(neighbor.id))
                {
                    VisitConsecutiveNeighbors(neighbor, cluster, visited, status, maxMembers);
                }
            }
        }
        #endregion

        public static List<int> GetGridEdgeVertexIndices(List<HexagonCellPrototype> allGroundEdgePrototypes, TerrainVertex[,] vertexGrid, float searchDistance = 36f)
        {
            List<int> vertexIndices = new List<int>();

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];
                    if (currentVertex.type == VertexType.Generic || currentVertex.type == VertexType.Unset)
                    {
                        Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
                        foreach (HexagonCellPrototype edge in allGroundEdgePrototypes)
                        {
                            Vector2 currentPosXZ = new Vector2(edge.center.x, edge.center.z);
                            float dist = Vector2.Distance(currentPosXZ, vertexPosXZ);
                            if (dist < searchDistance)
                            {
                                vertexIndices.Add(currentVertex.index);
                                vertexGrid[x, z].isOnTheEdgeOftheGrid = true;
                                break;
                            }
                        }
                    }
                }

            }
            return vertexIndices;
        }

        public static List<int> GetGridEdgeVertexIndices(List<Vector3> gridEdgeCornerPoints, TerrainVertex[,] vertexGrid, float searchDistance = 12f)
        {
            List<int> vertexIndices = new List<int>();

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];
                    if (currentVertex.type == VertexType.Generic || currentVertex.type == VertexType.Unset)
                    {
                        Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
                        foreach (Vector3 edgePoint in gridEdgeCornerPoints)
                        {
                            Vector2 currentPosXZ = new Vector2(edgePoint.x, edgePoint.z);
                            float dist = Vector2.Distance(currentPosXZ, vertexPosXZ);
                            if (dist < searchDistance)
                            {
                                vertexIndices.Add(currentVertex.index);
                                vertexGrid[x, z].isOnTheEdgeOftheGrid = true;
                                break;
                            }
                        }
                    }
                }

            }
            return vertexIndices;
        }


        public static List<Vector3> GetEdgeCornersOfEdgePrototypes(List<HexagonCellPrototype> allGroundEdgePrototypes)
        {
            List<Vector3> points = new List<Vector3>();
            foreach (HexagonCellPrototype edge in allGroundEdgePrototypes)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (edge.neighborsBySide[i] == null)
                    {
                        (HexagonCorner cornerA, HexagonCorner cornerB) = HexCoreUtil.GetCornersFromSide((HexagonSide)i);
                        points.Add(edge.sidePoints[i]);
                        points.Add(edge.cornerPoints[(int)cornerA % 6]);
                        points.Add(edge.cornerPoints[(int)cornerB % 6]);
                    }
                }
            }
            return points;
        }

        public static bool IsPrototypeCenterBelowAllVertices(
            HexagonCellPrototype prototype,
            List<int> vertexIndices,
            TerrainVertex[,] vertexGrid,
            Transform transform,
            float distanceYOffset
        )
        {
            if (vertexIndices == null || vertexIndices.Count == 0) return false;

            Vector3 cellPos = transform != null ? transform.TransformPoint(prototype.center) : prototype.center;
            float minElevation = (cellPos.y + distanceYOffset);

            int gridLength = vertexGrid.GetLength(0);

            foreach (int ix in vertexIndices)
            {
                TerrainVertex vertex = vertexGrid[ix / gridLength, ix % gridLength];
                Vector3 vertPosXYZ = transform != null ? transform.TransformPoint(vertex.position) : vertex.position;

                if (minElevation > vertPosXYZ.y) return false;
            }
            return true;
        }

        // public static HexagonCellPrototype ClearLayersAboveVertexElevationsAndSetGround(
        //     HexagonCellPrototype prototypeCell,
        //     List<int> vertexIndices,
        //     List<int>[] vertexIndicesBySide,
        //     TerrainVertex[,] vertexGrid,
        //     Transform transform,
        //     float distanceYOffset = 0.8f,
        //     bool fallbackOnBottomCell = false
        // )
        // {
        //     HexagonCellPrototype currentPrototype = prototypeCell;
        //     bool groundFound = false;
        //     while (groundFound == false && currentPrototype != null)
        //     {
        //         if (IsPrototypeCenterBelowAllVertices(currentPrototype, vertexIndices, vertexGrid, transform, distanceYOffset) == false)
        //         {
        //             // Debug.Log("ClearLayersAboveVertexElevationsAndSetGround - A");

        //             if (fallbackOnBottomCell && currentPrototype.layerNeighbors[0] == null)
        //             {
        //                 currentPrototype.SetToGround();
        //                 currentPrototype.vertexList = currentPrototype

        //                 currentPrototype._vertexIndices = vertexIndices;
        //                 currentPrototype._vertexIndicesBySide = vertexIndicesBySide;
        //             }
        //             else
        //             {
        //                 currentPrototype.SetCellStatus(CellStatus.AboveGround);

        //                 currentPrototype = currentPrototype.layerNeighbors[0];
        //             }
        //         }
        //         else
        //         {
        //             // Debug.Log("ClearLayersAboveVertexElevationsAndSetGround - B");

        //             groundFound = true;
        //             currentPrototype._vertexIndices = vertexIndices;
        //             currentPrototype._vertexIndicesBySide = vertexIndicesBySide;

        //             SetToGroundLevel(currentPrototype);
        //         }
        //     }
        //     if (groundFound) return currentPrototype;
        //     return null;
        // }

        // public static void ClearLayersAboveElevationAndSetGround(HexagonCellPrototype prototypeCell, float elevationY, float distanceYOffset = 1.8f, bool fallbackOnBottomCell = false)
        // {
        //     // Debug.Log("ClearLayersAboveElevationAndSetGround - elevationY:" + elevationY);
        //     // Set every cell below as underground
        //     HexagonCellPrototype currentPrototype = prototypeCell;
        //     bool groundFound = false;

        //     while (groundFound == false && currentPrototype != null)
        //     {
        //         if ((currentPrototype.center.y - distanceYOffset) > elevationY)
        //         {
        //             currentPrototype.cellStatus = CellStatus.AboveGround;

        //             if (fallbackOnBottomCell && currentPrototype.layerNeighbors[0] == null)
        //             {
        //                 currentPrototype.SetToGround();
        //             }
        //             else
        //             {
        //                 currentPrototype = currentPrototype.layerNeighbors[0];
        //             }
        //         }
        //         else
        //         {
        //             groundFound = true;
        //             SetToGroundLevel(currentPrototype);
        //         }
        //     }
        // }

        public static void SetToGroundLevel(HexagonCellPrototype prototypeCell)
        {
            // Set as ground cell
            prototypeCell.SetToGround(false);
            // Set every cell below as UnderGround
            HexagonCellPrototype bottomNeighbor = prototypeCell.layerNeighbors[0];
            while (bottomNeighbor != null)
            {
                bottomNeighbor.SetCellStatus(CellStatus.UnderGround);
                bottomNeighbor = bottomNeighbor.layerNeighbors[0];
            }
            // Set every cell above as AboveGround
            HexagonCellPrototype topNeighbor = prototypeCell.layerNeighbors[1];
            while (topNeighbor != null)
            {
                topNeighbor.SetCellStatus(CellStatus.AboveGround);
                topNeighbor = topNeighbor.layerNeighbors[1];
            }
        }

        public static void AssignParentsToChildren(List<HexagonCellPrototype> children, List<HexagonCellPrototype> parents)
        {
            foreach (HexagonCellPrototype child in children)
            {
                foreach (HexagonCellPrototype parent in parents)
                {
                    if (VectorUtil.IsPointWithinPolygon(child.center, parent.cornerPoints))
                    {
                        parent.SetChild(child);

                        // Debug.Log("AssignParentsToChildren - child - Size: " + child.GetSize() + ", Layer: " + child.GetGridLayer() + ",  parent - Size: " + parent.GetSize() + ", Layer: " + parent.GetGridLayer() + ", parentId: " + parent.GetId());
                        break;
                    }
                }
            }
        }


        public static Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> GenerateGridsByLayerBySize(
            Dictionary<int, List<Vector3>> baseCenterPointsBySize,
            int cellLayers,
            int cellLayerOffset,
            IHexCell parentCell,
            int baseLayerOffset,
            Transform transform = null,
            bool preAssignGround = false
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> newCellPrototypesBySize = new Dictionary<int, List<HexagonCellPrototype>>();
            List<Vector3> toPassDown = new List<Vector3>();
            int prevSize = -1;
            int baseLayer = 0 + baseLayerOffset;

            Vector3[] parentCorners = parentCell != null ? HexCoreUtil.GenerateHexagonPoints(parentCell.GetPosition(), parentCell.GetSize()) : new Vector3[6];

            foreach (var kvp in baseCenterPointsBySize)
            {
                int currentSize = kvp.Key;

                toPassDown.AddRange(kvp.Value);
                if (baseCenterPointsBySize.ContainsKey(prevSize))
                {
                    foreach (var item in baseCenterPointsBySize[prevSize])
                    {
                        Vector3[] newCorners = HexCoreUtil.GenerateHexagonPoints(item, prevSize);
                        if (parentCell != null)
                        {
                            foreach (var newCorner in newCorners)
                            {
                                if (VectorUtil.IsPointWithinPolygon(newCorner, parentCorners)) toPassDown.Add(newCorner);
                            }
                        }
                        else toPassDown.AddRange(newCorners);
                    }
                }

                // Debug.Log("GenerateGridsByLayerBySize - currentSize: " + currentSize + ", Count: " + kvp.Value.Count + ", toPassDown: " + toPassDown.Count);

                List<HexagonCellPrototype> newCellPrototypes = GenerateCellsFromCenterPoints(
                        toPassDown,
                        currentSize,
                        (prevSize == -1) ? parentCell : null,
                        cellLayerOffset,
                        transform,
                        preAssignGround
                    );
                // List<HexagonCellPrototype> newCellPrototypes = HexagonCellPrototype.GenerateCellsFromCenterPoints(kvp.Value, currentSize, parentCell, transform);

                if (prevSize > currentSize) AssignParentsToChildren(newCellPrototypes, newCellPrototypesBySize[prevSize]);

                newCellPrototypesBySize.Add(currentSize, newCellPrototypes);
                prevSize = currentSize;
            }

            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> newPrototypesBySizeByLayer = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>();

            prevSize = -1;
            foreach (var kvp in baseCenterPointsBySize)
            {
                int currentSize = kvp.Key;

                int startingLayer = baseLayer;
                Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();
                newPrototypesByLayer.Add(startingLayer, newCellPrototypesBySize[currentSize]);

                if (cellLayers > 1)
                {
                    cellLayers += startingLayer;

                    for (int i = startingLayer + 1; i < cellLayers; i++)
                    {
                        List<HexagonCellPrototype> newLayer;
                        if (i == startingLayer + 1)
                        {
                            newLayer = DuplicateGridToNewLayer_Above(newPrototypesByLayer[startingLayer], cellLayerOffset, parentCell);
                        }
                        else
                        {
                            List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                            newLayer = DuplicateGridToNewLayer_Above(previousLayer, cellLayerOffset, parentCell);
                        }

                        if (prevSize > currentSize) AssignParentsToChildren(newLayer, newPrototypesBySizeByLayer[prevSize][i]);

                        newPrototypesByLayer.Add(i, newLayer);
                    }

                    newPrototypesBySizeByLayer.Add(currentSize, newPrototypesByLayer);

                    prevSize = currentSize;
                }
            }
            return newPrototypesBySizeByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateGridsByLayer(
            List<Vector3> baseCenterPoints,
            HexCellSizes cellSize,
            int cellLayers,
            int cellLayerOffset,
            IHexCell parentCell,
            int baseLayerOffset,
            Transform transform = null,
            bool preAssignGround = false
        )
        {
            int startingLayer = 0 + baseLayerOffset;

            List<HexagonCellPrototype> newCellPrototypes = GenerateCellsFromCenterPoints(baseCenterPoints, (int)cellSize, parentCell, cellLayerOffset, transform, preAssignGround);

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonCellPrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateGridToNewLayer_Above(newPrototypesByLayer[startingLayer], cellLayerOffset, parentCell);
                    }
                    else
                    {
                        List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateGridToNewLayer_Above(previousLayer, cellLayerOffset, parentCell);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                }
            }
            return newPrototypesByLayer;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> GenerateGridsByLayer(
            Vector3 centerPos,
            float radius,
            HexCellSizes cellSize,
            int cellLayers,
            int cellLayerOffset,
            IHexCell parentCell,
            int baseLayerOffset = 0,
            Transform transform = null,
            List<int> useCorners = null,
            bool useGridErosion = false,
            bool preAssignGround = false
        )
        {
            int startingLayer = 0 + baseLayerOffset;
            List<HexagonCellPrototype> newCellPrototypes = HexGridUtil.GenerateHexGrid(
                centerPos,
                (int)cellSize,
                (int)radius,
                parentCell,
                cellLayerOffset,
                transform,
                useCorners,
                false
            );

            // Debug.Log("GenerateGridsByLayer - newCellPrototypes: " + newCellPrototypes.Count);

            if (useGridErosion)
            {
                HexagonCellPrototype.GetRandomGridErosion(newCellPrototypes, UnityEngine.Random.Range(radius * 0.2f, radius * 0.36f), 32);
                newCellPrototypes = newCellPrototypes.FindAll(c => c.GetCellStatus() != CellStatus.Remove);
            }
            if (preAssignGround)
            {
                foreach (var item in newCellPrototypes)
                {
                    item.SetToGround(true);
                }
            }
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonCellPrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateGridToNewLayer_Above(newPrototypesByLayer[startingLayer], cellLayerOffset, parentCell);
                    }
                    else
                    {
                        List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateGridToNewLayer_Above(previousLayer, cellLayerOffset, parentCell);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                }
            }
            return newPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> GenerateGridsByLayer(Vector3 centerPos, float radius, HexCellSizes cellSize, int cellLayers, int cellLayerOffset, Vector2 gridGenerationCenterPosXZOffeset, int baseLayerOffset, string parentId = "", Transform transform = null, bool useCorners = true)
        {
            List<HexagonCellPrototype> newCellPrototypes = HexGridUtil.GenerateHexGrid(centerPos, (int)cellSize, (int)radius, null, cellLayerOffset, null);
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            int startingLayer = 0 + baseLayerOffset;
            newPrototypesByLayer.Add(startingLayer, newCellPrototypes);

            if (cellLayers > 1)
            {
                cellLayers += startingLayer;

                for (int i = startingLayer + 1; i < cellLayers; i++)
                {
                    List<HexagonCellPrototype> newLayer;
                    if (i == startingLayer + 1)
                    {
                        newLayer = DuplicateGridToNewLayer_Above(newPrototypesByLayer[startingLayer], cellLayerOffset, null);
                    }
                    else
                    {
                        List<HexagonCellPrototype> previousLayer = newPrototypesByLayer[i - 1];
                        newLayer = DuplicateGridToNewLayer_Above(previousLayer, cellLayerOffset, null);
                    }
                    newPrototypesByLayer.Add(i, newLayer);
                }
            }

            return newPrototypesByLayer;
        }

        public static List<HexagonCellPrototype> GenerateCellsFromCenterPoints(
            List<Vector3> baseCenterPoints,
            int size,
            IHexCell parentCell,
            int layerOffset,
            Transform transform = null,
            bool preAssignGround = false
        )
        {
            List<HexagonCellPrototype> newPrototypes = new List<HexagonCellPrototype>();
            HashSet<Vector3> added = new HashSet<Vector3>();

            for (int i = 0; i < baseCenterPoints.Count; i++)
            {
                Vector3 centerPoint = transform != null ? transform.TransformPoint(baseCenterPoints[i]) : baseCenterPoints[i];
                if (added.Contains(centerPoint)) continue;

                // Filter out duplicate points & out of bounds
                bool skip = false;
                foreach (HexagonCellPrototype item in newPrototypes)
                {
                    if (Vector3.Distance(centerPoint, item.center) < 2f)
                    {
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    newPrototypes.Add(new HexagonCellPrototype(centerPoint, size, parentCell, layerOffset, "-" + i, preAssignGround));
                    added.Add(centerPoint);
                }

            }
            return newPrototypes;
        }

        public static List<Vector2> GenerateChildrenLookupCoordinates_X11(Vector3 center, int size)
        {
            HexagonCellPrototype centerHex = new HexagonCellPrototype(center, size, false);
            List<Vector2> results = new List<Vector2>();
            results.Add(HexCoreUtil.Calculate_CenterLookup(center, size));

            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(centerHex.cornerPoints[i], centerHex.cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);

                results.Add(HexCoreUtil.Calculate_CenterLookup(sidePoint, size));

                // Get Corner
                float angle = 60f * i;
                float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 cornerPoint = new Vector3(x, center.y, z);
                direction = (cornerPoint - center).normalized;
                edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                cornerPoint = center + direction * (edgeDistance * 3f);

                results.Add(HexCoreUtil.Calculate_CenterLookup(cornerPoint, size));
            }
            // Debug.Log("child lookups: " + results.Count);
            return results;
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

        public static List<HexagonCellPrototype> DuplicateGridToNewLayer_Above(List<HexagonCellPrototype> _cells, int layerOffset, IHexCell parentCell, bool log = false)
        {
            List<HexagonCellPrototype> newCells = new List<HexagonCellPrototype>();
            foreach (var _cell in _cells)
            {
                if (_cell.GetCellStatus() == CellStatus.Remove) continue;

                HexagonCellPrototype newPrototype = DuplicateCellToNewLayer_Above(_cell, layerOffset, parentCell, log);
                newCells.Add(newPrototype);
            }
            return newCells;
        }

        public static HexagonCellPrototype DuplicateCellToNewLayer_Above(HexagonCellPrototype originalCell, int layerOffset, IHexCell parentCell, bool log = false)
        {
            Vector3 new_center = new Vector3(originalCell.center.x, originalCell.center.y + layerOffset, originalCell.center.z);
            HexagonCellPrototype new_cell = new HexagonCellPrototype(new_center, originalCell.size, parentCell, layerOffset);

            new_cell.bottomNeighborId = originalCell.id;
            new_cell.SetWorldSpaceLookup(originalCell.GetWorldSpaceLookup());

            // Set layer neighbors
            new_cell.layerNeighbors[0] = originalCell;
            if (new_cell.neighbors.Contains(originalCell) == false) new_cell.neighbors.Add(originalCell);

            originalCell.layerNeighbors[1] = new_cell;
            if (originalCell.neighbors.Contains(new_cell) == false) originalCell.neighbors.Add(new_cell);

            if (log) Debug.Log("new_cell - size: " + new_cell.size + ", neighbors: " + new_cell.neighbors.Count);
            return new_cell;
        }

        public static HexagonCellPrototype DuplicateCellToNewLayer_Below(HexagonCellPrototype originalCell, int layerOffset, IHexCell parentCell, bool log = false)
        {
            Vector3 new_center = new Vector3(originalCell.center.x, originalCell.center.y - layerOffset, originalCell.center.z);
            HexagonCellPrototype new_cell = new HexagonCellPrototype(new_center, originalCell.size, parentCell, layerOffset);

            new_cell.bottomNeighborId = originalCell.id;
            new_cell.SetWorldSpaceLookup(originalCell.GetWorldSpaceLookup());

            // Set layer neighbors
            new_cell.layerNeighbors[1] = originalCell;
            if (new_cell.neighbors.Contains(originalCell) == false) new_cell.neighbors.Add(originalCell);

            originalCell.layerNeighbors[0] = new_cell;
            if (originalCell.neighbors.Contains(new_cell) == false) originalCell.neighbors.Add(new_cell);

            if (log) Debug.Log("new_cell - size: " + new_cell.size + ", neighbors: " + new_cell.neighbors.Count);
            return new_cell;
        }

        public static List<HexagonCellPrototype> GetPrototypesWithinHexagon(
            List<HexagonCellPrototype> prototypes,
            Vector3 position,
            float radius,
            List<Vector3> hexEdgePoints,
            bool logResults = false
        )
        {
            List<HexagonCellPrototype> prototypesWithinRadius = new List<HexagonCellPrototype>();
            Vector2 posXZ = new Vector2(position.x, position.z);
            int skipped = 0;
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in prototype.cornerPoints)
                {
                    float distance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), posXZ);
                    if (distance < radius * 0.95f) continue;

                    isWithinRadius = VectorUtil.IsPositionWithinPolygon(hexEdgePoints, cornerPoint);
                    if (!isWithinRadius)
                    {
                        skipped++;
                        break;
                    }
                }
                if (isWithinRadius) prototypesWithinRadius.Add(prototype);
            }
            if (skipped > 0 && logResults) Debug.LogError("GetPrototypesWithinHexagon - filtered: " + skipped + " prototypes. Outside radius: " + radius);
            return prototypesWithinRadius;
        }

        public static bool IsFullyInsideHexagonParent(HexagonCellPrototype cell, List<Vector3> allParentEdgePoints, Vector2 parentCenter, float radius)
        {
            bool isWithinRadius = true;
            foreach (Vector3 cornerPoint in cell.cornerPoints)
            {
                float distance = VectorUtil.DistanceXZ(cell.center, parentCenter);
                if (distance < radius * 0.9f) continue;
                isWithinRadius = VectorUtil.IsPositionWithinPolygon(allParentEdgePoints, cornerPoint);
                if (!isWithinRadius)
                {
                    return false;
                }
            }
            return isWithinRadius;
        }

        public static (List<HexagonCellPrototype>, List<HexagonCellPrototype>) GetPrototypesWithinHexagon_WithExcluded(
            List<HexagonCellPrototype> prototypes,
            Vector3 position,
            float radius,
            List<Vector3> hexEdgePoints,
            bool logResults = false
        )
        {
            List<HexagonCellPrototype> prototypesWithinRadius = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> outsideTheRadius = new List<HexagonCellPrototype>();

            Vector2 posXZ = new Vector2(position.x, position.z);
            int skipped = 0;
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in prototype.cornerPoints)
                {
                    float distance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), posXZ);
                    if (distance < radius * 0.95f) continue;

                    isWithinRadius = VectorUtil.IsPositionWithinPolygon(hexEdgePoints, cornerPoint);
                    if (!isWithinRadius)
                    {
                        outsideTheRadius.Add(prototype);
                        skipped++;
                        break;
                    }
                }
                if (isWithinRadius) prototypesWithinRadius.Add(prototype);
            }
            if (skipped > 0 && logResults) Debug.LogError("GetPrototypesWithinHexagon - filtered: " + skipped + " prototypes. Outside radius: " + radius);
            return (prototypesWithinRadius, outsideTheRadius);
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


        public static void GizmoShowNeighbors(HexagonCellPrototype prototype, float centerRadius = 3f, bool useExternalColors = false)
        {
            if (!useExternalColors) Gizmos.color = Color.green;
            foreach (HexagonCellPrototype neighbor in prototype.neighbors)
            {
                Gizmos.DrawSphere(neighbor.center, centerRadius);
            }
        }

        public static void GizmoShowSideAndCorners(HexagonCellPrototype prototype, HexagonSide side, float sideSize = 3f, float cornerSize = 2f, bool useExternalColors = false)
        {
            if (!useExternalColors) Gizmos.color = Color.green;

            Gizmos.DrawSphere(prototype.sidePoints[(int)side], sideSize);
            Gizmos.DrawWireSphere(prototype.sidePoints[(int)side], sideSize * 2f);

            if (!useExternalColors) Gizmos.color = Color.cyan;
            List<Vector3> corners = HexagonCellPrototype.GetCornersOnSide(prototype, (int)side);
            foreach (Vector3 corner in corners)
            {
                Gizmos.DrawSphere(corner, cornerSize);
            }
        }

        public static void DrawHexagonCellPrototype(HexagonCellPrototype prototype, float centerRadius, bool showCorners, bool showSides, bool showCenter, bool useExternalColors = false, float cornerRadius = 0.25f)
        {
            if (showCenter) Gizmos.DrawSphere(prototype.center, centerRadius);
            if (showCorners)
            {

                for (int j = 0; j < prototype.cornerPoints.Length; j++)
                {
                    Gizmos.DrawSphere(prototype.cornerPoints[j], 0.25f);
                }
            }
            if (showSides)
            {
                if (!useExternalColors) Gizmos.color = Color.gray;
                for (int j = 0; j < prototype.sidePoints.Length; j++)
                {
                    Gizmos.DrawSphere(prototype.sidePoints[j], 0.3f);
                }
            }
            if (!useExternalColors) Gizmos.color = Color.black;
            VectorUtil.DrawHexagonPointLinesInGizmos(prototype.cornerPoints);
        }



        public static void DrawHexagonCellPrototype(
            HexagonCellPrototype cell,
            GridFilter_Type filterType,
            CellDisplay_Type cellDisplayType,
            bool showCorners,
            bool showHighlights = true,
            Dictionary<string, Color> colors = null,
            bool useExternalColor = false
        )
        {
            if (cell == null) return;

            bool showAll = filterType == GridFilter_Type.All;
            Vector3 pointPos = cell.center;

            bool show = false;

            if (filterType != GridFilter_Type.None)
            {
                float showRadius = showAll ? 0.8f : 2f;

                if (!showAll)
                {
                    if (cell.size <= 4)
                    {
                        showRadius = 1.5f;
                    }
                    else if (cell.size > 12)
                    {
                        showRadius = (cell.size / 3f) - 2;
                    }
                }

                if ((showAll || showHighlights || filterType == GridFilter_Type.Highlight) && cell.isHighlighted)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(cell.center, showRadius);

                    cell.Draw_Highlights();
                    show = true;
                }

                else if ((showAll || filterType == GridFilter_Type.Entrance) && cell.IsEntry())
                {
                    if (!useExternalColor) Gizmos.color = Color.green;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.AnyEdge) && (cell.IsEdge() || cell.IsOriginalGridEdge())) // cell._edgeCellType > 0)
                {
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.GridEdge) && cell.GetEdgeCellType() == EdgeCellType.Default)
                {
                    if (!useExternalColor) Gizmos.color = Color.red;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.InnerEdge) && cell.GetEdgeCellType() == EdgeCellType.Inner)
                {
                    if (!useExternalColor) Gizmos.color = Color.blue;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.OriginalGridEdge) && cell.IsOriginalGridEdge())
                {
                    if (!useExternalColor) Gizmos.color = Color.red;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.HostChildEdge) && cell.IsEdgeOfParent())
                {
                    if (!useExternalColor) Gizmos.color = Color.red;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Path) && cell.IsPath())
                {
                    if (cell._pathCellType > PathCellType.Default)
                    {
                        if (!useExternalColor) Gizmos.color = cell.IsPathCellStart() ? Color.green : Color.red;
                    }
                    else if (!useExternalColor) Gizmos.color = (cell.IsGround() == false) ? Color.green : Color.red;

                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Ground) && cell.IsGround())
                {
                    if (!useExternalColor) Gizmos.color = colors != null && colors.ContainsKey("brown") ? colors["brown"] : Color.black;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.FlatGround) && cell.IsFlatGround())
                {
                    if (!useExternalColor) Gizmos.color = colors != null && colors.ContainsKey("brown") ? colors["brown"] : Color.black;
                    show = true;
                }
                else if ((filterType == GridFilter_Type.Cluster) && cell.IsInCluster())
                {
                    if (!useExternalColor) Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(cell.center, cell.size);
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Removed) && cell.cellStatus == CellStatus.Remove)
                {
                    if (!useExternalColor) Gizmos.color = Color.white;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.AboveGround) && cell.cellStatus == CellStatus.AboveGround)
                {
                    if (!useExternalColor) Gizmos.color = Color.white;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.UnderGround) && cell.cellStatus == CellStatus.UnderGround)
                {
                    if (!useExternalColor) Gizmos.color = Color.grey;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Underwater) && cell.IsUnderWater())
                {
                    if (!useExternalColor) Gizmos.color = Color.blue;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Unset) && cell.cellStatus == CellStatus.Unset)
                {
                    if (!useExternalColor) Gizmos.color = Color.black;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.Tunnel) && cell.isTunnel)
                {
                    if (!useExternalColor) Gizmos.color = Color.green;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.TunnelGroundEntry) && cell.isTunnelGroundEntry)
                {
                    if (!useExternalColor) Gizmos.color = Color.green;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.TunnelFloor) && cell.tunnelStatus == TunnelStatus.FlatGround)
                {
                    if (!useExternalColor) Gizmos.color = Color.green;
                    show = true;
                }
                else if ((showAll || filterType == GridFilter_Type.TunnelAir) && cell.tunnelStatus == TunnelStatus.AboveGround)
                {
                    if (!useExternalColor) Gizmos.color = Color.green;
                    show = true;
                }


                if (cellDisplayType != CellDisplay_Type.DrawLines && (showAll || show)) Gizmos.DrawSphere(cell.center, showRadius);
            }

            if (show)
            {

                if (cellDisplayType != CellDisplay_Type.DrawCenter)
                {
                    if (!useExternalColor) Gizmos.color = Color.black;
                    VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                }

                if (showCorners)
                {
                    Gizmos.color = Color.magenta;
                    for (int j = 0; j < cell.cornerPoints.Length; j++)
                    {
                        pointPos = cell.cornerPoints[j];
                        Gizmos.DrawSphere(pointPos, 0.25f);
                    }
                }
            }
        }


        public static void DrawHexagonCellPrototypes(
            List<HexagonCellPrototype> prototypes,
            Transform transform,
            GridFilter_Type filterType,
            bool drawLines = true,
            bool showCorners = false,
            bool showHighlights = true
        )
        {
            // Color brown = new Color(0.4f, 0.2f, 0f);
            // Color orange = new Color(1f, 0.5f, 0f);
            // Color purple = new Color(0.8f, 0.2f, 1f);
            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();

            bool showAll = filterType == GridFilter_Type.All;

            foreach (HexagonCellPrototype cell in prototypes)
            {
                DrawHexagonCellPrototype(
                    cell,
                    filterType,
                    (drawLines) ? CellDisplay_Type.DrawCenterAndLines : CellDisplay_Type.DrawCenter,
                    showCorners,
                    showHighlights,
                    customColors
                );
                // Vector3 pointPos = cell.center;
                // DrawHexagonCellPrototype()
                //     if (filterType != GridFilter_Type.None)
                //     {
                //         bool show = false;
                //         float showRadius = showAll ? 0.8f : 2f;

                //         if (!showAll)
                //         {
                //             if (item.size <= 4)
                //             {
                //                 showRadius = 1.5f;
                //             }
                //             else if (item.size > 12)
                //             {
                //                 showRadius = (item.size / 3f) - 2;
                //             }
                //         }


                //         if ((showAll || filterType == GridFilter_Type.Entrance) && item.IsEntry())
                //         {
                //             Gizmos.color = Color.green;
                //             show = true;
                //             // showRadius = 7f;
                //         }

                //         else if ((showAll || filterType == GridFilter_Type.AnyEdge) && (item.IsEdge() || item.IsOriginalGridEdge())) // item._edgeCellType > 0)
                //         {
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.GridEdge) && item.GetEdgeCellType() == EdgeCellType.Default)
                //         {
                //             Gizmos.color = Color.red;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.InnerEdge) && item.GetEdgeCellType() == EdgeCellType.Inner)
                //         {
                //             Gizmos.color = Color.blue;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.OriginalGridEdge) && item.IsOriginalGridEdge())
                //         {
                //             Gizmos.color = Color.red;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.HostChildEdge) && item.IsEdgeOfParent())
                //         {
                //             Gizmos.color = Color.red;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Path) && item.IsPath())
                //         {
                //             if (item._pathCellType > PathCellType.Default)
                //             {
                //                 Gizmos.color = item.IsPathCellStart() ? Color.green : Color.red;
                //             }
                //             else Gizmos.color = (item.IsGround() == false) ? Color.green : Color.red;

                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Ground) && item.IsGround())
                //         {
                //             Gizmos.color = brown;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.FlatGround) && item.IsFlatGround())
                //         {
                //             Gizmos.color = brown;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Cluster) && item.IsInCluster())
                //         {
                //             Gizmos.color = Color.cyan;
                //             Gizmos.DrawWireSphere(item.center, item.size);
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Removed) && item.cellStatus == CellStatus.Remove)
                //         {
                //             Gizmos.color = Color.white;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.AboveGround) && item.cellStatus == CellStatus.AboveGround)
                //         {
                //             Gizmos.color = Color.white;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.UnderGround) && item.cellStatus == CellStatus.UnderGround)
                //         {
                //             Gizmos.color = Color.grey;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Underwater) && item.IsUnderWater())
                //         {
                //             Gizmos.color = Color.blue;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Unset) && item.cellStatus == CellStatus.Unset)
                //         {
                //             Gizmos.color = Color.black;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.Tunnel) && item.isTunnel)
                //         {
                //             Gizmos.color = Color.green;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.TunnelGroundEntry) && item.isTunnelGroundEntry)
                //         {
                //             Gizmos.color = Color.green;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.TunnelFloor) && item.tunnelStatus == TunnelStatus.FlatGround)
                //         {
                //             Gizmos.color = Color.green;
                //             show = true;
                //         }
                //         else if ((showAll || filterType == GridFilter_Type.TunnelAir) && item.tunnelStatus == TunnelStatus.AboveGround)
                //         {
                //             Gizmos.color = Color.green;
                //             show = true;
                //         }
                //         else if ((showAll || showHighlights || filterType == GridFilter_Type.Highlight) && item.isHighlighted)
                //         {
                //             Gizmos.color = Color.red;
                //             show = true;
                //         }

                //         if (showAll || show) Gizmos.DrawSphere(item.center, showRadius);
                //     }

                //     if (showCorners)
                //     {
                //         Gizmos.color = Color.magenta;
                //         for (int j = 0; j < item.cornerPoints.Length; j++)
                //         {
                //             pointPos = item.cornerPoints[j];
                //             Gizmos.DrawSphere(pointPos, 0.25f);
                //         }
                //     }

                //     if (drawLines)
                //     {
                //         Gizmos.color = Color.black;
                //         ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(item.cornerPoints);
                //     }
            }
        }

        public static void DrawHexagonCellPrototypeGrid(
            Dictionary<Vector2, HexagonCellPrototype> cellsByLookup,
            Transform transform,
            GridFilter_Type filterType,
            GridFilter_Level filterLevel = GridFilter_Level.All,
            bool drawLines = true,
            bool showCorners = false,
            bool showHighlights = true
        )
        {
            if (cellsByLookup == null) return;

            bool showAllLevels = (filterLevel == GridFilter_Level.All);

            if (showAllLevels || filterLevel == GridFilter_Level.HostCells) DrawHexagonCellPrototypes(cellsByLookup.Values.ToList(), transform, filterType, drawLines, showCorners, showHighlights);

            if (showAllLevels || filterLevel == GridFilter_Level.MicroCells)
            {
                foreach (var cell in cellsByLookup.Values)
                {
                    if (cell.children != null && cell.children.Count > 0) DrawHexagonCellPrototypes(cell.children, transform, filterType, drawLines, showCorners, showHighlights);
                }
            }
        }

        public static void DrawHexagonCellPrototypeGrid(
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer,
            Transform transform,
            GridFilter_Type filterType,
            GridFilter_Level filterLevel = GridFilter_Level.All,
            bool drawLines = true,
            bool showCorners = false,
            bool showHighlights = true
        )
        {
            if (prototypesByLayer == null) return;

            bool showAllLevels = (filterLevel == GridFilter_Level.All);

            foreach (var kvp in prototypesByLayer)
            {
                int key = kvp.Key;

                if (showAllLevels || filterLevel == GridFilter_Level.HostCells)
                {
                    DrawHexagonCellPrototypes(kvp.Value, transform, filterType, drawLines, showCorners, showHighlights);
                }

                if (showAllLevels || filterLevel == GridFilter_Level.MicroCells)
                {
                    foreach (var prototype in kvp.Value)
                    {
                        // if (prototype.cellPrototypes_X4_ByLayer != null && prototype.cellPrototypes_X4_ByLayer.Keys.Count > 0)
                        // {
                        //     DrawHexagonCellPrototypeGrid(prototype.cellPrototypes_X4_ByLayer, transform, filterType, GridFilter_Level.All, drawLines, showCorners, showHighlights);
                        // }
                        if (prototype.children != null && prototype.children.Count > 0)
                        {
                            DrawHexagonCellPrototypes(prototype.children, transform, filterType, drawLines, showCorners, showHighlights);
                        }
                    }
                }
            }
        }

        public static void DrawHexagonCellPrototypeGrid(Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellLookups_ByLayer,
            GridFilter_Type filterType,
            GridFilter_Level filterLevel = GridFilter_Level.All,
            CellDisplay_Type cellDisplayType = CellDisplay_Type.DrawCenterAndLines,
            bool showCorners = false,
            bool showHighlights = true,
            bool useExternalColor = false
        )
        {
            if (cellLookups_ByLayer == null) return;

            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();

            bool showAllLevels = (filterLevel == GridFilter_Level.All);

            foreach (int currentLayer in cellLookups_ByLayer.Keys)
            {
                foreach (HexagonCellPrototype cell in cellLookups_ByLayer[currentLayer].Values)
                {
                    DrawHexagonCellPrototype(
                        cell,
                        filterType,
                        cellDisplayType,
                        showCorners,
                        showHighlights,
                        customColors,
                        useExternalColor
                    );
                }
            }
        }

        public static void DrawHexagonCellPrototypeGrid_WithTracking(
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> cellLookups_ByParentLookup,
            Transform trackFocusPoint,
            bool debug_EnableWorldPositionTracking,
            Color initialColor
        // GridFilter_Type filterType,
        // GridFilter_Level filterLevel = GridFilter_Level.All,
        // CellDisplay_Type cellDisplayType = CellDisplay_Type.DrawCenterAndLines,
        // bool showCorners = false,
        // bool showHighlights = true
        )
        {
            if (cellLookups_ByParentLookup == null) return;

            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
            // bool showAllLevels = (filterLevel == GridFilter_Level.All);

            foreach (var kvp in cellLookups_ByParentLookup.Values)
            {
                foreach (HexagonCellPrototype cell in kvp.Values)
                {
                    int rad = cell.size / 3;

                    // if (debug_EnableWorldPositionTracking == false)
                    // {
                    //     Gizmos.color = cell.IsRemoved() ? Color.yellow : Color.black;
                    //     Gizmos.DrawSphere(cell.center, cell.IsRemoved() ? rad : rad);
                    // }
                    Gizmos.color = cell.IsRemoved() ? Color.yellow : initialColor;
                    // Gizmos.DrawSphere(cell.center, cell.IsRemoved() ? 108 : 8);
                    VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                    if (cell.GetEdgeCellType() == EdgeCellType.Default)
                    // if (cell.IsEdge())
                    {
                        Gizmos.color = (debug_EnableWorldPositionTracking && VectorUtil.DistanceXZ(cell.center, trackFocusPoint.position) > cell.size) ? Color.black : Color.red;
                        Gizmos.DrawSphere(cell.center, rad);
                    }

                    if (debug_EnableWorldPositionTracking && VectorUtil.DistanceXZ(cell.center, trackFocusPoint.position) < cell.size)
                    {
                        if (cell.neighbors.Count > 0)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawSphere(cell.center, rad);

                            foreach (var item in cell.neighbors)
                            {
                                // Debug.Log("neighbor lookup: " + item.GetLookup());
                                Gizmos.color = Color.green;
                                Gizmos.DrawSphere(item.center, cell.size / 2);
                            }
                        }
                        else
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawSphere(cell.center, rad);
                        }
                    }
                }
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

        public static List<Mesh> GenerateTunnelMeshesFromFromProtoTypes(List<HexagonCellPrototype> prototypes, int layerElevation, Transform transform)
        {
            Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> vertexSurfaceSets = GenerateTunnelVertexPointsFromPrototypes(prototypes, layerElevation);

            return MeshUtil.GenerateMeshesFromVertexSurfaces(vertexSurfaceSets);
        }
        public static Mesh GenerateTunnelMeshFromFromProtoTypes(List<HexagonCellPrototype> prototypes, int layerElevation, Transform transform)
        {
            Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> vertexSurfaceSets = GenerateTunnelVertexPointsFromPrototypes(prototypes, layerElevation, transform);

            return MeshUtil.GenerateMeshFromVertexSurfaces(MeshUtil.GenerateMeshesFromVertexSurfaces(vertexSurfaceSets));
        }





        public static Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> GenerateTunnelVertexPointsFromPrototypes(List<HexagonCellPrototype> prototypes, int layerElevation, Transform transform = null)
        {
            Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> vertexSurfaceSets = new Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>>();
            HashSet<string> visited = new HashSet<string>();

            foreach (var prototype in prototypes)
            {
                if (visited.Contains(prototype.GetId())) continue;

                visited.Add(prototype.GetId());

                // prototype.EvaluateNeighborsBySide(0.8f);
                bool hasGroundEntryNeighbor = false;

                List<int> sideWalls = new List<int>();

                for (int side = 0; side < 6; side++)
                {
                    HexagonCellPrototype sideNeighbor = prototype.neighborsBySide[side];

                    if (sideNeighbor != null && sideNeighbor.isTunnelGroundEntry)
                    {
                        hasGroundEntryNeighbor = true;

                        if (prototype.isTunnelStart && prototype.tunnelStartOpenSides == null)
                        {
                            prototype.tunnelStartOpenSides = new List<int>();
                            prototype.tunnelStartOpenSides.Add(side);

                        }
                        continue;
                    }
                    if (sideNeighbor == null || (prototypes.Contains(sideNeighbor) == false))
                    {
                        sideWalls.Add(side);
                    }
                }

                // if (sideWalls.Count == 0)
                // {
                //     Debug.LogError("GenerateTunnelVertexPointsFromProtoTypes - NO sideWalls set");
                //     break;
                // }

                List<(MeshVertexSurfaceType, List<Vector3>)> vertexSurfaces = new List<(MeshVertexSurfaceType, List<Vector3>)>();
                Vector3 innerCenterPos = new Vector3(prototype.center.x, prototype.center.y + (layerElevation / 2f), prototype.center.z);

                if (sideWalls.Count > 0)
                {
                    List<List<Vector3>> sideSurfaces = GenerateSideSurfaceLists(prototype, sideWalls, layerElevation, transform);
                    foreach (var item in sideSurfaces)
                    {
                        vertexSurfaces.Add((MeshVertexSurfaceType.SideInner, item));
                        if (hasGroundEntryNeighbor)
                        {
                            vertexSurfaces.Add((MeshVertexSurfaceType.SideOuter, item));
                        }
                    }
                }
                List<Vector3> bottomFloor = new List<Vector3>();
                List<Vector3> topCeiling = new List<Vector3>();

                HexagonCellPrototype topNeighbor = prototype.layerNeighbors[1];

                if (!prototypes.Contains(topNeighbor) && (topNeighbor == null || !topNeighbor.isTunnelGroundEntry || topNeighbor.tunnelEntryType != TunnelEntryType.Basement))
                {
                    // bottomFloor.Add(prototype.center);
                    // bottomFloor.AddRange(prototype.sidePoints);
                    bottomFloor.AddRange(prototype.cornerPoints);

                    topCeiling = VectorUtil.DuplicatePositionsToNewYPos(bottomFloor, layerElevation, 1);
                    if (topCeiling.Count > 0) vertexSurfaces.Add((MeshVertexSurfaceType.Top, topCeiling));
                }
                if (prototypes.Contains(prototype.layerNeighbors[0]) == false)
                {
                    if (bottomFloor.Count == 0)
                    {
                        // bottomFloor.Add(prototype.center);
                        // bottomFloor.AddRange(prototype.sidePoints);
                        bottomFloor.AddRange(prototype.cornerPoints);
                    }

                    if (bottomFloor.Count > 0) vertexSurfaces.Add((MeshVertexSurfaceType.Bottom, bottomFloor));
                }

                // if (prototype.isTunnelStart || hasGroundEntryNeighbor)
                // {
                //     List<Vector3> roof = new List<Vector3>();
                //     roof.AddRange(prototype.cornerPoints);
                //     roof = VectorUtil.DuplicatePositionsToNewYPos(roof, layerElevation, 1);

                //     if (roof.Count > 0) vertexSurfaces.Add((MeshVertexSurfaceType.Bottom, roof));
                // }

                vertexSurfaceSets.Add(innerCenterPos, vertexSurfaces);
            }

            return vertexSurfaceSets;
        }

        public static Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> Generate_TunnelVertexPointsFromCell(
            HexagonCellPrototype cell,
            List<HexagonCellPrototype> allCellsList,
            HashSet<string> visited,
            Transform transform = null
        )
        {
            Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> vertexSurfaceSets = new Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>>();
            if (visited == null) visited = new HashSet<string>();

            if (visited.Contains(cell.GetId())) return null;

            visited.Add(cell.GetId());

            bool hasGroundEntryNeighbor = false;

            List<int> sideWalls = new List<int>();

            for (int side = 0; side < 6; side++)
            {
                HexagonCellPrototype sideNeighbor = cell.neighborsBySide[side];

                if (sideNeighbor != null && sideNeighbor.isTunnelGroundEntry)
                {
                    hasGroundEntryNeighbor = true;

                    if (cell.isTunnelStart && cell.tunnelStartOpenSides == null)
                    {
                        cell.tunnelStartOpenSides = new List<int>();
                        cell.tunnelStartOpenSides.Add(side);
                    }
                    continue;
                }

                if (sideNeighbor == null) sideWalls.Add(side);
            }

            List<(MeshVertexSurfaceType, List<Vector3>)> vertexSurfaces = new List<(MeshVertexSurfaceType, List<Vector3>)>();
            Vector3 innerCenterPos = new Vector3(cell.center.x, cell.center.y + (cell.layerOffset / 2f), cell.center.z);

            if (sideWalls.Count > 0)
            {
                List<List<Vector3>> sideSurfaces = GenerateSideSurfaceLists(cell, sideWalls, cell.layerOffset, transform);
                foreach (var item in sideSurfaces)
                {
                    vertexSurfaces.Add((MeshVertexSurfaceType.SideInner, item));
                    if (hasGroundEntryNeighbor)
                    {
                        vertexSurfaces.Add((MeshVertexSurfaceType.SideOuter, item));
                    }
                }
            }
            List<Vector3> bottomFloor = new List<Vector3>();
            List<Vector3> topCeiling = new List<Vector3>();

            HexagonCellPrototype topNeighbor = cell.layerNeighbors[1];

            if (!allCellsList.Contains(topNeighbor) && (topNeighbor == null || !topNeighbor.isTunnelGroundEntry || topNeighbor.tunnelEntryType != TunnelEntryType.Basement))
            {
                // bottomFloor.Add(prototype.center);
                // bottomFloor.AddRange(prototype.sidePoints);
                bottomFloor.AddRange(cell.cornerPoints);

                topCeiling = VectorUtil.DuplicatePositionsToNewYPos(bottomFloor, cell.layerOffset, 1);
                if (topCeiling.Count > 0) vertexSurfaces.Add((MeshVertexSurfaceType.Top, topCeiling));
            }
            if (allCellsList.Contains(cell.layerNeighbors[0]) == false)
            {
                if (bottomFloor.Count == 0)
                {
                    // bottomFloor.Add(prototype.center);
                    // bottomFloor.AddRange(prototype.sidePoints);
                    bottomFloor.AddRange(cell.cornerPoints);
                }

                if (bottomFloor.Count > 0) vertexSurfaces.Add((MeshVertexSurfaceType.Bottom, bottomFloor));
            }

            vertexSurfaceSets.Add(innerCenterPos, vertexSurfaces);

            return vertexSurfaceSets;
        }



        // public static Dictionary<Vector3, List<List<Vector3>>> GenerateTunnelVertexPointsFromPrototypes(List<HexagonCellPrototype> prototypes, int layerElevation)
        // {
        //     Dictionary<Vector3, List<List<Vector3>>> vertexSurfaceSets = new Dictionary<Vector3, List<List<Vector3>>>();
        //     HashSet<string> visited = new HashSet<string>();

        //     foreach (var prototype in prototypes)
        //     {
        //         if (visited.Contains(prototype.GetId())) continue;

        //         visited.Add(prototype.GetId());

        //         prototype.EvaluateNeighborsBySide(0.8f);

        //         List<int> sideWalls = new List<int>();

        //         for (int side = 0; side < 6; side++)
        //         {
        //             HexagonCellPrototype sideNeighbor = prototype.neighborsBySide[side];

        //             if (sideNeighbor == null || prototypes.Contains(sideNeighbor) == false)
        //             {
        //                 sideWalls.Add(side);
        //             }
        //         }

        //         if (sideWalls.Count == 0)
        //         {
        //             Debug.LogError("GenerateTunnelVertexPointsFromProtoTypes - NO sideWalls set");
        //             break;
        //         }

        //         List<List<Vector3>> vertexSurfaces = new List<List<Vector3>>();
        //         Vector3 innerCenterPos = new Vector3(prototype.center.x, prototype.center.y + (layerElevation / 2f), prototype.center.z);

        //         vertexSurfaces.AddRange(GenerateSideSurfaceLists(prototype, sideWalls, layerElevation));

        //         List<Vector3> bottomFloor = new List<Vector3>();
        //         List<Vector3> topCeiling = new List<Vector3>();
        //         if (prototypes.Contains(prototype.layerNeighbors[1]) == false)
        //         {
        //             bottomFloor.Add(prototype.center);
        //             bottomFloor.AddRange(prototype.sidePoints);
        //             bottomFloor.AddRange(prototype.cornerPoints);

        //             topCeiling = VectorUtil.DuplicatePositionsToNewYPos(bottomFloor, layerElevation, 2);
        //             if (topCeiling.Count > 0) vertexSurfaces.Add(topCeiling);
        //         }
        //         if (prototypes.Contains(prototype.layerNeighbors[0]) == false)
        //         {
        //             if (bottomFloor.Count == 0)
        //             {
        //                 bottomFloor.Add(prototype.center);
        //                 bottomFloor.AddRange(prototype.sidePoints);
        //                 bottomFloor.AddRange(prototype.cornerPoints);
        //             }

        //             if (bottomFloor.Count > 0) vertexSurfaces.Add(bottomFloor);
        //         }

        //         vertexSurfaceSets.Add(innerCenterPos, vertexSurfaces);
        //     }

        //     return vertexSurfaceSets;
        // }

        // public static List<List<Vector3>> GenerateTunnelVertexPointsFromPrototypes(List<HexagonCellPrototype> prototypes, int layerElevation)
        // {
        //     List<List<Vector3>> vertexSurfaces = new List<List<Vector3>>();

        //     foreach (var prototype in prototypes)
        //     {
        //         prototype.EvaluateNeighborsBySide(0.8f);

        //         List<int> sideWalls = new List<int>();

        //         for (int side = 0; side < 6; side++)
        //         {
        //             HexagonCellPrototype sideNeighbor = prototype.neighborsBySide[side];

        //             if (sideNeighbor == null || prototypes.Contains(sideNeighbor) == false)
        //             {
        //                 sideWalls.Add(side);
        //             }
        //         }

        //         if (sideWalls.Count == 0)
        //         {
        //             Debug.LogError("GenerateTunnelVertexPointsFromProtoTypes - NO sideWalls set");
        //             break;
        //         }

        //         Dictionary<Vector3, List<Vector3>> vertexSurfaceSet = new Dictionary<Vector3, List<Vector3>>();

        //         Vector3 innerCenterPos = new Vector3(prototype.center.x, prototype.center.y + layerElevation / 2f, prototype.center.z);


        //         vertexSurfaces.AddRange(GenerateSideSurfaceLists(prototype, sideWalls, layerElevation));

        //         List<Vector3> bottomFloor = new List<Vector3>();
        //         List<Vector3> topCeiling = new List<Vector3>();
        //         if (prototypes.Contains(prototype.layerNeighbors[1]) == false)
        //         {
        //             bottomFloor.Add(prototype.center);
        //             bottomFloor.AddRange(prototype.sidePoints);
        //             bottomFloor.AddRange(prototype.cornerPoints);

        //             topCeiling = VectorUtil.DuplicatePositionsToNewYPos(bottomFloor, layerElevation, 2);
        //             if (topCeiling.Count > 0) vertexSurfaces.Add(topCeiling);
        //         }
        //         if (prototypes.Contains(prototype.layerNeighbors[0]) == false)
        //         {
        //             if (bottomFloor.Count == 0)
        //             {
        //                 bottomFloor.Add(prototype.center);
        //                 bottomFloor.AddRange(prototype.sidePoints);
        //                 bottomFloor.AddRange(prototype.cornerPoints);
        //             }

        //             if (bottomFloor.Count > 0) vertexSurfaces.Add(bottomFloor);
        //         }
        //     }

        //     return vertexSurfaces;
        // }

        public static List<Vector3> GenerateTunnelVertexPointsFromProtoTypes_Debug(List<HexagonCellPrototype> prototypes, int layerElevation)
        {
            List<Vector3> allVertices = new List<Vector3>();

            foreach (var prototype in prototypes)
            {
                // prototype.EvaluateNeighborsBySide(0.8f);

                List<int> sideWalls = new List<int>();

                for (int side = 0; side < 6; side++)
                {
                    HexagonCellPrototype sideNeighbor = prototype.neighborsBySide[side];

                    if (sideNeighbor == null || prototypes.Contains(sideNeighbor) == false)
                    {
                        sideWalls.Add(side);
                    }
                }

                if (sideWalls.Count == 0)
                {
                    Debug.LogError("GenerateTunnelVertexPointsFromProtoTypes - NO sideWalls set");
                    break;
                }
                allVertices.AddRange(GenerateSideSurfaces(prototype, sideWalls, layerElevation));

                List<Vector3> bottomFloor = new List<Vector3>();
                List<Vector3> topCeiling = new List<Vector3>();
                if (prototypes.Contains(prototype.layerNeighbors[1]) == false)
                {
                    bottomFloor.Add(prototype.center);
                    bottomFloor.AddRange(prototype.sidePoints);
                    bottomFloor.AddRange(prototype.cornerPoints);

                    topCeiling = VectorUtil.DuplicatePositionsToNewYPos(bottomFloor, layerElevation, 2);
                    if (topCeiling.Count > 0) allVertices.AddRange(topCeiling);
                }
                if (prototypes.Contains(prototype.layerNeighbors[0]) == false)
                {
                    if (bottomFloor.Count == 0)
                    {
                        bottomFloor.Add(prototype.center);
                        bottomFloor.AddRange(prototype.sidePoints);
                        bottomFloor.AddRange(prototype.cornerPoints);
                    }

                    if (bottomFloor.Count > 0) allVertices.AddRange(bottomFloor);
                }
            }

            return allVertices;
        }

        public static List<Vector3> GetCornersOnSide(HexagonCellPrototype prototype, int side)
        {
            (int cornerA, int cornerB) = HexCoreUtil.GetCornersFromSide_Condensed((HexagonSide)side);

            List<Vector3> corners = new List<Vector3>();
            corners.Add(new Vector3(prototype.cornerPoints[(int)cornerA].x, prototype.cornerPoints[(int)cornerA].y, prototype.cornerPoints[(int)cornerA].z));
            corners.Add(new Vector3(prototype.cornerPoints[(int)cornerB].x, prototype.cornerPoints[(int)cornerB].y, prototype.cornerPoints[(int)cornerB].z));
            return corners;
        }

        public static List<Vector3> GenerateDottedLineOnSide(HexagonCellPrototype prototype, int side, int density = 8)
        {
            (int cornerA, int cornerB) = HexCoreUtil.GetCornersFromSide_Condensed((HexagonSide)side);

            List<Vector3> corners = new List<Vector3>();
            corners.Add(new Vector3(prototype.cornerPoints[(int)cornerA].x, prototype.cornerPoints[(int)cornerA].y, prototype.cornerPoints[(int)cornerA].z));
            corners.Add(new Vector3(prototype.cornerPoints[(int)cornerB].x, prototype.cornerPoints[(int)cornerB].y, prototype.cornerPoints[(int)cornerB].z));

            return VectorUtil.GenerateDottedLine(corners, density);
        }

        public static List<Vector3> GenerateSideSurfaceVertices(HexagonCellPrototype prototype, int side, int layerElevation, Transform transform = null)
        {

            // consolidate base edge points of side 
            (int cornerA, int cornerB) = HexCoreUtil.GetCornersFromSide_Condensed((HexagonSide)side);
            // (HexagonCorner cornerA, HexagonCorner cornerB) = HexagonCell.GetCornersFromSide((HexagonSide)side);
            // Debug.Log("GenerateTunnelVertexPointsFromProtoTypes - side: " + (HexagonSide)side + ", cornerA: " + (int)cornerA + ", cornerB: " + (int)cornerB);

            Vector3 corner_A = new Vector3(prototype.cornerPoints[(int)cornerA].x, prototype.cornerPoints[(int)cornerA].y, prototype.cornerPoints[(int)cornerA].z);
            Vector3 corner_B = new Vector3(prototype.cornerPoints[(int)cornerB].x, prototype.cornerPoints[(int)cornerB].y, prototype.cornerPoints[(int)cornerB].z);
            // Vector3 sidePt = new Vector3(prototype.sidePoints[side].x, prototype.sidePoints[side].y, prototype.sidePoints[side].z);

            List<Vector3> bottomEdgePoints = new List<Vector3>();
            bottomEdgePoints.Add(corner_A);
            // bottomEdgePoints.Add(sidePt);
            bottomEdgePoints.Add(corner_B);

            float offsetY = layerElevation;
            // List<Vector3> centerPoints = VectorUtil.DuplicatePositionsToNewYPos(bottomEdgePoints, offsetY / 2f, 1);
            List<Vector3> topEdgePoints = VectorUtil.DuplicatePositionsToNewYPos(bottomEdgePoints, offsetY, 1);

            List<Vector3> result = new List<Vector3>();
            result.AddRange(bottomEdgePoints);
            // result.AddRange(centerPoints);
            result.AddRange(topEdgePoints);

            return result;
        }

        public static Dictionary<int, List<Vector3>> GenerateSideSurfacesBySide(HexagonCellPrototype prototype, List<int> sides, int layerElevation)
        {
            Dictionary<int, List<Vector3>> vertexSurfacesBySide = new Dictionary<int, List<Vector3>>();

            for (int i = 0; i < 6; i++)
            {
                if (sides.Contains(i))
                {
                    vertexSurfacesBySide.Add(i, GenerateSideSurfaceVertices(prototype, i, layerElevation));
                }
            }

            return vertexSurfacesBySide;
        }

        public static List<List<Vector3>> GenerateSideSurfaceLists(HexagonCellPrototype prototype, List<int> sides, int layerElevation, Transform transform = null)
        {
            List<List<Vector3>> vertexSurfaces = new List<List<Vector3>>();

            for (int i = 0; i < 6; i++)
            {
                if (sides.Contains(i))
                {
                    List<Vector3> newSurface = GenerateSideSurfaceVertices(prototype, i, layerElevation, transform);
                    if (newSurface.Count != 9)
                    {
                        Debug.LogError("newSurface.Count:" + newSurface.Count);
                    }
                    vertexSurfaces.Add(newSurface);
                }
            }
            return vertexSurfaces;
        }

        public static List<Vector3> GenerateSideSurfaces(HexagonCellPrototype prototype, List<int> sides, int layerElevation)
        {
            List<Vector3> vertexSurfaces = new List<Vector3>();

            for (int i = 0; i < 6; i++)
            {
                if (sides.Contains(i))
                {
                    vertexSurfaces.AddRange(GenerateSideSurfaceVertices(prototype, i, layerElevation));
                }
            }
            return vertexSurfaces;
        }


        public static Vector3[,] GenerateRectangularGrid(Vector3 centerPosition, int width, int height)
        {
            Vector3[,] points = new Vector3[width, height];

            // Calculate half width and half height
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            // Calculate the starting position
            Vector3 start = new Vector3(centerPosition.x - halfWidth, centerPosition.y, centerPosition.z - halfHeight);

            // Calculate the spacing between points
            float spacingX = width > 1 ? width / (float)(width - 1) : 0;
            float spacingZ = height > 1 ? height / (float)(height - 1) : 0;

            // Generate the grid of points
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Vector3 point = new Vector3(start.x + x * spacingX, centerPosition.y, start.z + z * spacingZ);
                    points[x, z] = point;
                }
            }

            return points;
        }


        public class MeshGenerator : MonoBehaviour
        {
            public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
            {
                // Create a list to hold the individual surface meshes
                List<Mesh> surfaceMeshes = new List<Mesh>();

                foreach (var kvp in vertexSurfaceSetsByXZPos)
                {
                    Vector3 XZPos = new Vector3(kvp.Key.x, 0, kvp.Key.y);
                    List<TerrainVertex> surfaceVerts = kvp.Value;

                    // Remove points from the list until it's divisible by 3
                    while (surfaceVerts.Count % 3 != 0)
                    {
                        surfaceVerts.RemoveAt(surfaceVerts.Count - 1);
                    }

                    // Sort the surface vertices by XZ position
                    surfaceVerts.Sort((v1, v2) =>
                    {
                        int result = v1.position.x.CompareTo(v2.position.x);
                        if (result == 0)
                            result = v1.position.z.CompareTo(v2.position.z);
                        return result;
                    });

                    List<Vector3> surface = new List<Vector3>();
                    // Add the positions of surfaceVerts to surface
                    foreach (var vert in surfaceVerts)
                    {
                        surface.Add(vert.position);
                    }

                    // Get the min and max X and Z positions among the surface points
                    float minX = float.MaxValue;
                    float maxX = float.MinValue;
                    float minZ = float.MaxValue;
                    float maxZ = float.MinValue;

                    foreach (var vert in surfaceVerts)
                    {
                        minX = Mathf.Min(minX, vert.position.x);
                        maxX = Mathf.Max(maxX, vert.position.x);
                        minZ = Mathf.Min(minZ, vert.position.z);
                        maxZ = Mathf.Max(maxZ, vert.position.z);
                    }

                    float avgDistance = ProceduralTerrainUtility.CalculateAverageDistanceBetweenPointsXZ(surface);
                    // Debug.LogError("avgDistance: " + avgDistance);

                    // Iterate over the bounds and get the closest surface point
                    for (float x = minX; x <= maxX; x++)
                    {
                        for (float z = minZ; z <= maxZ; z++)
                        {
                            Vector3 closestVert = new Vector3();
                            float closestDistance = float.MaxValue;

                            foreach (var vert in surfaceVerts)
                            {
                                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(vert.position.x, vert.position.z));
                                if (distance < closestDistance && distance <= avgDistance)
                                {
                                    closestDistance = distance;
                                    closestVert = vert.position;
                                }
                            }

                            // Get the next 2 closest points along the x and z position
                            List<Vector3> nextClosestVerts = new List<Vector3>();
                            for (int i = 0; i < 2; i++)
                            {
                                Vector3 nextVert = new Vector3();
                                float nextDistance = float.MaxValue;

                                foreach (var vert in surfaceVerts)
                                {
                                    float distance = Vector2.Distance(new Vector2(x, z), new Vector2(vert.position.x, vert.position.z));
                                    if (distance > closestDistance && distance < nextDistance && distance <= avgDistance && !nextClosestVerts.Contains(vert.position))
                                    {
                                        nextDistance = distance;
                                        nextVert = vert.position;
                                    }
                                }

                                if (nextVert != Vector3.zero)
                                {
                                    nextClosestVerts.Add(nextVert);
                                }
                            }

                            // Draw triangles between the closest point and its next 2 closest points
                            if (closestVert != Vector3.zero && nextClosestVerts.Count == 2)
                            {
                                surface.Add(closestVert);
                                surface.Add(nextClosestVerts[0]);
                                surface.Add(nextClosestVerts[1]);
                            }
                        }
                    }

                    // Create a new mesh and set its
                    Mesh surfaceMesh = new Mesh();
                    surfaceMesh.SetVertices(surface);
                    surfaceMesh.SetNormals(surface);

                    // Create triangles from the surface points
                    int[] triangles = new int[surface.Count];
                    for (int i = 0; i < surface.Count; i++)
                    {
                        triangles[i] = i;
                    }
                    surfaceMesh.SetTriangles(triangles, 0);

                    // Recalculate bounds and normals
                    surfaceMesh.RecalculateBounds();
                    surfaceMesh.RecalculateNormals();

                    // Add the surface mesh to the list of surface meshes
                    surfaceMeshes.Add(surfaceMesh);
                }

                // Set the same direction for all triangles in the surface meshes
                SetTrianglesSameDirection(surfaceMeshes);

                return surfaceMeshes;
            }

            private static void SetTrianglesSameDirection(List<Mesh> meshes, bool clockwise = true)
            {
                foreach (var mesh in meshes)
                {
                    int[] triangles = mesh.triangles;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        if (clockwise)
                        {
                            int temp = triangles[i + 1];
                            triangles[i + 1] = triangles[i + 2];
                            triangles[i + 2] = temp;
                        }
                        else
                        {
                            int temp = triangles[i];
                            triangles[i] = triangles[i + 1];
                            triangles[i + 1] = temp;
                        }
                    }
                    mesh.triangles = triangles;
                    mesh.RecalculateNormals();
                }
            }


            // private static void SetTrianglesSameDirection(List<Mesh> meshes)
            // {
            //     foreach (var mesh in meshes)
            //     {
            //         int[] triangles = mesh.triangles;
            //         for (int i = 0; i < triangles.Length; i += 3)
            //         {
            //             int temp = triangles[i + 1];
            //             triangles[i + 1] = triangles[i + 2];
            //             triangles[i + 2] = temp;
            //         }
            //         mesh.triangles = triangles;
            //         mesh.RecalculateNormals();
            //     }
            // }

            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = new Vector3(kvp.Key.x, 0, kvp.Key.y);
            //         List<TerrainVertex> surfaceVerts = kvp.Value;

            //         // Remove points from the list until it's divisible by 3
            //         while (surfaceVerts.Count % 3 != 0)
            //         {
            //             surfaceVerts.RemoveAt(surfaceVerts.Count - 1);
            //         }

            //         // Sort the surface vertices by XZ position
            //         surfaceVerts.Sort((v1, v2) =>
            //         {
            //             int result = v1.position.x.CompareTo(v2.position.x);
            //             if (result == 0)
            //                 result = v1.position.z.CompareTo(v2.position.z);
            //             return result;
            //         });

            //         List<Vector3> surface = new List<Vector3>();
            //         // Add the positions of surfaceVerts to surface
            //         foreach (var vert in surfaceVerts)
            //         {
            //             surface.Add(vert.position);
            //         }

            //         // Get the min and max X and Z positions among the surface points
            //         float minX = float.MaxValue;
            //         float maxX = float.MinValue;
            //         float minZ = float.MaxValue;
            //         float maxZ = float.MinValue;

            //         foreach (var vert in surfaceVerts)
            //         {
            //             minX = Mathf.Min(minX, vert.position.x);
            //             maxX = Mathf.Max(maxX, vert.position.x);
            //             minZ = Mathf.Min(minZ, vert.position.z);
            //             maxZ = Mathf.Max(maxZ, vert.position.z);
            //         }

            //         // Iterate over the bounds and get the closest surface point
            //         for (float x = minX; x <= maxX; x++)
            //         {
            //             for (float z = minZ; z <= maxZ; z++)
            //             {
            //                 Vector3 closestVert = new Vector3();
            //                 float closestDistance = float.MaxValue;

            //                 foreach (var vert in surfaceVerts)
            //                 {
            //                     float distance = Vector2.Distance(new Vector2(x, z), new Vector2(vert.position.x, vert.position.z));
            //                     if (distance < closestDistance)
            //                     {
            //                         closestDistance = distance;
            //                         closestVert = vert.position;
            //                     }
            //                 }

            //                 // Get the next 2 closest points along the x and z position
            //                 List<Vector3> nextClosestVerts = new List<Vector3>();
            //                 for (int i = 0; i < 2; i++)
            //                 {
            //                     Vector3 nextVert = new Vector3();
            //                     float nextDistance = float.MaxValue;

            //                     foreach (var vert in surfaceVerts)
            //                     {
            //                         float distance = Vector2.Distance(new Vector2(x, z), new Vector2(vert.position.x, vert.position.z));
            //                         if (distance > closestDistance && distance < nextDistance && !nextClosestVerts.Contains(vert.position))
            //                         {
            //                             nextDistance = distance;
            //                             nextVert = vert.position;
            //                         }
            //                     }

            //                     if (nextVert != Vector3.zero)
            //                     {
            //                         nextClosestVerts.Add(nextVert);
            //                     }
            //                 }

            //                 // Draw triangles between the closest point and its next 2 closest points
            //                 if (closestVert != Vector3.zero && nextClosestVerts.Count == 2)
            //                 {
            //                     surface.Add(closestVert);
            //                     surface.Add(nextClosestVerts[0]);
            //                     surface.Add(nextClosestVerts[1]);
            //                 }
            //             }
            //         }

            //         // Create a new mesh and set its vertices and

            //         // Create a new mesh and set its vertices and normals
            //         Mesh surfaceMesh = new Mesh();
            //         surfaceMesh.SetVertices(surface);
            //         surfaceMesh.SetNormals(surface);

            //         // Create triangles from the surface points
            //         int[] triangles = new int[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             triangles[i] = i;
            //         }
            //         surfaceMesh.SetTriangles(triangles, 0);

            //         // Recalculate bounds and normals
            //         surfaceMesh.RecalculateBounds();
            //         surfaceMesh.RecalculateNormals();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }

            //     return surfaceMeshes;
            // }

            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = kvp.Key;
            //         List<TerrainVertex> surfaceVerts = kvp.Value;

            //         // Sort the surface list by Vector2 distance from XZPos
            //         surfaceVerts.Sort((a, b) => Vector2.Distance(new Vector2(a.position.x, a.position.z), new Vector2(XZPos.x, XZPos.z)).CompareTo(Vector2.Distance(new Vector2(b.position.x, b.position.z), new Vector2(XZPos.x, XZPos.z))));

            //         // Remove points from the list until it's divisible by 3
            //         while (surfaceVerts.Count % 3 != 0 && surfaceVerts.Count % 2 != 0)
            //         {
            //             surfaceVerts.RemoveAt(surfaceVerts.Count - 1);
            //         }

            //         // Sort the surface list by TerrainVertex.index
            //         // surfaceVerts.Sort((a, b) => a.index.CompareTo(b.index));

            //         // Sort the vertices by XZ position
            //         surfaceVerts.Sort((v1, v2) =>
            //         {
            //             float combinedValue1 = v1.position.x + v1.position.z;
            //             float combinedValue2 = v2.position.x + v2.position.z;
            //             return combinedValue1.CompareTo(combinedValue2);
            //         });

            //         // surfaceVerts.Sort((v1, v2) =>
            //         //     {


            //         //         int result = v1.position.x.CompareTo(v2.position.x);
            //         //         if (result == 0)
            //         //             result = v1.position.z.CompareTo(v2.position.z);
            //         //         return result;
            //         //     });


            //         // surfaceVerts = surfaceVerts.OrderBy(v => ComputeDistanceToNeighbors(v, surfaceVerts)).ToList();


            //         List<Vector3> surface = new List<Vector3>();

            //         HashSet<int> indices = new HashSet<int>();

            //         // Add the positions of surfaceVerts to surface
            //         foreach (var vert in surfaceVerts)
            //         {
            //             indices.Add(vert.index);
            //             surface.Add(vert.position);
            //         }

            //         // Create a new mesh to represent the current surface
            //         Mesh surfaceMesh = new Mesh();

            //         // Set the surface vertices to the mesh
            //         surfaceMesh.vertices = surface.ToArray();
            //         for (int i = 0; i < surfaceMesh.vertices.Length; i++)
            //         {
            //             indices.Add(i);
            //         }
            //         Debug.LogError("indices:" + indices.Count);
            //         int[] triangles = GenerateTriangles(surface.Count);

            //         // int[] triangles = ProceduralTerrainUtility.GenerateTerrainSurfaceTriangles(surface.Count, indices);
            //         surfaceMesh.triangles = triangles;

            //         // Set the UVs to the mesh (you can customize this based on your requirements)
            //         Vector2[] uvs = new Vector2[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //         }
            //         surfaceMesh.uv = uvs;

            //         // Recalculate normals and bounds for the surface mesh
            //         surfaceMesh.RecalculateNormals();
            //         surfaceMesh.RecalculateBounds();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }

            //     return surfaceMeshes;
            // }


            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = kvp.Key;
            //         List<TerrainVertex> surfaceVerts = kvp.Value;

            //         // Sort the surface list by Vector2 distance from XZPos
            //         surfaceVerts.Sort((a, b) => Vector2.Distance(new Vector2(a.position.x, a.position.z), new Vector2(XZPos.x, XZPos.z)).CompareTo(Vector2.Distance(new Vector2(b.position.x, b.position.z), new Vector2(XZPos.x, XZPos.z))));

            //         // Remove points from the list until it's divisible by 3
            //         while (surfaceVerts.Count % 3 != 0)
            //         {
            //             surfaceVerts.RemoveAt(surfaceVerts.Count - 1);
            //         }

            //         // Sort the surface list by TerrainVertex.index
            //         surfaceVerts.Sort((a, b) => a.index.CompareTo(b.index));

            //         List<Vector3> surface = new List<Vector3>();

            //         // Add the positions of surfaceVerts to surface
            //         foreach (var vert in surfaceVerts)
            //         {
            //             surface.Add(vert.position);
            //         }

            //         // Create a new mesh to represent the current surface
            //         Mesh surfaceMesh = new Mesh();

            //         // Set the surface vertices to the mesh
            //         surfaceMesh.vertices = surface.ToArray();

            //         int[] triangles = GenerateTriangles(surface.Count);

            //         // Reverse the winding order of certain triangles
            //         for (int i = 0; i < triangles.Length; i += 3)
            //         {
            //             // Get the vertices for the current triangle
            //             Vector3 v0 = surface[triangles[i]];
            //             Vector3 v1 = surface[triangles[i + 1]];
            //             Vector3 v2 = surface[triangles[i + 2]];

            //             // Compute the normal of the triangle
            //             Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            //             // Check if the normal is facing downwards (in the opposite direction of the up vector)
            //             if (normal.y < 0)
            //             {
            //                 // Flip the winding order of the triangle by swapping vertex indices
            //                 int temp = triangles[i + 1];
            //                 triangles[i + 1] = triangles[i + 2];
            //                 triangles[i + 2] = temp;
            //             }
            //         }

            //         surfaceMesh.triangles = triangles;

            //         // Set the UVs to the mesh (you can customize this based on your requirements)
            //         Vector2[] uvs = new Vector2[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //         }
            //         surfaceMesh.uv = uvs;

            //         // Recalculate normals and bounds for the surface mesh
            //         surfaceMesh.RecalculateNormals();
            //         surfaceMesh.RecalculateBounds();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }

            //     return surfaceMeshes;
            // }


            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = kvp.Key;
            //         List<TerrainVertex> surfaceVerts = kvp.Value;

            //         // Sort the surface list by Vector2 distance from XZPos
            //         surfaceVerts.Sort((a, b) => Vector2.Distance(new Vector2(a.position.x, a.position.z), new Vector2(XZPos.x, XZPos.z)).CompareTo(Vector2.Distance(new Vector2(b.position.x, b.position.z), new Vector2(XZPos.x, XZPos.z))));

            //         // Remove points from the list until it's divisible by 3
            //         while (surfaceVerts.Count % 3 != 0)
            //         {
            //             surfaceVerts.RemoveAt(surfaceVerts.Count - 1);
            //         }

            //         List<Vector3> surface =

            //         // Create a new mesh to represent the current surface
            //         Mesh surfaceMesh = new Mesh();

            //         // Set the surface vertices to the mesh
            //         surfaceMesh.vertices = surface.ToArray();

            //         int[] triangles = GenerateTriangles(surface.Count);

            //         // Reverse the winding order of certain triangles
            //         for (int i = 0; i < triangles.Length; i += 3)
            //         {
            //             // Get the vertices for the current triangle
            //             Vector3 v0 = surface[triangles[i]];
            //             Vector3 v1 = surface[triangles[i + 1]];
            //             Vector3 v2 = surface[triangles[i + 2]];

            //             // Compute the normal of the triangle
            //             Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            //             // Check if the normal is facing downwards (in the opposite direction of the up vector)
            //             if (normal.y < 0)
            //             {
            //                 // Flip the winding order of the triangle by swapping vertex indices
            //                 int temp = triangles[i + 1];
            //                 triangles[i + 1] = triangles[i + 2];
            //                 triangles[i + 2] = temp;
            //             }
            //         }

            //         surfaceMesh.triangles = triangles;

            //         // Set the UVs to the mesh (you can customize this based on your requirements)
            //         Vector2[] uvs = new Vector2[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //         }
            //         surfaceMesh.uv = uvs;

            //         // Recalculate normals and bounds for the surface mesh
            //         surfaceMesh.RecalculateNormals();
            //         surfaceMesh.RecalculateBounds();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }

            //     return surfaceMeshes;
            // }

            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<Vector3>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = kvp.Key;
            //         List<Vector3> surface = kvp.Value;

            //         // Sort the surface list by Vector2 distance from XZPos
            //         surface.Sort((a, b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(XZPos.x, XZPos.z)).CompareTo(Vector2.Distance(new Vector2(b.x, b.z), new Vector2(XZPos.x, XZPos.z))));

            //         // Remove points from the list until it's divisible by 3
            //         while (surface.Count % 3 != 0)
            //         {
            //             surface.RemoveAt(surface.Count - 1);
            //         }

            //         // Create a new mesh to represent the current surface
            //         Mesh surfaceMesh = new Mesh();

            //         // Set the surface vertices to the mesh
            //         surfaceMesh.vertices = surface.ToArray();

            //         int[] triangles = GenerateTriangles(surface.Count);

            //         // Reverse the winding order of certain triangles
            //         for (int i = 0; i < triangles.Length; i += 3)
            //         {
            //             // Get the vertices for the current triangle
            //             Vector3 v0 = surface[triangles[i]];
            //             Vector3 v1 = surface[triangles[i + 1]];
            //             Vector3 v2 = surface[triangles[i + 2]];

            //             // Compute the normal of the triangle
            //             Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            //             // Check if the normal is facing downwards (in the opposite direction of the up vector)
            //             if (normal.y < 0)
            //             {
            //                 // Flip the winding order of the triangle by swapping vertex indices
            //                 int temp = triangles[i + 1];
            //                 triangles[i + 1] = triangles[i + 2];
            //                 triangles[i + 2] = temp;
            //             }
            //         }

            //         surfaceMesh.triangles = triangles;

            //         // Set the UVs to the mesh (you can customize this based on your requirements)
            //         Vector2[] uvs = new Vector2[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //         }
            //         surfaceMesh.uv = uvs;

            //         // Recalculate normals and bounds for the surface mesh
            //         surfaceMesh.RecalculateNormals();
            //         surfaceMesh.RecalculateBounds();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }

            //     return surfaceMeshes;
            // }


            // public static List<Mesh> GenerateMeshesFromTerrainVertexSurfaces(Dictionary<Vector2, List<Vector3>> vertexSurfaceSetsByXZPos)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSetsByXZPos)
            //     {
            //         Vector3 XZPos = kvp.Key;
            //         List<Vector3> surface = kvp.Value;

            //         // Create a new mesh to represent the current surface
            //         Mesh surfaceMesh = new Mesh();

            //         // Set the surface vertices to the mesh
            //         surfaceMesh.vertices = surface.ToArray();

            //         // int[] traingles = ProceduralTerrainUtility.GenerateTerrainSurfaceTriangles(surface.Count);
            //         int[] traingles = GenerateTriangles(surface.Count);
            //         surfaceMesh.triangles = traingles;

            //         // ReverseNormals(surfaceMesh);
            //         // ReverseTriangles(surfaceMesh);

            //         // Set the UVs to the mesh (you can customize this based on your requirements)
            //         Vector2[] uvs = new Vector2[surface.Count];
            //         for (int i = 0; i < surface.Count; i++)
            //         {
            //             uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //         }
            //         surfaceMesh.uv = uvs;

            //         // Recalculate normals and bounds for the surface mesh
            //         surfaceMesh.RecalculateNormals();
            //         surfaceMesh.RecalculateBounds();

            //         // Add the surface mesh to the list of surface meshes
            //         surfaceMeshes.Add(surfaceMesh);
            //     }
            //     return surfaceMeshes;
            // }






            // public static List<Mesh> GenerateMeshesFromVertexSurfaces(Dictionary<Vector3, List<(MeshVertexSurfaceType, List<Vector3>)>> vertexSurfaceSets)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSets)
            //     {
            //         Vector3 meshFacingPosition = kvp.Key;

            //         foreach (var surface in kvp.Value)
            //         {
            //             // Create a new mesh to represent the current surface
            //             Mesh surfaceMesh = new Mesh();

            //             (MeshVertexSurfaceType surfaceType, List<Vector3> surfacePoints) = surface;

            //             // Set the surface vertices to the mesh
            //             surfaceMesh.vertices = surfacePoints.ToArray();

            //             switch (surfaceType)
            //             {
            //                 case MeshVertexSurfaceType.Bottom:
            //                     // Generate triangles based on the surface vertices
            //                     int[] bottomTriangles = GenerateRectangularTriangles(surfacePoints.Count);
            //                     surfaceMesh.triangles = bottomTriangles;

            //                     // Reverse the winding order of the triangles to make the surface face upward
            //                     ReverseNormals(surfaceMesh);
            //                     break;

            //                 case MeshVertexSurfaceType.Top:
            //                     // Generate triangles based on the surface vertices
            //                     int[] topTriangles = GenerateRectangularTriangles(surfacePoints.Count);
            //                     surfaceMesh.triangles = topTriangles;

            //                     // Reverse the winding order of the triangles to make the surface face downward
            //                     // ReverseNormals(surfaceMesh);
            //                     ReverseTriangles(surfaceMesh);
            //                     break;

            //                 default: // Side

            //                     // Generate triangles based on the surface vertices
            //                     int[] sideTriangles = GenerateRectangularTriangles(surfacePoints.Count);
            //                     surfaceMesh.triangles = sideTriangles;

            //                     // Reverse the winding order of the triangles
            //                     ReverseNormals(surfaceMesh);
            //                     // ReverseTriangles(surfaceMesh);

            //                     break;
            //             }

            //             // Set the UVs to the mesh (you can customize this based on your requirements)
            //             Vector2[] uvs = new Vector2[surfacePoints.Count];
            //             for (int i = 0; i < surfacePoints.Count; i++)
            //             {
            //                 uvs[i] = new Vector2(surfacePoints[i].x, surfacePoints[i].y); // Use x and y as UV coordinates
            //             }
            //             surfaceMesh.uv = uvs;

            //             // Recalculate normals and bounds for the surface mesh
            //             surfaceMesh.RecalculateNormals();
            //             surfaceMesh.RecalculateBounds();

            //             // Add the surface mesh to the list of surface meshes
            //             surfaceMeshes.Add(surfaceMesh);
            //         }
            //     }

            //     return surfaceMeshes;
            // }

            // public static List<Mesh> GenerateMeshesFromVertexSurfaces(Dictionary<Vector3, List<List<Vector3>>> vertexSurfaceSets, Transform transform)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSets)
            //     {
            //         Vector3 meshFacingPosition = kvp.Key;

            //         foreach (List<Vector3> surface in kvp.Value)
            //         {
            //             // Create a new mesh to represent the current surface
            //             Mesh surfaceMesh = new Mesh();

            //             // Set the surface vertices to the mesh
            //             surfaceMesh.vertices = surface.ToArray();

            //             // Generate triangles based on the surface vertices
            //             int[] triangles = GenerateRectangularTriangles(surface.Count);
            //             surfaceMesh.triangles = triangles;

            //             // Reverse the winding order of the triangles
            //             ReverseNormals(surfaceMesh);
            //             // ReverseTriangles(surfaceMesh);

            //             // Set the UVs to the mesh (you can customize this based on your requirements)
            //             Vector2[] uvs = new Vector2[surface.Count];
            //             for (int i = 0; i < surface.Count; i++)
            //             {
            //                 uvs[i] = new Vector2(surface[i].x, surface[i].y); // Use x and y as UV coordinates
            //             }
            //             surfaceMesh.uv = uvs;

            //             // Recalculate normals and bounds for the surface mesh
            //             surfaceMesh.RecalculateNormals();
            //             surfaceMesh.RecalculateBounds();

            //             // Add the surface mesh to the list of surface meshes
            //             surfaceMeshes.Add(surfaceMesh);
            //         }
            //     }

            //     return surfaceMeshes;
            // }


            // public static Mesh GenerateMeshFromVertexSurfaces(Dictionary<Vector3, List<List<Vector3>>> vertexSurfaceSets)
            // {
            //     // Create a list to hold the individual surface meshes
            //     List<Mesh> surfaceMeshes = new List<Mesh>();

            //     foreach (var kvp in vertexSurfaceSets)
            //     {
            //         Vector3 meshFacingPosition = kvp.Key;

            //         foreach (List<Vector3> surface in kvp.Value)
            //         {
            //             // Create a new mesh to represent the current surface
            //             Mesh surfaceMesh = new Mesh();

            //             // Convert the list of vertex positions into arrays of vertices, triangles, and UVs
            //             Vector3[] vertices = surface.ToArray();
            //             int[] triangles = GenerateTriangles(vertices.Length);
            //             Vector2[] uvs = GenerateUVs(vertices.Length);

            //             // Assign the vertices, triangles, and UVs to the surface mesh
            //             surfaceMesh.vertices = vertices;
            //             surfaceMesh.triangles = triangles;
            //             surfaceMesh.uv = uvs;

            //             // Calculate the normals of the vertices based on their relationship with the meshFacingPosition
            //             Vector3[] normals = new Vector3[vertices.Length];
            //             for (int i = 0; i < vertices.Length; i++)
            //             {
            //                 normals[i] = (vertices[i] - meshFacingPosition).normalized;
            //             }
            //             surfaceMesh.normals = normals;

            //             // Reverse the triangle winding order to ensure the triangles are facing the meshFacingPosition
            //             for (int i = 0; i < triangles.Length; i += 3)
            //             {
            //                 int temp = triangles[i];
            //                 triangles[i] = triangles[i + 2];
            //                 triangles[i + 2] = temp;
            //             }
            //             surfaceMesh.triangles = triangles;

            //             // Recalculate bounds for the surface mesh
            //             surfaceMesh.RecalculateBounds();

            //             // Add the surface mesh to the list of surface meshes
            //             surfaceMeshes.Add(surfaceMesh);
            //         }
            //     }

            //     // Create a new mesh to hold the combined surface meshes
            //     Mesh combinedMesh = new Mesh();

            //     // Combine the surface meshes into a single mesh
            //     CombineInstance[] combine = new CombineInstance[surfaceMeshes.Count];
            //     for (int i = 0; i < surfaceMeshes.Count; i++)
            //     {
            //         combine[i].mesh = surfaceMeshes[i];
            //         combine[i].transform = Matrix4x4.identity;
            //     }

            //     combinedMesh.CombineMeshes(combine, true, true);

            //     // Return the combined mesh
            //     return combinedMesh;
            // }


            // Helper method to generate UV coordinates for a mesh
            private static Vector2[] GenerateUVs(int vertexCount)
            {
                Vector2[] uvs = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    uvs[i] = new Vector2(0, 0); // Set UV coordinates to (0, 0) for simplicity
                }
                return uvs;
            }
        }

        public static HexagonCellPrototype GetClosestCenter(List<HexagonCellPrototype> prototypes, Vector3 position)
        {
            HexagonCellPrototype nearestCellToPos = prototypes[0];
            float nearestDist = float.MaxValue;

            for (int i = 0; i < prototypes.Count; i++)
            {
                float dist = VectorUtil.DistanceXZ(prototypes[i].center, position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestCellToPos = prototypes[i];
                }
            }
            return nearestCellToPos;
        }
        public static (HexagonCellPrototype, float) GetClosestCenter_WithDistance(List<HexagonCellPrototype> cells, Vector3 position)
        {
            HexagonCellPrototype nearest = cells[0];
            float nearestDist = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                float dist = VectorUtil.DistanceXZ(cells[i].center, position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = cells[i];
                }
            }
            return (nearest, nearestDist);
        }

        public static Vector3 CalculateCenterPositionFromGroup(List<HexagonCellPrototype> prototypes)
        {
            Vector3 center = Vector3.zero;

            // Sum up all the vertex positions
            for (int i = 0; i < prototypes.Count; i++)
            {
                center += prototypes[i].center;
            }

            // Divide by the total number of vertices to get the average position
            center /= prototypes.Count;

            return center;
        }

    }


    public enum UndergroundEntranceType
    {
        Basement,
        Pit,
        Cave,
    }

}