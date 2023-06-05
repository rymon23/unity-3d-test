using System.Collections.Generic;
using UnityEngine;

public class WorldPositionTracker : MonoBehaviour
{
    [SerializeField] private Vector2 _currentWorldPos_WorldspaceLookup = Vector2.zero;
    [SerializeField] private Vector2 _currentWorldPos_AreaLookup = Vector2.zero;
    [SerializeField] private Vector2 _currentWorldPos_RegionLookup = Vector2.zero;
    [Header(" ")]
    public List<Vector2> _active_worldspaceLookups = new List<Vector2>();
    public List<Vector2> _active_areaLookups = new List<Vector2>();

    public void UpdatePositionData(
        Vector2 worldspaceLookup,
        Vector2 areaLookup,
        Vector2 regionLookup
    )
    {
        _currentWorldPos_WorldspaceLookup = worldspaceLookup;
        _currentWorldPos_AreaLookup = areaLookup;
        _currentWorldPos_RegionLookup = regionLookup;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
