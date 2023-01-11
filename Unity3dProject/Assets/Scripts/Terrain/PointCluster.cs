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
    public void SetPoints(List<Vector3> points)
    {
        _points = points;

        center = CalculateClusterCenter();
        _boundsRadius = GetBoundsRadius();

        UpdateBounds();
    }
    public List<Vector3> GetPoints()
    {
        return _points;
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

    public void UpdateMaximumClusterSize(float minRadius)
    {
        GetMaximumClusterSize(minRadius);
        GeneratePointsWithinCluster(minRadius);
        gridPointPrototypes = ConsolidateNeighbors(12f);
        gridPointPrototypes = ConsolidateNeighbors(18f);
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

    // List<Vector3> GeneratePointsWithinCluster(float radius = 6f)
    // {
    //     // Sort the cluster points by distance from the center
    //     Vector3 center = _points.Aggregate((a, b) => a + b) / _points.Count;
    //     _points.Sort((a, b) => Vector3.Distance(a, center).CompareTo(Vector3.Distance(b, center)));

    //     // Create a list to store the generated points
    //     List<Vector3> points = new List<Vector3>();

    //     // Find the minimum and maximum x and z values of the cluster points
    //     float minX = _points.Min(p => p.x);
    //     float maxX = _points.Max(p => p.x);
    //     float minZ = _points.Min(p => p.z);
    //     float maxZ = _points.Max(p => p.z);

    //     // Iterate through the sorted cluster points
    //     foreach (Vector3 point in _points)
    //     {
    //         // Check if there is enough space for a new point at this position
    //         bool intersects = points.Any(p => Vector3.Distance(p, point) < radius * 2);

    //         // Check if the new point's bounds fit inside the cluster bounds
    //         bool fitsWithinBounds = point.x - radius >= minX && point.x + radius <= maxX && point.z - radius >= minZ && point.z + radius <= maxZ;

    //         if (!intersects && fitsWithinBounds)
    //         {
    //             // Add a new point at this position
    //             points.Add(point);
    //         }
    //     }

    //     gridPoints = points;

    //     return points;
    // }

    void GeneratePointsWithinCluster(float radius = 6f)
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

        List<GridPointPrototype> prototypes = new List<GridPointPrototype>();
        for (int i = 0; i < points.Count; i++)
        {
            prototypes.Add(new GridPointPrototype
            {
                position = points[i],
                radius = radius,
                id = i,
            });
        }

        gridPointPrototypes = prototypes;
        gridPoints = points;

        // return points;
    }

    public struct GridPointPrototype
    {
        public Vector3 position;
        public float radius;
        public int id;

    }
    public List<GridPointPrototype> gridPointPrototypes;

    List<GridPointPrototype> ConsolidateNeighbors(float newRadius)
    {
        List<GridPointPrototype> consolidatedPrototypes = new List<GridPointPrototype>();
        List<GridPointPrototype> removedPrototypes = new List<GridPointPrototype>();

        // Iterate through the prototypes
        foreach (GridPointPrototype prototype in gridPointPrototypes)
        {
            // Check if this prototype has already been consolidated or removed
            bool removedContainsId = removedPrototypes.Any(p => p.id == prototype.id);
            if (removedContainsId)
            {
                continue;
            }

            // Find all neighbors of this prototype
            List<GridPointPrototype> neighbors = gridPointPrototypes.Where(p => Vector2.Distance(new Vector2(p.position.x, p.position.z), new Vector2(prototype.position.x, prototype.position.z)) <= newRadius * 1f).ToList();

            // Remove any prototypes from the neighbors list that are in the removedPrototypes list
            neighbors.RemoveAll(p => removedPrototypes.Contains(p));

            // Calculate the combined radius of the neighbors
            float combinedRadius = neighbors.Sum(p => p.radius);

            // Debug.Log("neighbors: " + neighbors.Count + ", combinedRadius: " + combinedRadius);

            // Check if the combined radius fits within the new radius and that no other consolidated prototype overlaps with this one on the x and z axes
            bool overlap = consolidatedPrototypes.Any(p => Mathf.Abs(p.position.x - prototype.position.x) < (p.radius + prototype.radius) && Mathf.Abs(p.position.z - prototype.position.z) < (p.radius + prototype.radius));
            if (neighbors.Count > 1 && combinedRadius <= newRadius && !overlap)
            {
                // Calculate the center position between the neighbors
                Vector3 combinedPosition = Vector3.zero;
                foreach (GridPointPrototype neighbor in neighbors)
                {
                    combinedPosition += neighbor.position;
                }
                combinedPosition /= neighbors.Count;

                // Create a new prototype with the combined position and radius
                GridPointPrototype consolidatedPrototype = new GridPointPrototype { position = combinedPosition, radius = newRadius, id = prototype.id };

                // Add the new prototype to the consolidated list
                consolidatedPrototypes.Add(consolidatedPrototype);

                // Add the consolidated prototypes to the removed list
                removedPrototypes.AddRange(neighbors);
            }
            else
            {
                // Add the prototype to the consolidated list as is
                consolidatedPrototypes.Add(prototype);
            }

        }

        return consolidatedPrototypes;
    }

    // List<GridPointPrototype> ConsolidateNeighbors(float newRadius)
    // {
    //     List<GridPointPrototype> consolidatedPrototypes = new List<GridPointPrototype>();

    //     // Iterate through the prototypes
    //     foreach (GridPointPrototype prototype in gridPointPrototypes)
    //     {
    //         // Check if this prototype has already been consolidated
    //         if (consolidatedPrototypes.Any(p => p.id == prototype.id))
    //         {
    //             continue;
    //         }

    //         // Find all neighbors of this prototype
    //         // List<GridPointPrototype> neighbors = gridPointPrototypes.Where(p => Mathf.Abs(p.position.x - prototype.position.x) <= newRadius * 2 && Mathf.Abs(p.position.z - prototype.position.z) <= newRadius * 2).ToList();
    //         List<GridPointPrototype> neighbors = gridPointPrototypes.Where(p => Vector2.Distance(new Vector2(p.position.x, p.position.z), new Vector2(prototype.position.x, prototype.position.z)) <= newRadius * 0.5f).ToList();

    //         // Calculate the combined radius of the neighbors
    //         float combinedRadius = neighbors.Sum(p => p.radius);

    //         Debug.Log("neighbors: " + neighbors.Count + ", combinedRadius: " + combinedRadius);


    //         // Check if the combined radius fits within the new radius
    //         if (combinedRadius <= newRadius)
    //         {
    //             // Calculate the center position between the neighbors
    //             Vector3 combinedPosition = Vector3.zero;
    //             foreach (GridPointPrototype neighbor in neighbors)
    //             {
    //                 combinedPosition += neighbor.position;
    //             }
    //             combinedPosition /= neighbors.Count;

    //             // Create a new prototype with the combined position and radius
    //             GridPointPrototype consolidatedPrototype = new GridPointPrototype { position = combinedPosition, radius = newRadius, id = prototype.id };

    //             // Add the new prototype to the consolidated list
    //             consolidatedPrototypes.Add(consolidatedPrototype);

    //             // Remove the consolidated prototypes from the list
    //             // gridPointPrototypes.RemoveAll(p => neighbors.Contains(p));
    //         }
    //         else
    //         {
    //             // Add the prototype to the consolidated list as is
    //             consolidatedPrototypes.Add(prototype);
    //         }
    //     }

    //     return consolidatedPrototypes;
    // }

}