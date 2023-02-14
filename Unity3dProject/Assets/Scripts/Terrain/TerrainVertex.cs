using System.Collections;
using UnityEngine;

namespace ProceduralBase
{
    public enum VertexType { Unset = 0, Road = 1, Cell = 2, Border = 3, Generic = 4 }

    [System.Serializable]
    public struct TerrainVertex
    {
        public int index;
        public Vector3 position;
        public VertexType type;
        public bool isCellCenterPoint;
        public bool isCellCornerPoint;
        public int corner;
    }
}