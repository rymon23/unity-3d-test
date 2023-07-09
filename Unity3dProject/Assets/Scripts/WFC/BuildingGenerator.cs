using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralBase;
using WFCSystem;

[System.Serializable]
public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private Vector3[] points;
    // [SerializeField] private Vector3[] doorwayPoints;
    [SerializeField] private float width = 4;

    [SerializeField] private float depth = 1;

    [SerializeField] private float height = 3;
    [SerializeField] private int resolution = 12;
    [SerializeField] private int sideFeaturePoints = 4;
    private void Start()
    {
        // GenerateBuildingPoints();
    }

    private void OnValidate()
    {
        // GenerateBuildingPoints();

        // doorwayPoints = GenerateDoorwayPoints();
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

    // private void OnDrawGizmos()
    // {
    //     if (doorwayPoints == null) return;

    //     for (int i = 0; i < doorwayPoints.Length; i++)
    //     {
    //         // Draw a sphere at each point
    //         Gizmos.DrawSphere(doorwayPoints[i], 0.1f);

    //         // Draw lines connecting neighboring doorwayPoints
    //         if (i < doorwayPoints.Length - 1)
    //         {
    //             Gizmos.DrawLine(doorwayPoints[i], doorwayPoints[i + 1]);
    //         }
    //         else if (i == doorwayPoints.Length - 1)
    //         {
    //             Gizmos.DrawLine(doorwayPoints[i], doorwayPoints[0]);
    //         }
    //     }
    // }

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


    // public (Vector3[], Vector3[]) GenerateArchAndDoorwayPoints(float width, float height, float depth)
    // {
    //     // Generate the points of the arch and bottom points
    //     (Vector3[] archPoints, Vector3[] archBottomPoints) = GenerateArchPoints(width, height, depth);
    //     // Generate the points of the doorway
    //     Vector3[] doorwayPoints = GenerateDoorwayPoints(archBottomPoints, width, height);
    //     // Rotate the doorway points so that it is front-facing on the gameobject transform
    //     Vector3 forward = transform.forward;
    //     Quaternion rotation = Quaternion.LookRotation(forward);
    //     for (int i = 0; i < doorwayPoints.Length; i++)
    //     {
    //         doorwayPoints[i] = rotation * doorwayPoints[i];
    //     }
    //     return (archPoints, doorwayPoints);
    // }


    public void GenerateBuildingPoints()
    {
        // Create an empty array to store the points
        points = new Vector3[8 + (sideFeaturePoints * 6)];

        // Generate the points for the base of the building
        points[0] = new Vector3(-width / 2, 0, depth / 2);
        points[1] = new Vector3(width / 2, 0, depth / 2);
        points[2] = new Vector3(width / 2, 0, -depth / 2);
        points[3] = new Vector3(-width / 2, 0, -depth / 2);

        // Generate the points for the top of the building
        points[4] = new Vector3(-width / 2, height, depth / 2);
        points[5] = new Vector3(width / 2, height, depth / 2);
        points[6] = new Vector3(width / 2, height, -depth / 2);
        points[7] = new Vector3(-width / 2, height, -depth / 2);

        // Generate the points for the side features
        for (int i = 0; i < sideFeaturePoints; i++)
        {
            float x = -width / 2 + (i + 1) * (width / (sideFeaturePoints + 1));
            points[8 + (i * 2)] = new Vector3(x, 0, depth / 2);
            points[8 + (i * 2) + 1] = new Vector3(x, height, depth / 2);
        }
    }


    SurfaceBlock[,,] surfaceBlocksGrid;

    private void OnDrawGizmos()
    {

        // List<Vector3> points = GenerateVerticalGrid(transform.position, 5, 5, 5);
        // foreach (var pt in points)
        // {
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawSphere(pt, 0.3f);


        //     Gizmos.color = Color.white;
        //     Vector3[] cubePTs = VectorUtil.CreateCube(pt, 5f);
        //     foreach (var item in cubePTs)
        //     {
        //         Gizmos.DrawSphere(item, 0.2f);
        //     }
        // }

        // Gizmos.color = Color.white;
        // Vector3[] cubePTs = VectorUtil.CreateCube(transform.position, 2f);
        // foreach (var item in cubePTs)
        // {
        //     Gizmos.DrawSphere(item, 0.2f);
        // }
        // Gizmos.DrawSphere(cubePTs[0], 0.2f);

        //Front
        // Gizmos.DrawSphere(cubePTs[1], 0.2f);
        // Gizmos.DrawSphere(cubePTs[2], 0.2f);
        // Gizmos.DrawSphere(cubePTs[5], 0.2f);
        // Gizmos.DrawSphere(cubePTs[6], 0.2f);

        //Back
        // Gizmos.DrawSphere(cubePTs[0], 0.2f);
        // Gizmos.DrawSphere(cubePTs[3], 0.2f);
        // Gizmos.DrawSphere(cubePTs[4], 0.2f);
        // Gizmos.DrawSphere(cubePTs[7], 0.2f);

        //Bottom
        // Gizmos.DrawSphere(cubePTs[0], 0.2f);
        // Gizmos.DrawSphere(cubePTs[1], 0.2f);
        // Gizmos.DrawSphere(cubePTs[2], 0.2f);
        // Gizmos.DrawSphere(cubePTs[3], 0.2f);

        //Top
        // Gizmos.DrawSphere(cubePTs[4], 0.2f);
        // Gizmos.DrawSphere(cubePTs[5], 0.2f);
        // Gizmos.DrawSphere(cubePTs[6], 0.2f);
        // Gizmos.DrawSphere(cubePTs[7], 0.2f);

        //Right
        // Gizmos.DrawSphere(cubePTs[0], 0.2f);
        // Gizmos.DrawSphere(cubePTs[1], 0.2f);
        // Gizmos.DrawSphere(cubePTs[4], 0.2f);
        // Gizmos.DrawSphere(cubePTs[5], 0.2f);

        //Left
        // Gizmos.DrawSphere(cubePTs[2], 0.2f);
        // Gizmos.DrawSphere(cubePTs[3], 0.2f);
        // Gizmos.DrawSphere(cubePTs[6], 0.2f);
        // Gizmos.DrawSphere(cubePTs[7], 0.2f);


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
            // surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, 1);
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


        // // Draw a sphere at each point
        // for (int i = 0; i < archPoints.Length; i++)
        // {
        //     Gizmos.DrawSphere(archPoints[i], 0.1f);
        // }

        // if (doorwayPoints != null && doorwayPoints.Length > 0)
        // {
        //     // Vector3[] doorwayPoints = GenerateDoorwayPoints(archBottomPoints, width, height);

        //     for (int i = 1; i < doorwayPoints.Length; i++)
        //     {
        //         Vector3 currentPoint = doorwayPoints[i];
        //         Vector3 nextPoint = doorwayPoints[(i + 1) % doorwayPoints.Length];
        //         Gizmos.DrawLine(currentPoint, nextPoint);
        //         Gizmos.DrawSphere(currentPoint, 0.1f);
        //     }
        // }
    }

    // private void OnDrawGizmos()
    // {
    //     (Vector3[] allPoints, Vector3[] bottomPoints) = GenerateArchPoints(width, height, depth);
    //     points = allPoints;
    //     // points = GenerateDoorwayPoints(bottomPoints, width, height);

    //     if (points == null) return;

    //     // Draw lines between the points
    //     for (int i = 0; i < points.Length - 1; i++)
    //     {
    //         Gizmos.DrawLine(points[i], points[i + 1]);
    //     }

    //     // Draw a sphere at each point
    //     for (int i = 0; i < points.Length; i++)
    //     {
    //         Gizmos.DrawSphere(points[i], 0.1f);
    //     }


    //     for (int i = 0; i < bottomPoints.Length; i++)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawSphere(bottomPoints[i], 0.2f);
    //     }
    // }

    // private void GenerateBuildingPoints()
    // {
    //     // Create an empty array to store the points
    //     points = new Vector3[8 + (4 * sideFeaturePoints)];

    //     // Generate the points of the building
    //     int i = 0;
    //     points[i++] = new Vector3(-width / 2, 0, -depth / 2);
    //     points[i++] = new Vector3(-width / 2, 0, depth / 2);
    //     points[i++] = new Vector3(width / 2, 0, depth / 2);
    //     points[i++] = new Vector3(width / 2, 0, -depth / 2);
    //     points[i++] = new Vector3(-width / 2, height, -depth / 2);
    //     points[i++] = new Vector3(-width / 2, height, depth / 2);
    //     points[i++] = new Vector3(width / 2, height, depth / 2);
    //     points[i++] = new Vector3(width / 2, height, -depth / 2);

    //     // Add side feature points
    //     float featureSpacing = depth / (sideFeaturePoints + 1);
    //     for (int j = 1; j <= sideFeaturePoints; j++)
    //     {
    //         points[i++] = new Vector3(-width / 2, height / 2, -depth / 2 + (j * featureSpacing));
    //         points[i++] = new Vector3(width / 2, height / 2, -depth / 2 + (j * featureSpacing));
    //         points[i++] = new Vector3(-width / 2, height / 2, depth / 2 - (j * featureSpacing));
    //         points[i++] = new Vector3(width / 2, height / 2, depth / 2 - (j * featureSpacing));
    //     }
    // }
    // private void OnDrawGizmos()
    // {
    //     if (points == null)
    //     {
    //         GenerateBuildingPoints();
    //     }
    //     for (int i = 0; i < points.Length; i++)
    //     {
    //         if (i < points.Length - sideFeaturePoints) // for each side of the building
    //         {
    //             // Connect the lines between neighboring points
    //             Gizmos.DrawLine(points[i], points[i + 1]);
    //             Gizmos.DrawLine(points[i], points[i + sideFeaturePoints]);
    //             Gizmos.DrawLine(points[i], points[i + sideFeaturePoints + 1]);
    //         }
    //         else if (i < points.Length - 1) // for the last side of the building
    //         {
    //             // Connect the lines between neighboring points
    //             Gizmos.DrawLine(points[i], points[i + 1]);
    //         }
    //     }
    // }


    // private void GenerateBuildingPoints(float width, float depth, float height)
    // {
    //     // Create an empty array to store the points
    //     // points = new Vector3[8];
    //     points = new Vector3[8 + (4 * sideFeaturePoints)];


    //     // Generate the points of the building
    //     points[0] = new Vector3(-width / 2, 0, depth / 2);
    //     points[1] = new Vector3(width / 2, 0, depth / 2);
    //     points[2] = new Vector3(width / 2, 0, -depth / 2);
    //     points[3] = new Vector3(-width / 2, 0, -depth / 2);
    //     points[4] = new Vector3(-width / 2, height, depth / 2);
    //     points[5] = new Vector3(width / 2, height, depth / 2);
    //     points[6] = new Vector3(width / 2, height, -depth / 2);
    //     points[7] = new Vector3(-width / 2, height, -depth / 2);


    //     // Add the side feature points
    //     for (int i = 0; i < sideFeaturePoints; i++)
    //     {
    //         float x = -size / 2 + (size / (sideFeaturePoints + 1)) * (i + 1);
    //         points[8 + (i * 2)] = new Vector3(x, 0, -size / 2);
    //         points[8 + (i * 2) + 1] = new Vector3(x, height, -size / 2);
    //     }

    // }
    // private void OnDrawGizmos()
    // {
    //     if (points == null) return;

    //     // Draw lines connecting the points
    //     Gizmos.color = Color.black;
    //     for (int i = 0; i < 4; i++)
    //     {
    //         Gizmos.DrawLine(points[i], points[(i + 1) % 4]);
    //         Gizmos.DrawLine(points[i + 4], points[((i + 1) % 4) + 4]);
    //         Gizmos.DrawLine(points[i], points[i + 4]);
    //     }
    // }



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
