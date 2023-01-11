using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    public class SubZone : MonoBehaviour
    {
        [SerializeField] private Location locationParent;
        [SerializeField] private float radius;
        [SerializeField] private List<int> neighbors;
        [SerializeField] private List<HexagonTile> tiles;
        [SerializeField] private Vector3[] borderCorners;
        [SerializeField] private Vector3[] zoneConnectors;
    }

    [System.Serializable]
    public struct SubzonePrototype
    {
        public Vector3 position;
        public float radius;
        public Vector3[] borderCorners;
        public List<HexagonTilePrototype> cells;
    }

    [System.Serializable]
    public struct ZoneConnector
    {
        public Vector3 position;
        public Vector3[] zones;
    }
}