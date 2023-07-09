using System;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    public interface IHexagonTile
    {
        public HexCellSizes GetSize();
        public TileContext GetTileContext();
        public int[] GetRotatedLayerCornerSockets(bool top, int rotation, bool inverted = false);
        public int GetRotatedSideCornerSocketId(HexagonCorner corner, int rotation, bool top, bool inverted = false);
        public void SetCornerSocketSetIds(CornerSocketSetType socketSetType, int[] _newCornerSocketIds);
    }
}