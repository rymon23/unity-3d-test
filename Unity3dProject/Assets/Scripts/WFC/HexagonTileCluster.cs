using UnityEngine;
using ProceduralBase;
using UnityEditor;


namespace WFCSystem
{

    [System.Serializable]
    public class HexagonTileCluster : MonoBehaviour
    {
        public int id = -1;
        [Range(8, 24)][SerializeField] private int cellSize = 12;
        public int GetCellSize() => cellSize;
        [Range(2, 6)][SerializeField] public int cellCount = 2;
        public int GetCellCount() => cellCount;

        [Header("Tile Settings")]
        [SerializeField] private TileCategory _tileCategory;
        public TileCategory GetTileCategory() => _tileCategory;
        [SerializeField] private TileType _tileType;
        public TileType GetTileType() => _tileType;

        [Header("Tile Compatibility / Probability")]
        public bool isEdgeable; // can be placed on the edge / border or the grid
        [Range(0.05f, 1f)] public float probabilityWeight = 0.3f;

        public bool bCenterReset;

        public bool combine;

        private void OnValidate()
        {
            if (combine)
            {
                combine = false;
                GameObject go = GameObjectMerger.MergeNestedGameObjects(transform.root.gameObject);
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter.mesh != null)
                {
                    lastSavedMesh = meshFilter.mesh;
                    SaveMeshAsset(lastSavedMesh, " New HexagonTileCluster Mesh");
                }
            }

            if (bCenterReset)
            {
                bCenterReset = false;

                CenterGameObjectAtPosition(gameObject, this.transform.position);
            }
        }

        #region Save Mesh

        [SerializeField] private bool bSaveMesh = false;
        [SerializeField] private Mesh lastSavedMesh;
        void SaveMeshAsset(Mesh mesh, string assetName)
        {
            // Create a new mesh asset
            lastSavedMesh = Instantiate(mesh) as Mesh;
            lastSavedMesh.name = assetName;

            // Save the mesh asset to the project
            AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Prefabs/WFC" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }
        #endregion

        void CenterGameObjectAtPosition(GameObject go, Vector3 position)
        {
            // Get the renderer bounds of the GameObject
            Renderer renderer = go.GetComponent<Renderer>();
            Bounds bounds = renderer.bounds;

            // Calculate the center point of the GameObject
            Vector3 centerPoint = bounds.center;

            // Set the position of the Transform to the desired position minus the calculated center point
            go.transform.position = position - centerPoint;
        }

    }
}