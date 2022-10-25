using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public struct WaypointData
    {
        public WaypointData(int _index, Transform _transform)
        {
            index = _index;
            transform = _transform;
        }

        public int index { get; }

        public Transform transform { get; set; }
    }
}
