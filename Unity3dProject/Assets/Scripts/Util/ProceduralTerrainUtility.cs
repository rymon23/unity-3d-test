using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    public static class ProceduralTerrainUtility
    {
        public static float CalculateAverageDistanceBetweenPoints(List<Vector3> points)
        {
            if (points == null || points.Count < 2)
            {
                return 0f; // Return 0 if there are less than 2 points
            }

            float totalDistance = 0f;
            int numPoints = points.Count;

            for (int i = 0; i < numPoints; i++)
            {
                for (int j = i + 1; j < numPoints; j++)
                {
                    float distance = Vector3.Distance(points[i], points[j]);
                    totalDistance += distance;
                }
            }

            // Calculate the average distance
            float averageDistance = totalDistance / (numPoints * (numPoints - 1) / 2);

            return averageDistance;
        }

        public static float CalculateAverageDistanceBetweenPointsXZ(List<Vector3> points)
        {
            if (points == null || points.Count < 2)
            {
                return 0f; // Return 0 if there are less than 2 points
            }

            float totalDistance = 0f;
            int numPoints = points.Count;

            for (int i = 0; i < numPoints; i++)
            {
                for (int j = i + 1; j < numPoints; j++)
                {
                    Vector2 point1XZ = new Vector2(points[i].x, points[i].z);
                    Vector2 point2XZ = new Vector2(points[j].x, points[j].z);
                    float distance = Vector2.Distance(point1XZ, point2XZ);
                    totalDistance += distance;
                }
            }

            // Calculate the average distance
            float averageDistance = totalDistance / (numPoints * (numPoints - 1) / 2);

            return averageDistance;
        }


        public static int[] GenerateTerrainSurfaceTriangles(int numVertices, HashSet<int> indices)
        {
            // Create a list to store the triangle indices
            List<int> triangles = new List<int>();

            // Convert the HashSet to a list for indexing
            List<int> indicesList = new List<int>(indices);

            // Iterate over the indices and generate triangles
            for (int i = 0; i < indicesList.Count; i++)
            {
                int vertexIndex = indicesList[i];

                // Check if the current vertex index is valid
                if (vertexIndex >= 0 && vertexIndex < numVertices * numVertices)
                {
                    // Get the next two vertex indices in the indices collection
                    int nextVertexIndex1 = i + 1 < indicesList.Count ? indicesList[i + 1] : -1;
                    int nextVertexIndex2 = i + 2 < indicesList.Count ? indicesList[i + 2] : -1;

                    // Check if the next two vertex indices are valid
                    if (nextVertexIndex1 >= 0 && nextVertexIndex1 < numVertices * numVertices &&
                        nextVertexIndex2 >= 0 && nextVertexIndex2 < numVertices * numVertices)
                    {
                        // Add the triangle indices
                        triangles.Add(vertexIndex);
                        triangles.Add(nextVertexIndex2);
                        triangles.Add(nextVertexIndex1);
                    }
                }
            }

            return triangles.ToArray();
        }

        public static int[] GenerateTerrainTriangles(Vector2[,] worldspaceVertexKeys, HashSet<Vector2> meshTraingleExcludeList = null)
        {
            int numVerticesX = worldspaceVertexKeys.GetLength(0);
            int numVerticesZ = worldspaceVertexKeys.GetLength(1);
            // Create an array to store the triangle indices
            int[] triangles = new int[(numVerticesX - 1) * (numVerticesZ - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numVerticesX - 1; x++)
            {
                for (int z = 0; z < numVerticesZ - 1; z++)
                {
                    if (meshTraingleExcludeList != null && meshTraingleExcludeList.Contains(worldspaceVertexKeys[x, z]))
                    {
                        // Debug.LogError("meshTraingleExcludeList - exclude:  " + worldspaceVertexKeys[x, z]);

                        continue;
                    }
                    // if (meshTraingleExcludeList != null && meshTraingleExcludeList.Contains(worldspaceVertexKeys[z, x]))
                    // {
                    //     Debug.LogError("meshTraingleExcludeList - exclude:  " + worldspaceVertexKeys[z, x]);

                    //     continue;
                    // }

                    triangles[index++] = x + z * numVerticesX;
                    triangles[index++] = x + 1 + z * numVerticesX;
                    triangles[index++] = x + (z + 1) * numVerticesX;

                    triangles[index++] = x + 1 + z * numVerticesX;
                    triangles[index++] = x + 1 + (z + 1) * numVerticesX;
                    triangles[index++] = x + (z + 1) * numVerticesX;
                }
            }
            return triangles;
        }


        // public static int[] GenerateTerrainTriangles(Vector2[,] worldspaceVertexKeys, HashSet<(int, int)> meshTriangleExcludeList = null)
        // {
        //     int numVerticesX = worldspaceVertexKeys.GetLength(0);
        //     int numVerticesZ = worldspaceVertexKeys.GetLength(1);
        //     int numTriangles = (numVerticesX - 1) * (numVerticesZ - 1) * 6;
        //     int[] triangles = new int[numTriangles];

        //     int index = 0;
        //     for (int x = 0; x < numVerticesX - 1; x++)
        //     {
        //         for (int z = 0; z < numVerticesZ - 1; z++)
        //         {
        //             if (meshTriangleExcludeList != null && meshTriangleExcludeList.Contains((z, x)))
        //             {
        //                 continue;
        //             }

        //             int vertexIndex = x + z * numVerticesX;
        //             triangles[index++] = vertexIndex;
        //             triangles[index++] = vertexIndex + 1;
        //             triangles[index++] = vertexIndex + numVerticesZ;

        //             triangles[index++] = vertexIndex + 1;
        //             triangles[index++] = vertexIndex + numVerticesX + 1;
        //             triangles[index++] = vertexIndex + numVerticesX;
        //         }
        //     }

        //     return triangles;
        // }


        // public static int[] GenerateTerrainTriangles(Vector2[,] worldspaceVertexKeys, HashSet<(int, int)> meshTraingleExcludeList = null)
        // {
        //     int numVerticesX = worldspaceVertexKeys.GetLength(0);
        //     int numVerticesZ = worldspaceVertexKeys.GetLength(1);
        //     int numVertices = (numVerticesX - 1) * (numVerticesZ - 1) * 6;
        //     int[] triangles = new int[numVertices];

        //     int index = 0;
        //     for (int x = 0; x < numVerticesX - 1; x++)
        //     {
        //         for (int z = 0; z < numVerticesZ - 1; z++)
        //         {
        //             // if (meshTraingleExcludeList != null && meshTraingleExcludeList.Contains((z, x)))
        //             // {
        //             //     continue;
        //             // }

        //             triangles[index++] = x + z * numVertices;
        //             triangles[index++] = x + 1 + z * numVertices;
        //             triangles[index++] = x + (z + 1) * numVertices;

        //             triangles[index++] = x + 1 + z * numVertices;
        //             triangles[index++] = x + 1 + (z + 1) * numVertices;
        //             triangles[index++] = x + (z + 1) * numVertices;
        //         }
        //     }

        //     return triangles;
        // }

        // public static int[] GenerateTerrainTriangles(Vector2[,] worldspaceVertexKeys, HashSet<(int, int)> meshTraingleExcludeList = null)
        // {
        //     int numVertices = worldspaceVertexKeys.GetLength(0) > worldspaceVertexKeys.GetLength(1) ? worldspaceVertexKeys.GetLength(0) : worldspaceVertexKeys.GetLength(1);
        //     // Create an array to store the triangle indices
        //     int[] triangles = new int[(numVertices - 1) * (numVertices - 1) * 6];

        //     // Iterate through the grid and create the triangles
        //     int index = 0;
        //     for (int x = 0; x < numVertices - 1; x++)
        //     {
        //         for (int z = 0; z < numVertices - 1; z++)
        //         {

        //             // if (meshTraingleExcludeList != null && meshTraingleExcludeList.Contains((z, x))) continue;

        //             triangles[index++] = x + z * numVertices;
        //             triangles[index++] = x + 1 + z * numVertices;
        //             triangles[index++] = x + (z + 1) * numVertices;

        //             triangles[index++] = x + 1 + z * numVertices;
        //             triangles[index++] = x + 1 + (z + 1) * numVertices;
        //             triangles[index++] = x + (z + 1) * numVertices;
        //         }
        //     }
        //     return triangles;
        // }


        // public static int[] GenerateTerrainTriangles(TerrainVertex[,] vertexGrid, HashSet<(int, int)> excludeList = null)
        // {
        //     int numVerticesX = vertexGrid.GetLength(0);
        //     int numVerticesZ = vertexGrid.GetLength(1);
        //     int numTriangles = (numVerticesX - 1) * (numVerticesZ - 1) * 6;
        //     int[] triangles = new int[numTriangles];

        //     int index = 0;
        //     for (int x = 0; x < numVerticesX - 1; x++)
        //     {
        //         for (int z = 0; z < numVerticesZ - 1; z++)
        //         {
        //             if (excludeList != null && excludeList.Contains((z, x)))
        //             {
        //                 continue;
        //             }

        //             int vertexIndex = x + z * numVerticesX;
        //             triangles[index++] = vertexIndex;
        //             triangles[index++] = vertexIndex + 1;
        //             triangles[index++] = vertexIndex + numVerticesZ;

        //             triangles[index++] = vertexIndex + 1;
        //             triangles[index++] = vertexIndex + numVerticesX + 1;
        //             triangles[index++] = vertexIndex + numVerticesX;
        //         }
        //     }

        //     return triangles;
        // }

        // public static int[] GenerateTerrainTriangles(TerrainVertex[,] vertexGrid, HashSet<(int, int)> excludeList = null)
        // {
        //     int numVerticesX = vertexGrid.GetLength(0);
        //     int numVerticesZ = vertexGrid.GetLength(1);
        //     // Create an array to store the triangle indices
        //     int[] triangles = new int[(numVerticesX - 1) * (numVerticesZ - 1) * 6];

        //     // Iterate through the grid and create the triangles
        //     int index = 0;
        //     for (int x = 0; x < numVerticesX - 1; x++)
        //     {
        //         for (int z = 0; z < numVerticesZ - 1; z++)
        //         {
        //             if (excludeList != null && excludeList.Contains((z, x))) continue;

        //             int topLeftIndex = x + z * numVerticesX;
        //             int topRightIndex = (x + 1) + z * numVerticesX;
        //             int bottomLeftIndex = x + (z + 1) * numVerticesX;
        //             int bottomRightIndex = (x + 1) + (z + 1) * numVerticesX;

        //             triangles[index++] = topLeftIndex;
        //             triangles[index++] = bottomLeftIndex;
        //             triangles[index++] = topRightIndex;

        //             triangles[index++] = topRightIndex;
        //             triangles[index++] = bottomLeftIndex;
        //             triangles[index++] = bottomRightIndex;
        //         }
        //     }
        //     return triangles;
        // }



        public static int[] GenerateTerrainTriangles(TerrainVertex[,] vertexGrid, HashSet<(int, int)> excludeList = null)
        {
            int numVerticesX = vertexGrid.GetLength(0);
            int numVerticesZ = vertexGrid.GetLength(1);
            int numVertices = numVerticesX >= numVerticesZ ? numVerticesX : numVerticesZ;

            // Create an array to store the triangle indices
            int[] triangles = new int[(numVertices - 1) * (numVertices - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numVertices - 1; x++)
            {
                for (int z = 0; z < numVertices - 1; z++)
                {

                    if (excludeList.Contains((z, x))) continue;

                    triangles[index++] = x + z * numVertices;
                    triangles[index++] = x + 1 + z * numVertices;
                    triangles[index++] = x + (z + 1) * numVertices;

                    triangles[index++] = x + 1 + z * numVertices;
                    triangles[index++] = x + 1 + (z + 1) * numVertices;
                    triangles[index++] = x + (z + 1) * numVertices;
                }
            }
            return triangles;
        }

        public static int[] GenerateTerrainTriangles(Vector3[,] vertexGrid)
        {
            int numVertices = vertexGrid.GetLength(0);
            // Create an array to store the triangle indices
            int[] triangles = new int[(numVertices - 1) * (numVertices - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numVertices - 1; x++)
            {
                for (int y = 0; y < numVertices - 1; y++)
                {
                    triangles[index++] = x + y * numVertices;
                    triangles[index++] = x + 1 + y * numVertices;
                    triangles[index++] = x + (y + 1) * numVertices;

                    triangles[index++] = x + 1 + y * numVertices;
                    triangles[index++] = x + 1 + (y + 1) * numVertices;
                    triangles[index++] = x + (y + 1) * numVertices;
                }
            }
            return triangles;
        }

        public static int[] GenerateTerrainTriangles_MT(TerrainVertex[,] vertexGrid, HashSet<(int, int)> excludeList = null)
        {
            int numVertices = vertexGrid.GetLength(0);
            // Create an array to store the triangle indices
            int[] triangles = new int[(numVertices - 1) * (numVertices - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;

            Parallel.For(0, numVertices - 1, x =>
            {
                for (int z = 0; z < numVertices - 1; z++)
                {
                    if (excludeList != null && excludeList.Contains((z, x))) continue;

                    triangles[index++] = x + z * numVertices;
                    triangles[index++] = x + 1 + z * numVertices;
                    triangles[index++] = x + (z + 1) * numVertices;

                    triangles[index++] = x + 1 + z * numVertices;
                    triangles[index++] = x + 1 + (z + 1) * numVertices;
                    triangles[index++] = x + (z + 1) * numVertices;
                }
            });

            return triangles;
        }


        public static Vector2[] GenerateTerrainUVs(TerrainVertex[,] vertexGrid)
        {
            // Get the grid size from the vertex grid
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);

            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

            // Iterate through the vertices and set the UVs of each vertex
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);
                }
            }
            return uvs;
        }
        public static Vector2[] GenerateTerrainUVs(Vector3[,] vertexGrid)
        {
            // Get the grid size from the vertex grid
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);

            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

            // Iterate through the vertices and set the UVs of each vertex
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);
                }
            }
            return uvs;
        }

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