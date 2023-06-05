using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    [System.Serializable]
    public class TerrainChunkData
    {
        public TerrainChunkData(Vector2 _chunkLookup, Vector3 _chunkCenter, Vector2 _worldAreaLookup)
        {
            chunkLookup = _chunkLookup;
            chunkCoordinate = new Vector2(_chunkCenter.x, _chunkCenter.z);
            worldAreaLookup = _worldAreaLookup;
        }
        public TerrainChunkData(TerrainChunkJsonData jsonData)
        {
            chunkLookup = jsonData.chunkLookup.ToVector2();
            chunkCoordinate = jsonData.chunkCoordinate.ToVector2();
            worldAreaLookup = jsonData.worldAreaLookup.ToVector2();
            objectIndex = jsonData.objectIndex;
        }

        // public Vector2 worldAreaLookup;
        // public Vector2 chunkLookup;
        // public Vector2 chunkCoordinate;
        // public int objectIndex = -1;
        public Vector2 chunkLookup { get; private set; }
        public Vector2 chunkCoordinate { get; private set; }
        public Vector2 worldAreaLookup { get; private set; }
        public int objectIndex { get; private set; } = -1;
        public void SetChunkObjectIndex(int index)
        {
            objectIndex = index;
        }

        public TerrainChunkJsonData ConvertToJson() => ConvertToJson(this);
        public static Vector2 CalculateTerrainChunkLookup(Vector2 chunkCoord) => VectorUtil.RoundVector2ToNearest5(chunkCoord);
        public static Vector2 CalculateTerrainChunkLookup(Vector3 chunkPosition) => VectorUtil.RoundVector2ToNearest5(new Vector2(chunkPosition.x, chunkPosition.z));
        public static bool Generate_WorldspaceChunkData(HexagonCellPrototype worldspaceCell, Dictionary<Vector2, TerrainChunkData> terrainChunkData_ByLookup)
        {
            if (terrainChunkData_ByLookup == null)
            {
                Debug.LogError("terrainChunkData_ByLookup is null");
                return false;
            }

            int created = 0;
            for (var side = 1; side < worldspaceCell.sidePoints.Length; side++)
            {
                if (side == 0 || side == 3) continue;

                Vector3 new_chunkCenter = worldspaceCell.sidePoints[side];
                Vector2 new_ChunkLookup = CalculateTerrainChunkLookup(new_chunkCenter);

                if (terrainChunkData_ByLookup.ContainsKey(new_ChunkLookup) == false)
                {
                    terrainChunkData_ByLookup.Add(new_ChunkLookup, new TerrainChunkData(new_ChunkLookup, new_chunkCenter, worldspaceCell.GetParentLookup()));
                    created++;
                }
            }
            return (created > 0);
        }
        // public static TerrainChunkJsonData ConvertToJson(TerrainChunkData chunkData) => new TerrainChunkJsonData(chunkData);
        public static TerrainChunkJsonData ConvertToJson(TerrainChunkData chunkData)
        {
            TerrainChunkJsonData newJsonData = new TerrainChunkJsonData();
            newJsonData.chunkLookup = new Vector2Serialized(chunkData.chunkLookup);
            newJsonData.chunkCoordinate = new Vector2Serialized(chunkData.chunkCoordinate);
            newJsonData.worldAreaLookup = new Vector2Serialized(chunkData.worldAreaLookup);
            newJsonData.objectIndex = chunkData.objectIndex;
            return newJsonData;
        }
    }


    [System.Serializable]
    public class TerrainChunkJsonData
    {
        public Vector2Serialized worldAreaLookup;
        public Vector2Serialized chunkLookup;
        public Vector2Serialized chunkCoordinate;
        public int objectIndex;
    }
}