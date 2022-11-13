using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDist = 450;

    public Transform viewer;

    public static Vector2 viewerPosition;

    int chunkSize;

    int chunkVisibleInViewDist;

    private Dictionary<Vector2, TerrainChunk>
        terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    private List<TerrainChunk>
        terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
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
                        new TerrainChunk(viewChunkCoord, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;

        Vector2 position;

        Bounds bounds;

        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            float viewerDistFromNearestEdge =
                Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistFromNearestEdge <= maxViewDist;
            SetVisible (visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive (visible);
        }

        public bool isVisible() => meshObject.activeSelf;
    }
}
