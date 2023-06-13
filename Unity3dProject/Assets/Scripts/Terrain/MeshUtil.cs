using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProceduralBase
{

    public static class MeshUtil
    {
        public static float[,] ExtractVertexElevations(Mesh mesh, float steps)
        {
            Vector3[] vertices = mesh.vertices;
            Bounds bounds = mesh.bounds;

            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            int gridSizeX = xSteps + 1;
            int gridSizeZ = zSteps + 1;

            float[,] vertexElevations = new float[gridSizeX, gridSizeZ];

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    int vertexIndex = x * gridSizeX + z;
                    // int vertexIndex = x + z * gridSizeX;

                    float vertexElevation = vertices[vertexIndex].y;
                    vertexElevations[x, z] = vertexElevation;
                }
            }

            return vertexElevations;
        }

        public static List<Vector3> GetVerticesWithinRadiusOfPoints(Mesh mesh, List<Vector3> points, float radius)
        {
            List<Vector3> verticesWithinRadius = new List<Vector3>();

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 vertex = mesh.vertices[i];

                // Check if the vertex is within radius distance of any point in points
                foreach (Vector3 point in points)
                {
                    if (VectorUtil.DistanceXZ(vertex, point) <= radius)
                    {
                        verticesWithinRadius.Add(vertex);
                        break;
                    }
                }
            }

            return verticesWithinRadius;
        }

        public static void ClearMeshOnGameObject(GameObject go)
        {
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null) meshFilter.sharedMesh = null;

            MeshCollider meshCollider = go.GetComponent<MeshCollider>();
            if (meshCollider != null) meshCollider.sharedMesh = null;
        }

        public static GameObject InstantiatePrefabWithMesh(GameObject parentPrefab, Mesh mesh, Vector3 position, string name = "Surface Mesh")
        {
            // Instantiate the parentPrefab
            GameObject instantiatedPrefab = GameObject.Instantiate(parentPrefab, position, Quaternion.identity);

            // Get the MeshFilter component from the instantiatedPrefab
            MeshFilter meshFilter = instantiatedPrefab.GetComponent<MeshFilter>();
            MeshCollider meshCollider = instantiatedPrefab.GetComponent<MeshCollider>();

            mesh.name = name;
            mesh.RecalculateNormals();

            if (meshFilter != null)
            {
                // Assign the mesh to the MeshFilter component
                meshFilter.sharedMesh = mesh;
                meshCollider.sharedMesh = mesh;
            }
            else
            {
                // If there is no MeshFilter component, add one and assign the mesh to it
                meshFilter = instantiatedPrefab.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                meshCollider.sharedMesh = mesh;
            }

            // Return the instantiated prefab with the assigned mesh
            return instantiatedPrefab;
        }


        public static void CreateMeshFromVertices(TerrainVertex[,] vertices, MeshFilter meshFilter)
        {
            List<int> triangles = new List<int>();
            List<Vector3> verticePositions = new List<Vector3>();

            int rowCount = vertices.GetLength(0);
            int columnCount = vertices.GetLength(1);

            for (int row = 0; row < rowCount - 1; row++)
            {
                for (int col = 0; col < columnCount - 1; col++)
                {
                    int bottomLeft = row * columnCount + col;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + columnCount;
                    int topRight = topLeft + 1;

                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);

                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    triangles.Add(topRight);
                }
            }

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertices[row, col].position);
                    // Vector3 worldPosition = meshFilter.transform.TransformPoint(vertices[row, col].position);
                    verticePositions.Add(worldPosition);
                }
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(verticePositions);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;
        }



        public static List<Mesh> DivideMeshIntoChunks(Mesh mesh, int numChunks)
        {
            // Create a list to hold the resulting meshes
            List<Mesh> meshChunks = new List<Mesh>();

            // Calculate the number of vertices per chunk
            int numVerticesPerChunk = mesh.vertices.Length / numChunks;

            // Loop through each chunk
            for (int i = 0; i < numChunks; i++)
            {
                // Calculate the starting index for this chunk
                int startIndex = i * numVerticesPerChunk;

                // Calculate the ending index for this chunk
                int endIndex = (i + 1) * numVerticesPerChunk;
                if (i == numChunks - 1)
                {
                    endIndex = mesh.vertices.Length;
                }

                // Create a new mesh for this chunk
                Mesh meshChunk = new Mesh();

                // Set the vertices, triangles, and UVs for this chunk
                meshChunk.vertices = mesh.vertices.Skip(startIndex).Take(endIndex - startIndex).ToArray();
                meshChunk.triangles = mesh.triangles.Select(t => t - startIndex).Where(t => t >= 0 && t < (endIndex - startIndex)).ToArray();
                meshChunk.uv = mesh.uv.Skip(startIndex).Take(endIndex - startIndex).ToArray();

                // Recalculate the normals and bounds for this chunk
                meshChunk.RecalculateNormals();
                meshChunk.RecalculateBounds();

                // Add this chunk to the list of mesh chunks
                meshChunks.Add(meshChunk);
            }

            // Return the list of mesh chunks
            return meshChunks;
        }


    }
}