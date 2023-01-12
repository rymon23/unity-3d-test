using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using UnityEditor;

[System.Serializable]
public class HexagonTile : MonoBehaviour
{
    private Transform center;
    [SerializeField] private Transform[] _edgePoints; //TODO: rename
    public Vector3[] _cornerPoints; // Clear nonce calculations are done

    [SerializeField] private List<HexagonTile> _neighbors;
    public int size = 12;


    [SerializeField] private int[] sideSocketIds = new int[6];
    public Bounds bounds;

    public GameObject[] socketTextDisplay;


    [Header("Debug Settings")]
    [SerializeField] private bool enableEditMode;
    [SerializeField] private bool showSocketColorMap;
    [SerializeField] private bool showSocketLabels;
    [SerializeField] private bool showNeighbors;
    private bool _showSocketLabels;
    [SerializeField] private bool showPoints;

    [Header("Mesh Generation")]
    [SerializeField] private bool generateMesh;
    [SerializeField] private bool saveMesh;
    private Mesh lastGeneratedMesh;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        center = transform;
    }

    void OnValidate()
    {
        center = transform;

        if (!enableEditMode) return;

        if (_edgePoints == null || _edgePoints.Length == 0)
        {
            Vector3[] corners = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
            _edgePoints = UtilityHelpers.ConvertVector3sToTransformPositions(corners, gameObject.transform);
        }

        if (generateMesh)
        {
            generateMesh = false;

            lastGeneratedMesh = HexagonGenerator.CreateHexagonMesh(UtilityHelpers.GetTransformPositions(_edgePoints));
            if (meshFilter.sharedMesh == null)
            {
                meshFilter.mesh = lastGeneratedMesh;
                meshFilter.mesh.RecalculateNormals();
            }
        }
        if (saveMesh)
        {
            saveMesh = false;

            if (!lastGeneratedMesh) return;
            SaveMeshAsset(lastGeneratedMesh, "New Tile Mesh");
        }
    }
    private void OnDrawGizmos()
    {

        if (showNeighbors && _neighbors != null && _neighbors.Count > 0)
        {
            foreach (HexagonTile neighbor in _neighbors)
            {
                Gizmos.DrawSphere(neighbor.center.position, 3f);
            }
        }

        if (showPoints)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(center.position, 0.3f);
            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(UtilityHelpers.GetTransformPositions(_edgePoints), transform);
        }
    }

    private void SaveMeshAsset(Mesh mesh, string assetName)
    {
        // Create a new mesh asset
        lastGeneratedMesh = Instantiate(mesh) as Mesh;
        lastGeneratedMesh.name = assetName;
        // Save the mesh asset to the project
        AssetDatabase.CreateAsset(lastGeneratedMesh, "Assets/Meshes/" + assetName + ".asset");
        AssetDatabase.SaveAssets();
    }


    public static void PopulateNeighborsFromCornerPoints(List<HexagonTile> tiles, float offset = 0.3f)
    {
        foreach (HexagonTile tile1 in tiles)
        {
            //for each edgepoint on the current hexagontile
            for (int i = 0; i < tile1._cornerPoints.Length; i++)
            {
                //loop through all the hexagontile to check for neighbors
                for (int j = 0; j < tiles.Count; j++)
                {
                    //skip if the hexagontile is the current tile
                    if (tiles[j] == tile1)
                        continue;

                    //loop through the _cornerPoints of the neighboring tile
                    for (int k = 0; k < tiles[j]._cornerPoints.Length; k++)
                    {
                        if (Vector3.Distance(tiles[j]._cornerPoints[k], tile1._cornerPoints[i]) <= offset)
                        {
                            tile1._neighbors.Add(tiles[j]);
                            break;
                        }
                    }
                }
            }
        }
    }
    public static void PopulateNeighbors(List<HexagonTile> tiles, float offset = 0.2f)
    {
        foreach (HexagonTile tile1 in tiles)
        {
            //for each edgepoint on the current hexagontile
            for (int i = 0; i < tile1._edgePoints.Length; i++)
            {
                //loop through all the hexagontile to check for neighbors
                for (int j = 0; j < tiles.Count; j++)
                {
                    //skip if the hexagontile is the current tile
                    if (tiles[j] == tile1)
                        continue;

                    //loop through the _edgePoints of the neighboring tile
                    for (int k = 0; k < tiles[j]._edgePoints.Length; k++)
                    {
                        if (Vector3.Distance(tiles[j]._edgePoints[k].position, tile1._edgePoints[i].position) <= offset)
                        {
                            tile1._neighbors.Add(tiles[j]);
                            break;
                        }
                    }
                }
            }
        }
    }

}

[System.Serializable]
public struct HexagonTilePrototype
{
    public Vector3 center;
    public Vector3[] cornerPoints;
}