using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    [System.Serializable]
    public class TerrainChunkVertexData
    {
        public TerrainChunkVertexData(Vector2[,] _gridKeys, Dictionary<Vector2, Vector3> _verticesByKey)
        {
            gridKeys = _gridKeys;
            verticesByKey = _verticesByKey;
        }
        public TerrainChunkVertexData(Vector2[,] _gridKeys, Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid)
        {
            gridKeys = _gridKeys;

            Dictionary<Vector2, Vector3> _verticesByKey = new Dictionary<Vector2, Vector3>();
            for (int i = 0; i < gridKeys.GetLength(0); i++)
            {
                for (int j = 0; j < gridKeys.GetLength(1); j++)
                {
                    Vector2 key = gridKeys[i, j];
                    _verticesByKey.Add(key, globalTerrainVertexGrid[key].position);
                }
            }
            verticesByKey = _verticesByKey;
        }
        public TerrainChunkVertexData(TerrainChunkVertexJson jsonData)
        {
            Vector2[,] _gridKeys = new Vector2[jsonData.gridKeys.GetLength(0), jsonData.gridKeys.GetLength(1)];
            Dictionary<Vector2, Vector3> _verticesByKey = new Dictionary<Vector2, Vector3>();

            for (int i = 0; i < jsonData.gridKeys.GetLength(0); i++)
            {
                for (int j = 0; j < jsonData.gridKeys.GetLength(1); j++)
                {
                    Vector2Serialized key = jsonData.gridKeys[i, j];
                    _gridKeys[i, j] = key.ToVector2();
                    _verticesByKey.Add(_gridKeys[i, j], jsonData.verticesByKey[key].ToVector3());
                }
            }
            gridKeys = _gridKeys;
            verticesByKey = _verticesByKey;
        }

        // public Vector2 chunkLookup { get; private set; }
        public Vector2[,] gridKeys;
        public Dictionary<Vector2, Vector3> verticesByKey;

        public TerrainChunkVertexJson ConvertToJson() => ConvertToJson(this);

        public static TerrainChunkVertexJson ConvertToJson(TerrainChunkVertexData chunkData)
        {
            TerrainChunkVertexJson newJsonData = new TerrainChunkVertexJson();

            Vector2Serialized[,] serializedGrid = new Vector2Serialized[chunkData.gridKeys.GetLength(0), chunkData.gridKeys.GetLength(1)];
            Dictionary<Vector2Serialized, Vector3Serialized> serializedVerticesByKey = new Dictionary<Vector2Serialized, Vector3Serialized>();

            for (int i = 0; i < chunkData.gridKeys.GetLength(0); i++)
            {
                for (int j = 0; j < chunkData.gridKeys.GetLength(1); j++)
                {
                    Vector2 key = chunkData.gridKeys[i, j];
                    serializedGrid[i, j] = new Vector2Serialized(key);
                    serializedVerticesByKey.Add(serializedGrid[i, j], new Vector3Serialized(chunkData.verticesByKey[key]));
                }
            }
            newJsonData.gridKeys = serializedGrid;
            newJsonData.verticesByKey = serializedVerticesByKey;

            return newJsonData;
        }
    }

    [System.Serializable]
    public class TerrainChunkVertexJson
    {
        public Vector2Serialized[,] gridKeys;
        public Dictionary<Vector2Serialized, Vector3Serialized> verticesByKey;
    }
}