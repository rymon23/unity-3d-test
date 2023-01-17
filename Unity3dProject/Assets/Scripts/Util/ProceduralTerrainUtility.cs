using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    public static class ProceduralTerrainUtility
    {
        public static float Blend(float A, float B, float t)
        {
            return (1 - t) * A + t * B;
        }

        public static float GetAverage(float value1, float value2)
        {
            return (value1 + value2) / 2f;
        }

        public static float CalculateSlope(Vector3[] points)
        {
            // Initialize variables to store the coordinates of the first and last points in the array
            float x1 = points[0].x, y1 = points[0].y;
            float x2 = points[points.Length - 1].x, y2 = points[points.Length - 1].y;

            // Calculate the slope using the formula (y2 - y1) / (x2 - x1)
            float slope = (y2 - y1) / (x2 - x1);

            return slope;
        }

        public static int[] CreateTriangles(List<Vector3> vertices)
        {
            int numVertices = vertices.Count;
            int numTriangles = numVertices - 2;
            int[] triangles = new int[numTriangles * 3];
            for (int i = 0; i < numTriangles; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            return triangles;
        }
        public static int[] GenerateTerrainTriangles(int gridSize)
        {
            // Create an array to store the triangle indices
            int[] triangles = new int[(gridSize - 1) * (gridSize - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < gridSize - 1; x++)
            {
                for (int y = 0; y < gridSize - 1; y++)
                {
                    triangles[index++] = x + y * gridSize;
                    triangles[index++] = x + (y + 1) * gridSize;
                    triangles[index++] = x + 1 + y * gridSize;

                    triangles[index++] = x + 1 + y * gridSize;
                    triangles[index++] = x + (y + 1) * gridSize;
                    triangles[index++] = x + 1 + (y + 1) * gridSize;
                }
            }
            return triangles;
        }

        public static Vector2[] GenerateTerrainUVs(int gridSize, int vertices)
        {
            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[vertices];

            // Iterate through the vertices and set the UVs of each vertex
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    uvs[x + y * gridSize] = new Vector2(x / (float)gridSize, y / (float)gridSize);
                }
            }
            return uvs;
        }

        public static (Vector3, float) GetClosestPoint(Vector3[] points, Vector3 position)
        {
            Vector3 nearestPoint = points[0];
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                float dist = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(points[i].x, points[i].z));
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestPoint = points[i];
                }
            }
            return (nearestPoint, nearestDistance);
        }
        public static (Vector3, float, int index) GetClosestPoint(Vector3[] points, Vector2 position)
        {
            Vector3 nearestPoint = points[0];
            float nearestDistance = float.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < points.Length; i++)
            {
                float dist = Vector2.Distance(position, new Vector2(points[i].x, points[i].z));
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestPoint = points[i];
                    nearestIndex = i;
                }
            }
            return (nearestPoint, nearestDistance, nearestIndex);
        }

        // Moves all of the points to the position while keeping their relative placement unchanged
        public static void MoveGroupedPointsToPosition(Vector3[] points, Vector3 position)
        {
            Vector3 difference = position - points[points.Length - 1];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] += difference;
            }
        }
        // Moves all of the points to the position while keeping their relative placement unchanged
        public static void MoveGroupedPointsToPosition(List<Vector3> points, Vector3 position)
        {
            Vector3 difference = position - points[points.Count - 1];
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += difference;
            }
        }

        public static Vector3 GeneratePointWithinBounds(float minXZ, float maxXZ, float minY, float maxY)
        {
            // Generate a random position within the bounds
            float xPos = UnityEngine.Random.Range(minXZ, maxXZ);
            float zPos = UnityEngine.Random.Range(minXZ, maxXZ);
            float yPos = UnityEngine.Random.Range(minY, maxY);
            return new Vector3(xPos, yPos, zPos);
        }

        public static Vector3 GeneratePointWithinRadius(Vector3 position, Vector2 radiusRange, float minY, float maxY)
        {
            // Generate a random angle and distance within the bounds
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(radiusRange.x, radiusRange.y);
            float yPos = UnityEngine.Random.Range(minY, maxY);

            // Calculate the point based on the position, angle, and distance
            float xPos = position.x + distance * Mathf.Cos(angle);
            float zPos = position.z + distance * Mathf.Sin(angle);
            return new Vector3(xPos, yPos, zPos);
        }

        public static Vector3[] GeneratePointsWithinBounds(int number, Vector3 scale, Vector2 rangeXZ, Vector2 rangeY, float minDistance = 1f)
        {
            Vector3[] points = new Vector3[number];
            for (int i = 0; i < number; i++)
            {
                points[i] = GeneratePointWithinBounds(rangeXZ.x, rangeXZ.y, rangeY.x, rangeY.y);

                // Ensure that the point is a minimum distance away from all other points
                bool tooClose = false;
                do
                {
                    tooClose = false;
                    foreach (Vector3 point in points)
                    {
                        if (point != points[i] && Vector3.Distance(point, points[i]) < minDistance * scale.x)
                        {
                            tooClose = true;
                            points[i] = GeneratePointWithinBounds(rangeXZ.x, rangeXZ.y, rangeY.x, rangeY.y);
                            break;
                        }
                    }
                } while (tooClose);
            }
            return points;
        }

        public static Vector3[] GeneratePointsWithinRadius(int number, Vector3 position, Vector3 scale, Vector2 radiusRange, Vector2 rangeY, float minDistance = 1f)
        {
            Vector3[] points = new Vector3[number];
            for (int i = 0; i < number; i++)
            {
                Vector3 newPos = GeneratePointWithinRadius(position, radiusRange, rangeY.x, rangeY.y);
                points[i] = newPos;
                // Ensure that the point is a minimum distance away from all other points
                bool tooClose = false;
                do
                {
                    tooClose = false;
                    foreach (Vector3 point in points)
                    {
                        if (point != points[i] && Vector3.Distance(point, points[i]) < minDistance * scale.x)
                        {
                            tooClose = true;
                            newPos = GeneratePointWithinRadius(position, radiusRange, rangeY.x, rangeY.y);
                            points[i] = newPos;
                            break;
                        }
                    }
                } while (tooClose);
            }
            return points;
        }

        public static Vector3 GenerateOverlappingPoint(Vector3 point, float radius)
        {
            // Generate a random angle and distance such that the radius bounds overlap
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = radius + UnityEngine.Random.Range(0f, radius);
            float xPos = point.x + distance * Mathf.Cos(angle);
            float zPos = point.z + distance * Mathf.Sin(angle);
            return new Vector3(xPos, point.y, zPos);
        }


        public static Vector3 GenerateOverlappingPoint(Vector3 point, Vector2 radiusRange)
        {
            // Generate a random angle and distance such that the radius bounds overlap
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = radiusRange.y + UnityEngine.Random.Range(radiusRange.x, radiusRange.y);
            float xPos = point.x + distance * Mathf.Cos(angle);
            float zPos = point.z + distance * Mathf.Sin(angle);
            return new Vector3(xPos, point.y, zPos);
        }


        // Return the new point and the midpoint where they overlap 
        public static (Vector3, Vector3) GenerateOverlappingPointAndMidPoint(Vector3 point, Vector2 radiusRange)
        {
            // Generate a random angle and distance such that the radius bounds overlap
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = radiusRange.y + UnityEngine.Random.Range(radiusRange.x, radiusRange.y);
            float xPos = point.x + distance * Mathf.Cos(angle);
            float zPos = point.z + distance * Mathf.Sin(angle);
            Vector3 overlappingPoint = new Vector3(xPos, point.y, zPos);

            // Calculate the midpoint between the point and the overlapping point
            Vector3 midpoint = (point + overlappingPoint) / 2;

            return (overlappingPoint, midpoint);
        }

        public static Vector3 GenerateOverlappingPointWithinPositionRadius(Vector3 point, Vector2 radiusRange, Vector3 position, float maxDistance)
        {
            // Generate a random angle and distance such that the radius bounds overlap
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = radiusRange.y + UnityEngine.Random.Range(radiusRange.x, radiusRange.y);
            float xPos = point.x + distance * Mathf.Cos(angle);
            float zPos = point.z + distance * Mathf.Sin(angle);

            // Ensure that the point is within the maximum distance of the position
            Vector3 newPoint = new Vector3(xPos, point.y, zPos);
            if (Vector3.Distance(position, newPoint) > maxDistance)
            {
                // Generate a new point using the same angle but the maximum distance
                distance = maxDistance;
                xPos = position.x + distance * Mathf.Cos(angle);
                zPos = position.z + distance * Mathf.Sin(angle);
                newPoint = new Vector3(xPos, point.y, zPos);
            }

            return newPoint;
        }

        public static Vector3[] GenerateChainOfOverlappingPoints(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY, float minDistance)
        {
            Vector3[] points = new Vector3[number];
            for (int i = 0; i < number; i++)
            {
                float yMod = UnityEngine.Random.Range(rangeY.x, rangeY.y);
                Vector3 newPos = Vector3.zero;
                if (i == 0)
                {
                    // Generate the first point within the given radius range
                    newPos = GenerateOverlappingPoint(position, radiusRange);
                    newPos.y = position.y + yMod;
                }
                else
                {
                    // Generate a point within the given radius range, ensuring it is at least minDistance away from all other points
                    bool validPointFound = false;
                    while (!validPointFound)
                    {
                        newPos = GenerateOverlappingPoint(points[i - 1], radiusRange);
                        newPos.y = points[i - 1].y + yMod;
                        validPointFound = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (Vector3.Distance(newPos, points[j]) < minDistance)
                            {
                                validPointFound = false;
                                break;
                            }
                        }
                    }
                }
                points[i] = newPos;
            }
            return points;
        }

        // public static (Vector3[], List<Vector3>) GenerateChainOfOverlappingPointsWithMidpoints(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY, float minDistance)
        // {
        //     Vector3[] points = new Vector3[number];
        //     List<Vector3> midPoints = new List<Vector3>();

        //     for (int i = 0; i < number; i++)
        //     {
        //         float yMod = UnityEngine.Random.Range(-rangeY.x, rangeY.y);
        //         Vector3 newPos = Vector3.zero;
        //         if (i == 0)
        //         {
        //             // Generate the first point within the given radius range
        //             (Vector3 newPoint, Vector3 midpoint) = GenerateOverlappingPointAndMidPoint(position, radiusRange);
        //             newPos = newPoint;
        //             newPos.y = position.y + yMod;

        //             // midpoint.y = yMod * 0.5f;
        //             // midPoints.Add(midpoint);
        //         }
        //         else
        //         {
        //             // Generate a point within the given radius range, ensuring it is at least minDistance away from all other points
        //             bool validPointFound = false;
        //             while (!validPointFound)
        //             {
        //                 (Vector3 newPoint, Vector3 midpoint) = GenerateOverlappingPointAndMidPoint(points[i - 1], radiusRange);
        //                 newPos = newPoint;
        //                 newPos.y = points[i - 1].y + yMod;
        //                 validPointFound = true;
        //                 for (int j = 0; j < i; j++)
        //                 {
        //                     if (Vector3.Distance(newPos, points[j]) < minDistance)
        //                     {
        //                         validPointFound = false;
        //                         break;
        //                     }
        //                 }
        //                 if (validPointFound)
        //                 {
        //                     midPoints.Add(midpoint);
        //                     midpoint.y = yMod * 0.5f;
        //                 }
        //             }
        //         }
        //         points[i] = newPos;
        //     }
        //     return (points, midPoints);
        // }


        public static (Vector3[], Vector3[], ZoneConnectorPair[]) GenerateChainOfOverlappingPointsWithMidpoints(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY, float minDistance)
        {
            Vector3[] points = new Vector3[number];
            // Length of these should always be 1 less than the points
            Vector3[] midPoints = new Vector3[number - 1];
            ZoneConnectorPair[] pairs = new ZoneConnectorPair[number - 1];

            for (int i = 0; i < number; i++)
            {
                float yMod = UnityEngine.Random.Range(-rangeY.x, rangeY.y);
                Vector3 newPos = Vector3.zero;
                if (i == 0)
                {
                    // Generate the first point within the given radius range
                    (Vector3 newPoint, Vector3 midpoint) = GenerateOverlappingPointAndMidPoint(position, radiusRange);
                    newPos = newPoint;
                    newPos.y = position.y + yMod;
                }
                else
                {
                    // Generate a point within the given radius range, ensuring it is at least minDistance away from all other points
                    bool validPointFound = false;
                    while (!validPointFound)
                    {
                        (Vector3 newPoint, Vector3 midpoint) = GenerateOverlappingPointAndMidPoint(points[i - 1], radiusRange);
                        newPos = newPoint;
                        newPos.y = points[i - 1].y + yMod;
                        validPointFound = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (Vector3.Distance(newPos, points[j]) < minDistance)
                            {
                                validPointFound = false;
                                break;
                            }
                        }
                        if (validPointFound)
                        {
                            midpoint.y += yMod * 0.6f;
                            midPoints[i - 1] = midpoint;

                            ZoneConnectorPair newPair = new ZoneConnectorPair();
                            newPair.zones = new Vector3[2];
                            newPair.zones[0] = points[i - 1];
                            newPair.zones[1] = newPoint;
                            pairs[i - 1] = newPair;
                        }
                    }
                }
                points[i] = newPos;
            }
            return (points, midPoints, pairs);
        }

        public static Vector3[] GenerateChainOfOverlappingPoints(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY)
        {
            Vector3[] points = new Vector3[number];
            for (int i = 0; i < number; i++)
            {
                float yMod = UnityEngine.Random.Range(rangeY.x, rangeY.y);
                Vector3 newPos = Vector3.zero;
                if (i == 0)
                {
                    newPos = GenerateOverlappingPoint(position, radiusRange);
                    newPos.y = position.y + yMod;
                }
                else
                {
                    newPos = GenerateOverlappingPoint(points[i - 1], radiusRange);
                    newPos.y = points[i - 1].y + yMod;
                }
                points[i] = newPos;
            }
            return points;
        }

        // public static Vector3[] GenerateChainOfOverlappingPoints(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY, float maxRadius)
        // {
        //     Vector3[] points = new Vector3[number];
        //     for (int i = 0; i < number; i++)
        //     {
        //         float yMod = UnityEngine.Random.Range(rangeY.x, rangeY.y);
        //         Vector3 newPos = Vector3.zero;
        //         if (i == 0)
        //         {
        //             newPos = GenerateOverlappingPointWithinPositionRadius(position, radiusRange, position, maxRadius);
        //             newPos.y = position.y + yMod;
        //         }
        //         else
        //         {
        //             newPos = GenerateOverlappingPointWithinPositionRadius(points[i - 1], radiusRange, position, maxRadius);
        //             newPos.y = points[i - 1].y + yMod;
        //         }
        //         points[i] = newPos;
        //     }
        //     return points;
        // }

        public static Vector3[] GetOverlappingPoints(Vector3[] points, float radius)
        {
            List<Vector3> overlappingPoints = new List<Vector3>();
            for (int i = 0; i < points.Length; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    if (Vector3.Distance(points[i], points[j]) < radius * 2f)
                    {
                        overlappingPoints.Add(points[i]);
                        overlappingPoints.Add(points[j]);
                    }
                }
            }
            return overlappingPoints.ToArray();
        }


        public static List<Vector3> GetPointsBetweenPosition(Vector3 position, List<Vector3> points)
        {
            List<Vector3> betweenPoints = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 point1 = points[i];
                Vector3 point2 = points[i + 1];
                if (position.x >= Mathf.Min(point1.x, point2.x) && position.x <= Mathf.Max(point1.x, point2.x) &&
                    position.z >= Mathf.Min(point1.z, point2.z) && position.z <= Mathf.Max(point1.z, point2.z))
                {
                    betweenPoints.Add(point1);
                    betweenPoints.Add(point2);
                }
            }
            return betweenPoints;
        }
        public static List<Vector3> GetPointsBetweenPosition(Vector3 position, Vector3[] points)
        {
            List<Vector3> betweenPoints = new List<Vector3>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 point1 = points[i];
                Vector3 point2 = points[i + 1];
                if (position.x >= Mathf.Min(point1.x, point2.x) && position.x <= Mathf.Max(point1.x, point2.x) &&
                    position.z >= Mathf.Min(point1.z, point2.z) && position.z <= Mathf.Max(point1.z, point2.z))
                {
                    betweenPoints.Add(point1);
                    betweenPoints.Add(point2);
                }
            }
            return betweenPoints;
        }


        public static bool IsPositionBetweenPoints(Vector3 position, List<Vector3> points)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 point1 = points[i];
                Vector3 point2 = points[i + 1];
                if (position.x >= Mathf.Min(point1.x, point2.x) && position.x <= Mathf.Max(point1.x, point2.x) &&
                    position.z >= Mathf.Min(point1.z, point2.z) && position.z <= Mathf.Max(point1.z, point2.z))
                {
                    return true;
                }
            }
            return false;
        }

        public static float CalculateAverageSlope(Vector3 position, List<Vector3> points)
        {
            float totalSlope = 0f;
            int numPoints = 0;
            foreach (Vector3 point in points)
            {
                totalSlope += point.y - position.y;
                numPoints++;
            }
            return totalSlope / numPoints;
        }

        public static Vector3 GetCenterPosition(List<Vector3> points)
        {
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in points)
            {
                center += point;
            }
            center /= points.Count;
            return center;
        }
        public static Vector3 GetCenterPosition(Vector3[] points)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
            {
                center += points[i];
            }
            center /= points.Length;
            return center;
        }

        public static Vector3[] GenerateSquarePoints(Vector3 center, float radius)
        {
            Vector3[] squarePoints = new Vector3[4];

            squarePoints[0] = new Vector3(center.x - radius, center.y, center.z - radius); // bottom-left point
            squarePoints[1] = new Vector3(center.x + radius, center.y, center.z - radius); // bottom-right point
            squarePoints[2] = new Vector3(center.x - radius, center.y, center.z + radius); // top-left point
            squarePoints[3] = new Vector3(center.x + radius, center.y, center.z + radius); // top-right point

            return squarePoints;
        }

        public static List<VerticalCellPrototype> GenerateCubeStack(int floors, Vector3 position, float width, float height, float depth)
        {
            List<VerticalCellPrototype> cubeStack = new List<VerticalCellPrototype>();
            for (int i = 0; i < floors; i++)
            {
                Vector3 currentPosition = new Vector3(position.x, position.y, position.z);
                if (i > 0) currentPosition.y += (height * i);

                Vector3[] points = GenerateCubePoints(currentPosition, width, height, depth);
                VerticalCellPrototype cube = new VerticalCellPrototype
                {
                    position = currentPosition,
                    _cornerPoints = points,
                    height = (int)height,
                    width = (int)width,
                    depth = (int)depth

                };
                cubeStack.Add(cube);
            }
            return cubeStack;
        }

        public static Vector3[] GenerateCubePoints(Vector3 center, float width, float height, float depth)
        {
            Vector3[] points = new Vector3[8];
            float halfWidth = width / 2;
            // float halfHeight = height / 2;
            float halfDepth = depth / 2;
            points[0] = center + new Vector3(-halfWidth, height, halfDepth);
            points[1] = center + new Vector3(halfWidth, height, halfDepth);
            points[2] = center + new Vector3(halfWidth, 0, halfDepth);
            points[3] = center + new Vector3(-halfWidth, 0, halfDepth);
            points[4] = center + new Vector3(-halfWidth, height, -halfDepth);
            points[5] = center + new Vector3(halfWidth, height, -halfDepth);
            points[6] = center + new Vector3(halfWidth, 0, -halfDepth);
            points[7] = center + new Vector3(-halfWidth, 0, -halfDepth);
            return points;
        }

        public static Vector3[] GenerateCubePoints(Vector3 center, float size)
        {
            Vector3[] points = new Vector3[8];
            float halfSize = size / 2;
            points[0] = center + new Vector3(-halfSize, halfSize, halfSize);
            points[1] = center + new Vector3(halfSize, halfSize, halfSize);
            points[2] = center + new Vector3(halfSize, -halfSize, halfSize);
            points[3] = center + new Vector3(-halfSize, -halfSize, halfSize);
            points[4] = center + new Vector3(-halfSize, halfSize, -halfSize);
            points[5] = center + new Vector3(halfSize, halfSize, -halfSize);
            points[6] = center + new Vector3(halfSize, -halfSize, -halfSize);
            points[7] = center + new Vector3(-halfSize, -halfSize, -halfSize);
            return points;
        }

        public static Vector3[] GenerateHexagonPoints(Vector3 center, float radius)
        {
            Vector3[] points = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
                points[i] = new Vector3(x, center.y, z);
            }
            return points;
        }

        public static Vector3[] GeneratePointsWithinBounds(Vector3[] edgePoints, int numPoints, float distance, float elevation)
        {
            // Find the min and max x and z values
            float minX = edgePoints.Min(p => p.x);
            float maxX = edgePoints.Max(p => p.x);
            float minZ = edgePoints.Min(p => p.z);
            float maxZ = edgePoints.Max(p => p.z);

            // Create a list to store the new points
            List<Vector3> newPoints = new List<Vector3>();

            // Iterate through the x values
            for (float x = minX; x <= maxX; x += distance)
            {
                // Iterate through the z values
                for (float z = minZ; z <= maxZ; z += distance)
                {
                    Vector3 newPoint = new Vector3(x, elevation, z);
                    // check if the new point is inside the polygon
                    if (IsPointInsidePolygon(edgePoints, newPoint))
                    {
                        newPoints.Add(newPoint);
                    }
                    if (newPoints.Count == numPoints) return newPoints.ToArray();
                }
            }
            return newPoints.ToArray();
        }

        public static bool IsPointInsidePolygon(Vector3[] polygon, Vector3 point)
        {
            bool inside = false;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].z < point.z && polygon[j].z >= point.z || polygon[j].z < point.z && polygon[i].z >= point.z)
                {
                    if (polygon[i].x + (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x) < point.x)
                    {
                        inside = !inside;
                    }
                }
                j = i;
            }
            return inside;
        }
        public static Vector3[,] GenerateGrid(Vector3[] edgePoints, float gridSpacing, float elevation)
        {
            // Sort the edgePoints by their x and z values
            Array.Sort(edgePoints, (a, b) =>
            {
                if (a.x != b.x)
                    return a.x.CompareTo(b.x);
                else
                    return a.z.CompareTo(b.z);
            });

            float xMin = edgePoints[0].x;
            float xMax = edgePoints[0].x;
            float zMin = edgePoints[0].z;
            float zMax = edgePoints[0].z;

            // Determine the minimum and maximum x and z values from the edgePoints
            for (int i = 1; i < edgePoints.Length; i++)
            {
                if (edgePoints[i].x < xMin)
                    xMin = edgePoints[i].x;

                if (edgePoints[i].x > xMax)
                    xMax = edgePoints[i].x;

                if (edgePoints[i].z < zMin)
                    zMin = edgePoints[i].z;

                if (edgePoints[i].z > zMax)
                    zMax = edgePoints[i].z;
            }

            //set the dimensions of the grid to be an integer multiple of gridSpacing
            xMin = Mathf.Floor(xMin / gridSpacing) * gridSpacing;
            xMax = Mathf.Ceil(xMax / gridSpacing) * gridSpacing;
            zMin = Mathf.Floor(zMin / gridSpacing) * gridSpacing;
            zMax = Mathf.Ceil(zMax / gridSpacing) * gridSpacing;

            // Determine the number of grid cells needed in the x and z directions
            int xCells = Mathf.RoundToInt((xMax - xMin) / gridSpacing);
            int zCells = Mathf.RoundToInt((zMax - zMin) / gridSpacing);

            // Compute the offset to center the grid
            float xOffset = xMin + (xMax - xMin - xCells * gridSpacing) / 2;
            float zOffset = zMin + (zMax - zMin - zCells * gridSpacing) / 2;

            // Create the grid to return
            Vector3[,] grid = new Vector3[xCells, zCells];

            // Fill the grid with evenly spaced points
            for (int x = 0; x < xCells; x++)
            {
                for (int z = 0; z < zCells; z++)
                {
                    float xPos = xOffset + x * gridSpacing;
                    float zPos = zOffset + z * gridSpacing;
                    grid[x, z] = new Vector3(xPos, elevation, zPos);
                }
            }
            return grid;
        }


        // public static Vector3[,] GenerateGrid(Vector3[] edgePoints, float gridSpacing, float elevation)
        // {
        //     // Sort the edgePoints by their x and z values
        //     Array.Sort(edgePoints, (a, b) =>
        //     {
        //         if (a.x != b.x)
        //             return a.x.CompareTo(b.x);
        //         else
        //             return a.z.CompareTo(b.z);
        //     });

        //     float xMin = edgePoints[0].x;
        //     float xMax = edgePoints[0].x;
        //     float zMin = edgePoints[0].z;
        //     float zMax = edgePoints[0].z;

        //     // Determine the minimum and maximum x and z values from the edgePoints
        //     for (int i = 1; i < edgePoints.Length; i++)
        //     {
        //         if (edgePoints[i].x < xMin)
        //             xMin = edgePoints[i].x;

        //         if (edgePoints[i].x > xMax)
        //             xMax = edgePoints[i].x;

        //         if (edgePoints[i].z < zMin)
        //             zMin = edgePoints[i].z;

        //         if (edgePoints[i].z > zMax)
        //             zMax = edgePoints[i].z;
        //     }

        //     // Determine the number of grid cells needed in the x and z directions
        //     int xCells = Mathf.CeilToInt((xMax - xMin) / gridSpacing);
        //     int zCells = Mathf.CeilToInt((zMax - zMin) / gridSpacing);

        //     //Compute the offset to make the grid fits exactly with the edgePoints
        //     float xOffset = xMin - (gridSpacing / 2);
        //     float zOffset = zMin - (gridSpacing / 2);

        //     // Create the grid to return
        //     Vector3[,] grid = new Vector3[xCells, zCells];

        //     // Fill the grid with evenly spaced points
        //     for (int x = 0; x < xCells; x++)
        //     {
        //         for (int z = 0; z < zCells; z++)
        //         {
        //             float xPos = xOffset + x * gridSpacing;
        //             float zPos = zOffset + z * gridSpacing;
        //             grid[x, z] = new Vector3(xPos, elevation, zPos);
        //         }
        //     }

        //     return grid;
        // }




        // public static Vector3[,] GenerateGrid(Vector3[] edgePoints, float gridSpacing, float elevation)
        // {
        //     // Sort the edgePoints by their x and z values
        //     Array.Sort(edgePoints, (a, b) =>
        //     {
        //         if (a.x != b.x)
        //             return a.x.CompareTo(b.x);
        //         else
        //             return a.z.CompareTo(b.z);
        //     });

        //     float xMin = edgePoints[0].x;
        //     float xMax = edgePoints[0].x;
        //     float zMin = edgePoints[0].z;
        //     float zMax = edgePoints[0].z;

        //     // Determine the minimum and maximum x and z values from the edgePoints
        //     for (int i = 1; i < edgePoints.Length; i++)
        //     {
        //         if (edgePoints[i].x < xMin)
        //             xMin = edgePoints[i].x;

        //         if (edgePoints[i].x > xMax)
        //             xMax = edgePoints[i].x;

        //         if (edgePoints[i].z < zMin)
        //             zMin = edgePoints[i].z;

        //         if (edgePoints[i].z > zMax)
        //             zMax = edgePoints[i].z;
        //     }

        //     // Determine the number of grid cells needed in the x and z directions
        //     int xCells = Mathf.RoundToInt((xMax - xMin) / gridSpacing);
        //     int zCells = Mathf.RoundToInt((zMax - zMin) / gridSpacing);

        //     // Create the grid to return
        //     Vector3[,] grid = new Vector3[xCells, zCells];

        //     // Fill the grid with evenly spaced points
        //     for (int x = 0; x < xCells; x++)
        //     {
        //         for (int z = 0; z < zCells; z++)
        //         {
        //             float xPos = xMin + x * gridSpacing;
        //             float zPos = zMin + z * gridSpacing;
        //             grid[x, z] = new Vector3(xPos, elevation, zPos);
        //         }
        //     }

        //     return grid;
        // }

        public static void DrawRectangleInGizmos(Vector3[] corners)
        {

            if (corners == null) return;

            // Draw lines connecting the corners
            Gizmos.color = Color.black;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
                Gizmos.DrawLine(corners[i + 4], corners[((i + 1) % 4) + 4]);
                Gizmos.DrawLine(corners[i], corners[i + 4]);
            }


            // if (corners == null) return;

            // for (int i = 0; i < corners.Length; i++)
            // {
            //     Gizmos.DrawSphere(corners[i], 0.1f);
            // }
            // for (int i = 0; i < corners.Length; i++)
            // {
            //     int j = (i + 1) % corners.Length;
            //     Gizmos.DrawLine(corners[i], corners[j]);
            // }
        }

        public static void DrawGridInGizmos(Vector3[,] grid)
        {
            if (grid == null)
                return;

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(1); z++)
                {
                    Vector3 center = grid[x, z];
                    Vector3 right = grid[x + 1, z];
                    Vector3 top = grid[x, z + 1];
                    Vector3 topRight = grid[x + 1, z + 1];

                    // Draw lines around the cell
                    Gizmos.DrawLine(center, right);
                    Gizmos.DrawLine(center, top);
                    Gizmos.DrawLine(top, topRight);
                    Gizmos.DrawLine(right, topRight);
                }
            }
        }
        public static void DrawGridInGizmos(Vector3[,] grid, Transform transform)
        {
            if (grid == null)
                return;

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(1); z++)
                {
                    Vector3 center = transform.TransformPoint(grid[x, z]);
                    Vector3 right = transform.TransformPoint(grid[x + 1, z]);
                    Vector3 top = transform.TransformPoint(grid[x, z + 1]);
                    Vector3 topRight = transform.TransformPoint(grid[x + 1, z + 1]);

                    // Draw lines around the cell
                    Gizmos.DrawLine(center, right);
                    Gizmos.DrawLine(center, top);
                    Gizmos.DrawLine(top, topRight);
                    Gizmos.DrawLine(right, topRight);
                }
            }
        }
        public static void DrawGridPointsInGizmos(Vector3[,] grid, float radius, Transform transform)
        {
            if (grid == null)
                return;

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int z = 0; z < grid.GetLength(1); z++)
                {
                    Vector3 position = transform.TransformPoint(grid[x, z]);
                    Gizmos.DrawWireSphere(position, radius * 2f);
                    // Gizmos.DrawWireSphere(position, (radius * 0.5f) * transform.lossyScale.x);
                }
            }
        }

        public static void DrawHexagonPointLinesInGizmos(Vector3[] corners, Transform transform)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 pointA = transform.TransformPoint(corners[i]);
                Vector3 pointB = transform.TransformPoint(corners[(i + 1) % corners.Length]);
                Gizmos.DrawLine(pointA, pointB);
            }
        }
        public static void DrawHexagonPointLinesInGizmos(Vector3[] corners)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 pointA = corners[i];
                Vector3 pointB = corners[(i + 1) % corners.Length];
                Gizmos.DrawLine(pointA, pointB);
            }
        }

        public static Vector3[] GeneratePath(Vector3[,] grid)
        {
            int xCells = grid.GetLength(0);
            int zCells = grid.GetLength(1);

            // Randomly select the starting point on one edge of the grid
            Vector3 start = grid[0, UnityEngine.Random.Range(0, zCells)];
            Vector3 end = grid[xCells - 1, UnityEngine.Random.Range(0, zCells)];

            // Generate a path from the starting point to the other edge
            Vector3[] path = new Vector3[xCells];
            Vector3 current = start;
            path[0] = current;

            // Create a list of all the possible next steps in the path
            List<Vector3> nextSteps = new List<Vector3>();

            // iterate through the cells and create the path
            for (int i = 1; i < xCells; i++)
            {
                if (current.x < end.x)
                    nextSteps.Add(grid[(int)current.x + 1, (int)current.z]);
                if (current.x > end.x)
                    nextSteps.Add(grid[(int)current.x - 1, (int)current.z]);
                if (current.z < end.z)
                    nextSteps.Add(grid[(int)current.x, (int)current.z + 1]);
                if (current.z > end.z)
                    nextSteps.Add(grid[(int)current.x, (int)current.z - 1]);

                if (nextSteps.Count > 0)
                {
                    current = nextSteps[UnityEngine.Random.Range(0, nextSteps.Count)];
                    path[i] = current;
                    nextSteps.Clear();
                }
                else
                {
                    break;
                }

                if (current == end)
                {
                    break;
                }
            }

            return path;
        }



        // This method takes a 2D grid of hexagons, the x and y coordinates of the target hexagon and returns a List of neighboring hexagons 
        public static List<Hexagon> GetNeighbors(Hexagon[,] grid, int x, int y)
        {
            List<Hexagon> neighbors = new List<Hexagon>();
            int gridWidth = grid.GetLength(0);
            int gridHeight = grid.GetLength(1);

            // Check the hexagon on the top-left 
            if (x - 1 >= 0 && y - 1 >= 0)
            {
                neighbors.Add(grid[x - 1, y - 1]);
            }
            // Check the hexagon on the top-right
            if (x + 1 < gridWidth && y - 1 >= 0)
            {
                neighbors.Add(grid[x + 1, y - 1]);
            }
            // Check the hexagon on the left
            if (x - 1 >= 0)
            {
                neighbors.Add(grid[x - 1, y]);
            }
            // Check the hexagon on the right
            if (x + 1 < gridWidth)
            {
                neighbors.Add(grid[x + 1, y]);
            }
            // Check the hexagon on the bottom-left
            if (x - 1 >= 0 && y + 1 < gridHeight)
            {
                neighbors.Add(grid[x - 1, y + 1]);
            }
            // Check the hexagon on the bottom-right
            if (x + 1 < gridWidth && y + 1 < gridHeight)
            {
                neighbors.Add(grid[x + 1, y + 1]);
            }
            return neighbors;
        }



    }

}