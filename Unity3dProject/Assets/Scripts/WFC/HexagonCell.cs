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
    public class HexagonCell : MonoBehaviour, IHexCell
    {
        #region Static Vars
        public static float neighborSearchCenterDistMult = 2.4f;
        public static float GetCornerNeighborSearchDist(int cellSize) => 1.2f * ((float)cellSize / 12f);
        #endregion

        #region Interface Methods
        public string GetId() => id;
        public string Get_Uid() => null;
        public string GetLayerStackId() => layerStackId;
        public int GetSize() => size;
        public int GetLayer() => _layer;

        public void SetGridLayer(int value)
        {
            _layer = value;
        }

        public bool IsEdge() => isEdgeCell;
        public bool IsEdgeOfParent() => isEdgeOfParent;
        public bool IsOriginalGridEdge() => isOGGridEdge;

        public bool IsEntry() => isEntryCell;
        public bool IsPath() => isPathCell;
        public bool IsGround() => cellStatus == CellStatus.GenericGround || cellStatus == CellStatus.FlatGround;
        public bool IsGroundCell() => IsGround() || isLeveledGroundCell;
        public bool IsGenericGround() => cellStatus == CellStatus.GenericGround;
        public bool IsFlatGround() => cellStatus == CellStatus.FlatGround;
        public bool IsUnderGround() => cellStatus == CellStatus.UnderGround;
        public bool IsUnderWater() => cellStatus == CellStatus.Underwater;
        public void SetToGround(bool isFlatGround)
        {
            if (isFlatGround)
            {
                SetCellStatus(CellStatus.FlatGround);
            }
            else SetCellStatus(CellStatus.GenericGround);
        }


        public Vector2 parentCoordinate { get; private set; } = Vector2.positiveInfinity;
        public Vector2 worldCoordinate { get; private set; } = Vector2.positiveInfinity;
        public Vector2 GetLookup() => worldCoordinate;
        public Vector2 GetParentLookup() => parentCoordinate;
        public Vector2 GetWorldSpaceLookup() => worldCoordinate;


        public bool IsGridHost() => isGridHost;

        [SerializeField] private bool isGridHost;
        public void SetGridHost(bool enable)
        {
            // Debug.LogError("SetGridHost on HexagonCell");
            isGridHost = enable;
        }

        public CellStatus GetCellStatus() => cellStatus;
        public void SetCellStatus(CellStatus status)
        {
            cellStatus = status;
        }
        public EdgeCellType GetEdgeCellType() => _edgeCellType;
        public Vector3 GetPosition() => transform.position;
        public Vector3[] GetCorners() => _cornerPoints;
        public Vector3[] GetSides() => _sides;
        #endregion

        public string id = "-1";
        public string layerStackId { get; private set; }
        [SerializeField] private int _layer = 0;

        [Header("Base Settings")]
        [SerializeField] private int size = 12;
        public void SetSize(int value)
        {
            size = value;
        }


        [Header("Vector Points")]
        #region Vector Points & Vertices
        public Vector3[] _cornerPoints;
        public Vector3[] _sides;
        public List<int> _vertexIndices;
        public List<int>[] _vertexIndicesBySide;
        #endregion

        [Header("Neighbors")]
        #region Neighbors
        public List<HexagonCell> _neighbors;
        public HexagonCell[] layeredNeighbor = new HexagonCell[2]; // 0= bottom , 1= top
        public HexagonCell[] neighborsBySide = new HexagonCell[6];

        public void SetTopNeighbor(HexagonCell cell)
        {
            layeredNeighbor[1] = cell;
        }
        public void SetBottomNeighbor(HexagonCell cell)
        {
            layeredNeighbor[0] = cell;
        }
        public HexagonCell GetTopNeighbor() => layeredNeighbor[1];
        public HexagonCell GetBottomNeighbor() => layeredNeighbor[0];
        public bool HasTopNeighbor() => layeredNeighbor[1] != null;
        public bool HasBottomNeighbor() => layeredNeighbor[0] != null;

        public int GetSideNeighborCount(bool scopeToParentCell)
        {
            int count = 0;
            foreach (HexagonCell item in neighborsBySide)
            {
                if (item != null && (scopeToParentCell == false || item.GetParenCellId() == _parentId))
                {
                    count++;
                }
            }
            return count;
        }

        public List<HexagonCell> GetLayerNeighbors() => _neighbors.FindAll(n => n.GetLayer() == GetLayer());
        public bool HasEntryNeighbor() => _neighbors.Find(n => n.isEntryCell);
        public int GetUnassignedNeighborCount(bool includeLayers) => _neighbors.FindAll(n => n.IsAssigned() == false && n.GetLayer() == GetLayer()).Count;

        public List<HexagonCell> GetPathNeighbors()
        {
            List<HexagonCell> found = new List<HexagonCell>();
            found.AddRange(neighborsBySide.ToList().FindAll(n => n.isPathCell));
            foreach (var layerNeighbor in layeredNeighbor)
            {
                if (layerNeighbor != null)
                {
                    found.AddRange(layerNeighbor._neighbors.FindAll(n => n.isPathCell && !found.Contains(n)));
                }
            }
            return found;
        }
        public int GetNeighborsRelativeSide(HexagonSide side)
        {
            if (neighborsBySide[(int)side] == null) return -1;

            for (int neighborSide = 0; neighborSide < 6; neighborSide++)
            {
                if (neighborsBySide[(int)side].neighborsBySide[neighborSide] == this)
                {
                    return neighborSide;
                }
            }
            return -1;
        }
        #endregion


        public bool road;

        private Dictionary<TileCategory, float> categoyBias;
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
            if (GetLayer() == 0) return null;

            if (layeredNeighbor[0] == null) return null;

            if (layeredNeighbor[0].currentTile == null) return null;

            return layeredNeighbor[0].currentTile.GetRotatedLayerCornerSockets(top, layeredNeighbor[0].currentTileRotation, layeredNeighbor[0].isCurrentTileInverted);
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
            NeighborLayerCornerSockets bottomNeighborTopCornerSockets = EvaluateLayeredNeighborTileSockets(layeredNeighbor[0], true, tileContext);
            NeighborLayerCornerSockets topNeighborBtmCornerSockets = EvaluateLayeredNeighborTileSockets(layeredNeighbor[1], false, tileContext);

            layeredNeighborCornerSockets[0] = bottomNeighborTopCornerSockets;
            layeredNeighborCornerSockets[1] = topNeighborBtmCornerSockets;
            return layeredNeighborCornerSockets;
        }

        public NeighborLayerCornerSockets EvaluateLayeredNeighborTileSockets(HexagonCell layerNeighbor, bool top, TileContext tileContext)
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

        public NeighborLayerCornerSockets GetLayeredNeighborRelativeTileSockets(HexagonCell layerNeighbor, bool top)
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
            HexagonCell sideNeighbor = neighborsBySide[side];

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
                HexagonCell sideNeighbor = neighborsBySide[side];

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
                            else if (isLeveledCell)
                            {
                                GlobalSockets defaultSocketId;

                                if (isLeveledEdge)
                                {
                                    if (!sideNeighbor.isLeveledCell)
                                    {
                                        defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                                    }
                                    else
                                    {
                                        defaultSocketId = sideNeighbor.isLeveledEdge ? GlobalSockets.Leveled_Edge_Part : defaultSocketId = GlobalSockets.Leveled_Inner;
                                    }
                                }
                                else
                                {
                                    defaultSocketId = GlobalSockets.Leveled_Inner;
                                }
                                neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(defaultSocketId);
                                neighborCornerSockets.topCorners = GetDefaultSideSocketSet(defaultSocketId);

                            }
                            else
                            {
                                GlobalSockets defaultSocketId;

                                if (IsGridEdge())
                                {
                                    // Edge Connectors
                                    if (GetEdgeCellType() == EdgeCellType.Connector && (sideNeighbor.GetParenCellId() != GetParenCellId()))
                                    {
                                        defaultSocketId = GlobalSockets.Unset_Edge_Connector;
                                    }
                                    else
                                    {
                                        defaultSocketId = (useWalledEdgePreference && sideNeighbor.isEdgeCell) ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                                    }
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

        public NeighborSideCornerSockets[] GetSideNeighborTileSockets(bool useWalledEdgePreference = false, bool microTile = true)
        {
            NeighborSideCornerSockets[] neighborCornerSocketsBySide = new NeighborSideCornerSockets[6];

            for (int side = 0; side < 6; side++)
            {
                NeighborSideCornerSockets neighborCornerSockets = new NeighborSideCornerSockets();
                HexagonCell sideNeighbor = neighborsBySide[side];

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

                        if (isEdgeCell)
                        {
                            // Edge Connectors
                            // if (GetEdgeCellType() == EdgeCellType.Connector && (sideNeighbor.GetParenCellId() != GetParenCellId()))
                            // {
                            //     defaultSocketId = GlobalSockets.Unset_Edge_Connector;
                            // }
                            // else
                            // {
                            // defaultSocketId = sideNeighbor.isEdgeCell ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                            defaultSocketId = (useWalledEdgePreference && sideNeighbor.isEdgeCell) ? GlobalSockets.Unassigned_EdgeCell : GlobalSockets.Unassigned_InnerCell;
                            // }
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

        private Transform center;
        public CellStatus cellStatus;
        public bool isGroundRamp;

        [Header("WFC Params")]
        public float highestProbability;
        public bool isEntryCell { private set; get; }
        public void SetEntryCell(bool enable)
        {
            isEntryCell = enable;
        }
        private bool isOGGridEdge;
        public bool isEdgeCell;//{ private set; get; }
        public bool isEdgeOfParent;//{ private set; get; }
        public bool IsInnerEdge() => isEdgeCell && _edgeCellType == EdgeCellType.Inner;
        public bool IsGridEdge() => isEdgeCell && _edgeCellType == EdgeCellType.Default;
        public void SetEdgeCell(bool enable, EdgeCellType type)
        {
            isEdgeCell = enable;
            SetEdgeCellType(enable ? type : EdgeCellType.None);
        }

        [SerializeField] private EdgeCellType _edgeCellType;
        public void SetEdgeCellType(EdgeCellType type)
        {
            _edgeCellType = type;
        }

        public bool isPathCell;//{ private set; get; }
        public void SetPathCell(bool enable)
        {
            isPathCell = enable;
        }
        public bool isPathPrototype;//{ private set; get; }
        public void SetPathPrototype(bool enable)
        {
            isPathPrototype = enable;
        }

        #region World Space
        public Vector2 worldspaceCoord { get; private set; } = Vector2.positiveInfinity;
        public Vector2 GetWorldSpaceCoordinate() => worldspaceCoord;
        public void SetWorldSpaceCoordinate(Vector2 coordinate)
        {
            worldspaceCoord = coordinate;
        }

        public bool HasWorldSpaceCoordinate() => worldspaceCoord != Vector2.positiveInfinity;
        public bool isWorldSpaceCellActive;
        #endregion

        public bool isLeveledGroundCell;//{ private set; get; }
        public void SetLeveledGroundCell(bool enable, bool isFlatGround)
        {
            isLeveledGroundCell = enable;
            SetToGround(isFlatGround);
        }

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



        [Header("Host Parent Cell")]
        [SerializeField] private string _parentId = null;
        public string GetParenCellId() => _parentId;
        public void SetParentCellId(string _id)
        {
            _parentId = _id;
        }

        [Header("Micro Cluster System")]


        [Header("Cluster")]
        [SerializeField] public bool isClusterPrototype;
        [SerializeField] private string clusterId = null;
        public bool IsInCluster() => clusterId != null;
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
            if (_neighbors.Count == 0) return 0;
            int num = 0;
            foreach (HexagonCell item in _neighbors)
            {
                if (item.isClusterPrototype == false) num++;
            }
            return num;
        }
        private bool _isClusterCellParent;
        public bool IsClusterCellParent() => _isClusterCellParent;
        public bool IsInClusterSystem() => IsInCluster() || IsClusterCellParent() || (_clusterCellParentId != "" && _clusterCellParentId != null);

        [SerializeField] private string _clusterCellParentId;
        public string GetClusterCellParentId() => _clusterCellParentId;
        public void SetClusterCellParentId(string _id)
        {
            _clusterCellParentId = _id;
        }

        [Header("Tile")]
        [SerializeField] private IHexagonTile currentTile;
        [SerializeField] private int currentTileRotation = 0;
        [SerializeField] private bool isCurrentTileInverted;
        public int GetTileRotation() => currentTileRotation;
        public bool IsTileInverted() => isCurrentTileInverted;
        public HexagonTileCore GetCurrentTile() => (HexagonTileCore)currentTile;
        public void SetTile(HexagonTileCore newTile, int rotation, bool inverted = false)
        {
            // if (IsInCluster())
            // {
            //     Debug.LogError("Trying to set a Tile on a cell with a clusterId assigned");
            //     return;
            // }
            currentTile = (IHexagonTile)newTile;
            currentTileRotation = rotation;
            isCurrentTileInverted = inverted;
        }

        // public void SetTile(HexagonTile newTile, int rotation)
        // {
        //     if (IsInCluster())
        //     {
        //         Debug.LogError("Trying to set a Tile on a cell with a clusterId assigned");
        //         return;
        //     }
        //     currentTile = (IHexagonTile)newTile;
        //     currentTileRotation = rotation;
        //     isLeveledRampCell = newTile.isLeveledRamp;
        // }

        public bool isIgnored;
        public void SetIgnore(bool ignore)
        {
            isIgnored = ignore;
        }

        public bool IsWFC_Assigned() => (isPathCell || isGridHost || currentTile != null || (isLeveledCell && !isLeveledGroundCell));
        public bool IsPreAssigned() => (isPathCell || isGridHost || IsInCluster());
        public bool IsAssigned() => currentTile != null || isGridHost || isIgnored || IsInClusterSystem() || IsDisposable() || (isLeveledCell && !isLeveledGroundCell);
        public bool IsDisposable() => cellStatus == CellStatus.Remove;
        public bool IsRemoved() => (cellStatus == CellStatus.Remove);

        public IHexagonTile GetTile() => currentTile;

        public void ClearTile()
        {
            currentTile = null;
            currentTileRotation = 0;
            SetIgnore(false);
        }

        public void RecalculateEdgePoints()
        {
            _cornerPoints = HexCoreUtil.GenerateHexagonPoints(transform.position, size);
            _sides = HexagonGenerator.GenerateHexagonSidePoints(_cornerPoints);
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
                _currentSize = size;
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
        [SerializeField] private bool showNeighborSearchRadius;
        [SerializeField] private bool showSides;
        [SerializeField] private bool showCorners;
        [SerializeField] private bool showEdges;
        [SerializeField] private bool resetPoints;

        public bool isHighlighted { get; private set; }
        public void Highlight(bool enable)
        {
            isHighlighted = enable;
        }


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
                float radius = 0.36f;
                if (size < 12)
                {
                    radius = 0.24f;


                    if (isEntryCell)
                    {
                        Gizmos.color = Color.green;
                        radius = 0.4f;
                    }
                    else
                    {

                        switch (_edgeCellType)
                        {
                            case EdgeCellType.Connector:
                                Gizmos.color = Color.yellow;
                                break;

                            case EdgeCellType.Default:
                                Gizmos.color = Color.red;
                                break;
                            default:
                                Gizmos.color = Color.blue;
                                break;
                        }
                    }
                }
                else
                {

                    Gizmos.color = Color.black;
                    if (isEntryCell)
                    {
                        Gizmos.color = Color.green;
                        radius = 0.5f;
                    }
                    else if (_edgeCellType == EdgeCellType.Default)
                    {
                        Gizmos.color = Color.red;
                        radius = 0.9f;
                    }
                }
                Gizmos.DrawSphere(center.position, radius);
                if (_edgeCellType == EdgeCellType.Connector) Gizmos.DrawWireSphere(center.position, radius * 2f);
            }

            // float radius = 4f;
            // if (highlight)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawSphere(center.position, radius);
            // }

            // if (isPathCell)
            // {
            //     Gizmos.color = Color.blue;
            //     radius = 8f;
            //     Gizmos.DrawSphere(center.position, radius);
            // }
            // else if (isEntryCell)
            // {
            //     Gizmos.color = Color.yellow;
            //     radius = 8f;
            //     Gizmos.DrawSphere(center.position, radius);
            // }
            // else if (isLeveledEdge)
            // {
            //     Gizmos.color = Color.red;
            //     radius = 12f;
            //     Gizmos.DrawSphere(center.position, radius);
            // }
            // else if (isLeveledCell)
            // {
            //     Gizmos.color = Color.black;
            //     radius = 8f;
            //     Gizmos.DrawSphere(center.position, radius);
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

            if (showCorners || showNeighborSearchRadius)
            {
                Gizmos.color = Color.magenta;
                foreach (Vector3 item in _cornerPoints)
                {
                    Gizmos.DrawSphere(item, 0.3f);

                    if (showNeighborSearchRadius)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(item, GetCornerNeighborSearchDist(size));
                    }
                }

                if (showNeighborSearchRadius)
                {
                    Gizmos.DrawWireSphere(transform.position, (size * neighborSearchCenterDistMult));
                }
            }

            if (showEdges)
            {
                Gizmos.color = Color.magenta;
                VectorUtil.DrawHexagonPointLinesInGizmos(_cornerPoints);
            }

            if (showNeighbors)
            {
                Gizmos.color = Color.green;
                foreach (HexagonCell neighbor in _neighbors)
                {
                    Gizmos.DrawSphere(neighbor.center.position, 3f);
                }
            }
        }
        public void SetNeighborsBySide(float offset = 0.33f)
        {
            HexagonCell[] _neighborsBySide = new HexagonCell[6];
            HashSet<string> added = new HashSet<string>();

            RecalculateEdgePoints();

            for (int side = 0; side < 6; side++)
            {
                Vector3 sidePoint = _sides[side];

                for (int neighbor = 0; neighbor < _neighbors.Count; neighbor++)
                {
                    if (_neighbors[neighbor].GetLayer() != GetLayer() || added.Contains(_neighbors[neighbor].id)) continue;

                    _neighbors[neighbor].RecalculateEdgePoints();

                    for (int neighborSide = 0; neighborSide < 6; neighborSide++)
                    {
                        Vector3 neighborSidePoint = _neighbors[neighbor]._sides[neighborSide];

                        // float dist = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z));
                        // Debug.Log("offset: " + offset + ", dist: " + dist);
                        // Debug.Log("sidePoint.x: " + sidePoint.x + ", neighborSidePoint.x: " + neighborSidePoint.x);

                        if (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z)) <= offset)
                        {
                            _neighborsBySide[side] = _neighbors[neighbor];
                            added.Add(_neighbors[neighbor].id);
                            break;
                        }
                    }
                }
            }
            neighborsBySide = _neighborsBySide;
        }



        public static HexagonCell EvaluateLevelGroundPath(HexagonCell pathCell)
        {
            if (pathCell.layeredNeighbor[1] == null) return pathCell;

            HexagonCell currentCell = pathCell.layeredNeighbor[1];
            while (currentCell != null)
            {
                if (currentCell.isLeveledGroundCell)
                {
                    currentCell.SetPathCell(true);
                    currentCell.ClearTile();
                    pathCell.SetPathCell(false);
                    return currentCell;
                }
                else
                {
                    currentCell = currentCell.layeredNeighbor[1];
                }
            }
            return pathCell;
        }



        public static List<HexagonCell> GetLeveledEdgeCells(List<HexagonCell> layerCells, bool assign)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();

            foreach (HexagonCell cell in layerCells)
            {
                int neighborCount = cell._neighbors.FindAll(n => layerCells.Contains(n) && n.GetLayer() == cell.GetLayer()).Count;
                if (neighborCount < 6)
                {
                    edgeCells.Add(cell);
                    if (assign) cell.SetLeveledEdge(true);
                }
            }
            // Order edge cells by the fewest neighbors first
            return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
        }

        public static List<HexagonCell> GetRandomEntryCells(List<HexagonCell> edgeCells, int num, bool assign, int gridLayer = 0, bool excludeAdjacentNeighbors = true)
        {
            List<HexagonCell> cells = new List<HexagonCell>();
            cells.AddRange(gridLayer == -1 ? edgeCells : edgeCells.FindAll(c => c.GetLayer() == gridLayer));

            HexCellUtil.ShuffleCells(cells);

            List<HexagonCell> entrances = new List<HexagonCell>();

            foreach (HexagonCell cell in cells)
            {
                if (entrances.Count >= num) break;

                bool isNeighbor = false;
                foreach (HexagonCell item in entrances)
                {
                    if ((item._neighbors.Contains(cell) && !excludeAdjacentNeighbors) || (excludeAdjacentNeighbors && item._neighbors.Find(nb => nb._neighbors.Contains(cell))))
                    {
                        isNeighbor = true;
                        break;
                    }
                }
                if (!isNeighbor)
                {
                    entrances.Add(cell);
                    if (assign) cell.SetEntryCell(true);
                }

            }
            return entrances.OrderByDescending(x => x._neighbors.Count).ToList();
        }

        public static List<HexagonCell> PickRandomEntryCellsFromEdgeCells(List<HexagonCell> allEdgeCells, int num, bool assign, bool excludeAdjacentNeighbors = true)
        {
            // sort the layers by edge cell count;
            // sort edge cells by neighbor count
            // select random cells from beginning of lists
            List<HexagonCell> possibleCells = new List<HexagonCell>();
            foreach (HexagonCell edgeCell in allEdgeCells)
            {
                if (edgeCell.cellStatus != CellStatus.GenericGround) continue;
                int groundNeighborCount = edgeCell._neighbors.FindAll(
                        n => n.cellStatus == CellStatus.GenericGround && n.GetLayer() == edgeCell.GetLayer()).Count;
                if (groundNeighborCount >= 3) possibleCells.Add(edgeCell);
            }

            return GetRandomEntryCells(possibleCells, num, assign, -1, excludeAdjacentNeighbors);
        }

        public static (Dictionary<int, List<HexagonCell>>, Dictionary<int, List<HexagonCell>>) GetRandomGridPathsForLevels(Dictionary<int, List<HexagonCell>> cellsByLevel, Vector3 position, int max, bool ignoreEdgeCells, int maxEntryCellsPerLevel = 2)
        {
            Dictionary<int, List<HexagonCell>> pathsByLevel = new Dictionary<int, List<HexagonCell>>();
            Dictionary<int, List<HexagonCell>> rampsByLevel = new Dictionary<int, List<HexagonCell>>();

            int lastLevel = cellsByLevel.Count;

            foreach (var kvp in cellsByLevel)
            {
                int level = kvp.Key;
                List<HexagonCell> levelCells = kvp.Value;

                List<HexagonCell> entryCells = GetRandomEntryCells(levelCells.FindAll(c => c.isEdgeCell), maxEntryCellsPerLevel, false, level);
                List<HexagonCell> newPaths = GetRandomCellPaths(entryCells, levelCells, position, ignoreEdgeCells);

                if (newPaths.Count > 0)
                {
                    pathsByLevel.Add(level, newPaths);

                    if (level < lastLevel)
                    {
                        List<HexagonCell> newRamps = GetRandomLevelConnectorCells(newPaths, maxEntryCellsPerLevel);
                        rampsByLevel.Add(level, newRamps);
                    }
                }
            }

            return (pathsByLevel, rampsByLevel);
        }


        public static HexagonCell GetClosestCellByCenterPoint(List<HexagonCell> cells, Vector3 position)
        {
            HexagonCell nearestCellToPos = cells[0];
            float nearestDist = float.MaxValue;

            for (int i = 0; i < cells.Count; i++)
            {
                float dist = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(cells[i].transform.position.x, cells[i].transform.position.z));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestCellToPos = cells[i];
                }
            }
            return nearestCellToPos;
        }

        public static List<HexagonCell> GetRandomCellPaths(List<HexagonCell> entryCells, List<HexagonCell> allCells, Vector3 centerPosition, bool ignoreEdgeCells = true)
        {
            Debug.Log("GetRandomCellPaths => entryCells: " + entryCells.Count + ", allCells: " + allCells.Count);

            List<HexagonCell> finalPath = new List<HexagonCell>();
            HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(allCells, centerPosition);
            // entryCells = HexagonCell.GetRandomEntryCells(edgeCells, 3, false);

            foreach (HexagonCell entryCell in entryCells)
            {
                HexagonCell innerNeighbor = entryCell._neighbors.Find(n => n.isEdgeCell == false);

                List<HexagonCell> cellPath = HexagonCell.FindPath(innerNeighbor, centerCell, ignoreEdgeCells);
                if (cellPath != null)
                {
                    finalPath.AddRange(cellPath);
                    if (finalPath.Contains(innerNeighbor) == false) finalPath.Add(innerNeighbor);
                }
            }

            Debug.Log("GetRandomCellPaths => finalPath: " + finalPath.Count + ", centerCell: " + centerCell.id);
            return finalPath;
        }

        public static List<HexagonCell> ClearPathCellClumps(List<HexagonCell> pathCells)
        {
            List<HexagonCell> result = new List<HexagonCell>();
            List<HexagonCell> cleared = new List<HexagonCell>();

            foreach (HexagonCell cell in pathCells)
            {
                List<HexagonCell> pathNeighbors = cell._neighbors.FindAll(n => pathCells.Contains(n) && cleared.Contains(n) == false && n.GetLayer() == cell.GetLayer());

                if (pathNeighbors.Count >= 4)
                {
                    // bool neighborHasMultipleConnections = pathNeighbors.Find(n => n._neighbors.FindAll(n => pathNeighbors.Contains(n)).Count > 1);
                    // if (neighborHasMultipleConnections)
                    // {
                    cell.SetPathCell(false);
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

        public static List<HexagonCell> GenerateNewCellPath(HexagonCell startCell, HexagonCell endCell, bool ignoreEdgeCells, bool startCellIgnoresLayeredNeighbors = true, bool clearPathCellClumps = true)
        {
            List<HexagonCell> newCellPath = FindPath(startCell, endCell, ignoreEdgeCells, startCellIgnoresLayeredNeighbors);
            return clearPathCellClumps ? ClearPathCellClumps(newCellPath) : newCellPath;
        }

        public static List<HexagonCell> FindPath(HexagonCell startCell, HexagonCell endCell, bool ignoreEdgeCells, bool startCellIgnoresLayeredNeighbors = true)
        {
            // Create a queue to store the cells to be visited
            Queue<HexagonCell> queue = new Queue<HexagonCell>();

            // Create a dictionary to store the parent of each cell
            Dictionary<HexagonCell, HexagonCell> parent = new Dictionary<HexagonCell, HexagonCell>();

            // Create a set to store the visited cells
            HashSet<HexagonCell> visited = new HashSet<HexagonCell>();

            // Enqueue the start cell and mark it as visited
            queue.Enqueue(startCell);
            visited.Add(startCell);

            // Get an inner neighbor of endCell if it is on the edge 
            if (ignoreEdgeCells && endCell.isEdgeCell || endCell.isEntryCell)
            {
                HexagonCell newEndCell = endCell._neighbors.Find(n => n.GetLayer() == endCell.GetLayer() && !n.isEdgeCell && !n.isEntryCell);
                if (newEndCell != null) endCell = newEndCell;
            }

            // Run the BFS loop
            while (queue.Count > 0)
            {
                HexagonCell currentCell = queue.Dequeue();

                // Check if the current cell is the end cell
                if (currentCell == endCell)
                {
                    // Create a list to store the path
                    List<HexagonCell> path = new List<HexagonCell>();

                    // Trace back the path from the end cell to the start cell
                    HexagonCell current = endCell;
                    while (current != startCell)
                    {
                        path.Add(current);
                        current = parent[current];
                    }
                    path.Reverse();
                    return path;
                }

                // Enqueue the unvisited neighbors
                foreach (HexagonCell neighbor in currentCell._neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);

                        if (ignoreEdgeCells && neighbor.isEdgeCell) continue;

                        // If entry cell, dont use layered neighbors
                        if (((currentCell == startCell && startCellIgnoresLayeredNeighbors) || currentCell.isEntryCell) && currentCell.layeredNeighbor.Contains(neighbor)) continue;

                        //  Dont use layered neighbors if not leveled
                        if (currentCell.layeredNeighbor[1] == neighbor && neighbor.isLeveledCell == false) continue;

                        queue.Enqueue(neighbor);
                        parent[neighbor] = currentCell;
                    }
                }
            }
            // If there is no path between the start and end cells
            return null;
        }



        public static List<HexagonCell> GenerateRandomCellPath(List<HexagonCell> entryCells, HexagonCell topEdgeCell, Dictionary<int, List<HexagonCell>> allCellsByLayer, Vector3 position)
        {
            HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(allCellsByLayer[0], position);

            List<HexagonCell> result = FindPath(entryCells[0], topEdgeCell ? topEdgeCell : centerCell, true);

            int paths = 0;
            for (int i = 1; i < entryCells.Count; i++)
            {
                paths++;
                result.AddRange(HexagonCell.FindPath(entryCells[i], centerCell, true));
                paths++;
                result.AddRange(HexagonCell.FindPath(entryCells[i], entryCells[i - 1], true));
            }
            return ClearPathCellClumps(result);
        }

        public static List<HexagonCell> GenerateRandomCellPath(List<HexagonCell> entryCells, Dictionary<int, List<HexagonCell>> allCellsByLayer, Vector3 position)
        {
            HexagonCell topEdgeCell = allCellsByLayer[allCellsByLayer.Count - 1].Find(c => c.isLeveledEdge);
            return GenerateRandomCellPath(entryCells, topEdgeCell, allCellsByLayer, position);
        }

        public static Dictionary<int, List<HexagonCell>> GenerateRandomCellPaths(List<HexagonCell> entryCells, HexagonCell topEdgeCell, Dictionary<int, List<HexagonCell>> allCellsByLayer, Vector3 position)
        {
            Dictionary<int, List<HexagonCell>> newCellPaths = new Dictionary<int, List<HexagonCell>>();

            HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(allCellsByLayer[0], position);

            newCellPaths.Add(0, HexagonCell.GenerateNewCellPath(entryCells[0], topEdgeCell ? topEdgeCell : centerCell, true));

            int paths = 0;
            for (int i = 1; i < entryCells.Count; i++)
            {
                paths++;
                newCellPaths.Add(paths, HexagonCell.GenerateNewCellPath(entryCells[i], centerCell, true));
                paths++;
                newCellPaths.Add(paths, HexagonCell.GenerateNewCellPath(entryCells[i], entryCells[i - 1], true));
            }

            return newCellPaths;
        }

        public static Dictionary<int, List<HexagonCell>> GenerateRandomCellPaths(List<HexagonCell> entryCells, Dictionary<int, List<HexagonCell>> allCellsByLayer, Vector3 position)
        {

            HexagonCell topEdgeCell = allCellsByLayer[allCellsByLayer.Count - 1].Find(c => c.isLeveledEdge);
            return GenerateRandomCellPaths(entryCells, topEdgeCell, allCellsByLayer, position);
        }


        public static List<HexagonCell> GetRandomLevelConnectorCells(List<HexagonCell> levelPathCells, int max = 2)
        {
            List<HexagonCell> results = new List<HexagonCell>();
            foreach (HexagonCell item in levelPathCells)
            {
                if (results.Count == max) break;

                HexagonCell found = item._neighbors.Find(c => !c.isEntryCell && !c.isEdgeCell && !levelPathCells.Contains(c) && c.GetLayer() == item.GetLayer());
                if (found != null && !results.Contains(found)) results.Add(found);
            }
            return results;
        }

        public static List<HexagonCell> SelectCellsInRadiusOfCell(List<HexagonCell> cells, HexagonCell centerCell, float radius)
        {
            Vector2 centerPos = new Vector2(centerCell.transform.position.x, centerCell.transform.position.z);
            //Initialize a list to store cells within the radius distance
            List<HexagonCell> selectedCells = new List<HexagonCell>();

            //Iterate through each cell in the input list
            foreach (HexagonCell cell in cells)
            {
                Vector2 cellPos = new Vector2(cell.transform.position.x, cell.transform.position.z);

                // Check if the distance between the cell and the center cell is within the given radius
                if (Vector2.Distance(centerPos, cellPos) <= radius)
                {
                    //If the distance is within the radius, add the current cell to the list of selected cells
                    selectedCells.Add(cell);
                }
            }
            //Return the list of selected cells
            return selectedCells;
        }

        public static List<HexagonCell> SelectCellsInRadiusOfRandomCell(List<HexagonCell> cells, float radius)
        {
            //Select a random center cell
            HexagonCell centerCell = cells[UnityEngine.Random.Range(0, cells.Count)];
            return SelectCellsInRadiusOfCell(cells, centerCell, radius);
        }


        public static List<HexagonCell> GetRandomLeveledCells(List<HexagonCell> allLayerCells, float maxRadius, bool assign, int clumpSets = 5)
        {
            List<HexagonCell> result = SelectCellsInRadiusOfRandomCell(allLayerCells, maxRadius);
            for (int i = 0; i < clumpSets; i++)
            {
                List<HexagonCell> newClump = SelectCellsInRadiusOfCell(allLayerCells, result[UnityEngine.Random.Range(0, result.Count)], maxRadius * 0.9f);
                result.AddRange(newClump.FindAll(c => result.Contains(c) == false));
            }

            if (assign)
            {
                foreach (HexagonCell cell in result)
                {
                    cell.SetLeveledCell(true);
                }
            }
            return result;
        }


        public static List<HexagonCell> GetLeveledCellsOfLayer(List<HexagonCell> lastLayerInnerCells, bool assign, int offsetNextLevelEdgeBy = 2)
        {
            List<HexagonCell> result = new List<HexagonCell>();
            foreach (HexagonCell item in lastLayerInnerCells)
            {
                if (item.layeredNeighbor[1] != null && result.Contains(item.layeredNeighbor[1]) == false)
                {
                    result.Add(item.layeredNeighbor[1]);
                }
            }

            //Offeset the next level edge by one 
            List<HexagonCell> toRemoveOffsetEdgeCells;
            for (int offset = 0; offset < offsetNextLevelEdgeBy; offset++)
            {
                toRemoveOffsetEdgeCells = HexagonCell.GetLeveledEdgeCells(result, false);
                result = result.Except(toRemoveOffsetEdgeCells).ToList();
            }

            if (assign)
            {
                List<HexagonCell> leveledEdgeCells = HexagonCell.GetLeveledEdgeCells(result, true);
                foreach (HexagonCell cell in result)
                {
                    cell.SetLeveledCell(true);
                }
            }
            return result;
        }

        public static Dictionary<int, List<HexagonCell>> SetRandomLeveledCellsForAllLayers(Dictionary<int, List<HexagonCell>> allCellsByLayer, float maxRadius, int clumpSets = 5)
        {
            Dictionary<int, List<HexagonCell>> allLeveledCellsForAllLayers = new Dictionary<int, List<HexagonCell>>();

            List<HexagonCell> lastLayerInnerCells = new List<HexagonCell>();

            foreach (var kvp in allCellsByLayer)
            {
                int layer = kvp.Key;

                List<HexagonCell> allLayerCells = kvp.Value;
                List<HexagonCell> levelCells;

                if (layer == 0)
                {
                    levelCells = GetRandomLeveledCells(allLayerCells.FindAll(c => c.isEdgeCell == false && c.HasEntryNeighbor() == false), maxRadius, true, clumpSets);
                }
                else
                {
                    // levelCells = GetLeveledCellsOfLayer(allLayerCells, true);
                    levelCells = GetLeveledCellsOfLayer(lastLayerInnerCells, true);
                }

                // List<HexagonCell> leveledEdgeCells = HexagonCell.GetLeveledEdgeCells(levelCells, true);
                // List<HexagonCell> innerLeveledCells = levelCells.Except(leveledEdgeCells).ToList();
                List<HexagonCell> innerLeveledCells = levelCells;
                foreach (var item in innerLeveledCells)
                {
                    if (item.layeredNeighbor[1] != null)
                    {
                        item.layeredNeighbor[1].SetLeveledGroundCell(true, false);
                        item.SetLeveledGroundCell(false, false);
                    }
                }

                lastLayerInnerCells = innerLeveledCells;
            }

            return allLeveledCellsForAllLayers;
        }

        public static List<HexagonCell> GetRandomLeveledRampCells(List<HexagonCell> leveledEdgeCells, int num, bool assign, bool allowNeighbors = true)
        {
            List<HexagonCell> cells = new List<HexagonCell>();
            cells.AddRange(leveledEdgeCells);

            HexCellUtil.ShuffleCells(cells);

            List<HexagonCell> rampCells = new List<HexagonCell>();

            foreach (HexagonCell cell in cells)
            {
                if (rampCells.Count == num) break;

                bool isNeighbor = false;
                if (allowNeighbors == false)
                {
                    foreach (HexagonCell item in rampCells)
                    {
                        if (item._neighbors.Contains(cell))
                        {
                            isNeighbor = true;
                            break;
                        }
                    }
                }
                if (allowNeighbors || !isNeighbor)
                {
                    rampCells.Add(cell);
                    if (assign) cell.SetLeveledRampCell(true);
                }

            }
            return rampCells.OrderByDescending(x => x._neighbors.Count).ToList();
        }


        // public static void SmoothElevationAlongPath(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        // {
        //     List<int> vertexIndices = new List<int>();
        //     foreach (HexagonCell cell in pathCells)
        //     {
        //         foreach (int vertexIndex in cell._vertexIndices)
        //         {
        //             vertexIndices.Add(vertexIndex);
        //         }
        //     }

        //     float minX = float.MaxValue;
        //     float maxX = float.MinValue;
        //     float minZ = float.MaxValue;
        //     float maxZ = float.MinValue;
        //     float minY = float.MaxValue;
        //     float maxY = float.MinValue;
        //     foreach (int vertexIndex in vertexIndices)
        //     {
        //         TerrainVertex vertice = vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)];
        //         if (vertice.position.x < minX)
        //             minX = vertice.position.x;
        //         if (vertice.position.x > maxX)
        //             maxX = vertice.position.x;
        //         if (vertice.position.z < minZ)
        //             minZ = vertice.position.z;
        //         if (vertice.position.z > maxZ)
        //             maxZ = vertice.position.z;
        //         if (vertice.position.y < minY)
        //             minY = vertice.position.y;
        //         if (vertice.position.y > maxY)
        //             maxY = vertice.position.y;
        //     }

        //     foreach (int vertexIndex in vertexIndices)
        //     {
        //         TerrainVertex vertice = vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)];
        //         float xNormalized = (vertice.position.x - minX) / (maxX - minX);
        //         float zNormalized = (vertice.position.z - minZ) / (maxZ - minZ);
        //         vertice.position.y = Mathf.Lerp(minY, maxY, Mathf.PerlinNoise(xNormalized, zNormalized));
        //         vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)].position = vertice.position;
        //     }
        // }




        // public static void SmoothElevationAlongPath(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        // {
        //     List<TerrainVertex> pathVertices = new List<TerrainVertex>();

        //     // Get all vertices from all path cells
        //     foreach (HexagonCell cell in pathCells)
        //     {
        //         foreach (int vertexIndex in cell._vertexIndices)
        //         {
        //             pathVertices.Add(vertices[vertexIndex / vertices.GetLength(1), vertexIndex % vertices.GetLength(1)]);
        //         }
        //     }

        //     // Smoothly slope the y position of the vertices across the x and z axis
        //     float yMin = pathVertices.Min(v => v.position.y);
        //     float yMax = pathVertices.Max(v => v.position.y);
        //     float yDiff = yMax - yMin;
        //     float xMin = pathVertices.Min(v => v.position.x);
        //     float xMax = pathVertices.Max(v => v.position.x);
        //     float xDiff = xMax - xMin;
        //     float zMin = pathVertices.Min(v => v.position.z);
        //     float zMax = pathVertices.Max(v => v.position.z);
        //     float zDiff = zMax - zMin;

        //     foreach (TerrainVertex vertice in pathVertices)
        //     {
        //         float slopeX = (vertice.position.x - xMin) / xDiff;
        //         float slopeZ = (vertice.position.z - zMin) / zDiff;
        //         vertice.position = new Vector3(vertice.position.x, yMin + slopeX * slopeZ * yDiff, vertice.position.z);

        //     }
        // }



        // TEMP

        // public static void SetUpMicroClusterGrid(List<HexagonCell> allUnassisngedCells, int cellLayers, int cellLayerElevation = 4)
        // {
        //     // Get random parentCell
        //     HexagonCell parentCell = allUnassisngedCells[UnityEngine.Random.Range(0, allUnassisngedCells.Count)];

        //     // Get child cells
        //     List<HexagonCell> childCells = GetChildrenForMicroClusterParent(parentCell);
        //     if (childCells == null || childCells.Count == 0)
        //     {
        //         Debug.LogError("No child Cells found");
        //         return;
        //     }

        //     Dictionary<int, List<HexagonTilePrototype>> prototypeGridByLayer = GenerateMicroClusterGridProtoypes(parentCell, childCells, cellLayers, cellLayerElevation);
        // }

        public static List<HexagonCell> GetChildrenForMicroClusterParent(HexagonCell parentCell, int howMNanyDegreesFromDirectNeighbors = 1, int maxMembers = 6)
        {
            List<HexagonCell> children = new List<HexagonCell>();
            string parentCellId = parentCell.id;
            int found = 0;

            for (var side = 0; side < parentCell.neighborsBySide.Length; side++)
            {
                HexagonCell neighbor = parentCell.neighborsBySide[side];

                if (neighbor == null) continue;

                // Check if direct neighbor is available
                if (neighbor.IsAssigned() == false)
                {
                    neighbor.SetClusterCellParentId(parentCellId);
                    children.Add(neighbor);
                    found++;
                }
                else if (howMNanyDegreesFromDirectNeighbors > 0)
                {
                    // Check if neighbor above is available
                    HexagonCell offNeighborTop = neighbor.GetTopNeighbor();
                    if (offNeighborTop != null && offNeighborTop.IsAssigned() == false)
                    {
                        offNeighborTop.SetClusterCellParentId(parentCellId);
                        children.Add(offNeighborTop);
                        found++;
                    }

                    if (found >= maxMembers) break;

                    // Check if 2nd degree neighbor is available
                    HexagonCell offNeighbor = neighbor.neighborsBySide[side];
                    if (offNeighbor != null && offNeighbor.IsAssigned() == false && offNeighbor.layeredNeighbor[1] != null && offNeighbor.layeredNeighbor[1].IsAssigned() == false)
                    {
                        offNeighbor.SetClusterCellParentId(parentCellId);
                        children.Add(offNeighbor);
                        found++;
                        // // Check if neighbor above is available
                        // if (offNeighbor.layeredNeighbor[1] != null) {
                        //     children.Add(offNeighbor.layeredNeighbor[1]);
                        // }
                    }
                }

                if (found >= maxMembers) break;
            }

            return children;
        }




    }
}