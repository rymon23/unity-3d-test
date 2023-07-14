using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

[System.Serializable]
public class BuildingGenerator : MonoBehaviour
{
    [Range(12, 128)][SerializeField] private float boundsSize = 25;
    [Range(0.25f, 20f)][SerializeField] private float baseCellSize = 1f;
    SurfaceBlock[,,] surfaceBlocksGrid;
    // [SerializeField] private float width = 4;
    // [SerializeField] private float depth = 1;
    // [SerializeField] private float height = 3;

    private void OnValidate()
    {
        if (
             _lastPosition != transform.position
            || _boundsSize != boundsSize
            || _baseCellSize != baseCellSize
            // || _cellLayerOffset != cellLayerOffset
            // || _centerPosYOffset != centerPosYOffset
            )
        {
            surfaceBlocksGrid = null;

            _lastPosition = transform.position;

            boundsSize = UtilityHelpers.RoundToNearestStep(boundsSize, 2f);
            _boundsSize = boundsSize;

            baseCellSize = UtilityHelpers.RoundToNearestStep(baseCellSize, 0.25f);
            _baseCellSize = baseCellSize;
        }
    }

    #region Saved State
    Vector3 _lastPosition;
    float _boundsSize;
    float _baseCellSize;

    #endregion


    private void OnDrawGizmos()
    {
        // Gizmos.color = Color.white;
        // Vector3[] cubePTs = VectorUtil.CreateCube(transform.position, 2f);
        // foreach (var item in cubePTs)
        // {
        //     Gizmos.DrawSphere(item, 0.2f);
        // }
        // Gizmos.DrawSphere(cubePTs[0], 0.2f);

        // Vector3[] corners = VectorUtil.CreateCube(position, size);
        // VectorUtil.DrawGridPointsGizmos(points);

        // Vector3[] neighborCenters = GenerateNeighborCenters(transform.position, 2);
        // foreach (var item in neighborCenters)
        // {
        //     Gizmos.DrawSphere(item, 0.2f);
        // }
        // Gizmos.DrawSphere(neighborCenters[(int)SurfaceBlockSide.Top], 0.2f);

        if (surfaceBlocksGrid == null)
        {
            // Vector3[,,] points = Generate3DGrid(transform.position, 5, 5, 5, 1);
            // Vector3[,,] points = VectorUtil.Generate3DGrid(transform.position, 5, 5, 5, 1);

            Vector3[] boundsBlock = SurfaceBlock.CreateCorners(transform.position, boundsSize);
            Bounds totalBounds = VectorUtil.CalculateBounds_V2(boundsBlock.ToList());
            (
                Vector3[,,] points,
                float spacing
            ) = VectorUtil.Generate3DGrid(totalBounds, baseCellSize, transform.position.y, boundsSize);

            List<Bounds> structureBounds = new List<Bounds>() { totalBounds };
            surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);
        }
        else
        {
            SurfaceBlock.DrawGrid(surfaceBlocksGrid);
        }

        // (Vector3[] allPoints, Vector3[] archPoints, Vector3[] doorwayPoints) = GenerateArchAndDoorwayPoints(width, height, depth, resolution, transform);
        // // (Vector3[] allPoints, Vector3[] archBottomPoints) = GenerateArchPoints(width, height, depth);
        // // points = allPoints;
        // Gizmos.color = Color.grey;

        // // Draw lines between the points
        // for (int i = 0; i < archPoints.Length - 1; i++)
        // {
        //     Gizmos.DrawLine(archPoints[i], archPoints[i + 1]);
        // }
        // // for (int i = 0; i < archBottomPoints.Length; i++)
        // // {
        // //     Gizmos.color = Color.red;
        // //     Gizmos.DrawSphere(archBottomPoints[i], 0.2f);
        // // }
        // // Gizmos.color = Color.blue;
    }


    public static Vector3[] GenerateDoorwayPoints(
        float width = 4,
        float depth = 1,
        float height = 3,
        int resolution = 12
    )
    {
        // Create an empty array to store the points
        Vector3[] points = new Vector3[(resolution + 1) * 2];
        // Generate the points of the arch
        int i = 0;
        for (float u = 0f; u <= 1f; u += 1f / resolution)
        {
            float x = width * (1 - u);
            float y = height * (0.5f + 0.5f * Mathf.Sin(Mathf.PI * u));
            float z = depth * 0.5f;
            points[i++] = new Vector3(x, y, z);
        }

        // Generate the points of the bottom of the arch
        for (float u = 0f; u <= 1f; u += 1f / resolution)
        {
            float x = width * (1 - u);
            float y = 0;
            float z = depth * (0.5f - 0.5f * u);
            points[i++] = new Vector3(x, y, z);
        }
        return points;
    }

    public static (Vector3[], Vector3[]) GenerateArchPoints(float width, float height, float depth, int resolution)
    {
        // Create an empty array to store the points
        Vector3[] points = new Vector3[resolution + 1];

        // Create an empty array to store the bottom 2 points of the arch
        Vector3[] bottomPoints = new Vector3[2];

        // Generate the points of the arch
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float x = width * t;
            float y = height * Mathf.Sin(Mathf.PI * t);
            float z = depth * Mathf.Cos(Mathf.PI * t);
            points[i] = new Vector3(x, y, z);

            // Store the bottom 2 points of the arch
            if (i == 0 || i == resolution)
            {
                if (i == 0)
                {
                    bottomPoints[0] = new Vector3(x, 0, z);
                }
                else
                {
                    bottomPoints[1] = new Vector3(x, 0, z);

                }

            }
        }

        return (points, bottomPoints);
    }

    public static Vector3[] GenerateDoorwayPoints(Vector3[] archBottom, float width, float height)
    {
        Vector3[] doorwayPoints = new Vector3[4];
        Vector3 topLeft = archBottom[0];
        Vector3 topRight = archBottom[1];
        Vector3 bottomLeft = new Vector3(topLeft.x, topLeft.y - height, topLeft.z);
        Vector3 bottomRight = new Vector3(topRight.x, topRight.y - height, topRight.z);
        // Vector3 bottomRight = archBottom[0];
        // Vector3 bottomRight = archBottom[1];

        doorwayPoints[0] = topRight;
        doorwayPoints[1] = topLeft;
        doorwayPoints[2] = bottomLeft;
        doorwayPoints[3] = bottomRight;

        return doorwayPoints;
    }

    public static (Vector3[], Vector3[], Vector3[]) GenerateArchAndDoorwayPoints(float width, float height, float depth, int resolution, Transform transform)
    {
        // Generate the points of the arch and doorway
        (Vector3[] archPoints, Vector3[] archBottom) = GenerateArchPoints(width, height, depth, resolution);
        Vector3[] doorwayPoints = GenerateDoorwayPoints(archBottom, width, height);

        // Determine the rotation that aligns the shape's front with the game object's forward direction
        Quaternion rotation = Quaternion.LookRotation(transform.forward) * Quaternion.AngleAxis(90, Vector3.up);


        Vector3 difference = transform.position - archPoints[archPoints.Length - 1];

        // Apply the rotation to each point in the arrays
        for (int i = 0; i < archPoints.Length; i++)
        {
            archPoints[i] = rotation * archPoints[i];
            archPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        difference = transform.position - doorwayPoints[doorwayPoints.Length - 1];
        for (int i = 0; i < doorwayPoints.Length; i++)
        {
            doorwayPoints[i] = rotation * doorwayPoints[i];
            doorwayPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        // Merge the archPoints and doorwayPoints array
        Vector3[] points = new Vector3[archPoints.Length + doorwayPoints.Length];
        archPoints.CopyTo(points, 0);
        doorwayPoints.CopyTo(points, archPoints.Length);

        return (points, archPoints, doorwayPoints);
    }


    public static List<Vector3> GenerateVerticalGrid(Vector3 origin, float gridSize, int numRows, int numColumns)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        for (int row = 0; row < numRows; row++)
        {
            for (int column = 0; column < numColumns; column++)
            {
                float x = origin.x + column * gridSize;
                float y = origin.y;
                float z = origin.z + row * gridSize;

                Vector3 point = new Vector3(x, y, z);
                gridPoints.Add(point);
            }
        }
        return gridPoints;
    }

}
