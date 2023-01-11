using System.Collections.Generic;
using UnityEngine;

public class ConvexHull
{
    public static List<Vector3> GetConvexHull(Vector3[] points)
    {
        // Find the point with the lowest x-coordinate
        Vector3 startingPoint = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].x < startingPoint.x)
            {
                startingPoint = points[i];
            }
        }

        // Initialize the list for the convex hull
        List<Vector3> convexHull = new List<Vector3>();
        Vector3 currentPoint = startingPoint;

        do
        {
            convexHull.Add(currentPoint);
            Vector3 nextPoint = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i] == currentPoint)
                {
                    continue;
                }
                // find the next point
                double crossProduct = CrossProduct(currentPoint, nextPoint, points[i]);
                if (crossProduct > 0 || (crossProduct == 0 && Distance(currentPoint, points[i]) > Distance(currentPoint, nextPoint)))
                {
                    nextPoint = points[i];
                }
            }
            currentPoint = nextPoint;
        } while (currentPoint != startingPoint);

        return convexHull;
    }
    private static double CrossProduct(Vector3 A, Vector3 B, Vector3 C)
    {
        return (C.x - A.x) * (B.z - A.z) - (C.z - A.z) * (B.x - A.x);
    }
    private static double Distance(Vector3 A, Vector3 B)
    {
        return Mathf.Sqrt(Mathf.Pow(A.x - B.x, 2) + Mathf.Pow(A.z - B.z, 2));
    }
}
