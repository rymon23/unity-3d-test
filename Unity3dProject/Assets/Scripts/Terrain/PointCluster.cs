using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PointCluster : MonoBehaviour
{
    private List<Vector3> _points;
    public Vector3 center { get; private set; }
    private float _boundsRadius;
    public Bounds bounds;
    public Color color = Color.green;
    public PointCluster(Vector3 point)
    {
        _points = new List<Vector3> { point };
        center = point;
        _boundsRadius = 0f;

        UpdateBounds();
    }


    public float maxRectSize;
    public Vector3 topLeft;
    public Vector3 topRight;
    public Vector3 bottomLeft;
    public Vector3 bottomRight;


    public void AddPoint(Vector3 point)
    {
        _points.Add(point);

        // Recalculate the cluster's center and bounds radius
        center = CalculateClusterCenter();
        _boundsRadius = GetBoundsRadius();

        UpdateBounds();
    }

    public void UpdateBounds()
    {
        // Calculate the bounds of the cluster
        bounds = new Bounds();
        foreach (Vector3 point in _points)
        {
            bounds.Encapsulate(point);
        }
    }

    public float GetBoundsRadius()
    {
        float maxDistance = 0f;
        foreach (Vector3 point in _points)
        {
            float distance = Vector3.Distance(center, point);
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }
        }
        return maxDistance;
    }

    public List<Vector3> GetBorderPoints()
    {
        List<Vector3> borderPoints = new List<Vector3>();
        // Create an expanded bounds to include points on the border
        Bounds expandedBounds = new Bounds(bounds.center, bounds.size + Vector3.one * 0.1f);
        foreach (Vector3 point in _points)
        {
            if (bounds.Contains(point) && !expandedBounds.Contains(point))
            {
                borderPoints.Add(point);
            }
        }
        return borderPoints;
    }


    public Vector3 CalculateClusterCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in _points)
        {
            center += point;
        }
        center /= _points.Count;
        return center;
    }

    public List<Vector3> GetPoints()
    {
        return _points;
    }

    public Vector3 GetSize()
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (Vector3 point in _points)
        {
            if (point.x < minX)
            {
                minX = point.x;
            }
            if (point.x > maxX)
            {
                maxX = point.x;
            }
            if (point.y < minY)
            {
                minY = point.y;
            }
            if (point.y > maxY)
            {
                maxY = point.y;
            }
            if (point.z < minZ)
            {
                minZ = point.z;
            }
            if (point.z > maxZ)
            {
                maxZ = point.z;
            }
        }

        return new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
    }


    public Vector2 maxRectangleSize;
    public Vector3[] maxRectangleBorderPoints = new Vector3[4];

    public void UpdateMaximumClusterSize(float distance)
    {
        GetMaximumClusterSize(distance);
        GeneratePointsWithinCluster(distance);
    }

    public Vector2 GetMaximumClusterSize(float distance)
    {
        // Sort the cluster of points by their x and z values
        // _points.Sort((p1, p2) => p1.x.CompareTo(p2.x));
        // _points.Sort((p1, p2) => p1.z.CompareTo(p2.z));

        // _points.Sort((p1, p2) => p1.x.CompareTo(p2.x).CompareTo(Vector3.Distance(p2, center)));
        _points.Sort((p1, p2) => Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(center.x, center.z))
                                        .CompareTo(Vector2.Distance(new Vector2(p2.x, p2.z), new Vector2(center.x, center.z))));


        float maxWidth = 0;
        float maxHeight = 0;

        // Iterate through the sorted list of points
        for (int i = 0; i < _points.Count; i++)
        {
            // Find the neighbors of the current point within the set distance
            List<Vector3> neighbors = _points.FindAll(p => p.x >= _points[i].x - distance && p.x <= _points[i].x + distance && p.z >= _points[i].z - distance && p.z <= _points[i].z + distance);

            float minX = neighbors.Min(p => p.x);
            float maxX = neighbors.Max(p => p.x);
            float minZ = neighbors.Min(p => p.z);
            float maxZ = neighbors.Max(p => p.z);

            // Find the corner points of the rectangle
            Vector3 bottomLeft = new Vector3(minX, 0, minZ);
            Vector3 topLeft = new Vector3(minX, 0, maxZ);
            Vector3 bottomRight = new Vector3(maxX, 0, minZ);
            Vector3 topRight = new Vector3(maxX, 0, maxZ);

            // Return the corner points and the border points
            maxRectangleBorderPoints = new Vector3[4];
            maxRectangleBorderPoints[0] = bottomLeft;
            maxRectangleBorderPoints[1] = topLeft;
            maxRectangleBorderPoints[2] = bottomRight;
            maxRectangleBorderPoints[3] = topRight;


            // Calculate the width and height of the rectangle using the point and its neighbors
            float width = maxX - minX;
            float height = maxZ - minZ;

            // Update the maximum width and height if the calculated width or height is greater than the current value
            maxWidth = Mathf.Max(maxWidth, width);
            maxHeight = Mathf.Max(maxHeight, height);
        }


        maxRectangleSize = new Vector2(maxWidth, maxHeight);

        // Return the maximum size as a Vector2
        return maxRectangleSize;
    }

    public List<Vector3> gridPoints;

    // List<Vector3> GeneratePointsWithinCluster(float radius)
    // {
    //     // Sort the cluster points by distance from the center
    //     Vector3 center = _points.Aggregate((a, b) => a + b) / _points.Count;
    //     _points.Sort((a, b) => Vector3.Distance(a, center).CompareTo(Vector3.Distance(b, center)));

    //     // Create a list to store the generated points
    //     List<Vector3> points = new List<Vector3>();

    //     // Iterate through the sorted cluster points
    //     foreach (Vector3 point in _points)
    //     {
    //         // Check if there is enough space for a new point at this position
    //         bool intersects = points.Any(p => Vector3.Distance(p, point) < radius * 2);
    //         if (!intersects)
    //         {
    //             // Add a new point at this position
    //             points.Add(point);
    //         }
    //     }

    //     gridPoints = points;

    //     return points;
    // }
    // This method first sorts the cluster points by distance from the center, then iterates through the sorted points and adds a new point at each position if there is enough space for it(i.e. if it does not intersect with any existing points). It returns a list of the generated points.

    // Note that this method assumes that the cluster points are already within the bounds of the cluster, and that the radius is a valid size for the points. You may need to add additional checks to ensure this.

    List<Vector3> GeneratePointsWithinCluster(float radius)
    {
        // Sort the cluster points by distance from the center
        Vector3 center = _points.Aggregate((a, b) => a + b) / _points.Count;
        _points.Sort((a, b) => Vector3.Distance(a, center).CompareTo(Vector3.Distance(b, center)));

        // Create a list to store the generated points
        List<Vector3> points = new List<Vector3>();

        // Find the minimum and maximum x and z values of the cluster points
        float minX = _points.Min(p => p.x);
        float maxX = _points.Max(p => p.x);
        float minZ = _points.Min(p => p.z);
        float maxZ = _points.Max(p => p.z);

        // Iterate through the sorted cluster points
        foreach (Vector3 point in _points)
        {
            // Check if there is enough space for a new point at this position
            bool intersects = points.Any(p => Vector3.Distance(p, point) < radius * 2);

            // Check if the new point's bounds fit inside the cluster bounds
            bool fitsWithinBounds = point.x - radius >= minX && point.x + radius <= maxX && point.z - radius >= minZ && point.z + radius <= maxZ;

            if (!intersects && fitsWithinBounds)
            {
                // Add a new point at this position
                points.Add(point);
            }
        }

        gridPoints = points;

        return points;
    }








}