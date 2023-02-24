using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Newtonsoft.Json;

namespace WFCSystem
{
    public enum HexagonTileCompatibilitySide
    {
        Front = 0,
        FrontRight,
        BackRight,
        Back,
        BackLeft,
        FrontLeft,

        Bottom,
        Top,
    }

    [CreateAssetMenu(fileName = "New Tile Compatibility Directory", menuName = "Compatibility Directory")]
    public class TileCompatibilityDirectory : ScriptableObject
    {
        [SerializeField] private int entries = 0;
        [SerializeField] private int totalRotations = 0;
        public Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] tileDirectCompatibilityMatrix; // by Tile id, existing tile side => rotations
        public bool AreTilesCombatible(int incomingTileId, int existingTileId, HexagonTileCompatibilitySide existingTileSide, int incomingTileRotation, int existingTileRotation)
        {
            HexagonTileCompatibilitySide existingTileRotatedSide = GetRotatedTargetSide(existingTileSide, existingTileRotation);

            if (tileDirectCompatibilityMatrix[incomingTileId, existingTileId][existingTileRotatedSide].Count == 0) return false;


            foreach (int rotationOffset in tileDirectCompatibilityMatrix[incomingTileId, existingTileId][existingTileRotatedSide])
            {
                int validRotation = (existingTileRotation + rotationOffset) % 6;

                // Debug.Log("validRotation: " + validRotation + ", rotationOffset: " + rotationOffset);


                if (validRotation == incomingTileRotation) return true;
            }
            // Debug.Log("existingTileSide: " + existingTileSide + ", incomingTileId: " + incomingTileId);

            return false;
        }

        public List<int> GetCompatibleTileRotations(int incomingTileId, int existingTileId, HexagonTileCompatibilitySide side, int existingTileRotation)
        {
            if (tileDirectCompatibilityMatrix[incomingTileId, existingTileId][side].Count == 0) return null;
            List<int> rotations = new List<int>();

            foreach (int rotationOffset in tileDirectCompatibilityMatrix[incomingTileId, existingTileId][side])
            {
                int validRotation = (existingTileRotation + rotationOffset) % 6;
                rotations.Add(validRotation);
            }
            return rotations;
        }


        public HexagonTileCompatibilitySide GetRotatedTargetSide(HexagonTileCompatibilitySide existingTileSide, int existingTileRotation)
        {
            if (existingTileSide < HexagonTileCompatibilitySide.Bottom)
            {
                return (HexagonTileCompatibilitySide)(((int)existingTileSide + existingTileRotation) % 6);
            }
            return existingTileSide;
        }

        public int GetRotatedTargetSide(int existingTileSide, int existingTileRotation) => GetRotatedTargetSide(existingTileSide, existingTileRotation);

        public int GetRotationOffset(int rotationA, int rotationB)
        {
            int rotationOffset = rotationB - rotationA;
            if (rotationOffset < 0)
            {
                rotationOffset += 6;
            }
            return rotationOffset;
        }

        public int GetSideOffset(HexagonSide sideA, HexagonSide SideB)
        {
            int sideOffset = SideB - sideA;
            if (sideOffset < 0)
            {
                sideOffset += 6;
            }
            return sideOffset;
        }

        public List<TileCompatibilityEntry> _tileEntries;

        private void OnValidate()
        {
            if (saveTable)
            {
                saveTable = false;
                SaveData(tileDirectCompatibilityMatrix, savedfilePath, savefileName);

                return;
            }

            if (loadTable || tileDirectCompatibilityMatrix == null)
            {
                loadTable = false;
                tileDirectCompatibilityMatrix = LoadData(savedfilePath, savefileName);

                UpdateTileEntries();

                return;
            }

            if (tileDirectCompatibilityMatrix != null)
            {

                UpdateTileEntries();
            }
        }

        public void ShowDebugData()
        {
            Debug.Log("tileDirectCompatibilityMatrix.GetLength: " + tileDirectCompatibilityMatrix.GetLength(0) + ", totalRotations: " + totalRotations + ", entries: " + entries);

        }

        public Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] GetCompatibilityMatrix()
        {
            return tileDirectCompatibilityMatrix;
        }

        public void UpdateTable(Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] updatedMatrix)
        {

            Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] newMatrix
                    = new Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[updatedMatrix.GetLength(0), updatedMatrix.GetLength(1)];

            // Debug.Log("updatedMatrix.GetLength(0): " + updatedMatrix.GetLength(0) + ", newMatrix.GetLength(0): " + newMatrix.GetLength(0));
            // Debug.Log("updatedMatrix.GetLength(1): " + updatedMatrix.GetLength(1) + ", newMatrix.GetLength(1): " + newMatrix.GetLength(1));

            for (int i = 0; i < newMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < newMatrix.GetLength(1); j++)
                {
                    newMatrix[i, j] = new Dictionary<HexagonTileCompatibilitySide, HashSet<int>>();

                    for (int compSide = 0; compSide < 8; compSide++)
                    {
                        newMatrix[i, j].Add((HexagonTileCompatibilitySide)compSide, new HashSet<int>());

                        // Debug.Log("newMatrix[" + i + " ," + j + "]: " + newMatrix[i, j]);
                        // Debug.Log("updatedMatrix[" + i + " ," + j + "]: " + updatedMatrix[i, j]);

                        if (updatedMatrix[i, j] == null)
                        {
                            // Debug.LogError(" Nothing there");
                            continue;
                        }

                        foreach (int item in updatedMatrix[i, j][(HexagonTileCompatibilitySide)compSide])
                        {
                            newMatrix[i, j][(HexagonTileCompatibilitySide)compSide].Add(item);
                        }
                    }
                }
            }

            tileDirectCompatibilityMatrix = newMatrix;
            entries = tileDirectCompatibilityMatrix.GetLength(0);

            UpdateTileEntries();
        }


        private void UpdateTileEntries()
        {
            if (tileDirectCompatibilityMatrix == null || tileDirectCompatibilityMatrix.GetLength(0) == 0)
            {
                entries = 0;
                _tileEntries = null;
                totalRotations = 0;

                Debug.LogError("tileDirectCompatibilityMatrix is null or empty");
                return;
            }

            List<TileCompatibilityEntry> newTileEntries = new List<TileCompatibilityEntry>();
            bool hasAnyCompatibilities = false;

            entries = tileDirectCompatibilityMatrix.GetLength(0);
            int _totalRotations = 0;

            for (int i = 0; i < tileDirectCompatibilityMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < tileDirectCompatibilityMatrix.GetLength(1); j++)
                {
                    Dictionary<HexagonTileCompatibilitySide, HashSet<int>> row = tileDirectCompatibilityMatrix[i, j];

                    int compatibleRotationsFound = 0;

                    foreach (KeyValuePair<HexagonTileCompatibilitySide, HashSet<int>> entry in row)
                    {
                        HexagonTileCompatibilitySide key = entry.Key;
                        HashSet<int> value = entry.Value;

                        compatibleRotationsFound += value.Count;
                    }

                    if (compatibleRotationsFound > 0 && hasAnyCompatibilities == false)
                    {
                        hasAnyCompatibilities = true;
                    }

                    _totalRotations += compatibleRotationsFound;

                    TileCompatibilityEntry newTileEntry = new TileCompatibilityEntry();
                    newTileEntry.name = i + "x" + j;
                    newTileEntry.compatibleRotations = compatibleRotationsFound;

                    newTileEntries.Add(newTileEntry);
                }
            }
            _tileEntries = newTileEntries.OrderByDescending(c => c.compatibleRotations > 0).ToList();
            totalRotations = _totalRotations;
        }

        [System.Serializable]
        public struct TileCompatibilityEntry
        {
            public string name;
            public int compatibleRotations;
        }


        [Header("Save / Load Settings")]
        [SerializeField] private bool saveTable;
        [SerializeField] private bool loadTable;
        [SerializeField] private string savedfilePath = "Assets/WFC/";
        [SerializeField] private string savefileName = "saved_tile_compatibility_Data";

        public static void SaveData(Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] table, string directoryPath, string fileName)
        {
            // string json = JsonUtility.ToJson(table);
            string json = JsonConvert.SerializeObject(table, Formatting.Indented);

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string filePath = Path.Combine(directoryPath, fileName + ".json");
                File.WriteAllText(filePath, json);

                Debug.Log("SaveData!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while saving data: " + ex.Message);
            }
        }


        public static Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] LoadData(string directoryPath, string fileName)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, fileName + ".json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] table = JsonConvert.DeserializeObject<Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,]>(json);

                    Debug.Log("LoadData!");
                    return table;
                }
                else
                {
                    Debug.LogError("Error while loading data: file not found");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while loading data: " + ex.Message);
                return null;
            }
        }
    }

}