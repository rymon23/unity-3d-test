using System.Collections.Generic;
using UnityEngine;
using WFCSystem;
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

    [SerializeField] private bool enableinternakCellTracker;
    [SerializeField] private HexCellSizes debug_currentCellSize = HexCellSizes.Default;
    [SerializeField] private bool enableChildPoints;
    [SerializeField] private bool enableNeighborPoints;
    [SerializeField] private bool debug_sideNeighborPoint;

    [SerializeField] private int debug_currentCellHeight = 2;
    [SerializeField] private float debug_centerRadiusMult = 0.5f;
    [SerializeField] private HexagonSide debug_sideNeighbor;
    private HexagonCellPrototype debug_currentHexCell;
    private List<Vector3> _children = new List<Vector3>();
    private List<Vector3> _neighbors = new List<Vector3>();
    private List<HexagonCellPrototype> _childrenHX = new List<HexagonCellPrototype>();
    private List<HexagonCellPrototype> _neighborsHX = new List<HexagonCellPrototype>();
    private Vector3 _sideCenter;

    private Vector3 debug_lastPosition;
    private float debug_updateDistanceMult = 0.3f;

    private HexCellSizes _currentCellSize_;
    private HexagonSide _currentSide_;
    private int _currentCellHeight_;

    public void Debug_EvaluatePosition()
    {
        if (
            debug_currentHexCell == null ||
            _currentCellSize_ != debug_currentCellSize ||
            _currentCellHeight_ != debug_currentCellHeight ||
            _currentSide_ != debug_sideNeighbor ||

            Vector3.Distance(debug_lastPosition, transform.position) > ((int)debug_currentCellSize * debug_updateDistanceMult)
        )
        {
            _currentCellSize_ = debug_currentCellSize;
            _currentCellHeight_ = debug_currentCellHeight;
            _currentSide_ = debug_sideNeighbor;

            debug_lastPosition = transform.position;

            debug_currentHexCell = Generate_NearestHexCell(transform.position, debug_currentCellSize, debug_currentCellHeight);

            if (debug_sideNeighborPoint)
            {
                _sideCenter = HexCoreUtil.Generate_HexNeighborCenterOnSide(debug_currentHexCell.center, (int)debug_currentCellSize, debug_sideNeighbor);
            }

            if (enableNeighborPoints)
            {
                _neighbors = HexCoreUtil.GenerateHexCenterPoints_X12(debug_currentHexCell.center, (int)debug_currentCellSize);
                _neighborsHX = new List<HexagonCellPrototype>();
                foreach (var item in _neighbors)
                {
                    _neighborsHX.Add(Generate_HexCellAtPosition(item, debug_currentCellSize, debug_currentCellHeight));
                }
            }

            if (enableChildPoints)
            {
                int size = (int)debug_currentCellSize / 3;
                _children = HexCoreUtil.GenerateHexCenterPoints_X19(debug_currentHexCell.center, size);
                // _children = HexCoreUtil.GenerateHexCenterPoints_X7(debug_currentHexCell.center, size);
                _childrenHX = new List<HexagonCellPrototype>();
                foreach (var item in _children)
                {
                    _childrenHX.Add(Generate_HexCellAtPosition(item, (HexCellSizes)size, debug_currentCellHeight));
                }

            }
        }
    }

    public static HexagonCellPrototype Generate_HexCellAtPosition(Vector3 position, HexCellSizes hexSize, int layerHeight)
    {
        Vector3 hexCenter = position;
        hexCenter.y = (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerHeight);
        return new HexagonCellPrototype(hexCenter, (int)hexSize, false);
    }

    public static HexagonCellPrototype Generate_NearestHexCell(Vector3 position, HexCellSizes hexSize, int layerHeight)
    {
        Vector3 hexCenter = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, (int)hexSize);
        hexCenter.y = (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerHeight);
        return new HexagonCellPrototype(hexCenter, (int)hexSize, false);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);

        if (enableinternakCellTracker)
        {
            Debug_EvaluatePosition();

            if (debug_currentHexCell != null)
            {
                Gizmos.color = Color.red;
                VectorUtil.DrawHexagonPointLinesInGizmos(debug_currentHexCell.cornerPoints);
            }

            float size;

            if (enableChildPoints && _childrenHX != null)
            {
                size = ((int)_currentCellSize_ * debug_centerRadiusMult) / 3;
                Gizmos.color = Color.yellow;

                foreach (var item in _childrenHX)
                {
                    Gizmos.DrawSphere(item.center, size);
                    VectorUtil.DrawHexagonPointLinesInGizmos(item.cornerPoints);
                }
            }

            size = ((int)_currentCellSize_ * debug_centerRadiusMult);

            if (debug_sideNeighborPoint && _sideCenter != null)
            {
                Gizmos.color = Color.green;
                // Gizmos.DrawSphere(_sideCenter, size);
                VectorUtil.DrawHexagonPointLinesInGizmos(
                    Generate_NearestHexCell(_sideCenter, debug_currentCellSize, debug_currentCellHeight).cornerPoints
                    );

            }
            else
            {

                if (enableNeighborPoints && _neighborsHX != null)
                {
                    Gizmos.color = Color.green;

                    foreach (var item in _neighborsHX)
                    {
                        // Gizmos.DrawSphere(item, size);
                        VectorUtil.DrawHexagonPointLinesInGizmos(item.cornerPoints);
                    }
                }
            }

        }
    }
}
