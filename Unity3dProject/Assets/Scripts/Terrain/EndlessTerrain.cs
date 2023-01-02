using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;

    const float
        sqrViewerMoveThresholdForChunkUpdate
        =
        viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;

    public static float maxViewDist;

    public Transform viewer;

    public Material mapMaterial;

    public static Vector2 viewerPosition;

    public static Vector2 viewerPositionOld;

    static MapGenerator mapGenerator;

    int chunkSize;

    int chunkVisibleInViewDist;

    private Dictionary<Vector2, TerrainChunk>
        terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    private List<TerrainChunk>
        terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist =
            detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if (
            (viewerPositionOld - viewerPosition).sqrMagnitude >
            sqrViewerMoveThresholdForChunkUpdate
        )
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (
            int yOffset = -chunkVisibleInViewDist;
            yOffset <= chunkVisibleInViewDist;
            yOffset++
        )
        {
            for (
                int xOffset = -chunkVisibleInViewDist;
                xOffset < chunkVisibleInViewDist;
                xOffset++
            )
            {
                Vector2 viewChunkCoord =
                    new Vector2(currentChunkCoordX + xOffset,
                        currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewChunkCoord))
                {
                    terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewChunkCoord].isVisible())
                    {
                        terrainChunksVisibleLastUpdate
                            .Add(terrainChunkDictionary[viewChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary
                        .Add(viewChunkCoord,
                        new TerrainChunk(viewChunkCoord,
                            chunkSize,
                            detailLevels,
                            transform,
                            mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;

        Vector2 position;

        Bounds bounds;

        MeshRenderer meshRenderer;

        MeshFilter meshFilter;

        LODInfo[] detailLevels;

        LODMesh[] lodMeshes;

        MapData mapData;

        bool mapDataReceived;

        int previousLODIndex = -1;

        public TerrainChunk(
            Vector2 coord,
            int size,
            LODInfo[] detailLevels,
            Transform parent,
            Material material
        )
        {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] =
                    new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            mapGenerator.RequestMapData (OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            // print("Map data received");
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived) return;

            float viewerDistFromNearestEdge =
                Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistFromNearestEdge <= maxViewDist;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (
                        viewerDistFromNearestEdge >
                        detailLevels[i].visibleDistanceThreshold
                    )
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh (mapData);
                    }
                }
            }

            SetVisible (visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive (visible);
        }

        public bool isVisible() => meshObject.activeSelf;
    }

    class LODMesh
    {
        public Mesh mesh;

        public bool hasRequestedMesh;

        public bool hasMesh;

        int lod;

        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData (mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;

        public float visibleDistanceThreshold;
    }
}
