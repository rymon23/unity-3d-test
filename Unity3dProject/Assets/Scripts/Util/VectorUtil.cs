using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;
using ProceduralBase;
using System.Linq;

public static class VectorUtil
{
    public static bool IsPointWithinBounds(Bounds bounds, Vector3 point)
    {
        return point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;
    }
    public static Vector3 RoundVector3To1Decimal(Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x * 10f) / 10f, Mathf.Round(v.y * 10f) / 10f, Mathf.Round(v.z * 10f) / 10f);
    }
    public static Vector3 RoundVector3To1DecimalXZ(Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x * 10f) / 10f, v.y, Mathf.Round(v.z * 10f) / 10f);
    }
    public static Vector2 RoundVector2To1Decimal(Vector2 v)
    {
        return new Vector2(Mathf.Round(v.x * 10f) / 10f, Mathf.Round(v.y * 10f) / 10f);
    }
    public static Vector2Int ToVector2Int(this Vector2 vector)
    {
        int x = Mathf.RoundToInt(vector.x);
        int y = Mathf.RoundToInt(vector.y);
        return new Vector2Int(x, y);
    }
    public static Vector3 ToVector3IntXZ(this Vector3 vector)
    {
        int x = Mathf.RoundToInt(vector.x);
        int z = Mathf.RoundToInt(vector.z);
        return new Vector3(x, vector.y, z);
    }

    public static Vector2 RoundVector2ToNearest10(Vector2 value)
    {
        float roundedX = Mathf.RoundToInt(value.x / 10f) * 10f;
        float roundedY = Mathf.RoundToInt(value.y / 10f) * 10f;
        return new Vector2(roundedX, roundedY);
    }
    public static Vector2 RoundVector2ToNearest5(Vector2 value)
    {
        float roundedX = Mathf.RoundToInt(value.x / 5f) * 5f;
        float roundedY = Mathf.RoundToInt(value.y / 5f) * 5f;
        return new Vector2(roundedX, roundedY);
    }
    public static Vector3 RoundVector3ToNearestValue(Vector3 vector, int value)
    {
        float round = value;
        float roundedX = Mathf.RoundToInt(vector.x / round) * round;
        float roundedZ = Mathf.RoundToInt(vector.z / round) * round;
        return new Vector3(roundedX, vector.y, roundedZ);
    }
    // public static Vector3 RoundVector3ToDyanmicValueXZ(Vector3 position, int size)
    // {
    //     int roundAmount = Mathf.RoundToInt((size / 12) / 5f) * 5;
    //     float roundedX = Mathf.Round(position.x / roundAmount) * roundAmount;
    //     float roundedZ = Mathf.Round(position.z / roundAmount) * roundAmount;
    //     return new Vector3(roundedX, position.y, roundedZ);
    // }

    public static Vector2 Calculate_Coordinate(Vector3 position) => new Vector2(position.x, position.z);
    public static Vector2 Calculate_AproximateCoordinate(Vector2 coord) => ToVector2Int(coord);
    public static Vector2 Calculate_AproximateCoordinate(Vector3 position) => ToVector2Int(new Vector2(position.x, position.z));


    public static float DistanceXZ(Vector3 pointA, Vector3 pointB)
    {
        Vector3 point1XZ = new Vector3(pointA.x, 0f, pointA.z);
        Vector3 point2XZ = new Vector3(pointB.x, 0f, pointB.z);
        return Vector3.Distance(point1XZ, point2XZ);

        // Vector2 pointAXZ = new Vector2(pointA.x, pointA.z);
        // Vector2 pointBXZ = new Vector2(pointB.x, pointB.z);
        // return Vector2.Distance(pointAXZ, pointBXZ);
    }

    public static float DistanceXZ(Vector3 pointA, Vector2 pointB)
    {
        Vector3 pointA_XZ = new Vector3(pointA.x, 0f, pointA.z);
        Vector3 pointB_XZ = new Vector3(pointB.x, 0f, pointB.y);
        return Vector3.Distance(pointA_XZ, pointB_XZ);
    }

    public static List<Vector3> DuplicatePositionsToNewYPos(List<Vector3> basePositions, float offsetY, int times = 1)
    {
        List<Vector3> duplicatedPositions = new List<Vector3>();
        int time = 0;
        do
        {
            time++;
            foreach (Vector3 pos in basePositions)
            {
                Vector3 duplicatedPosition = new Vector3(pos.x, pos.y + (offsetY * time), pos.z);
                duplicatedPositions.Add(duplicatedPosition);
            }
        } while (time < times);

        return duplicatedPositions;
    }

    public static Vector3[] DuplicateCornerPointsTowardsCenter(Vector3 centerPos, Vector3 corner1, Vector3 corner2, float distance)
    {
        Vector3 newPoint = (corner1 + corner2) * 0.5f; // Generate a new point centered between the 2 corners
        Vector3 direction = (centerPos - newPoint).normalized; // Get direction from newPoint to centerPos

        Vector3 point1 = corner1 + direction * distance; // Generate a new point for corner1 in the direction of newPoint to centerPos by the distance value
        Vector3 point2 = corner2 + direction * distance; // Generate a new point for corner2 in the direction of newPoint to centerPos by the distance value

        return new Vector3[] { point1, point2 }; // Return the new corner points as an array of Vector3
    }

    public static List<Vector3> GenerateDottedLineBetweenPoints(Vector3 corner1, Vector3 corner2, int steps = 4)
    {
        List<Vector3> dottedLinePoints = new List<Vector3>();

        float xStep = (corner2.x - corner1.x) / steps;
        float zStep = (corner2.z - corner1.z) / steps;

        for (int i = 0; i <= steps; i++)
        {
            Vector3 newPoint = new Vector3(corner1.x + (i * xStep), corner1.y, corner1.z + (i * zStep));
            dottedLinePoints.Add(newPoint);
        }

        return dottedLinePoints;
    }

    public static List<Vector3> GenerateDottedLineBetweenPoints_Diagonal(Vector3 corner1, Vector3 corner2, int steps = 4)
    {
        List<Vector3> dottedLinePoints = new List<Vector3>();

        // Calculate the distance and direction of the diagonal
        Vector3 diagonal = corner2 - corner1;
        float distance = diagonal.magnitude;
        Vector3 direction = diagonal.normalized;

        // Calculate the distance between each dot along the diagonal
        float dotDistance = distance / (steps + 1);

        // Add the dotted line points
        for (int i = 1; i <= steps; i++)
        {
            Vector3 dotPosition = corner1 + (i * dotDistance * direction);
            dottedLinePoints.Add(dotPosition);
        }

        return dottedLinePoints;
    }

    public static List<Vector3> GenerateDottedLine(List<Vector3> corners, int steps = 4)
    {
        List<Vector3> dottedLinePoints = new List<Vector3>();

        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 currentCorner = corners[i];
            Vector3 nextCorner = corners[(i + 1) % corners.Count]; // Wrap around to the first corner for the last corner

            dottedLinePoints.AddRange(GenerateDottedLineBetweenPoints(currentCorner, nextCorner, steps));
        }

        return dottedLinePoints;
    }

    public static Vector3 GetClosestPoint_XZ(Vector3[] points, Vector3 position)
    {
        Vector3 nearestPoint = Vector3.zero;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float dist = DistanceXZ(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return nearestPoint;
    }

    public static Vector2 GetClosestPoint_XZ(List<Vector2> points, Vector3 position)
    {
        Vector2 nearestPoint = Vector2.positiveInfinity;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = DistanceXZ(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return nearestPoint;
    }

    public static (Vector3, float) GetClosestPoint_XZ_WithDistance(Vector3[] points, Vector3 position)
    {
        Vector3 nearestPoint = Vector3.positiveInfinity;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float dist = DistanceXZ(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return (nearestPoint, nearestDistance);
    }

    public static (Vector3, float) GetClosestPoint_XZ_WithDistance(List<Vector3> points, Vector3 position)
    {
        Vector3 nearestPoint = Vector3.positiveInfinity;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = DistanceXZ(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return (nearestPoint, nearestDistance);
    }

    public static (Vector2, float) GetClosestPoint_XZ_WithDistance(List<Vector2> points, Vector2 position)
    {
        Vector2 nearestPoint = Vector2.positiveInfinity;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector2.Distance(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return (nearestPoint, nearestDistance);
    }
    public static (Vector2, float) GetClosestPoint_XZ_WithDistance(List<Vector2> points, Vector3 position)
    {
        Vector2 nearestPoint = Vector2.positiveInfinity;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float dist = Vector2.Distance(new Vector2(position.x, position.z), points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
            }
        }
        return (nearestPoint, nearestDistance);
    }

    public static (Vector3, float, int) GetClosestPoint_XZ_WithDistanceAndIndex(Vector3[] points, Vector3 position)
    {
        Vector3 nearestPoint = Vector3.zero;
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < points.Length; i++)
        {
            float dist = DistanceXZ(position, points[i]);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPoint = points[i];
                nearestIndex = i;
            }
        }
        return (nearestPoint, nearestDistance, nearestIndex);
    }
    public static float AverageDistanceFromPoints(Vector3 position, List<Vector3> points)
    {
        float totalDistance = 0f;
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            totalDistance += Vector3.Distance(position, points[i]);
        }

        return totalDistance / count;
    }
    public static float AverageDistanceFromPointsXZ(HexagonCellPrototype cell, List<HexagonCellPrototype> cells)
    {
        float totalDistance = 0f;
        int count = cells.Count;

        for (int i = 0; i < count; i++)
        {
            if (cells[i] == cell) continue;

            totalDistance += DistanceXZ(cell.center, cells[i].center);
        }

        return totalDistance / count;
    }

    public static float AverageDistanceFromPointsXZ(Vector3 position, List<Vector3> points)
    {
        float totalDistance = 0f;
        int count = points.Count;

        for (int i = 0; i < count; i++)
        {
            totalDistance += DistanceXZ(position, points[i]);
        }

        return totalDistance / count;
    }

    public static int GetClosestPointIndex(Vector3[] points, Vector2 position)
    {
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;

        for (int i = 0; i < points.Length; i++)
        {
            float dist = Vector2.Distance(position, new Vector2(points[i].x, points[i].z));
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }

    public static Vector3 FindClosestPointToCenteredPosition(Vector3[] points)
    {
        // Calculate the center point
        Vector3 center = Vector3.zero;
        for (int i = 0; i < points.Length; i++)
            center += points[i];
        center /= points.Length;

        // Initialize the closest point and its distance
        Vector3 closestPoint = points[0];
        float closestDistance = Vector3.Distance(center, closestPoint);

        // Iterate through the points and check if any point is closer to the center
        for (int i = 1; i < points.Length; i++)
        {
            float distance = Vector3.Distance(center, points[i]);
            if (distance < closestDistance)
            {
                closestPoint = points[i];
                closestDistance = distance;
            }
        }

        return closestPoint;
    }


    private static Vector3 CalculateSurfaceCenter(Vector3[] vertices)
    {
        Vector3 center = Vector3.zero;

        // Sum up all the vertex positions
        for (int i = 0; i < vertices.Length; i++)
        {
            center += vertices[i];
        }

        // Divide by the total number of vertices to get the average position
        center /= vertices.Length;

        return center;
    }



    public static List<Vector3> GetPointsWithinPolygon(List<Vector3> points, List<Vector3> edgePoints, Vector3 polyCenter)
    {
        List<Vector3> results = new List<Vector3>();
        foreach (Vector3 point in points)
        {
            if (IsPositionWithinPolygon(edgePoints, point))
            {
                results.Add(point);
            }
        }
        return results;
    }

    public static bool IsPositionWithinPolygon(List<Vector3> edgePoints, Vector3 position)
    {
        int numVertices = edgePoints.Count;
        bool isInside = false;

        for (int i = 0, j = numVertices - 1; i < numVertices; j = i++)
        {
            if (((edgePoints[i].z <= position.z && position.z < edgePoints[j].z) ||
                 (edgePoints[j].z <= position.z && position.z < edgePoints[i].z)) &&
                (position.x < (edgePoints[j].x - edgePoints[i].x) * (position.z - edgePoints[i].z) / (edgePoints[j].z - edgePoints[i].z) + edgePoints[i].x))
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }

    public static bool IsPositionWithinPolygon(Vector3[] polygon, Vector3 point)
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

    public static Vector3[,] GenerateVector3Grid(Vector3 centerPosition, int radius, float stepSize = 3f)
    {
        int size = (int)(radius / stepSize) * 2 + 1;
        Vector3[,] grid = new Vector3[size, size];
        int index = 0;
        for (float x = -radius; x <= radius; x += stepSize)
        {
            for (float z = -radius; z <= radius; z += stepSize)
            {
                Vector3 point = new Vector3(centerPosition.x + x, centerPosition.y, centerPosition.z + z);
                grid[index / size, index % size] = point;
                index++;
            }
        }
        return grid;
    }

    public static Vector3[] GenerateGridPoints(Vector3 startingPoint, float size, float radius)
    {
        List<Vector3> points = new List<Vector3>();

        float halfSize = size / 2.0f;
        int numPointsPerAxis = Mathf.FloorToInt(radius * 2.0f / size);
        float stepSize = radius * 2.0f / numPointsPerAxis;

        for (int i = 0; i < numPointsPerAxis; i++)
        {
            for (int j = 0; j < numPointsPerAxis; j++)
            {
                Vector3 point = startingPoint + new Vector3(i * stepSize - radius + halfSize, 0.0f, j * stepSize - radius + halfSize);

                if (Vector3.Distance(startingPoint, point) <= radius)
                {
                    points.Add(point);
                }
            }
        }

        return points.ToArray();
    }

    public static Vector2 CalculateSquareSize(Bounds bounds)
    {
        float squareSizeX = bounds.size.x / bounds.extents.x;
        float squareSizeZ = bounds.size.z / bounds.extents.z;

        return new Vector2(squareSizeX, squareSizeZ);
    }
    // public static Vector2 CalculateSquareSize(Bounds bounds)
    // {
    //     float squareSizeX = bounds.size.x / Mathf.CeilToInt(bounds.size.x);
    //     float squareSizeZ = bounds.size.z / Mathf.CeilToInt(bounds.size.z);

    //     return new Vector2(squareSizeX, squareSizeZ);
    // }
    public static float ConvertToBoundsToSquareMiles(Bounds bounds)
    {
        const float metersToMilesConversionFactor = 0.000621371f;

        float squareMeters = bounds.size.x * bounds.size.z;
        float squareMiles = squareMeters * metersToMilesConversionFactor * metersToMilesConversionFactor;

        return squareMiles;
    }

    public static Vector2 ConvertToSquareMiles(Vector2 squareSize, float unitToMileConversionFactor)
    {
        float squareSizeInMilesX = squareSize.x * unitToMileConversionFactor;
        float squareSizeInMilesZ = squareSize.y * unitToMileConversionFactor;

        return new Vector2(squareSizeInMilesX, squareSizeInMilesZ);
    }

    public static float ConvertToSquareMiles(Vector2 squareSize)
    {
        // Calculate the area in square meters
        float squareMeters = squareSize.x * squareSize.y;

        // Convert square meters to square miles using the MetersToMiles conversion method
        float squareMiles = MetersToMiles(squareMeters);

        return squareMiles;
    }

    public static float MetersToFeet(float meters)
    {
        float feet = meters * 3.28084f;
        return feet;
    }

    public static float MetersToMiles(float meters)
    {
        float miles = meters * 0.000621371f;
        return miles;
    }



    public static Vector3 CalculateCenterPositionFromPoints(Vector3[] points)
    {
        // Calculate the center point
        Vector3 center = Vector3.zero;
        for (int i = 0; i < points.Length; i++)
            center += points[i];
        center /= points.Length;

        return center;
    }
    public static Vector3 CalculateCenterPositionFromPoints(List<Vector3> points)
    {
        // Calculate the center point
        Vector3 center = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
            center += points[i];
        center /= points.Count;

        return center;
    }

    // public static List<Vector3> HexagonCornersToRectangleCorners(Vector3 centerPos, int size)
    // {
    //     Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
    //     List<Vector3> cornersForWidth = new List<Vector3>();
    //     cornersForWidth.Add(corners[2]);
    //     cornersForWidth.Add(corners[5]);

    //     List<Vector3> cornersForHeight = new List<Vector3>();
    //     cornersForHeight.Add(corners[0]);
    //     cornersForHeight.Add(corners[4]);

    //     List<Vector3> rectangleCorners = new List<Vector3>();

    //     float width = DistanceXZ(cornersForWidth[0],cornersForWidth[1]);
    //     float height = DistanceXZ(cornersForHeight[0],cornersForHeight[1]);



    //     return rectangleCorners;
    // }

    public static List<Vector3> HexagonCornersToRectangleCorners(Vector3 centerPos, int size)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(corners);


        List<Vector3> cornersForWidth = new List<Vector3>();
        cornersForWidth.Add(corners[0]);
        cornersForWidth.Add(corners[3]);

        List<Vector3> cornersForHeight = new List<Vector3>();
        cornersForHeight.Add(corners[1]);
        cornersForHeight.Add(corners[5]);

        List<Vector3> rectangleCorners = new List<Vector3>();

        float halfDistW = DistanceXZ(cornersForWidth[0], centerPos) / 2f;
        float halfDistH = DistanceXZ(sides[3], centerPos);

        float width = DistanceXZ(cornersForWidth[0], cornersForWidth[1]);
        float height = DistanceXZ(cornersForHeight[0], cornersForHeight[1]);

        Vector3 topCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[5].z - halfDistH);
        Vector3 topCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[4].z - halfDistH);

        Vector3 bottomCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[1].z + halfDistH);
        Vector3 bottomCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[2].z + halfDistH);

        rectangleCorners.Add(topCornerA);
        rectangleCorners.Add(topCornerB);
        rectangleCorners.Add(bottomCornerA);
        rectangleCorners.Add(bottomCornerB);

        return rectangleCorners;
    }
    public static Dictionary<int, Vector3[]> HexagonCornersToRectangleCorners2(Vector3 centerPos, int size)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(corners);

        List<Vector3> cornersForWidth = new List<Vector3>();
        cornersForWidth.Add(corners[0]);
        cornersForWidth.Add(corners[3]);

        List<Vector3> cornersForHeight = new List<Vector3>();
        cornersForHeight.Add(corners[1]);
        cornersForHeight.Add(corners[5]);

        List<Vector3> rectangleCorners = new List<Vector3>();

        float halfDistW = DistanceXZ(cornersForWidth[0], centerPos) / 2f;
        float halfDistH = DistanceXZ(sides[3], centerPos) / 2f;

        // float width = DistanceXZ(cornersForWidth[0], cornersForWidth[1]);
        // float height = DistanceXZ(cornersForHeight[0], cornersForHeight[1]);

        Vector3 topCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[5].z);
        Vector3 topCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[4].z);

        Vector3 midCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, centerPos.z);
        Vector3 midCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, centerPos.z);

        Vector3 bottomCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[1].z);
        Vector3 bottomCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[2].z);

        // rectangleCorners.Add(topCornerA);
        // rectangleCorners.Add(topCornerB);
        // rectangleCorners.Add(midCornerA);
        // rectangleCorners.Add(midCornerB);
        // rectangleCorners.Add(bottomCornerA);
        // rectangleCorners.Add(bottomCornerB);

        Vector3[] topLeft = new Vector3[4];
        topLeft[0] = topCornerA;
        topLeft[1] = sides[3];
        topLeft[2] = midCornerA;
        topLeft[3] = centerPos;
        Vector3[] topRight = new Vector3[4];
        topRight[0] = sides[3];
        topRight[1] = topCornerB;
        topRight[2] = centerPos;
        topRight[3] = midCornerB;
        Vector3[] bottomLeft = new Vector3[4];
        bottomLeft[0] = midCornerA;
        bottomLeft[1] = centerPos;
        bottomLeft[2] = bottomCornerA;
        bottomLeft[3] = sides[0];
        Vector3[] bottomRight = new Vector3[4];
        bottomRight[0] = centerPos;
        bottomRight[1] = midCornerB;
        bottomRight[2] = sides[0];
        bottomRight[3] = bottomCornerB;

        Dictionary<int, Vector3[]> rectangleVertices = new Dictionary<int, Vector3[]>() {
            {0, topLeft},
            {1, topRight},
            {2, bottomLeft},
            {3, bottomRight},
        };

        return rectangleVertices;
    }

    public static List<Vector3> HexagonCornersToRectangleCorner(Vector3 centerPos, int size)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(corners);

        List<Vector3> cornersForWidth = new List<Vector3>();
        cornersForWidth.Add(corners[0]);
        cornersForWidth.Add(corners[3]);

        List<Vector3> cornersForHeight = new List<Vector3>();
        cornersForHeight.Add(corners[1]);
        cornersForHeight.Add(corners[5]);

        List<Vector3> rectangleCorners = new List<Vector3>();

        float halfDistW = DistanceXZ(cornersForWidth[0], centerPos) / 2f;
        float halfDistH = DistanceXZ(sides[3], centerPos) / 2f;

        Vector3 topCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[5].z);
        Vector3 topCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[4].z);

        Vector3 bottomCornerA = new Vector3(corners[0].x + halfDistW, centerPos.y, corners[1].z);
        Vector3 bottomCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[2].z);

        rectangleCorners.Add(topCornerA);
        rectangleCorners.Add(topCornerB);
        rectangleCorners.Add(bottomCornerA);
        rectangleCorners.Add(bottomCornerB);

        return rectangleCorners;
    }

    public static List<Vector3> HexagonCornersToRectangleCorner_2(Vector3 centerPos, int size)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(corners);

        List<Vector3> cornersForWidth = new List<Vector3>();
        cornersForWidth.Add(corners[0]);
        cornersForWidth.Add(corners[3]);

        List<Vector3> cornersForHeight = new List<Vector3>();
        cornersForHeight.Add(corners[1]);
        cornersForHeight.Add(corners[5]);

        List<Vector3> rectangleCorners = new List<Vector3>();

        float halfDistW = DistanceXZ(cornersForWidth[0], centerPos) / 2f;
        float halfDistH = DistanceXZ(sides[3], centerPos) / 2f;

        Vector3 topCornerA = new Vector3(sides[0].x, centerPos.y, corners[5].z);
        Vector3 topCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, corners[4].z);

        Vector3 bottomCornerA = new Vector3(centerPos.x, centerPos.y, centerPos.z);
        Vector3 bottomCornerB = new Vector3(corners[3].x - halfDistW, centerPos.y, centerPos.z);

        rectangleCorners.Add(topCornerA);
        rectangleCorners.Add(topCornerB);
        rectangleCorners.Add(bottomCornerA);
        rectangleCorners.Add(bottomCornerB);

        return rectangleCorners;
    }

    public static List<Vector3> GenerateGridInRectangle(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight, int steps)
    {
        List<Vector3> grid = new List<Vector3>();

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;

            Vector3 left = Vector3.Lerp(bottomLeft, topLeft, t);
            Vector3 right = Vector3.Lerp(bottomRight, topRight, t);

            for (int j = 0; j <= steps; j++)
            {
                float s = (float)j / steps;
                Vector3 vertex = Vector3.Lerp(left, right, s);
                grid.Add(vertex);
            }
        }

        return grid;
    }


    public static List<Vector3> HexagonCornersToRectangle(Vector3 centerPos, int size, int widthMod = 0, int heightMod = 0)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        List<Vector3> rectangleCorners = new List<Vector3>();
        // List<Vector3> cornersForWidth = new List<Vector3>();
        // cornersForWidth.Add(corners[0]);
        // cornersForWidth.Add(corners[3]);

        // List<Vector3> cornersForHeight = new List<Vector3>();
        // cornersForHeight.Add(corners[1]);
        // cornersForHeight.Add(corners[5]);


        // float width = DistanceXZ(cornersForWidth[0], cornersForWidth[1]);
        // float height = DistanceXZ(cornersForHeight[0], cornersForHeight[1]);

        Vector3 topCornerA = new Vector3(corners[0].x + widthMod, centerPos.y, corners[5].z - heightMod);
        Vector3 topCornerB = new Vector3(corners[3].x - widthMod, centerPos.y, corners[4].z - heightMod);
        Vector3 bottomCornerA = new Vector3(corners[0].x + widthMod, centerPos.y, corners[1].z + heightMod);
        Vector3 bottomCornerB = new Vector3(corners[3].x - widthMod, centerPos.y, corners[2].z + heightMod);

        rectangleCorners.Add(topCornerA);
        rectangleCorners.Add(topCornerB);
        rectangleCorners.Add(bottomCornerA);
        rectangleCorners.Add(bottomCornerB);

        return rectangleCorners;
    }
    public static List<Vector3> HexagonCornersToRectangle2(Vector3 centerPos, int size)
    {
        Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        List<Vector3> cornersForWidth = new List<Vector3>();
        cornersForWidth.Add(corners[0]);
        cornersForWidth.Add(corners[3]);

        List<Vector3> cornersForHeight = new List<Vector3>();
        cornersForHeight.Add(corners[1]);
        cornersForHeight.Add(corners[5]);

        List<Vector3> rectangleCorners = new List<Vector3>();

        float width = DistanceXZ(cornersForWidth[0], cornersForWidth[1]) * 2f;
        float height = DistanceXZ(cornersForHeight[0], cornersForHeight[1]);

        Vector3 topCornerA = new Vector3(corners[0].x, centerPos.y, corners[5].z);
        Vector3 topCornerB = new Vector3(corners[3].x, centerPos.y, corners[4].z);
        Vector3 bottomCornerA = new Vector3(corners[0].x, centerPos.y, corners[1].z);
        Vector3 bottomCornerB = new Vector3(corners[3].x, centerPos.y, corners[2].z);

        rectangleCorners.Add(topCornerA);
        rectangleCorners.Add(topCornerB);
        rectangleCorners.Add(bottomCornerA);
        rectangleCorners.Add(bottomCornerB);

        return rectangleCorners;
    }


    public static (bool, float) IsPointWithinEdgeBounds_WithDistance(Vector3 point, Vector3 centerPos, List<Vector3> corners, float radius, int lineDensity = 10, float maxEdgeDistance = 0.66f)
    {
        float distance = VectorUtil.DistanceXZ(point, centerPos);

        if (distance < (radius * 0.93f)) return (true, distance);

        List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(corners, lineDensity);

        (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);

        if (closestPoint == Vector3.zero || edgeDistance > maxEdgeDistance) return (false, distance);
        return (true, distance);
    }

    public static (bool, float) IsPointWithinEdgeBounds_WithDistance2(Vector3 point, Vector3 centerPos, List<Vector3> dottedEdgeLine, float radius, float maxEdgeDistance = 0.66f)
    {
        float distance = VectorUtil.DistanceXZ(point, centerPos);

        if (distance < (radius * 0.93f)) return (true, distance);

        (Vector3 closestPoint, float edgeDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeLine, point);

        if (closestPoint == Vector3.zero || edgeDistance > maxEdgeDistance) return (false, distance);
        return (true, distance);
    }

    public static bool IsPointWithinPolygon(Vector3 point, List<Vector3> corners)
    {
        int intersectCount = 0;
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[(i + 1) % corners.Count];
            if (point.z > Mathf.Min(p1.z, p2.z))
            {
                if (point.z <= Mathf.Max(p1.z, p2.z))
                {
                    if (point.x <= Mathf.Max(p1.x, p2.x))
                    {
                        if (p1.z != p2.z)
                        {
                            float xIntersection = (point.z - p1.z) * (p2.x - p1.x) / (p2.z - p1.z) + p1.x;
                            if (p1.x == p2.x || point.x <= xIntersection)
                            {
                                intersectCount++;
                            }
                        }
                    }
                }
            }
        }
        return intersectCount % 2 != 0;
    }
    public static bool IsPointWithinPolygon(Vector3 point, Vector3[] corners)
    {
        int intersectCount = 0;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[(i + 1) % corners.Length];
            if (point.z > Mathf.Min(p1.z, p2.z))
            {
                if (point.z <= Mathf.Max(p1.z, p2.z))
                {
                    if (point.x <= Mathf.Max(p1.x, p2.x))
                    {
                        if (p1.z != p2.z)
                        {
                            float xIntersection = (point.z - p1.z) * (p2.x - p1.x) / (p2.z - p1.z) + p1.x;
                            if (p1.x == p2.x || point.x <= xIntersection)
                            {
                                intersectCount++;
                            }
                        }
                    }
                }
            }
        }
        return intersectCount % 2 != 0;
    }

    public static bool IsPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        float distanceToLine = Vector3.Distance(lineStart, lineEnd);
        float distanceToPointToStart = Vector3.Distance(lineStart, point);
        float distanceToPointToEnd = Vector3.Distance(lineEnd, point);

        return Mathf.Approximately(distanceToLine, distanceToPointToEnd + distanceToPointToStart);
    }

    public static bool IsPointOnHexagonEdge(Vector3 point, List<Vector3> corners)
    {
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[(i + 1) % corners.Count];

            if (IsPointOnLine(point, p1, p2))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPointOnLineXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Mathf.Approximately((point.x - lineStart.x) * (lineEnd.z - lineStart.z), (lineEnd.x - lineStart.x) * (point.z - lineStart.z))
            && point.x >= Mathf.Min(lineStart.x, lineEnd.x) && point.x <= Mathf.Max(lineStart.x, lineEnd.x)
            && point.z >= Mathf.Min(lineStart.z, lineEnd.z) && point.z <= Mathf.Max(lineStart.z, lineEnd.z);
    }

    public static bool IsPointOnHexagonEdgeXZ(Vector3 point, List<Vector3> corners)
    {
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[(i + 1) % corners.Count];

            if (IsPointOnLineXZ(point, p1, p2))
            {
                return true;
            }
        }

        return false;
    }

    public static float CalculateRadius(List<Vector3> points)
    {
        // Calculate the center of the points
        Vector3 center = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
        {
            center += points[i];
        }
        center /= points.Count;

        // Calculate the distance from the center to the farthest point on the x-z plane
        float radius = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector2.Distance(new Vector2(points[i].x, points[i].z), new Vector2(center.x, center.z));
            if (distance > radius)
            {
                radius = distance;
            }
        }

        return radius;
    }

    public static Bounds CalculateBounds_V2(List<Vector3> points)
    {
        if (points.Count == 0)
        {
            // No points, return an empty bounds
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Vector3 minPoint = points[0];
        Vector3 maxPoint = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 point = points[i];
            minPoint = Vector3.Min(minPoint, point);
            maxPoint = Vector3.Max(maxPoint, point);
        }

        Vector3 size = maxPoint - minPoint;
        Vector3 center = (minPoint + maxPoint) / 2f;

        return new Bounds(center, size);
    }


    public static Bounds CalculateBounds(List<Vector3> corners)
    {
        // Initialize the bounds with the first point in the list
        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        // Loop through all the remaining points in the list
        for (int i = 1; i < corners.Count; i++)
        {
            // Expand the bounds to include the current point
            bounds.Encapsulate(corners[i]);
        }
        // Create a new bounds object that only considers the x and z axis
        Bounds xzBounds = new Bounds(bounds.center, Vector3.zero);
        xzBounds.Encapsulate(new Vector3(bounds.min.x, 0, bounds.min.z));
        xzBounds.Encapsulate(new Vector3(bounds.min.x, 0, bounds.max.z));
        xzBounds.Encapsulate(new Vector3(bounds.max.x, 0, bounds.min.z));
        xzBounds.Encapsulate(new Vector3(bounds.max.x, 0, bounds.max.z));

        return xzBounds;
    }
    public static Bounds CalculateBounds(List<Vector2> corners)
    {
        // Initialize the bounds with the first point in the list
        Bounds bounds = new Bounds(new Vector3(corners[0].x, 0, corners[0].y), Vector3.zero);
        // Loop through all the remaining points in the list
        for (int i = 1; i < corners.Count; i++)
        {
            // Expand the bounds to include the current point
            bounds.Encapsulate(new Vector3(corners[i].x, 0, corners[i].y));
        }
        // Create a new bounds object that only considers the x and z axis
        Bounds xzBounds = new Bounds(bounds.center, Vector3.zero);
        xzBounds.Encapsulate(new Vector3(bounds.min.x, 0, bounds.min.z));
        xzBounds.Encapsulate(new Vector3(bounds.min.x, 0, bounds.max.z));
        xzBounds.Encapsulate(new Vector3(bounds.max.x, 0, bounds.min.z));
        xzBounds.Encapsulate(new Vector3(bounds.max.x, 0, bounds.max.z));

        return xzBounds;
    }

    // public static List<Vector3[]> DivideBounds(Bounds bounds, int slices)
    // {
    //     List<Vector3[]> chunkCorners = new List<Vector3[]>();

    //     // Calculate the size of each slice in each axis
    //     Vector3 sliceSize = bounds.size / slices;

    //     // Divide the bounds evenly by slices
    //     for (int i = 0; i < slices; i++)
    //     {
    //         // Calculate the min and max positions for the current slice
    //         Vector3 minPosition = bounds.min + sliceSize * i;
    //         Vector3 maxPosition = minPosition + sliceSize;

    //         // Generate the corners for the current chunk
    //         Vector3[] corners = GetChunkCorners(minPosition, maxPosition);

    //         // Add the chunk corners to the list
    //         chunkCorners.Add(corners);
    //     }

    //     return chunkCorners;
    // }

    // public static Vector3[] GetChunkCorners(Vector3 minPosition, Vector3 maxPosition)
    // {
    //     Vector3[] corners = new Vector3[4];

    //     corners[0] = minPosition;                                     // Bottom-left
    //     corners[1] = new Vector3(maxPosition.x, minPosition.y, minPosition.z);   // Bottom-right
    //     corners[2] = maxPosition;                                     // Top-right
    //     corners[3] = new Vector3(minPosition.x, minPosition.y, maxPosition.z);   // Top-left

    //     return corners;
    // }

    public static List<Vector3[]> DivideRectangle(Vector3[] corners, int chunks)
    {
        if (corners == null || corners.Length != 4)
        {
            Debug.LogError("Invalid corners array.");
            return null;
        }

        List<Vector3[]> chunkCorners = new List<Vector3[]>();

        // Calculate the number of chunks in each axis
        int chunksX = Mathf.Max(1, chunks);
        int chunksZ = Mathf.Max(1, chunks);

        // Calculate the size of each chunk in each axis
        float chunkSizeX = (corners[1].x - corners[0].x) / chunksX;
        float chunkSizeZ = (corners[2].z - corners[1].z) / chunksZ;

        // Divide the rectangle into smaller chunks
        for (int z = 0; z < chunksZ; z++)
        {
            for (int x = 0; x < chunksX; x++)
            {
                // Calculate the min and max positions for the current chunk
                float minX = corners[0].x + chunkSizeX * x;
                float maxX = minX + chunkSizeX;
                float minZ = corners[0].z + chunkSizeZ * z;
                float maxZ = minZ + chunkSizeZ;

                // Generate the corners for the current chunk
                Vector3[] chunk = new Vector3[4];
                chunk[0] = new Vector3(minX, corners[0].y, minZ);   // Bottom-left
                chunk[1] = new Vector3(minX, corners[1].y, maxZ);   // Top-left
                chunk[2] = new Vector3(maxX, corners[2].y, maxZ);   // Top-right
                chunk[3] = new Vector3(maxX, corners[3].y, minZ);   // Bottom-right

                // Add the chunk corners to the list
                chunkCorners.Add(chunk);
            }
        }

        return chunkCorners;
    }

    public static List<Vector2[]> DivideBoundsIntoChunks(Bounds bounds, int chunks)
    {
        float startX = bounds.min.x;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endZ = bounds.max.z;

        int columns = Mathf.Max(1, chunks);
        int rows = Mathf.Max(1, chunks);

        float xInterval = (endX - startX) / columns;
        float zInterval = (endZ - startZ) / rows;

        List<Vector2[]> chunkCorners = new List<Vector2[]>();

        // Generate the corners of each chunk
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                float x = startX + j * xInterval;
                float z = startZ + i * zInterval;

                Vector2 bottomLeft = new Vector2(x, z);
                Vector2 bottomRight = new Vector2(x + xInterval, z);
                Vector2 topRight = new Vector2(x + xInterval, z + zInterval);
                Vector2 topLeft = new Vector2(x, z + zInterval);

                Vector2[] corners = { bottomLeft, bottomRight, topRight, topLeft };
                chunkCorners.Add(corners);
            }
        }

        return chunkCorners;
    }

    public static List<Vector2[]> DivideBoundsIntoChunks(Bounds bounds, int chunks, float overlap = 1f)
    {
        float startX = bounds.min.x;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endZ = bounds.max.z;

        int columns = Mathf.Max(1, chunks);
        int rows = Mathf.Max(1, chunks);

        float xInterval = (endX - startX) / columns;
        float zInterval = (endZ - startZ) / rows;

        float overlapAmountX = xInterval * overlap;
        float overlapAmountZ = zInterval * overlap;

        List<Vector2[]> chunkCorners = new List<Vector2[]>();

        // Generate the corners of each chunk
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                float x = startX + j * xInterval;
                float z = startZ + i * zInterval;

                float xStart = x - overlapAmountX;
                float zStart = z - overlapAmountZ;

                float xEnd = x + xInterval + overlapAmountX;
                float zEnd = z + zInterval + overlapAmountZ;

                Vector2 bottomLeft = new Vector2(xStart, zStart);
                Vector2 bottomRight = new Vector2(xEnd, zStart);
                Vector2 topRight = new Vector2(xEnd, zEnd);
                Vector2 topLeft = new Vector2(xStart, zEnd);

                Vector2[] corners = { bottomLeft, bottomRight, topRight, topLeft };
                chunkCorners.Add(corners);
            }
        }

        return chunkCorners;
    }



    public static float CalculateBoundingSphereRadius(Bounds bounds)
    {
        // Find the center of the bounds object
        Vector3 center = bounds.center;

        // Find the distance between the center and one of the corners of the bounds object
        Vector3 corner = bounds.extents;
        float distance = Vector3.Distance(center, corner);

        // Return the distance as the radius of the bounding sphere
        return distance;
    }



    public static Bounds GenerateBoundsFromCenter(Vector2 sizeXZ, Vector3 centerPoint)
    {
        float halfWidth = sizeXZ.x / 2f;
        float halfHeight = sizeXZ.y / 2f;

        Vector3 min = new Vector3(centerPoint.x - halfWidth, centerPoint.y, centerPoint.z - halfHeight);
        Vector3 max = new Vector3(centerPoint.x + halfWidth, centerPoint.y, centerPoint.z + halfHeight);

        Bounds bounds = new Bounds(centerPoint, Vector3.zero);
        bounds.SetMinMax(min, max);

        return bounds;
    }

    public static Vector3[] GenerateRectangleCorners(Vector3 centerPoint, float width, float height)
    {
        Vector3[] corners = new Vector3[4];

        // Calculate half-width and half-height
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Calculate the corners
        corners[0] = new Vector3(centerPoint.x - halfWidth, centerPoint.y, centerPoint.z - halfHeight); // Bottom-left corner
        corners[1] = new Vector3(centerPoint.x + halfWidth, centerPoint.y, centerPoint.z - halfHeight); // Bottom-right corner
        corners[2] = new Vector3(centerPoint.x - halfWidth, centerPoint.y, centerPoint.z + halfHeight); // Top-left corner
        corners[3] = new Vector3(centerPoint.x + halfWidth, centerPoint.y, centerPoint.z + halfHeight); // Top-right corner

        return corners;
    }

    public static Vector2 CalculateStepSizes(Bounds bounds)
    {
        Vector2 stepSizes = new Vector2(bounds.size.x, bounds.size.z);
        return stepSizes;
    }


    public static List<Vector2> GenerateDottedGridLines(Bounds bounds, int columns = 3, int rows = 3, int lineDensity = 30)
    {
        float startX = bounds.min.x;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endZ = bounds.max.z;

        float xInterval = (endX - startX) / (columns - 1);
        float zInterval = (endZ - startZ) / (rows - 1);

        int dotsPerSegment = Mathf.Max(1, lineDensity - 1);

        List<Vector2> points = new List<Vector2>();

        // Generate vertical dotted lines
        for (int i = 0; i < columns; i++)
        {
            float x = startX + i * xInterval;

            for (int j = 0; j < dotsPerSegment; j++)
            {
                float z = Mathf.Lerp(startZ, endZ, (float)(j + 1) / lineDensity);

                points.Add(new Vector2(x, z));
            }
        }

        // Generate horizontal dotted lines
        for (int i = 0; i < rows; i++)
        {
            float z = startZ + i * zInterval;

            for (int j = 0; j < dotsPerSegment; j++)
            {
                float x = Mathf.Lerp(startX, endX, (float)(j + 1) / lineDensity);

                points.Add(new Vector2(x, z));
            }
        }

        return points;
    }

    public static List<Vector2> GenerateDottedGridLines(Bounds bounds, float cellWidth, float cellHeight, int lineDensity = 30)
    {
        float startX = bounds.min.x;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endZ = bounds.max.z;

        int columns = Mathf.Max(1, Mathf.FloorToInt((endX - startX) / cellWidth)) + 1;
        int rows = Mathf.Max(1, Mathf.FloorToInt((endZ - startZ) / cellHeight)) + 1;

        float xInterval = (endX - startX) / (columns - 1);
        float zInterval = (endZ - startZ) / (rows - 1);

        int dotsPerSegment = Mathf.Max(1, lineDensity - 1);

        List<Vector2> points = new List<Vector2>();

        // Generate vertical dotted lines
        for (int i = 0; i < columns; i++)
        {
            float x = startX + i * xInterval;

            for (int j = 0; j < dotsPerSegment; j++)
            {
                float z = Mathf.Lerp(startZ, endZ, (float)(j + 1) / lineDensity);

                points.Add(new Vector2(x, z));
            }
        }

        // Generate horizontal dotted lines
        for (int i = 0; i < rows; i++)
        {
            float z = startZ + i * zInterval;

            for (int j = 0; j < dotsPerSegment; j++)
            {
                float x = Mathf.Lerp(startX, endX, (float)(j + 1) / lineDensity);

                points.Add(new Vector2(x, z));
            }
        }

        return points;
    }


    public static List<Vector3> GenerateGridPoints(Bounds bounds, Vector2 stepSizes)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        // Calculate the number of steps along the X and Z axes
        int stepsX = Mathf.CeilToInt(bounds.size.x / stepSizes.x);
        int stepsZ = Mathf.CeilToInt(bounds.size.z / stepSizes.y);

        // Generate the grid points
        for (int x = 0; x <= stepsX; x++)
        {
            for (int z = 0; z <= stepsZ; z++)
            {
                Vector3 point = Vector3.zero + new Vector3(x * stepSizes.x, 0f, z * stepSizes.y);
                gridPoints.Add(point);
            }
        }

        return gridPoints;
    }

    // public static List<Vector3> GenerateGridPoints(Bounds bounds, Vector2 stepSizes)
    // {
    //     List<Vector3> gridPoints = new List<Vector3>();

    //     // Calculate the number of steps along the X and Z axes
    //     int stepsX = Mathf.CeilToInt(bounds.size.x / stepSizes.x);
    //     int stepsZ = Mathf.CeilToInt(bounds.size.z / stepSizes.y);

    //     // Calculate the starting point of the grid (center of the bounds)
    //     Vector3 startPoint = bounds.center - new Vector3(bounds.size.x / 2f, 0f, bounds.size.z / 2f);

    //     // Generate the grid points
    //     for (int x = 0; x <= stepsX; x++)
    //     {
    //         for (int z = 0; z <= stepsZ; z++)
    //         {
    //             Vector3 point = startPoint + new Vector3(x * stepSizes.x, 0f, z * stepSizes.y);
    //             gridPoints.Add(point);
    //         }
    //     }

    //     return gridPoints;
    // }


    public static Dictionary<Vector2, Vector3> GenerateGridPoints(Bounds bounds, float steps)
    {
        // Calculate the minimum x and z positions that are divisible by steps
        float minX = Mathf.Floor(bounds.min.x / steps) * steps;
        float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

        // Calculate the number of steps along the x and z axis based on the spacing
        int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
        int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

        // Initialize a dictionary to hold the grid points
        Dictionary<Vector2, Vector3> gridPoints = new Dictionary<Vector2, Vector3>();

        // Loop through all the steps along the x and z axis, and generate the corresponding grid points
        for (int z = 0; z <= zSteps; z++)
        {
            for (int x = 0; x <= xSteps; x++)
            {
                // Calculate the x and z coordinates of the current grid point
                float xPos = minX + x * steps;
                float zPos = minZ + z * steps;

                Vector3 position = new Vector3(xPos, 0, zPos);

                // Set the current grid point in the dictionary
                gridPoints[new Vector2(xPos, zPos)] = position;
            }
        }

        return gridPoints;
    }

    public static Dictionary<Vector2, Vector3> GenerateGridPoints(Bounds bounds, float steps, Transform transform)
    {
        // Calculate the number of steps along the x and z axis based on the spacing
        int xSteps = Mathf.CeilToInt(bounds.size.x / steps);
        int zSteps = Mathf.CeilToInt(bounds.size.z / steps);

        // Initialize a dictionary to hold the grid points
        Dictionary<Vector2, Vector3> gridPoints = new Dictionary<Vector2, Vector3>();

        // Loop through all the steps along the x and z axis, and generate the corresponding grid points
        for (int z = 0; z <= zSteps; z++)
        {
            for (int x = 0; x <= xSteps; x++)
            {
                // Calculate the x and z coordinates of the current grid point
                float xPos = bounds.min.x + x * steps;
                float zPos = bounds.min.z + z * steps;

                Vector3 position = new Vector3(xPos, 0, zPos);
                Vector3 worldCoord = transform.TransformVector(position); // transform.TransformPoint(position);

                // Set the current grid point in the dictionary
                gridPoints[new Vector2(xPos, zPos)] = worldCoord;
            }
        }

        return gridPoints;
    }

    public static Dictionary<Vector2, Vector3> GenerateGridPoints(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, float steps)
    {
        // Calculate the number of steps along the x and z axis based on the spacing
        float xLength = Vector3.Distance(topLeft, topRight);
        float zLength = Vector3.Distance(topLeft, bottomLeft);
        int xSteps = Mathf.CeilToInt(xLength / steps);
        int zSteps = Mathf.CeilToInt(zLength / steps);

        // Calculate the starting x and z positions that are evenly divisible by the step size
        float startX = Mathf.Floor((Mathf.Min(topLeft.x, bottomLeft.x) - Vector2.zero.x) / steps) * steps + Vector2.zero.x;
        float startZ = Mathf.Floor((Mathf.Min(topLeft.z, topRight.z) - Vector2.zero.y) / steps) * steps + Vector2.zero.y;

        // Initialize a dictionary to hold the grid points
        Dictionary<Vector2, Vector3> gridPoints = new Dictionary<Vector2, Vector3>();

        // Loop through all the steps along the x and z axis, and generate the corresponding grid points
        for (int z = 0; z <= zSteps; z++)
        {
            for (int x = 0; x <= xSteps; x++)
            {
                // Calculate the x and z coordinates of the current grid point
                float xPos = startX + x * steps;
                float zPos = startZ + z * steps;

                Vector3 position = new Vector3(xPos, 0, zPos);

                // Set the current grid point in the dictionary
                gridPoints[new Vector2(xPos, zPos)] = position;
            }
        }

        return gridPoints;
    }

    // public static List<Vector3> CalculateGoldenRatioPattern(Vector2 stepDistance, float radius)
    // {
    //     List<Vector3> points = new List<Vector3>();

    //     float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

    //     float angleIncrement = 360f / goldenRatio;

    //     for (float angle = 0f; angle < 360f; angle += angleIncrement)
    //     {
    //         float xPos = Mathf.Cos(Mathf.Deg2Rad * angle) * 5f; //* radius * stepDistance.x;
    //         float zPos = Mathf.Sin(Mathf.Deg2Rad * angle) * 5f; //* radius * stepDistance.y;
    //         Vector3 point = new Vector3(xPos, 0f, zPos);
    //         points.Add(point);
    //     }

    //     return points;
    // }

    public static List<Vector3> CalculateGoldenRatioPattern(Vector2 stepDistance, float radius)
    {
        List<Vector3> points = new List<Vector3>();

        float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

        float angleIncrement = 360f / goldenRatio;

        float currentRadius = 0f;

        while (currentRadius <= radius)
        {
            for (float angle = 0f; angle < 360f; angle += angleIncrement)
            {
                float xPos = Mathf.Cos(Mathf.Deg2Rad * angle) * currentRadius * stepDistance.x;
                float zPos = Mathf.Sin(Mathf.Deg2Rad * angle) * currentRadius * stepDistance.y;
                Vector3 point = new Vector3(xPos, 0f, zPos);
                points.Add(point);
            }

            currentRadius += Mathf.Max(stepDistance.x, stepDistance.y);
        }

        return points;
    }

    public static List<Vector3> GenerateVoronoiDiagram(Vector3 centerPosition, float radius, int numPoints)
    {
        List<Vector3> points = new List<Vector3>();

        // Generate random points within the specified radius
        for (int i = 0; i < numPoints; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * radius;
            Vector3 point = new Vector3(randomPoint.x, 0f, randomPoint.y);
            points.Add(centerPosition + point);
        }

        return GenerateVoronoiDiagramFromPoints(points);
    }

    public static List<Vector3> GenerateVoronoiDiagram(float radius, int numPoints)
    {
        List<Vector3> points = new List<Vector3>();

        // Generate random points within the specified radius
        for (int i = 0; i < numPoints; i++)
        {
            Vector3 point = Random.insideUnitCircle * radius;
            points.Add(new Vector3(point.x, 0f, point.y));
        }
        return GenerateVoronoiDiagramFromPoints(points);
    }

    public static List<Vector3> GenerateVoronoiDiagramFromPoints(List<Vector3> points)
    {
        // Calculate Voronoi diagram
        List<Vector3> voronoiDiagram = new List<Vector3>();
        foreach (Vector3 point in points)
        {
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;

            foreach (Vector3 otherPoint in points)
            {
                if (otherPoint == point)
                    continue;

                float distance = Vector3.Distance(point, otherPoint);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = otherPoint;
                }
            }

            voronoiDiagram.Add(closestPoint);
        }
        return voronoiDiagram;
    }



    public static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Vector3[] corners = new Vector3[4];
        corners[0] = center - new Vector3(extents.x, 0f, extents.z); // Bottom-left
        corners[1] = center + new Vector3(-extents.x, 0f, extents.z); // Top-left
        corners[2] = center + new Vector3(extents.x, 0f, -extents.z); // Top-right
        corners[3] = center + new Vector3(extents.x, 0f, extents.z); // Bottom-right

        return corners;
    }

    public static Vector2[] GetBoundsCornersXZ(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector2[] corners = new Vector2[4];
        corners[0] = new Vector2(min.x, min.z); // Bottom-left corner
        corners[1] = new Vector2(min.x, max.z); // Top-left corner
        corners[2] = new Vector2(max.x, max.z); // Top-right corner
        corners[3] = new Vector2(max.x, min.z); // Bottom-right corner

        return corners;
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
                if (IsPositionWithinPolygon(edgePoints, newPoint))
                {
                    newPoints.Add(newPoint);
                }
                if (newPoints.Count == numPoints) return newPoints.ToArray();
            }
        }
        return newPoints.ToArray();
    }

    public static Vector3[] GeneratePyramid(float size, float height)
    {
        Vector3[] points = new Vector3[5];
        // Bottom four points
        points[0] = new Vector3(-size / 2, 0, size / 2);
        points[1] = new Vector3(size / 2, 0, size / 2);
        points[2] = new Vector3(size / 2, 0, -size / 2);
        points[3] = new Vector3(-size / 2, 0, -size / 2);
        // Top point
        points[4] = new Vector3(0, height, 0);
        return points;
    }

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
    }

    public static void DrawRectangleLines(Vector3[] corners)
    {
        if (corners == null || corners.Length < 4)
        {
            Debug.LogError("Invalid corners array.");
            return;
        }
        // Draw lines connecting the corners
        Gizmos.DrawLine(corners[0], corners[1]); // Bottom-left to bottom-right
        Gizmos.DrawLine(corners[1], corners[3]); // Bottom-right to top-right
        Gizmos.DrawLine(corners[3], corners[2]); // Top-right to top-left
        Gizmos.DrawLine(corners[2], corners[0]); // Top-left to bottom-left
    }

    public static void DrawRectangleLines(Bounds bounds)
    {
        Vector3[] corners = GetBoundsCorners(bounds);

        if (corners == null || corners.Length < 4)
        {
            Debug.LogError("Invalid corners array.");
            return;
        }
        // Draw lines connecting the corners
        Gizmos.DrawLine(corners[0], corners[1]); // Bottom-left to bottom-right
        Gizmos.DrawLine(corners[1], corners[3]); // Bottom-right to top-right
        Gizmos.DrawLine(corners[3], corners[2]); // Top-right to top-left
        Gizmos.DrawLine(corners[2], corners[0]); // Top-left to bottom-left
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
    public static void DrawHexagonPointLinesInGizmos(Vector3[] corners, float elevation)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 pointA = corners[i];
            pointA.y = elevation;
            Vector3 pointB = corners[(i + 1) % corners.Length];
            pointB.y = elevation;
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
