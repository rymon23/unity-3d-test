using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MarchingCubes : MonoBehaviour
{
    // The size of the grid
    public int gridSize = 32;

    // The voxels of the grid
    private bool[,,] voxels;

    // void Update()
    // {
    //     // Initialize the voxels
    //     voxels = new bool[gridSize, gridSize, gridSize];

    //     // Set some voxels to true to create a simple shape
    //     for (int x = 0; x < gridSize; x++)
    //     {
    //         for (int y = 0; y < gridSize; y++)
    //         {
    //             for (int z = 0; z < gridSize; z++)
    //             {
    //                 voxels[x, y, z] = (x > 10 && x < 20) && (y > 10 && y < 20) && (z > 10 && z < 20);
    //             }
    //         }
    //     }

    //     // Generate the mesh using the marching cubes algorithm
    //     Mesh mesh = GenerateMesh();

    //     // Assign the mesh to a MeshFilter component
    //     MeshFilter meshFilter = GetComponent<MeshFilter>();
    //     meshFilter.mesh = mesh;
    // }

    // Mesh GenerateMesh()
    // {
    //     // Initialize the lists for storing the vertices and triangles of the mesh
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();

    //     // Iterate over each voxel in the grid
    //     for (int x = 0; x < gridSize - 1; x++)
    //     {
    //         for (int y = 0; y < gridSize - 1; y++)
    //         {
    //             for (int z = 0; z < gridSize - 1; z++)
    //             {
    //                 // Get the 8 voxels that make up the cube
    //                 bool v0 = voxels[x, y, z];
    //                 bool v1 = voxels[x + 1, y, z];
    //                 bool v2 = voxels[x + 1, y, z + 1];
    //                 bool v3 = voxels[x, y, z + 1];
    //                 bool v4 = voxels[x, y + 1, z];
    //                 bool v5 = voxels[x + 1, y + 1, z];
    //                 bool v6 = voxels[x + 1, y + 1, z + 1];
    //                 bool v7 = voxels[x, y + 1, z + 1];

    //                 // Calculate the index for the current cube
    //                 int cubeIndex = 0;
    //                 if (v0) cubeIndex |= 1;
    //                 if (v1) cubeIndex |= 2;
    //                 if (v2) cubeIndex |= 4;
    //                 if (v3) cubeIndex |= 8;
    //                 if (v4) cubeIndex |= 16;
    //                 if (v5) cubeIndex |= 32;
    //                 if (v6) cubeIndex |= 64;
    //                 if (v7) cubeIndex |= 128;

    //                 // Look up the vertices and triangles for the current cube in the lookup table
    //                 int[] verticesIndex = MarchingCubesTables.vertexTable[cubeIndex];
    //                 int[] trianglesIndex = MarchingCubesTables.edgeTable[cubeIndex];

    //                 // Add the vertices and triangles for the current cube to the lists
    //                 for (int i = 0; i < verticesIndex.Length; i++)
    //                 {
    //                     int index = verticesIndex[i];
    //                     if (index == -1) break;
    //                     vertices.Add(MarchingCubesTables.edgeVertices[index] + new Vector3(x, y, z));
    //                 }
    //                 for (int i = 0; i < trianglesIndex.Length; i += 3)
    //                 {
    //                     triangles.Add(trianglesIndex[i] + vertices.Count - verticesIndex.Length);
    //                     triangles.Add(trianglesIndex[i + 1] + vertices.Count - verticesIndex.Length);
    //                     triangles.Add(trianglesIndex[i + 2] + vertices.Count - verticesIndex.Length);
    //                 }
    //             }
    //         }
    //     }

    //     // Create a new mesh and assign the vertices and triangles
    //     Mesh mesh = new Mesh();
    //     mesh.vertices = vertices.ToArray();
    //     mesh.triangles = triangles.ToArray();

    //     // Calculate the normals for the mesh
    //     mesh.RecalculateNormals();

    //     return mesh;
    // }
}