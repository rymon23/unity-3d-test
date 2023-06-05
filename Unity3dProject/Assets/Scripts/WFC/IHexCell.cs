using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WFCSystem
{
    public enum HexCellSizes { Default = 12, X_12 = 12, X_4 = 4, X_36 = 36, X_108 = 108 }
    public interface IHexCell
    {
        public string GetId();
        public string Get_Uid();
        public int GetSize();
        public EdgeCellType GetEdgeCellType();
        public Vector3 GetPosition();
        public Vector3[] GetCorners();
        public Vector3[] GetSides();
        public int GetLayer();
        public string GetLayerStackId();
        public Vector2 GetLookup();
        public Vector2 GetParentLookup();
        public Vector2 GetWorldSpaceLookup();
        public bool IsEdge();
        public bool IsEdgeOfParent();
        public bool IsOriginalGridEdge();
        public bool IsEntry();
        public bool IsPath();
        public bool IsGround();
        public bool IsGenericGround();
        public bool IsFlatGround();
        public void SetToGround(bool isFlatGround);
        public bool IsUnderGround();
        public bool IsUnderWater();
        public bool IsGridHost();
        public void SetGridHost(bool enable);
        public CellStatus GetCellStatus();
        public void SetCellStatus(CellStatus status);

        public bool IsInCluster();
        public bool IsAssigned();
        public bool IsDisposable();

        public bool HasTopNeighbor();
        public bool HasBottomNeighbor();

        public IHexagonTile GetTile();
        public HexagonTileCore GetCurrentTile();
        public void SetTile(HexagonTileCore newTile, int rotation, bool inverted = false);
        public int GetTileRotation();
        public bool IsTileInverted();

        public void Highlight(bool enable);
        public void SetIgnore(bool enable);
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