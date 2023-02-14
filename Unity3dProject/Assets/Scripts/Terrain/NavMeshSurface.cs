using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEditor;

public class NavMeshSurface : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    public NavMeshBuildSettings buildSettings;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void BuildNavMesh()
    {
        BuildNavMesh();
        // if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

        // NavMeshData navMeshData = new NavMeshData();
        // List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

        // NavMeshBuildSource source = new NavMeshBuildSource();
        // source.shape = NavMeshBuildSourceShape.Mesh;
        // source.sourceObject = meshFilter.sharedMesh;
        // source.transform = transform.localToWorldMatrix;
        // sources.Add(source);

        // Bounds localBounds = new Bounds(Vector3.zero, Vector3.one * 1000000);
        // Vector3 position = transform.position;
        // Quaternion rotation = transform.rotation;

        // navMeshData = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, localBounds, position, rotation);
        // NavMesh.AddNavMeshData(navMeshData);
    }
}
