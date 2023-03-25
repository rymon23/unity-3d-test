using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

namespace ProceduralBase
{
    public class HexGridArea : MonoBehaviour
    {

        [SerializeField] private HexagonCellManager cellManager;
        [SerializeField] private WFC_HexMicroGrid wfc;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshCollider meshCollider;
        // [Header("Settings")]
        // [Range(12, 296)] public int areaSize = 72;

        #region Saved State
        #endregion
        private Mesh mesh;

        [Header("Generate")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool generateAll;

        [Header("World Space Data")]
        [SerializeField] private List<Transform> entranceMarkers;

        public void InitialSetup()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            cellManager = GetComponent<HexagonCellManager>();
            wfc = GetComponent<WFC_HexMicroGrid>();

            if (!mesh)
            {
                mesh = new Mesh();
                mesh.name = "Procedural Terrain";
            }
        }

        public void Generate()
        {
            // Setup Cells
            // cellManager.GenerateCells(true);

            cellManager.GenerateCells(true, false);
            (Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells) = cellManager.GetCells();
            // Add cells to WFC
            wfc.SetRadius(cellManager.radius);
            wfc.SetCells(_allCellsByLayer, _allCells);

            // Add cells to WFC
            // wfc.SetRadius(cellManager.radius);
            // wfc.SetCells(cellManager.GetCells());
            // Run WFC
            wfc.ExecuteWFC();
        }

        private void OnValidate()
        {
            InitialSetup();

            if (bSaveMesh)
            {
                bSaveMesh = false;
                if (mesh != null) SaveMeshAsset(mesh, "New World Area Mesh");
            }
        }

        private void Awake() => InitialSetup();

        private void Start()
        {
            InitialSetup();

            if (runOnStart) Generate();
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
            AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Terrain/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }
        #endregion

        //     [Header("Debug Settings")]
        //     [SerializeField] private ShowVertexState debug_showVertices;
        //     [SerializeField] private bool debug_showBounds;
        //     [SerializeField] private bool debug_showBlendRadius;
        //     [SerializeField] private bool debug_editorUpdateTerrainOnce;
        //     public enum ShowVertexState { None, All, Path, Terrain, Cell, CellCenter, CellCorner }

        //     private void OnDrawGizmos()
        //     {

        //         if (debug_showBounds)
        //         {
        //             Gizmos.color = Color.black;
        //             float radius = areaSize / 2;
        //             Gizmos.DrawWireSphere(transform.position, radius);
        //         }

        //         if (debug_showBlendRadius)
        //         {
        //             Gizmos.color = Color.red;
        //             float radius = cellManager.radius * blendRadiusMult;
        //             Gizmos.DrawWireSphere(transform.position, radius);
        //         }

        //         if (debug_showVertices != ShowVertexState.None)
        //         {

        //             if (vertexGrid != null)
        //             {
        //                 for (int x = 0; x < vertexGrid.GetLength(0); x++)
        //                 {
        //                     for (int z = 0; z < vertexGrid.GetLength(1); z++)
        //                     {
        //                         TerrainVertex currentVertex = vertexGrid[x, z];
        //                         bool show = false;
        //                         float rad = 0.33f;

        //                         Gizmos.color = Color.black;
        //                         if (debug_showVertices == ShowVertexState.All)
        //                         {
        //                             show = true;
        //                         }
        //                         else if (debug_showVertices == ShowVertexState.CellCenter)
        //                         {
        //                             show = currentVertex.isCellCenterPoint;
        //                             Gizmos.color = Color.red;
        //                             rad = 0.66f;
        //                         }
        //                         else if (debug_showVertices == ShowVertexState.Cell)
        //                         {
        //                             show = currentVertex.type == VertexType.Cell;
        //                             Gizmos.color = Color.red;
        //                             rad = 0.66f;
        //                         }
        //                         else if (debug_showVertices == ShowVertexState.Path)
        //                         {
        //                             show = currentVertex.type == VertexType.Road;
        //                             Gizmos.color = Color.red;
        //                             rad = 0.66f;
        //                         }
        //                         else if (debug_showVertices == ShowVertexState.Terrain)
        //                         {
        //                             show = currentVertex.type == VertexType.Generic;
        //                             Gizmos.color = Color.red;
        //                             rad = 0.66f;
        //                         }
        //                         else if (debug_showVertices == ShowVertexState.CellCorner)
        //                         {
        //                             show = currentVertex.isCellCornerPoint;
        //                             Gizmos.color = Color.red;
        //                             rad = 0.66f;
        //                         }

        //                         if (show) Gizmos.DrawSphere(currentVertex.position, rad);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
    }
}