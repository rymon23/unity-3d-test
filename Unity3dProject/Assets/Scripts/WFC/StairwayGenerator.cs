using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

[System.Serializable]
public class StairwayGenerator : MonoBehaviour
{

    [SerializeField] private bool show_rampwayBounds;
    [SerializeField] private bool show_ramp;
    [Header(" ")]
    [SerializeField] private bool resetPrototypes;
    [Header(" ")]
    [SerializeField] private bool generate_mesh;
    [Header(" ")]
    public Vector3 rampway_dimensions = new Vector3(3f, 2f, 2f);

    [Header("Ramp ")]
    public Vector4 ramp_dimensions = new Vector4(3f, 0.6f, 1.8f, 1f);
    [SerializeField] private bool highlight_rampSide;
    [SerializeField] private RampSide stairSide;
    [Range(0, 6)][SerializeField] int ramp_rotation = 0;
    [SerializeField] private RotationType rotationType;
    [Header(" ")]
    [SerializeField] private bool enable_neighborBlock;
    [SerializeField] private RampTopSide neighborBlockSide;

    RectangleBounds stairWayBounds = null;
    StairPrototype stairPrototype = null;
    RectangleBounds neighborBlock = null;

    [Header(" ")]
    [SerializeField] private GameObject prefab;
    [Header(" ")]

    #region Saved State
    Vector3 gridStartPos;
    Vector3 _lastPosition;
    Vector3 _rampway_dimensions;
    Vector4 _ramp_dimensions;
    float _boundsSize;
    float _blockSize;
    float _updateDist = 1f;
    int _ramp_rotation;
    #endregion


    private void OnValidate()
    {
        if (
            resetPrototypes
            || _lastPosition != transform.position
            || _ramp_rotation != ramp_rotation
            || _ramp_dimensions != ramp_dimensions
            || _rampway_dimensions != rampway_dimensions

        // || _boundsSize != boundsSize
        // || _blockSize != blockSize
        )
        {
            resetPrototypes = false;

            stairWayBounds = null;
            stairPrototype = null;
            neighborBlock = null;

            _lastPosition = transform.position;
            _ramp_rotation = ramp_rotation;

            // boundsSize = UtilityHelpers.RoundToNearestStep(boundsSize, 2f);
            // _boundsSize = boundsSize;

            // blockSize = UtilityHelpers.RoundToNearestStep(blockSize, 0.2f);
            // _blockSize = blockSize;
        }

        if (generate_mesh)
        {
            generate_mesh = false;

            if (stairPrototype != null) stairPrototype.Generate_MeshObject(prefab, transform);
        }
    }

    public void ResetPrototypes()
    {
        resetPrototypes = true;
        OnValidate();
    }



    Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
    private void OnDrawGizmos()
    {
        if (_lastPosition != transform.position)
        {
            if (Vector3.Distance(_lastPosition, transform.position) > _updateDist) ResetPrototypes();
        }

        if (stairPrototype == null)
        {
            // RampPrototype new_ramp = new RampPrototype(transform.position, ramp_rotation, ramp_dimensions, rotationType);
            // new_ramp.Draw();
            // stairPrototype = new StairPrototype(new_ramp);

            stairWayBounds = new RectangleBounds(transform.position, 2, ramp_rotation, rampway_dimensions, rotationType);
            RampPrototype new_ramp = new RampPrototype(stairWayBounds, ramp_rotation, ramp_dimensions, rotationType);
            stairPrototype = new StairPrototype(new_ramp);

            if (enable_neighborBlock)
            {
                neighborBlock = stairPrototype.rampPrototype.Generate_NeighborBlock(neighborBlockSide);
            }
        }
        else
        {
            if (show_ramp)
            {
                stairPrototype.DrawBounds();

                if (highlight_rampSide) stairPrototype.DrawSide(stairSide);

                if (neighborBlock != null)
                {
                    neighborBlock.Draw();
                }

            }
        }

        if (stairWayBounds == null)
        {
            stairWayBounds = new RectangleBounds(transform.position, 2, ramp_rotation, rampway_dimensions);
        }
        else
        {
            if (show_rampwayBounds)
            {

                Gizmos.color = Color.red;
                stairWayBounds.DrawFace();

                Gizmos.color = Color.cyan;
                stairWayBounds.Draw();
            }
        }
    }

}
