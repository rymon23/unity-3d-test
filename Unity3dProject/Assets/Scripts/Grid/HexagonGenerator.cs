using System;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

public class HexagonGenerator
{
    public static List<Vector3> GetTopAndBottomEdgePointsOfRectangle(Vector3[] corners)
    {
        List<Vector3> topAndBottomPoints = new List<Vector3>();
        // for (int i = 0; i < corners.Length; i++)
        // {
        //     Vector3 current = corners[i];
        //     Vector3 next = corners[(i + 1) % corners.Length];

        //     topAndBottomPoints.Add(current);
        //     topAndBottomPoints.Add(new Vector3((current.x + next.x) / 2, current.y, (current.z + next.z) / 2));
        // }
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 current = corners[i];
            Vector3 next;
            if (i == corners.Length - 1)
                next = corners[0];
            else
                next = corners[i + 1];

            topAndBottomPoints.Add(current);
            topAndBottomPoints.Add(new Vector3((current.x + next.x) / 2, current.y, (current.z + next.z) / 2));
        }
        return topAndBottomPoints;
    }

    public Vector3[] GeneratePyramid(float size, float height)
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


    public static Vector3[] GenerateHexagonSidePoints(Vector3[] corners)
    {
        Vector3[] hexagonSides = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            // Find the center point between the current corner and the next corner
            Vector3 side = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);
            hexagonSides[(i + 5) % 6] = side; // Places index 0 at the front side
        }
        return hexagonSides;
    }

    public static Vector3[] GenerateHexagonPoints(Vector3 center, float size)
    {
        Vector3[] points = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i;
            float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
            float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);
            points[i] = new Vector3(x, center.y, z);
        }
        return points;
    }

    // public static List<Vector3[]> GenerateHexagons(Vector3 center, float radius, int rows)
    // {
    //     List<Vector3[]> hexagons = new List<Vector3[]>();

    //     // Generate center hexagon
    //     hexagons.Add(GenerateHexagonPoints(center, radius));

    //     // Generate rows of hexagons on the sides of the center hexagon
    //     for (int i = 1; i <= rows; i++)
    //     {
    //         // Calculate the center position of the row
    //         Vector3 rowCenter = center + new Vector3(0f, 0f, 2f * radius * i);

    //         // Calculate the positions of the hexagons in the row
    //         for (int j = 0; j < 3; j++)
    //         {
    //             Vector3 offset = new Vector3(1.5f * radius * (j - 1), 0f, 0f);
    //             Vector3[] hexagon = GenerateHexagonPoints(rowCenter + offset, radius);
    //             hexagons.Add(hexagon);
    //         }
    //     }

    //     return hexagons;
    // }


    public static Mesh CreateHexagonMesh(Vector3[] hexagonCorners)
    {
        // Create new Mesh
        Mesh hexMesh = new Mesh();

        // Assign corner vertices to the Mesh
        hexMesh.vertices = hexagonCorners;

        // Define triangles of the Mesh
        int[] triangles = new int[] {
            2, 1, 0,
            3, 2, 0,
            4, 3, 0,
            5, 4, 0
        };
        hexMesh.triangles = triangles;

        // Recalculate Normals
        hexMesh.RecalculateNormals();

        // Return the created Mesh
        return hexMesh;
    }
    public static GameObject CreateHexagonMesh(Vector3[] hexagonCorners, Material material)
    {
        Mesh hexMesh = new Mesh();
        // Assign corner vertices to the Mesh
        hexMesh.vertices = hexagonCorners;
        // Define triangles of the Mesh
        int[] triangles = new int[] {
            2, 1, 0,
            3, 2, 0,
            4, 3, 0,
            5, 4, 0
        };
        hexMesh.triangles = triangles;
        // Recalculate Normals
        hexMesh.RecalculateNormals();

        // Instantiate a new empty GameObject
        GameObject newObject = new GameObject();
        // Add a MeshFilter component to the new GameObject
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
        // Assign the hexagon mesh to the MeshFilter
        meshFilter.mesh = hexMesh;
        // Add a MeshRenderer component to the new GameObject
        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();

        // Assign the hexagon material to the MeshRenderer
        meshRenderer.material = material;

        return newObject;
    }


    public static List<HexagonTilePrototype> DetermineHexagonTilePrototypeGrideSize(Vector3 position, float radius, int hexagonSize, float adjusterMult = 1.734f, string appendToId = "")
    {
        float bottomCornerX = position.x - (radius * 0.9f);
        float bottomCornerZ = position.z - (radius * 0.9f);
        Vector3 bottomCorner = new Vector3(bottomCornerX, position.y, bottomCornerZ);

        int numHexagons = (int)((radius * 2.2f) / (hexagonSize * 1.5f));
        float numRowsf = ((radius * 2f) / (hexagonSize * adjusterMult));
        int numRows = Mathf.CeilToInt(numRowsf);
        List<HexagonTilePrototype> hexagons = GenerateHexagonTilePrototypeGrid(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId);
        return hexagons;
    }
    public static List<HexagonTilePrototype> DetermineHexagonTilePrototypeGrideSize(Vector3 position, float radius, int hexagonSize, HexagonCell parentCell, float adjusterMult = 1.734f, string appendToId = "")
    {
        float bottomCornerX = position.x - (radius * 0.9f);
        float bottomCornerZ = position.z - (radius * 0.9f);
        Vector3 bottomCorner = new Vector3(bottomCornerX, position.y, bottomCornerZ);

        int numHexagons = (int)((radius * 2.2f) / (hexagonSize * 1.5f));
        float numRowsf = ((radius * 2f) / (hexagonSize * adjusterMult));
        int numRows = Mathf.CeilToInt(numRowsf);
        List<HexagonTilePrototype> hexagons = GenerateHexagonTilePrototypeGrid(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult, appendToId, parentCell);
        return hexagons;
    }
    public static List<HexagonTilePrototype> GenerateHexagonTilePrototypeGrid(int hexagonSize, int numHexagons, int numRows, Vector3 startPos, float adjusterMult = 1.734f, string appendToId = "", HexagonCell parentCell = null)
    {
        // //
        // List<Vector3[]> hexpoints = GenerateHexagons(startPos, hexagonSize, numHexagons);
        // List<HexagonTilePrototype> hexagonTilePrototypes = new List<HexagonTilePrototype>();
        // int idFragment = Mathf.Abs((int)(startPos.z + startPos.x));

        // for (var i = 0; i < hexpoints.Count; i++)
        // {
        //     Vector3[] hexagonPoints = hexpoints[i];

        //     HexagonTilePrototype prototype = new HexagonTilePrototype();
        //     if (parentCell != null)
        //     {
        //         prototype.parentId = parentCell.id;
        //         prototype.id = "p_" + parentCell.id + "-";
        //     }
        //     prototype.id += "X" + hexagonSize + "-" + idFragment + "-" + i;
        //     prototype.name = "Cell_Prototype-" + appendToId + prototype.id;
        //     prototype.size = hexagonSize;
        //     prototype.cornerPoints = hexagonPoints;
        //     prototype.center = HexagonGenerator.GetPolygonCenter(hexagonPoints);
        //     hexagonTilePrototypes.Add(prototype);
        // }
        // return hexagonTilePrototypes;
        // //




        List<HexagonTilePrototype> hexagonTilePrototypes = new List<HexagonTilePrototype>();
        float angle = 60 * Mathf.Deg2Rad;
        float currentX = startPos.x;
        float currentZ = startPos.z;
        float lastZ = startPos.z;

        int idFragment = Mathf.Abs((int)(startPos.z + startPos.x));

        for (int k = 0; k < numRows; k++)
        {
            currentX = startPos.x;
            for (int i = 0; i < numHexagons; i++)
            {
                float adjusterMultB = 0.88f;

                Vector3[] hexagonPoints = new Vector3[6];
                if (i % 2 == 1)
                {
                    currentZ -= hexagonSize * adjusterMultB;
                }
                else
                {
                    currentZ += hexagonSize * adjusterMultB;
                }

                for (int j = 0; j < 6; j++)
                {
                    float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                    float z = currentZ + hexagonSize * Mathf.Sin(angle * j);

                    hexagonPoints[j] = (new Vector3(x, startPos.y, z));

                    if (j == 2) lastZ = z;
                }

                HexagonTilePrototype prototype = new HexagonTilePrototype();
                if (parentCell != null)
                {
                    prototype.parentId = parentCell.id;
                    prototype.id = "p_" + parentCell.id + "-";
                }
                prototype.id += "X" + hexagonSize + "-" + idFragment + "-" + k + i;
                prototype.name = "Cell_Prototype-" + appendToId + prototype.id;
                prototype.size = hexagonSize;
                prototype.cornerPoints = hexagonPoints;
                prototype.center = HexagonGenerator.GetPolygonCenter(hexagonPoints);
                hexagonTilePrototypes.Add(prototype);

                currentX += hexagonSize * 1.5f;
            }
            currentZ += hexagonSize * adjusterMult;
        }
        return hexagonTilePrototypes;
    }
    public static List<List<Vector3>> DetermineGridSize(Vector3 position, float radius, int hexagonSize, float adjusterMult = 1.734f)
    {
        float bottomCornerX = position.x - radius;
        float bottomCornerZ = position.z - radius;
        Vector3 bottomCorner = new Vector3(bottomCornerX, position.y, bottomCornerZ);

        int numHexagons = (int)((radius * 2f) / (hexagonSize * 1.5f));
        float numRowsf = ((radius * 2f) / (hexagonSize * adjusterMult));
        int numRows = Mathf.CeilToInt(numRowsf);
        List<List<Vector3>> hexagons = GenerateHexagonGrid(hexagonSize, numHexagons, numRows, bottomCorner, adjusterMult);
        return hexagons;
    }
    public static List<List<Vector3>> GenerateHexagonGrid(float hexagonSize, int numHexagons, int numRows, Vector3 startPos, float adjusterMult = 1.734f)
    {
        List<List<Vector3>> hexagons = new List<List<Vector3>>();
        float angle = 60 * Mathf.Deg2Rad;
        float currentX = startPos.x;
        float currentZ = startPos.z;
        float lastZ = startPos.z;

        for (int k = 0; k < numRows; k++)
        {
            currentX = startPos.x;
            for (int i = 0; i < numHexagons; i++)
            {
                float adjusterMultB = 0.88f;


                List<Vector3> hexagonPoints = new List<Vector3>();
                if (i % 2 == 1)
                {
                    currentZ -= hexagonSize * adjusterMultB;
                }
                else
                {
                    currentZ += hexagonSize * adjusterMultB;
                }

                for (int j = 0; j < 6; j++)
                {
                    float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                    float z = currentZ + hexagonSize * Mathf.Sin(angle * j);

                    hexagonPoints.Add(new Vector3(x, startPos.y, z));
                    if (j == 2) lastZ = z;
                }
                hexagons.Add(hexagonPoints);

                currentX += hexagonSize * 1.5f;
            }
            currentZ += hexagonSize * adjusterMult;
        }
        return hexagons;
    }

    public static Vector3[] GenerateHexagonChain(float hexagonSize, int numHexagons, int numRows, float elevation = 1f)
    {
        Vector3[] hexagonPoints = new Vector3[numHexagons * 6 * numRows];
        int currentHexagonIndex = 0;
        float angle = 60 * Mathf.Deg2Rad;
        float currentX = 0;
        float currentZ = 0;

        for (int k = 0; k < numRows; k++)
        {
            currentX = 0;
            for (int i = 0; i < numHexagons; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                    float z = currentZ + hexagonSize * Mathf.Sin(angle * j);
                    if (k > 0 && j == 0) z = hexagonPoints[currentHexagonIndex - 6].z;
                    hexagonPoints[currentHexagonIndex++] = new Vector3(x, elevation, z);
                }
                currentX += hexagonSize;
            }
            currentZ += hexagonSize * 1.69f;
        }
        return hexagonPoints;
    }

    public static Vector3[] GenerateHexagons(float hexagonSize, int numHexagons, int numRows)
    {
        Vector3[] hexagonPoints = new Vector3[numHexagons * 6 * numRows];

        float angle = 60 * Mathf.Deg2Rad;
        int currentHexagonIndex = 0;
        for (int k = 0; k < numRows; k++)
        {
            float currentX = hexagonSize * k * 0.5f;
            for (int i = 0; i < numHexagons; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                    float z = hexagonSize * Mathf.Sin(angle * j) + hexagonSize * k;
                    hexagonPoints[currentHexagonIndex++] = new Vector3(x, 0, z);
                }
                currentX += hexagonSize;
            }
        }
        return hexagonPoints;
    }

    public static Vector3[] GenerateHexagonRowsWithinRadius(float hexagonSize, int numHexagons, float radius)
    {
        List<Vector3> hexagonPoints = new List<Vector3>();
        Vector3 currentPosition = Vector3.zero;
        while (currentPosition.y + hexagonSize * Mathf.Sqrt(3) / 2 < radius)
        {
            Vector3[] rowPoints = GenerateHexagonChain(hexagonSize, numHexagons);
            for (int i = 0; i < rowPoints.Length; i++)
            {
                if (Mathf.Sqrt((rowPoints[i].x - currentPosition.x) * (rowPoints[i].x - currentPosition.x) + (rowPoints[i].z - currentPosition.z) * (rowPoints[i].z - currentPosition.z)) < radius)
                {
                    rowPoints[i] += currentPosition;
                    hexagonPoints.Add(rowPoints[i]);
                }
            }
            currentPosition.x += hexagonSize * 3 / 2;
            currentPosition.z += hexagonSize * Mathf.Sqrt(3) / 2;
        }
        return hexagonPoints.ToArray();
    }


    public static Vector3[] GenerateHexagonChain(float hexagonSize, int numHexagons)
    {
        Vector3[] hexagonPoints = new Vector3[numHexagons * 6];

        float angle = 60 * Mathf.Deg2Rad;
        float currentX = 0;
        int currentHexagonIndex = 0;
        for (int i = 0; i < numHexagons; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                float x = currentX + hexagonSize * Mathf.Cos(angle * j);
                float z = hexagonSize * Mathf.Sin(angle * j);
                if (i > 0 && j % 2 == 1)
                {
                    hexagonPoints[currentHexagonIndex++] = hexagonPoints[currentHexagonIndex - 7];
                }
                else
                {
                    hexagonPoints[currentHexagonIndex++] = new Vector3(x, 0, z);
                }
            }
            currentX += hexagonSize;
        }
        return hexagonPoints;
    }
    // public static Vector3[] GenerateHexagonChainPoints(float hexagonSize, int numHexagons)
    // {
    //     Vector3[] hexagonPoints = new Vector3[numHexagons * 6];

    //     float angle = 60 * Mathf.Deg2Rad;
    //     float currentX = 0;

    //     int currentHexagonIndex = 0;
    //     for (int i = 0; i < numHexagons; i++)
    //     {
    //         for (int j = 0; j < 6; j++)
    //         {
    //             float x = currentX + hexagonSize * Mathf.Cos(angle * j);
    //             float z = hexagonSize * Mathf.Sin(angle * j);
    //             hexagonPoints[currentHexagonIndex++] = new Vector3(x, 0, z);
    //         }
    //         currentX += hexagonSize;
    //     }
    //     return hexagonPoints;
    // }

    // public static Vector3[] GenerateHexagonChainPoints(float hexagonSize, int numHexagons)
    // {
    //     Vector3[] hexagonPoints = new Vector3[numHexagons * 6];

    //     // Hexagon corner point on the x-z plane is a distance of hexagonSize from the origin
    //     float cornerPoint = hexagonSize / Mathf.Sqrt(3);

    //     int currentHexagonIndex = 0;
    //     for (int i = 0; i < numHexagons; i++)
    //     {
    //         // calculate hexagon corner points
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i, 0, 0);
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i + cornerPoint, 0, -hexagonSize / 2);
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i + cornerPoint, 0, hexagonSize / 2);
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i + hexagonSize, 0, 0);
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i + cornerPoint, 0, -hexagonSize / 2);
    //         hexagonPoints[currentHexagonIndex++] = new Vector3(hexagonSize * i + cornerPoint, 0, hexagonSize / 2);
    //     }
    //     return hexagonPoints;
    // }


    public static List<Hexagon> GenerateHexagonGrid(Vector3[] edgePoints, float hexSize, float elevation)
    {
        // Create the list for the hexagons
        List<Hexagon> hexagons = new List<Hexagon>();

        // Create a convex hull from the edge points
        List<Vector3> convexHull = ConvexHull.GetConvexHull(edgePoints);
        // Find the rectangle that encompasses the convex hull
        Bounds bounds = GetBounds(convexHull);

        // Calculate the number of hexagons that fit in the bounds
        int hexCountX = (int)(bounds.size.x / hexSize);
        int hexCountZ = (int)(bounds.size.z / hexSize);
        // center the grid
        Vector3 center = bounds.center;

        // Create the hexagons
        for (int x = 0; x < hexCountX; x++)
        {
            for (int z = 0; z < hexCountZ; z++)
            {
                // calculate the position of the hexagon
                Vector3 position = new Vector3(center.x - bounds.size.x / 2 + x * hexSize + hexSize / 2, 0, center.z - bounds.size.z / 2 + z * hexSize + hexSize / 2);
                // Create the triangles
                Vector3 v1 = new Vector3(position.x - hexSize / 2, elevation, position.z - hexSize / 4);
                Vector3 v2 = new Vector3(position.x + hexSize / 2, elevation, position.z - hexSize / 4);
                Vector3 v3 = new Vector3(position.x + hexSize, elevation, position.z);
                Vector3 v4 = new Vector3(position.x + hexSize / 2, elevation, position.z + hexSize / 4);
                Vector3 v5 = new Vector3(position.x + hexSize / 2, elevation, position.z + hexSize / 4);
                Vector3 v6 = new Vector3(position.x - hexSize / 2, elevation, position.z + hexSize / 4);
                Vector3 v7 = new Vector3(position.x - hexSize, elevation, position.z);

                Triangle t1 = new Triangle(v1, v2, v3);
                Triangle t2 = new Triangle(v3, v4, v5);
                Triangle t3 = new Triangle(v5, v6, v7);
                // Create the hexagon
                Hexagon hex = new Hexagon(t1, t2, t3);
                // Add the hexagon to the list
                hexagons.Add(hex);
            }
        }

        return hexagons;
    }


    // public static List<Hexagon> GenerateHexagonGrid(Vector3[] edgePoints, float hexSize)
    // {
    //     // Create the list for the hexagons
    //     List<Hexagon> hexagons = new List<Hexagon>();

    //     // Create a convex hull from the edge points
    //     List<Vector3> convexHull = ConvexHull.GetConvexHull(edgePoints);
    //     // Find the rectangle that encompasses the convex hull
    //     Bounds bounds = GetBounds(convexHull);

    //     // Calculate the number of hexagons that fit in the bounds
    //     int hexCountX = (int)(bounds.size.x / hexSize);
    //     int hexCountZ = (int)(bounds.size.z / hexSize);
    //     // center the grid
    //     Vector3 center = bounds.center;

    //     // Create the hexagons
    //     for (int x = 0; x < hexCountX; x++)
    //     {
    //         for (int z = 0; z < hexCountZ; z++)
    //         {
    //             // calculate the position of the hexagon
    //             Vector3 position = new Vector3(center.x - bounds.size.x / 2 + x * hexSize + hexSize / 2, 0, center.z - bounds.size.z / 2 + z * hexSize + hexSize / 2);
    //             // Create the hexagon
    //             Hexagon hex = new Hexagon(position, hexSize);
    //             // Add the hexagon to the list
    //             hexagons.Add(hex);
    //         }
    //     }

    //     return hexagons;
    // }

    // public static List<Hexagon> GenerateHexagonGrid(Vector3[] edgePoints)
    // {
    //     //1. Calculate the convex hull of the edge points
    //     List<Vector3> convexHull = ConvexHull.GetConvexHull(edgePoints);

    //     //2. Create a list to store the triangles
    //     List<Triangle> triangles = new List<Triangle>();

    //     //3. Iterate through the edges of the convex hull
    //     for (int i = 0; i < convexHull.Count; i++)
    //     {
    //         Vector3 vertex1 = convexHull[i];
    //         Vector3 vertex2 = convexHull[(i + 1) % convexHull.Count];

    //         //4. Calculate the midpoint of the edge
    //         Vector3 midpoint = (vertex1 + vertex2) / 2f;

    //         //5. Find the midpoints of the two edges that share the current vertex
    //         Vector3 midpoint1 = (midpoint + vertex1) / 2f;
    //         Vector3 midpoint2 = (midpoint + vertex2) / 2f;

    //         //6. Create a new triangle from the vertex, midpoint1, and midpoint2
    //         triangles.Add(new Triangle(vertex1, midpoint1, midpoint2));
    //     }

    //     //7. Create a list to store the hexagons
    //     List<Hexagon> hexagons = new List<Hexagon>();

    //     //8. Iterate through the triangles and group every three adjacent triangles to form a hexagon
    //     for (int i = 0; i < triangles.Count; i += 3)
    //     {
    //         Hexagon hexagon = new Hexagon(triangles[i], triangles[i + 1], triangles[i + 2]);
    //         hexagons.Add(hexagon);
    //     }
    //     return hexagons;
    // }

    public static Vector3 GetPolygonCenter(Vector3[] edgePoints)
    {
        Vector3 center = Vector3.zero;
        if (edgePoints != null && edgePoints.Length > 0)
        {
            for (int i = 0; i < edgePoints.Length; i++)
            {
                center += edgePoints[i];
            }
            center /= edgePoints.Length;
        }
        return center;
    }


    private static Bounds GetBounds(List<Vector3> convexHull)
    {
        Bounds bounds = new Bounds(convexHull[0], Vector3.zero);
        for (int i = 1; i < convexHull.Count; i++)
        {
            bounds.Encapsulate(convexHull[i]);
        }
        return bounds;
    }

    public static void DrawHexagonInGizmos(List<Hexagon> hexagons, Transform transform)
    {
        if (hexagons == null || hexagons.Count == 0) return;

        foreach (Hexagon hexagon in hexagons)
        {
            // Transform the vertices of the first triangle to the correct position
            Vector3 vertex1 = transform.TransformPoint(hexagon.t1.vertex1);
            Vector3 vertex2 = transform.TransformPoint(hexagon.t1.vertex2);
            Vector3 vertex3 = transform.TransformPoint(hexagon.t1.vertex3);

            // Draw a line between the first and second vertex of the first triangle
            Gizmos.DrawLine(vertex1, vertex2);
            // Draw a line between the second and third vertex of the first triangle
            Gizmos.DrawLine(vertex2, vertex3);
            // Draw a line between the first and third vertex of the first triangle
            Gizmos.DrawLine(vertex1, vertex3);

            // Transform the vertices of the second triangle to the correct position
            vertex1 = transform.TransformPoint(hexagon.t2.vertex1);
            vertex2 = transform.TransformPoint(hexagon.t2.vertex2);
            vertex3 = transform.TransformPoint(hexagon.t2.vertex3);

            // Draw a line between the first and second vertex of the second triangle
            Gizmos.DrawLine(vertex1, vertex2);
            // Draw a line between the second and third vertex of the second triangle
            Gizmos.DrawLine(vertex2, vertex3);
            // Draw a line between the first and third vertex of the second triangle
            Gizmos.DrawLine(vertex1, vertex3);

            // Transform the vertices of the third triangle to the correct position
            vertex1 = transform.TransformPoint(hexagon.t3.vertex1);
            vertex2 = transform.TransformPoint(hexagon.t3.vertex2);
            vertex3 = transform.TransformPoint(hexagon.t3.vertex3);
            // Draw a line between the first and second vertex of the third triangle
            Gizmos.DrawLine(vertex1, vertex2);
            // Draw a line between the second and third vertex of the third triangle
            Gizmos.DrawLine(vertex2, vertex3);
            // Draw a line between the first and third vertex of the third triangle
            Gizmos.DrawLine(vertex1, vertex3);
        }
    }



    public static void DrawHexagonInGizmos(List<Hexagon> hexagons)
    {
        if (hexagons == null || hexagons.Count == 0) return;

        foreach (Hexagon hexagon in hexagons)
        {
            // Draw a line between the first and second vertex of the first triangle
            Gizmos.DrawLine(hexagon.t1.vertex1, hexagon.t1.vertex2);
            // Draw a line between the second and third vertex of the first triangle
            Gizmos.DrawLine(hexagon.t1.vertex2, hexagon.t1.vertex3);
            // Draw a line between the first and third vertex of the first triangle
            Gizmos.DrawLine(hexagon.t1.vertex1, hexagon.t1.vertex3);

            // Draw a line between the first and second vertex of the second triangle
            Gizmos.DrawLine(hexagon.t2.vertex1, hexagon.t2.vertex2);
            // Draw a line between the second and third vertex of the second triangle
            Gizmos.DrawLine(hexagon.t2.vertex2, hexagon.t2.vertex3);
            // Draw a line between the first and third vertex of the second triangle
            Gizmos.DrawLine(hexagon.t2.vertex1, hexagon.t2.vertex3);

            // Draw a line between the first and second vertex of the third triangle
            Gizmos.DrawLine(hexagon.t3.vertex1, hexagon.t3.vertex2);
            // Draw a line between the second and third vertex of the third triangle
            Gizmos.DrawLine(hexagon.t3.vertex2, hexagon.t3.vertex3);
            // Draw a line between the first and third vertex of the third triangle
            Gizmos.DrawLine(hexagon.t3.vertex1, hexagon.t3.vertex3);
        }
    }
    public static void DrawHexagonPointsInGizmos(List<Hexagon> hexagons, float radius, Transform transform)
    {
        if (hexagons == null || hexagons.Count == 0) return;

        foreach (Hexagon hexagon in hexagons)
        {
            // Vector3 position = transform.TransformPoint(hexagon.center);
            // Gizmos.DrawSphere(position, radius);

            hexagon.DrawHexagonInGizmos();
        }
    }

}


public class Hexagon
{
    public Triangle t1, t2, t3;
    public Vector3 center;

    public Hexagon(Triangle t1, Triangle t2, Triangle t3)
    {
        this.t1 = t1;
        this.t2 = t2;
        this.t3 = t3;
        this.center = CalculateCenter();
    }
    public Vector3 CalculateCenter()
    {
        // Sum the three vertices of the triangles 
        Vector3 center = t1.vertex1 + t1.vertex2 + t1.vertex3 +
                         t2.vertex1 + t2.vertex2 + t2.vertex3 +
                         t3.vertex1 + t3.vertex2 + t3.vertex3;
        // Divide by 9 to find the average position
        center /= 9f;
        return center;
    }

    public void DrawHexagonInGizmos()
    {
        float size = 0.2f;
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(center, size);

        Gizmos.DrawSphere(t1.vertex1, size);
        Gizmos.DrawSphere(t1.vertex2, size);
        Gizmos.DrawSphere(t1.vertex3, size);

        Gizmos.DrawSphere(t2.vertex1, size);
        Gizmos.DrawSphere(t2.vertex2, size);
        Gizmos.DrawSphere(t2.vertex3, size);

        Gizmos.DrawSphere(t3.vertex1, size);
        Gizmos.DrawSphere(t3.vertex2, size);
        Gizmos.DrawSphere(t3.vertex3, size);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center, size);

        // Draw a line between the first and second vertex of the first triangle
        // Gizmos.DrawLine(t1.vertex1, t1.vertex2);
        // // Draw a line between the second and third vertex of the first triangle
        // Gizmos.DrawLine(t1.vertex2, t1.vertex3);
        // // Draw a line between the first and third vertex of the first triangle
        // Gizmos.DrawLine(t1.vertex1, t1.vertex3);

        // // Draw a line between the first and second vertex of the second triangle
        // Gizmos.DrawLine(t2.vertex1, t2.vertex2);
        // // Draw a line between the second and third vertex of the second triangle
        // Gizmos.DrawLine(t2.vertex2, t2.vertex3);
        // // Draw a line between the first and third vertex of the second triangle
        // Gizmos.DrawLine(t2.vertex1, t2.vertex3);

        // // Draw a line between the first and second vertex of the third triangle
        // Gizmos.DrawLine(t3.vertex1, t3.vertex2);
        // // Draw a line between the second and third vertex of the third triangle
        // Gizmos.DrawLine(t3.vertex2, t3.vertex3);
        // // Draw a line between the first and third vertex of the third triangle
        // Gizmos.DrawLine(t3.vertex1, t3.vertex3);
    }

}
public class Triangle
{
    public Vector3 vertex1, vertex2, vertex3;
    public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;
        this.vertex3 = vertex3;
    }
}