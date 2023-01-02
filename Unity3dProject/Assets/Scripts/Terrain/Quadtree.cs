using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quadtree<T>
{
    private const int MaxCapacity = 4;

    private readonly Rect bounds;
    private readonly List<Vector3> points = new List<Vector3>();
    private Quadtree<T>[] nodes;

    public Quadtree(Rect bounds)
    {
        this.bounds = bounds;
    }

    public void Insert(Vector2 point)
    {
        // Check if the point is outside of the bounds of the quadtree
        if (!bounds.Contains(point))
        {
            return;
        }

        // If the quadtree has not reached capacity, add the point to the list
        if (points.Count < MaxCapacity)
        {
            points.Add(point);
            return;
        }

        // If the quadtree is already at capacity, divide it into quadrants and insert the point into the appropriate quadrant
        if (nodes == null)
        {
            Divide();
        }

        foreach (Quadtree<T> node in nodes)
        {
            node.Insert(point);
        }
    }

    public List<Vector3> GetRange(Vector2 point, float range)
    {
        // Create a list to store the points within the range
        List<Vector3> pointsInRange = new List<Vector3>();

        // Check if the point is outside of the bounds of the quadtree
        if (!bounds.Contains(point))
        {
            return pointsInRange;
        }

        // Add the points in the current quadtree node to the list if they are within the range
        foreach (Vector3 p in points)
        {
            if (Vector2.Distance(point, p) <= range)
            {
                pointsInRange.Add(p);
            }
        }

        // If the quadtree is divided, check the child nodes for points within the range
        if (nodes != null)
        {
            foreach (Quadtree<T> node in nodes)
            {
                pointsInRange.AddRange(node.GetRange(point, range));
            }
        }

        return pointsInRange;
    }

    private void Divide()
    {
        // Calculate the dimensions of the quadrants
        float halfWidth = bounds.width / 2;
        float halfHeight = bounds.height / 2;
        float x = bounds.x;
        float y = bounds.y;

        // Create the quadrants
        Quadtree<T> topLeft = new Quadtree<T>(new Rect(x, y, halfWidth, halfHeight));
        Quadtree<T> topRight = new Quadtree<T>(new Rect(x + halfWidth, y, halfWidth, halfHeight));
        Quadtree<T> bottomLeft = new Quadtree<T>(new Rect(x, y + halfHeight, halfWidth, halfHeight));
        Quadtree<T> bottomRight = new Quadtree<T>(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight));

        // Assign the quadrants to the nodes array
        nodes = new Quadtree<T>[] { topLeft, topRight, bottomLeft, bottomRight };
    }
}