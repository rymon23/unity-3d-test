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
    public class HexagonCell : MonoBehaviour
    {
        #region Static Vars
        static float neighborSearchCenterDistMult = 2.4f;
        public static float GetCornerNeighborSearchDist(int cellSize) => 1.2f * ((float)cellSize / 12f);
        #endregion

        public string id = "-1";

        [Header("Base Settings")]
        [SerializeField] private int size = 12;
        public int GetSize() => size;
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

        public int GetSideNeighborCount()
        {
            int count = 0;
            foreach (HexagonCell item in neighborsBySide)
            {
                if (item != null) count++;
            }
            return count;
        }

        public List<HexagonCell> GetLayerNeighbors() => _neighbors.FindAll(n => n.GetGridLayer() == GetGridLayer());
        public bool HasEntryNeighbor() => _neighbors.Find(n => n.isEntryCell);
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
            if (GetGridLayer() == 0) return null;

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
                if (layerNeighbor.currentTile == null)
                {

                    layeredNeighborCornerSockets.corners = GetDefaultLayeredSocketSet(GlobalSockets.Edge);
                }
                else
                {
                    layeredNeighborCornerSockets = GetLayeredNeighborRelativeTileSockets(layerNeighbor, top);
                }
            }
            return layeredNeighborCornerSockets;
        }

        public NeighborLayerCornerSockets GetLayeredNeighborRelativeTileSockets(HexagonCell layerNeighbor, bool top)
        {
            NeighborLayerCornerSockets neighborCornerSockets = new NeighborLayerCornerSockets();
            neighborCornerSockets.corners = new int[6];
            neighborCornerSockets.corners = layerNeighbor.currentTile.GetRotatedLayerCornerSockets(top, layerNeighbor.currentTileRotation);
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

            (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)neighborRelativeSide);
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
                    neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
                    neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Edge);
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
                        if (useWalledEdgePreference && isEdgeCell && !IsInCluster())
                        {
                            if (!sideNeighbor.isEdgeCell && !sideNeighbor.isEntryCell)
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                                neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                            }

                            else
                            {
                                if (sideNeighbor.isEntryCell || (isEntryCell && sideNeighbor.isEntryCell == false))
                                {
                                    neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Entrance_Generic);
                                    neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Entrance_Generic);
                                }
                                else
                                {
                                    if (IsGroundCell())
                                    {
                                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Edge);
                                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Edge);
                                        // neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.WallPart);
                                        // neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.WallPart);
                                    }
                                    else
                                    {
                                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Empty_Space);
                                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Empty_Space);
                                    }
                                }
                            }
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
                                if (isLeveledEdge)
                                {
                                    if (!sideNeighbor.isLeveledCell)
                                    {
                                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                                    }
                                    else
                                    {

                                        if (sideNeighbor.isLeveledEdge)
                                        {
                                            neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Edge_Part);
                                            neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Edge_Part);
                                        }
                                        else
                                        {
                                            neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Inner);
                                            neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Inner);
                                        }
                                    }
                                }
                                else
                                {

                                    neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Inner);
                                    neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Leveled_Inner);


                                }
                            }
                            else
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                                neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
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
                    else
                    {
                        // Neighbor Has No Tile
                        // if (useWalledEdgePreference && isEdgeCell && !IsInCluster() && (sideNeighbor.isEdgeCell || sideNeighbor.isEntryCell))
                        // {
                        //     neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.UnassignedEdge);
                        //     neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.UnassignedEdge);
                        // }
                        // else
                        // {
                        neighborCornerSockets.bottomCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                        neighborCornerSockets.topCorners = GetDefaultSideSocketSet(GlobalSockets.Unassigned_Inner);
                        // }
                    }
                }
                neighborCornerSocketsBySide[side] = neighborCornerSockets;
            }
            return neighborCornerSocketsBySide;
        }


        private Transform center;

        [Header("WFC Params")]
        public float highestProbability;
        public bool isEntryCell { private set; get; }
        public void SetEntryCell(bool enable)
        {
            isEntryCell = enable;
        }
        public bool isEdgeCell;//{ private set; get; }
        public void SetEdgeCell(bool enable)
        {
            isEdgeCell = enable;
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

        // public bool isGroundCell;
        // public void SetGroundCell(bool enable)
        // {
        //     isGroundCell = enable;
        // }
        public bool IsGroundCell()
        {
            return isLeveledGroundCell || (_gridLayer == 0 && !isLeveledCell);
        }
        public bool isLeveledGroundCell;//{ private set; get; }
        public void SetLeveledGroundCell(bool enable)
        {
            isLeveledGroundCell = enable;
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

        [Header("Grid Layer")]
        [SerializeField] private int _gridLayer = 0;
        public int GetGridLayer() => _gridLayer;
        public void SetGridLayer(int gridlayer)
        {
            _gridLayer = gridlayer;
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
        [SerializeField] private int clusterId = -1;
        public bool IsInCluster() => clusterId != -1;
        public int GetClusterID() => clusterId;
        public void SetClusterID(int _id)
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

        [Header("Tile")]
        [SerializeField] private IHexagonTile currentTile;
        [SerializeField] private int currentTileRotation = 0;
        [SerializeField] private bool isCurrentTileInverted;
        public int GetTileRotation() => currentTileRotation;
        public bool IsTileInverted() => isCurrentTileInverted;
        public HexagonTileCore GetCurrentTile() => (HexagonTileCore)currentTile;
        public void SetTile(HexagonTileCore newTile, int rotation, bool inverted = false)
        {
            if (IsInCluster())
            {
                Debug.LogError("Trying to set a Tile on a cell with a clusterId assigned");
                return;
            }
            currentTile = (IHexagonTile)newTile;
            currentTileRotation = rotation;
            isCurrentTileInverted = inverted;
        }

        public void SetTile(HexagonTile newTile, int rotation)
        {
            if (IsInCluster())
            {
                Debug.LogError("Trying to set a Tile on a cell with a clusterId assigned");
                return;
            }
            currentTile = (IHexagonTile)newTile;
            currentTileRotation = rotation;
            isLeveledRampCell = newTile.isLeveledRamp;
        }
        public bool isIgnored;
        public void SetIgnored(bool ignore)
        {
            isIgnored = ignore;
        }
        public bool IsAssigned() => currentTile != null || isIgnored || IsInClusterSystem() || (isLeveledCell && !isLeveledGroundCell);
        public IHexagonTile GetTile() => currentTile;

        public void ClearTile()
        {
            currentTile = null;
            currentTileRotation = 0;
            SetIgnored(false);
        }

        private void RecalculateEdgePoints()
        {
            _cornerPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
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
                float radius = 0.34f;
                if (size < 12)
                {
                    Gizmos.color = Color.blue;
                    radius = 0.24f;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                Gizmos.DrawSphere(center.position, radius);
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
                ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_cornerPoints);
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
                    if (_neighbors[neighbor].GetGridLayer() != GetGridLayer() || added.Contains(_neighbors[neighbor].id)) continue;

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


        public static (HexagonCorner, HexagonCorner) GetCornersFromSide(HexagonSide side)
        {
            switch (side)
            {
                case HexagonSide.Front:
                    return (HexagonCorner.FrontA, HexagonCorner.FrontB);
                case HexagonSide.FrontRight:
                    return (HexagonCorner.FrontRightA, HexagonCorner.FrontRightB);
                case HexagonSide.BackRight:
                    return (HexagonCorner.BackRightA, HexagonCorner.BackRightB);
                case HexagonSide.Back:
                    return (HexagonCorner.BackA, HexagonCorner.BackB);
                case HexagonSide.BackLeft:
                    return (HexagonCorner.BackLeftA, HexagonCorner.BackLeftB);
                case HexagonSide.FrontLeft:
                    return (HexagonCorner.FrontLeftA, HexagonCorner.FrontLeftB);
                // Front
                default:
                    return (HexagonCorner.FrontA, HexagonCorner.FrontB);
            }
        }

        public static HexagonSide GetSideFromCorner(HexagonCorner corner)
        {
            if (corner == HexagonCorner.FrontA || corner == HexagonCorner.FrontB) return HexagonSide.Front;
            if (corner == HexagonCorner.FrontRightA || corner == HexagonCorner.FrontRightB) return HexagonSide.FrontRight;
            if (corner == HexagonCorner.BackRightA || corner == HexagonCorner.BackRightB) return HexagonSide.BackRight;

            if (corner == HexagonCorner.BackA || corner == HexagonCorner.BackB) return HexagonSide.Back;
            if (corner == HexagonCorner.BackLeftA || corner == HexagonCorner.BackLeftB) return HexagonSide.BackLeft;
            if (corner == HexagonCorner.FrontLeftA || corner == HexagonCorner.FrontLeftB) return HexagonSide.FrontLeft;

            return HexagonSide.Front;
        }

        // public static HexagonSide GetInvertedSide(HexagonSide side)
        // {
        //     switch (side)
        //     {
        //         // case HexagonSide.Front:
        //         //     return HexagonSide.Back;
        //         case HexagonSide.FrontRight:
        //             return HexagonSide.FrontLeft;
        //         case HexagonSide.BackLeft:
        //             return HexagonSide.BackRight;
        //         case HexagonSide.Back:
        //             return HexagonSide.Back;
        //         case HexagonSide.BackRight:
        //             return HexagonSide.BackLeft;
        //         case HexagonSide.FrontLeft:
        //             return HexagonSide.FrontRight;
        //         // Front
        //         default:
        //             return HexagonSide.Front;
        //     }
        // }

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

        public static bool IsEdgeCell(HexagonCell cell, bool isMultilayerCellGrid, bool assignToName)
        {
            if (cell == null) return false;

            int sideNeighborCount = cell.GetSideNeighborCount();
            int totalNeighborCount = cell._neighbors.Count;
            bool isEdge = false;

            if (sideNeighborCount < 6 || (isMultilayerCellGrid && totalNeighborCount < 7) || (cell.GetGridLayer() > 0 && cell.layeredNeighbor[0] != null && cell.layeredNeighbor[0]._neighbors.Count < 7))
            {
                cell.SetEdgeCell(true);
                isEdge = true;

                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += "_EDGE";
            }
            return isEdge;
        }

        public static List<HexagonCell> GetEdgeCells(List<HexagonCell> cells, bool assignToName = true)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();
            bool hasMultilayerCells = cells.Any(c => c.GetGridLayer() > 0);

            foreach (HexagonCell cell in cells)
            {
                if (IsEdgeCell(cell, hasMultilayerCells, assignToName))
                {
                    edgeCells.Add(cell);
                    cell.SetEdgeCell(true);
                }
                // int sideNeighborCount = cell.GetSideNeighborCount();
                // int neighborCount = cell._neighbors.Count;
                // if (neighborCount < 6 || (hasMultilayerCells && neighborCount < 7) || (cell.GetGridLayer() > 0 && cell.layeredNeighbor[0]._neighbors.Count < 7))
                // {
                //     edgeCells.Add(cell);
                //     cell.SetEdgeCell(true);
                // }
            }

            // Order edge cells by the fewest neighbors first
            return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
        }

        public static List<HexagonCell> GetLeveledEdgeCells(List<HexagonCell> layerCells, bool assign)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();

            foreach (HexagonCell cell in layerCells)
            {
                int neighborCount = cell._neighbors.FindAll(n => layerCells.Contains(n) && n.GetGridLayer() == cell.GetGridLayer()).Count;
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
            cells.AddRange(gridLayer == -1 ? edgeCells : edgeCells.FindAll(c => c.GetGridLayer() == gridLayer));

            ShuffleCells(cells);

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
                        if (cells[j] == cell || cells[j].GetGridLayer() != cell.GetGridLayer())
                            continue;

                        Vector3 distA = cell.transform.TransformPoint(cell.transform.position);
                        Vector3 distB = cell.transform.TransformPoint(cells[j].transform.position);

                        float distance = Vector3.Distance(distA, distB);
                        if (distance > cell.size * neighborSearchCenterDistMult) continue;

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
                HexagonCell newEndCell = endCell._neighbors.Find(n => n.GetGridLayer() == endCell.GetGridLayer() && !n.isEdgeCell && !n.isEntryCell);
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

        public static List<HexagonCell> ClearSoloPathCells(List<HexagonCell> pathCells)
        {
            foreach (HexagonCell cell in pathCells)
            {
                bool hasPathNeighbor = cell._neighbors.Any(c => c.isPathCell == true);
                if (hasPathNeighbor == false) cell.SetPathCell(false);
            }
            return pathCells.FindAll(c => c.isPathCell);
        }

        public static Dictionary<int, List<HexagonCell>> GenerateRandomCellPaths(List<HexagonCell> entryCells, HexagonCell topEdgeCell, Dictionary<int, List<HexagonCell>> allCellsByLayer, Vector3 position)
        {
            Dictionary<int, List<HexagonCell>> newCellPaths = new Dictionary<int, List<HexagonCell>>();

            HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(allCellsByLayer[0], position);

            newCellPaths.Add(0, HexagonCell.FindPath(entryCells[0], topEdgeCell ? topEdgeCell : centerCell, true));

            int paths = 0;
            for (int i = 1; i < entryCells.Count; i++)
            {
                paths++;
                newCellPaths.Add(paths, HexagonCell.FindPath(entryCells[i], centerCell, true));
                paths++;
                newCellPaths.Add(paths, HexagonCell.FindPath(entryCells[i], entryCells[i - 1], true));
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

                HexagonCell found = item._neighbors.Find(c => !c.isEntryCell && !c.isEdgeCell && !levelPathCells.Contains(c) && c.GetGridLayer() == item.GetGridLayer());
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

        // public static List<HexagonCell> GetLeveledCellsOfLayer(List<HexagonCell> alllevelCells, bool assign, int offsetNextLevelEdgeBy = 2)
        // {
        //     List<HexagonCell> result = new List<HexagonCell>();

        //     foreach (HexagonCell item in alllevelCells)
        //     {
        //         if (item.layeredNeighbor[0] != null && item.layeredNeighbor[0].isLeveledCell && item.layeredNeighbor[0].isLeveledEdge == false)
        //         {
        //             if (result.Contains(item) == false)
        //             {
        //                 result.Add(item);
        //             }
        //         }
        //     }

        //     //Offeset the next level edge by one 
        //     List<HexagonCell> toRemoveOffsetEdgeCells;
        //     for (int offset = 0; offset < offsetNextLevelEdgeBy; offset++)
        //     {
        //         toRemoveOffsetEdgeCells = HexagonCell.GetLeveledEdgeCells(result, false);
        //         result = result.Except(toRemoveOffsetEdgeCells).ToList();
        //     }

        //     if (assign)
        //     {
        //         List<HexagonCell> leveledEdgeCells = HexagonCell.GetLeveledEdgeCells(result, true);
        //         foreach (HexagonCell cell in result)
        //         {
        //             cell.SetLeveledCell(true);
        //         }

        //         HexagonCell.GetRandomLeveledRampCells(leveledEdgeCells, 4, true, true);
        //     }
        //     return result;
        // }

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
                        item.layeredNeighbor[1].SetLeveledGroundCell(true);
                        item.SetLeveledGroundCell(false);
                    }
                }

                lastLayerInnerCells = innerLeveledCells;

                // allLeveledCellsForAllLayers.Add(layer, leveledEdgeCells);
            }

            return allLeveledCellsForAllLayers;
        }

        // public static Dictionary<int, List<HexagonCell>> SetRandomLeveledCellsForAllLayers(Dictionary<int, List<HexagonCell>> allCellsByLayer, float maxRadius, int clumpSets = 5)
        // {
        //     Dictionary<int, List<HexagonCell>> allLeveledCellsForAllLayers = new Dictionary<int, List<HexagonCell>>();

        //     // Get first layer
        //     List<HexagonCell> result = GetRandomLeveledCells(allCellsByLayer[0], maxRadius, true, clumpSets);
        //     allLeveledCellsForAllLayers.Add(0, result);

        //     List<HexagonCell> leveledEdgeCells = HexagonCell.GetLeveledEdgeCells(result, true);
        //      HexagonCell.GetRandomLeveledRampCells(leveledEdgeCells, 5, true, true);

        //     foreach (var kvp in allCellsByLayer)
        //     {
        //         int layer = kvp.Key;

        //         // Skip level 0
        //         if (layer == 0) continue;

        //         List<HexagonCell> allLayerCells = kvp.Value;

        //         List<HexagonCell> levelCells = GetLeveledCellsOfLayer(allLayerCells, true);

        //         allLeveledCellsForAllLayers.Add(layer, levelCells);
        //     }
        //     return allLeveledCellsForAllLayers;
        // }

        public static List<HexagonCell> GetAvailableCellsForNextLayer(List<HexagonCell> allLayerCells)
        {
            List<HexagonCell> available = new List<HexagonCell>();

            foreach (HexagonCell currentCell in allLayerCells)
            {
                // if (!currentCell.isLeveledCell && currentCell.layeredNeighbor[0] != null && !currentCell.layeredNeighbor[0].isLeveledRampCell && currentCell.layeredNeighbor[0].isLeveledCell  )
                if (!currentCell.isLeveledCell && currentCell.layeredNeighbor[0] != null && !currentCell.layeredNeighbor[0].isLeveledRampCell)
                {
                    if (available.Contains(currentCell) == false) available.Add(currentCell);
                }
            }
            return available;
        }

        public static List<HexagonCell> GetRandomLeveledRampCells(List<HexagonCell> leveledEdgeCells, int num, bool assign, bool allowNeighbors = true)
        {
            List<HexagonCell> cells = new List<HexagonCell>();
            cells.AddRange(leveledEdgeCells);

            ShuffleCells(cells);

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


        public static Dictionary<int, List<HexagonCell>> OrganizeCellsByLevel(List<HexagonCell> cells)
        {
            Dictionary<int, List<HexagonCell>> cellsByLevel = new Dictionary<int, List<HexagonCell>>();

            foreach (HexagonCell cell in cells)
            {
                int level = cell.GetGridLayer();

                if (cellsByLevel.ContainsKey(level) == false)
                {
                    cellsByLevel.Add(level, new List<HexagonCell>());
                }

                cellsByLevel[level].Add(cell);
            }
            return cellsByLevel;
        }

        public static HexagonCell[,] CreateRectangularGrid(List<HexagonCell> cellGrid)
        {
            float hexagonSize = cellGrid[0].size;

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


        public static void MapHeightmapValues(float[,] heightmap, HexagonCell[,] cellGrid, bool assign)
        {
            //Check that the dimensions of the heightmap match the dimensions of the cell grid
            // if (heightmap.GetLength(0) != cellGrid.GetLength(0) || heightmap.GetLength(1) != cellGrid.GetLength(1))
            // {
            //     Debug.LogError("The dimensions of the heightmap do not match the dimensions of the cell grid.");
            //     return;
            // }

            //Iterate through each element of the cell grid
            for (int row = 0; row < cellGrid.GetLength(0); row++)
            {
                for (int col = 0; col < cellGrid.GetLength(1); col++)
                {
                    HexagonCell cell = cellGrid[row, col];

                    //Check if there is a HexagonCell at the current element
                    if (cell != null)
                    {
                        //Assign the value of the corresponding element in the heightmap to the HexagonCell's height property
                        // cellGrid[row, col].height = heightmap[row, col];
                        // Set cell to path if value within a set range
                        if (assign)
                        {
                            cell.road = (heightmap[row, col] <= 0.2f);

                            if (cell.isEdgeCell == false)
                            {
                                cell.SetPathCell(cell.road);
                            }
                        }
                        else
                        {
                            cell.road = !cell.isEdgeCell && (heightmap[row, col] <= 0.2f);
                        }


                    }
                }
            }
        }



        public static void SmoothElevation(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        {
            List<Vector3> modifiedVertices = new List<Vector3>();

            foreach (HexagonCell cell in pathCells)
            {
                foreach (int vertexIndex in cell._vertexIndices)
                {
                    TerrainVertex vertice = vertices[vertexIndex % vertices.GetLength(0), vertexIndex / vertices.GetLength(0)];
                    modifiedVertices.Add(new Vector3(vertice.position.x, cell.transform.position.y, vertice.position.z));
                }
            }

            int verticeIndex = 0;
            for (int x = 0; x < vertices.GetLength(0); x++)
            {
                for (int z = 0; z < vertices.GetLength(1); z++)
                {
                    vertices[x, z].position = modifiedVertices[verticeIndex++];
                }
            }
        }

        public static float SoftenSlope(float y1, float y2, float t)
        {
            float mu2 = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;
            return (y1 * (1f - mu2) + y2 * mu2);
        }


        public static List<TerrainVertex> GetVerticesBetweenPositions(List<TerrainVertex> vertexList, Vector2 minPos, Vector2 maxPos)
        {
            List<TerrainVertex> verticesBetweenPositions = new List<TerrainVertex>();
            Vector2 sortedMinPos = new Vector2(Mathf.Min(minPos.x, maxPos.x), Mathf.Min(minPos.y, maxPos.y));
            Vector2 sortedMaxPos = new Vector2(Mathf.Max(minPos.x, maxPos.x), Mathf.Max(minPos.y, maxPos.y));

            foreach (TerrainVertex vertex in vertexList)
            {
                Vector2 vertexPos = new Vector2(vertex.position.x, vertex.position.z);
                if (vertexPos.x >= sortedMinPos.x && vertexPos.x <= sortedMaxPos.x && vertexPos.y >= sortedMinPos.y && vertexPos.y <= sortedMaxPos.y)
                {
                    verticesBetweenPositions.Add(vertex);
                }
            }

            return verticesBetweenPositions;
        }


        public static void SlopeYPositionAlongPath(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        {
            foreach (HexagonCell cell in pathCells)
            {
                Vector2 cellPosXZ = new Vector2(cell.transform.position.x, cell.transform.position.z);
                // Get the vertices of the current cell
                List<TerrainVertex> currentCellVertexList = new List<TerrainVertex>();
                foreach (int vertexIndex in cell._vertexIndices)
                {
                    currentCellVertexList.Add(vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)]);
                }

                List<HexagonCell> pathNeighbors = cell.GetPathNeighbors();

                foreach (HexagonCell neighbor in pathNeighbors)
                {
                    float difference = Mathf.Abs(cell.transform.position.y - neighbor.transform.position.y);
                    if (difference < 0.5f) continue;

                    Vector2 neighborPosXZ = new Vector2(neighbor.transform.position.x, neighbor.transform.position.z);

                    List<TerrainVertex> neighborCellVertexList = new List<TerrainVertex>();
                    // Get the vertices of the neighbor cell
                    foreach (int vertexIndex in neighbor._vertexIndices)
                    {
                        neighborCellVertexList.Add(vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)]);
                    }

                    List<TerrainVertex> btwVertices = new List<TerrainVertex>();
                    btwVertices.AddRange(currentCellVertexList);
                    btwVertices.AddRange(neighborCellVertexList);
                    btwVertices = GetVerticesBetweenPositions(btwVertices, cellPosXZ, neighborPosXZ);

                    // Slope the y position of all vertices based on distance from closest position
                    Vector3 startPos = cell.transform.position;
                    Vector3 endPos = neighbor.transform.position;

                    float maxY = Mathf.Max(startPos.y, endPos.y);
                    float minY = Mathf.Min(startPos.y, endPos.y);

                    foreach (TerrainVertex vertex in btwVertices)
                    {
                        Vector2 vertexPosXZ = new Vector2(vertex.position.x, vertex.position.z);
                        float distanceFromStart = Vector3.Distance(vertexPosXZ, startPos);
                        float distanceFromEnd = Vector3.Distance(vertexPosXZ, endPos);

                        float closestDistance = Mathf.Min(distanceFromStart, distanceFromEnd);
                        float slope = Mathf.InverseLerp(0, closestDistance, minY);

                        Vector3 newPos = new Vector3(vertex.position.x, Mathf.Lerp(minY, maxY, slope), vertex.position.z);
                        vertices[vertex.index / vertices.GetLength(0), vertex.index % vertices.GetLength(0)].position = newPos;
                    }
                }
            }
        }

        public static void SmoothElevationAlongPath(List<TerrainVertex> vertexList, float maxHeightDifference, float radius, TerrainVertex[,] vertices)
        {
            // Create a list of vertices that need to be smoothed
            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();
            foreach (TerrainVertex vertex in vertexList)
            {
                float maxDifference = 0f;
                foreach (TerrainVertex neighbor in vertexList)
                {
                    if (vertex.position != neighbor.position)
                    {
                        float difference = Mathf.Abs(vertex.position.y - neighbor.position.y);
                        if (difference > maxDifference)
                        {
                            maxDifference = difference;
                        }
                    }
                }
                if (maxDifference > maxHeightDifference)
                {
                    verticesToSmooth.Add(vertex);
                }
            }

            // Smooth the elevation of the vertices within the radius
            foreach (TerrainVertex vertex in verticesToSmooth)
            {
                float lowestY = vertex.position.y;
                float highestY = vertex.position.y;
                foreach (TerrainVertex neighbor in vertexList)
                {
                    if (Vector3.Distance(vertex.position, neighbor.position) <= radius)
                    {
                        if (neighbor.position.y < lowestY)
                        {
                            lowestY = neighbor.position.y;
                        }
                        if (neighbor.position.y > highestY)
                        {
                            highestY = neighbor.position.y;
                        }
                    }
                }
                foreach (TerrainVertex neighbor in vertexList)
                {
                    if (Vector3.Distance(vertex.position, neighbor.position) <= radius)
                    {
                        float newY = Mathf.Lerp(lowestY, highestY, Vector3.Distance(neighbor.position, vertex.position) / radius);
                        vertices[neighbor.index / vertices.GetLength(0), neighbor.index % vertices.GetLength(0)].position = new Vector3(neighbor.position.x, newY, neighbor.position.z);
                    }
                }
            }
        }


        public static void SmoothElevationAlongPath(List<TerrainVertex> vertexList, float maxHeightDifference, TerrainVertex[,] vertices)
        {
            for (int i = 0; i < vertexList.Count - 1; i++)
            {
                TerrainVertex currentVertex = vertexList[i];
                TerrainVertex nextVertex = vertexList[i + 1];
                float heightDifference = nextVertex.position.y - currentVertex.position.y;

                if (Mathf.Abs(heightDifference) > maxHeightDifference)
                {
                    float step = heightDifference / (nextVertex.index - currentVertex.index);

                    for (int j = currentVertex.index + 1; j < nextVertex.index; j++)
                    {
                        if (j >= 0 && j < vertexList.Count)
                        {
                            Vector3 newPos = new Vector3(vertexList[j].position.x, vertexList[j].position.y, vertexList[j].position.z);
                            newPos.y += step * (j - currentVertex.index);
                            vertices[vertexList[j].index / vertices.GetLength(0), vertexList[j].index % vertices.GetLength(0)].position = newPos;
                        }
                    }
                }
            }
        }


        static float DistanceXZ(Vector3 a, Vector3 b)
        {
            Vector2 aXZ = new Vector2(a.x, a.z);
            Vector2 bXZ = new Vector2(b.x, b.z);
            return Vector2.Distance(a, b);
        }
        public static void SmoothElevationAlongPathNeighbors(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        {
            // Get the side vertices and neighbor relative side vertices 
            foreach (HexagonCell cell in pathCells)
            {
                for (int i = 0; i < cell.neighborsBySide.Length; i++)
                {
                    HexagonCell neighbor = cell.neighborsBySide[i];
                    if (neighbor == null) continue;
                    if (neighbor.isPathCell) continue;

                    int side = cell.GetNeighborsRelativeSide((HexagonSide)i);
                    Vector3 neighborSidePoint = neighbor._sides[side];

                    List<TerrainVertex> vertexList = new List<TerrainVertex>();
                    foreach (int vertexIndex in cell._vertexIndicesBySide[i])
                    {
                        vertexList.Add(vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)]);
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
                        vertices[vertexList[i].index / vertices.GetLength(0), vertexList[i].index % vertices.GetLength(0)].position = currVertex.position;
                    }
                }
            }
        }


        public static void SmoothVertexList(List<TerrainVertex> vertexList, TerrainVertex[,] vertices)
        {
            vertexList.Sort((v1, v2) =>
            {
                int result = v1.position.x.CompareTo(v2.position.x);
                if (result == 0)
                    result = v1.position.z.CompareTo(v2.position.z);
                return result;
            });

            for (int i = 1; i < vertexList.Count - 1; i++)
            {
                TerrainVertex prevVertex = vertexList[i - 1];
                TerrainVertex currVertex = vertexList[i];
                TerrainVertex nextVertex = vertexList[i + 1];
                Vector2 currentPosXZ = new Vector2(currVertex.position.x, currVertex.position.z);
                Vector2 prevPosXZ = new Vector2(prevVertex.position.x, prevVertex.position.z);
                Vector2 nextPosXZ = new Vector2(nextVertex.position.x, nextVertex.position.z);

                float slopeY = Mathf.Lerp(prevVertex.position.y, nextVertex.position.y, 0.03f);
                currVertex.position.y = slopeY;

                vertexList[i] = currVertex;
                vertices[vertexList[i].index / vertices.GetLength(0), vertexList[i].index % vertices.GetLength(0)].position = currVertex.position;
                vertices[vertexList[i].index / vertices.GetLength(0), vertexList[i].index % vertices.GetLength(0)].type = VertexType.Road;
            }
        }

        public static void SmoothElevationAlongPath(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        {
            List<TerrainVertex> vertexList = new List<TerrainVertex>();
            foreach (HexagonCell cell in pathCells)
            {
                foreach (int vertexIndex in cell._vertexIndices)
                {
                    vertexList.Add(vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)]);
                }
            }
            SmoothVertexList(vertexList, vertices);
        }


        // public static void SmoothElevationAlongPath(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        // {
        //     List<int> vertexIndices = new List<int>();
        //     List<TerrainVertex> vertexList = new List<TerrainVertex>();
        //     foreach (HexagonCell cell in pathCells)
        //     {
        //         foreach (int vertexIndex in cell._vertexIndices)
        //         {
        //             vertexIndices.Add(vertexIndex);
        //             vertexList.Add(vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)]);
        //         }
        //     }

        //     vertexList.Sort((v1, v2) => v1.position.x.CompareTo(v2.position.x));
        //     float minX = vertexList[0].position.x;
        //     float maxX = vertexList[vertexList.Count - 1].position.x;

        //     vertexList.Sort((v1, v2) => v1.position.z.CompareTo(v2.position.z));
        //     float minZ = vertexList[0].position.z;
        //     float maxZ = vertexList[vertexList.Count - 1].position.z;

        //     vertexList.Sort((v1, v2) => v1.position.y.CompareTo(v2.position.y));
        //     float minY = vertexList[0].position.y;
        //     float maxY = vertexList[vertexList.Count - 1].position.y;

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


        // public static void SmoothPathElevation(List<HexagonCell> pathCells, TerrainVertex[,] vertices)
        // {
        //     foreach (HexagonCell cell in pathCells)
        //     {
        //         float avgElevation = 0f;
        //         foreach (int vertexIndex in cell._vertexIndices)
        //         {
        //             avgElevation += vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)].position.y;
        //             // avgElevation += vertices[vertexIndex / vertices.GetLength(1), vertexIndex % vertices.GetLength(1)].position.y;
        //         }
        //         avgElevation /= cell._vertexIndices.Count;

        //         foreach (int vertexIndex in cell._vertexIndices)
        //         {
        //             vertices[vertexIndex / vertices.GetLength(0), vertexIndex % vertices.GetLength(0)].position.y = 2f;
        //             // vertices[vertexIndex / vertices.GetLength(1), vertexIndex % vertices.GetLength(1)].position.y = avgElevation;
        //         }
        //     }
        // }

        public static TerrainVertex[,] GenerateVertexGrid(Vector3 centerPosition, int areaSize, float scale, VertexType vertexType = VertexType.Generic)
        {
            // Calculate the half size of the grid
            float halfSize = areaSize / 2.0f;

            // Get the number of steps in each direction using the scale parameter
            int steps = Mathf.RoundToInt(areaSize / scale);
            float stepSize = areaSize / steps;

            // Create a 2D array to store the vertices
            TerrainVertex[,] grid = new TerrainVertex[steps + 1, steps + 1];
            int index = 0;

            for (int x = 0; x < steps + 1; x++)
            {
                for (int z = 0; z < steps + 1; z++)
                {
                    float xPos = centerPosition.x - halfSize + x * stepSize;
                    float zPos = centerPosition.z - halfSize + z * stepSize;
                    grid[x, z] = new TerrainVertex
                    {
                        position = new Vector3(xPos, centerPosition.y, zPos),
                        index = index,
                        type = VertexType.Generic
                    };
                    index++;
                }
            }
            return grid;
        }

        public static TerrainVertex[,] GenerateVertexGrid(Vector3 centerPosition, int areaSize, VertexType vertexType = VertexType.Generic)
        {
            TerrainVertex[,] grid = new TerrainVertex[areaSize, areaSize];
            int index = 0;

            float halfSize = areaSize / 2.0f;

            for (int x = 0; x < areaSize; x++)
            {
                for (int z = 0; z < areaSize; z++)
                {
                    grid[x, z] = new TerrainVertex
                    {
                        position = new Vector3(centerPosition.x - halfSize + x, centerPosition.y, centerPosition.z - halfSize + z),
                        index = index,
                        type = VertexType.Generic
                    };
                    index++;
                }
            }

            return grid;
        }


        // public static TerrainVertex[,] GenerateVertexGrid(Vector3 centerPosition, int radius, float stepSize = 3f)
        // {
        //     int size = (int)(radius / stepSize) * 2 + 1;
        //     TerrainVertex[,] grid = new TerrainVertex[size, size];
        //     int index = 0;
        //     for (float x = -radius; x <= radius; x += stepSize)
        //     {
        //         for (float z = -radius; z <= radius; z += stepSize)
        //         {
        //             Vector3 point = new Vector3(centerPosition.x + x, centerPosition.y, centerPosition.z + z);
        //             grid[index / size, index % size] = new TerrainVertex { position = point, index = index };
        //             index++;
        //         }
        //     }
        //     return grid;
        // }
        public static Vector3[,] GenerateVector3Grid(Vector3 centerPosition, int radius, float stepSize = 3f)
        {
            int size = (int)(radius / stepSize) * 2 + 1;
            Vector3[,] grid = new Vector3[size, size];
            int index = 0;
            for (float x = -radius; x <= radius; x += stepSize)
            {
                for (float z = -radius; z <= radius; z += stepSize)
                {
                    Vector3 point = new Vector3(centerPosition.x + x, centerPosition.y, centerPosition.z + z);
                    grid[index / size, index % size] = point;
                    index++;
                }
            }
            return grid;
        }

        public static void AssignVerticesToCells(TerrainVertex[,] vertices, List<HexagonCell> cells, float unassignedYOffset = -1f)
        {
            foreach (HexagonCell cell in cells)
            {
                cell._vertexIndices = new List<int>();
                cell._vertexIndicesBySide = new List<int>[cell._cornerPoints.Length];
                for (int i = 0; i < cell._vertexIndicesBySide.Length; i++)
                {
                    cell._vertexIndicesBySide[i] = new List<int>();
                }
            }

            int verticeIndex = 0;
            foreach (TerrainVertex vertice in vertices)
            {
                float closestDistance = float.MaxValue;
                HexagonCell closestCell = null;
                Vector2 vertexPosXZ = new Vector2(vertice.position.x, vertice.position.z);

                foreach (HexagonCell cell in cells)
                {
                    Vector2 currentPosXZ = new Vector2(cell.transform.position.x, cell.transform.position.z);

                    float distance = Vector2.Distance(vertexPosXZ, currentPosXZ);
                    if (distance < closestDistance)
                    {
                        Vector3[] cellCorners = cell._cornerPoints;
                        float xMin = cellCorners[0].x;
                        float xMax = cellCorners[0].x;
                        float zMin = cellCorners[0].z;
                        float zMax = cellCorners[0].z;
                        for (int i = 1; i < cellCorners.Length; i++)
                        {
                            if (cellCorners[i].x < xMin)
                                xMin = cellCorners[i].x;
                            if (cellCorners[i].x > xMax)
                                xMax = cellCorners[i].x;
                            if (cellCorners[i].z < zMin)
                                zMin = cellCorners[i].z;
                            if (cellCorners[i].z > zMax)
                                zMax = cellCorners[i].z;
                        }

                        if (vertice.position.x >= xMin && vertice.position.x <= xMax && vertice.position.z >= zMin && vertice.position.z <= zMax)
                        {
                            closestDistance = distance;
                            closestCell = cell;
                        }
                    }
                }

                int indexX = verticeIndex / vertices.GetLength(0);
                int indexZ = verticeIndex % vertices.GetLength(0);

                if (closestCell != null)
                {
                    closestCell._vertexIndices.Add(verticeIndex);

                    bool isCellCenterVertex = false;
                    if (closestDistance < closestCell.GetSize() * 0.5f)
                    {
                        isCellCenterVertex = true;
                    }
                    else
                    {
                        // Get Closest Corner if not within center radius   
                        (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = ProceduralTerrainUtility.
                                                            GetClosestPoint(closestCell._cornerPoints, vertexPosXZ);
                        if (nearestDistance != float.MaxValue)
                        {
                            HexagonSide side = GetSideFromCorner((HexagonCorner)nearestIndex);
                            closestCell._vertexIndicesBySide[(int)side].Add(verticeIndex);
                            vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].corner = nearestIndex;
                            vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].isCellCornerPoint = true;
                        }
                    }

                    float pathCellYOffset = 0f;
                    if (closestCell.isLeveledRampCell)
                    {
                        pathCellYOffset = 2f;
                    }
                    else if (closestCell.isPathCell || closestCell.isEntryCell)
                    {
                        pathCellYOffset = closestCell.GetGridLayer() == 0 ? 0.3f : -0.8f;
                    }
                    vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].position = new Vector3(vertice.position.x, closestCell.transform.position.y + pathCellYOffset, vertice.position.z);
                    vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].isCellCenterPoint = isCellCenterVertex;
                    vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].type = VertexType.Cell;
                }
                else
                {
                    vertices[verticeIndex / vertices.GetLength(0), verticeIndex % vertices.GetLength(0)].position = new Vector3(vertice.position.x, vertice.position.y + unassignedYOffset, vertice.position.z);
                }
                verticeIndex++;
            }
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

        public static void CreateMeshFromVertices(TerrainVertex[,] vertices, MeshFilter meshFilter)
        {
            List<int> triangles = new List<int>();
            List<Vector3> verticePositions = new List<Vector3>();

            int rowCount = vertices.GetLength(0);
            int columnCount = vertices.GetLength(1);

            for (int row = 0; row < rowCount - 1; row++)
            {
                for (int col = 0; col < columnCount - 1; col++)
                {
                    int bottomLeft = row * columnCount + col;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + columnCount;
                    int topRight = topLeft + 1;

                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);

                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    triangles.Add(topRight);
                }
            }

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertices[row, col].position);
                    // Vector3 worldPosition = meshFilter.transform.TransformPoint(vertices[row, col].position);
                    verticePositions.Add(worldPosition);
                }
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(verticePositions);
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

        // public static void CreateGridMesh(List<Vector3> vertices, MeshFilter meshFilter)
        // {
        //     List<int> triangles = new List<int>();

        //     for (int i = 0; i < vertices.Count; i++)
        //     {
        //         for (int j = i + 1; j < vertices.Count; j++)
        //         {
        //             if (Vector3.Distance(vertices[i], vertices[j]) <= 50f)
        //             {
        //                 triangles.Add(i);
        //                 triangles.Add(j);
        //                 triangles.Add(i);
        //             }
        //         }
        //     }

        //     // Set the mesh data
        //     Mesh mesh = new Mesh();
        //     mesh.Clear();
        //     mesh.SetVertices(vertices);
        //     mesh.SetTriangles(triangles, 0);
        //     mesh.RecalculateNormals();

        //     meshFilter.mesh = mesh;
        // }

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


        // TEMP
        public static void SetUpMicroClusterGrid(List<HexagonCell> allUnassisngedCells, int cellLayers, int cellLayerElevation = 4)
        {
            // Get random parentCell
            HexagonCell parentCell = allUnassisngedCells[UnityEngine.Random.Range(0, allUnassisngedCells.Count)];

            // Get child cells
            List<HexagonCell> childCells = GetChildrenForMicroClusterParent(parentCell);
            if (childCells == null || childCells.Count == 0)
            {
                Debug.LogError("No child Cells found");
                return;
            }

            Dictionary<int, List<HexagonTilePrototype>> prototypeGridByLayer = GenerateMicroClusterGridProtoypes(parentCell, childCells, cellLayers, cellLayerElevation);
        }

        public static List<HexagonCell> GetChildrenForMicroClusterParent(HexagonCell parentCell, int howMNanyDegreesFromDirectNeighbors = 1)
        {
            List<HexagonCell> children = new List<HexagonCell>();
            string parentCellId = parentCell.id;

            for (var side = 0; side < parentCell.neighborsBySide.Length; side++)
            {
                HexagonCell neighbor = parentCell.neighborsBySide[side];

                if (neighbor == null) continue;

                // Check if direct neighbor is available
                if (neighbor.IsAssigned() == false)
                {
                    neighbor.SetClusterCellParentId(parentCellId);
                    children.Add(neighbor);
                }
                else
                {
                    // Check if neighbor above is available
                    HexagonCell offNeighborTop = neighbor.layeredNeighbor[1];
                    if (offNeighborTop != null && offNeighborTop.IsAssigned() == false)
                    {
                        offNeighborTop.SetClusterCellParentId(parentCellId);
                        children.Add(offNeighborTop);
                    }
                }

                // Check if 2nd degree neighbor is available
                HexagonCell offNeighbor = neighbor.neighborsBySide[side];
                if (offNeighbor != null && offNeighbor.IsAssigned() == false && offNeighbor.layeredNeighbor[1] != null && offNeighbor.layeredNeighbor[1].IsAssigned() == false)
                {
                    offNeighbor.SetClusterCellParentId(parentCellId);
                    children.Add(offNeighbor);
                    // // Check if neighbor above is available
                    // if (offNeighbor.layeredNeighbor[1] != null) {
                    //     children.Add(offNeighbor.layeredNeighbor[1]);
                    // }
                }
            }

            return children;
        }

        public static Dictionary<int, List<HexagonTilePrototype>> GenerateMicroClusterGridProtoypes(HexagonCell parentCell, List<HexagonCell> childCells, int cellLayers, int cellLayerElevation = 4)
        {
            Dictionary<int, List<HexagonTilePrototype>> newPrototypesByLayer = null;
            Vector2 gridGenerationCenterPosXZOffeset = new Vector2(-1.18f, 0.35f);

            List<HexagonCell> allHostCells = new List<HexagonCell>();
            allHostCells.Add(parentCell);
            allHostCells.AddRange(childCells);

            foreach (HexagonCell hostCell in allHostCells)
            {
                Vector3 center = hostCell.transform.position;
                int layerBaseOffset = 0;

                if (hostCell.GetGridLayer() != parentCell.GetGridLayer())
                {
                    int layerDifference = hostCell.GetGridLayer() - parentCell.GetGridLayer();
                    layerBaseOffset = 1 * layerDifference;

                    center.y = parentCell.transform.position.y + (cellLayerElevation * layerDifference);
                }

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonTilePrototype>> prototypesByLayer = HexagonCellManager.Generate_HexagonCellPrototypes(center, 12, 4, cellLayers, cellLayerElevation, gridGenerationCenterPosXZOffeset, layerBaseOffset);

                if (newPrototypesByLayer == null)
                {
                    newPrototypesByLayer = prototypesByLayer;
                }
                else
                {
                    foreach (var kvp in prototypesByLayer)
                    {
                        int key = kvp.Key;
                        List<HexagonTilePrototype> prototypes = kvp.Value;

                        if (newPrototypesByLayer.ContainsKey(key) == false)
                        {
                            newPrototypesByLayer.Add(key, prototypes);
                        }
                        else
                        {
                            newPrototypesByLayer[key].AddRange(prototypes);
                        }
                    }
                }
            }

            return newPrototypesByLayer;
        }



        [System.Serializable]
        public struct NeighborSideCornerSockets
        {
            public int[] topCorners;
            public int[] bottomCorners;
        }

        [System.Serializable]
        public struct NeighborLayerCornerSockets
        {
            public int[] corners;
        }
    }
}