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
        // [SerializeField] private bool anyCompatibilities;
        public Dictionary<HexagonTileCompatibilitySide, bool[]>[,] tileDirectCompatibilityMatrix; // by Tile id, existing tile side => rotations

        // public Dictionary<HexagonTileCompatibilitySide, List<int>>[,] tileDirectCompatibilityMatrix; // by Tile id, existing tile side => rotations
        // public bool AreTilesCombatible(int incomingTileId, int existingTileId, HexagonTileCompatibilitySide side, int incomingTileRotation, int existingTileRotation)
        // {
        //     if (tileDirectCompatibilityMatrix[incomingTileId, existingTileId][side].Count == 0) return false;
        //     foreach (int rotationOffset in tileDirectCompatibilityMatrix[incomingTileId, existingTileId][side])
        //     {
        //         int validRotation = (existingTileRotation + rotationOffset) % 6;
        //         if (validRotation == incomingTileRotation) return true;
        //     }
        //     return false;
        // }
        public int GetRotationOffset(int rotationA, int rotationB)
        {
            int rotationOffset = rotationB - rotationA;
            if (rotationOffset < 0)
            {
                rotationOffset += 6;
            }
            return rotationOffset;
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

        public Dictionary<HexagonTileCompatibilitySide, bool[]>[,] GetCompatibilityMatrix()
        {
            return tileDirectCompatibilityMatrix;
        }

        public void UpdateTable(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] _newMatrix)
        {

            Dictionary<HexagonTileCompatibilitySide, bool[]>[,] newMatrix
                    = new Dictionary<HexagonTileCompatibilitySide, bool[]>[_newMatrix.GetLength(0), _newMatrix.GetLength(1)];

            for (int i = 0; i < newMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < newMatrix.GetLength(1); j++)
                {
                    newMatrix[i, j] = new Dictionary<HexagonTileCompatibilitySide, bool[]>();
                    for (int compSide = 0; compSide < 8; compSide++)
                    {
                        newMatrix[i, j].Add((HexagonTileCompatibilitySide)compSide, new bool[6]);

                        for (int rotation = 0; rotation < 6; rotation++)
                        {
                            newMatrix[i, j][(HexagonTileCompatibilitySide)compSide][rotation] = _newMatrix[i, j][(HexagonTileCompatibilitySide)compSide][rotation];
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
                    Dictionary<HexagonTileCompatibilitySide, bool[]> row = tileDirectCompatibilityMatrix[i, j];

                    int compatibleRotationsFound = 0;

                    foreach (KeyValuePair<HexagonTileCompatibilitySide, bool[]> entry in row)
                    {
                        HexagonTileCompatibilitySide key = entry.Key;
                        bool[] value = entry.Value;
                        // Do something with key and value

                        foreach (var item in value)
                        {
                            if (item) compatibleRotationsFound++;
                        }
                        // compatibleRotationsFound += value.ToList().FindAll(r => r == true).Count;

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

        public static void SaveData(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table, string directoryPath, string fileName)
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


        public static Dictionary<HexagonTileCompatibilitySide, bool[]>[,] LoadData(string directoryPath, string fileName)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, fileName + ".json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table = JsonConvert.DeserializeObject<Dictionary<HexagonTileCompatibilitySide, bool[]>[,]>(json);

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

        // private const string FILE_NAME = "saved_table.dat";
        // public void SaveTable(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table, string _path = "/Assets/WFC")
        // {
        //     BinaryFormatter formatter = new BinaryFormatter();
        //     string path = Application.persistentDataPath + _path + "/" + FILE_NAME;
        //     FileStream stream = new FileStream(path, FileMode.Create);

        //     formatter.Serialize(stream, table);
        //     stream.Close();
        // }

        // public Dictionary<int, bool[]>[,] LoadTable()
        // {
        //     string path = Application.persistentDataPath + "/" + FILE_NAME;

        //     if (File.Exists(path))
        //     {
        //         BinaryFormatter formatter = new BinaryFormatter();
        //         FileStream stream = new FileStream(path, FileMode.Open);

        //         Dictionary<int, bool[]>[,] table = formatter.Deserialize(stream) as Dictionary<int, bool[]>[,];
        //         stream.Close();

        //         return table;
        //     }
        //     else
        //     {
        //         Debug.LogError("Save file not found in " + path);
        //         return null;
        //     }
        // }



        // public static void SaveTable(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table, string filePath)
        // {
        //     int rows = table.GetLength(0);
        //     int columns = table.GetLength(1);

        //     for (int row = 0; row < rows; row++)
        //     {
        //         for (int col = 0; col < columns; col++)
        //         {
        //             foreach (var item in table[row, col])
        //             {
        //                 int key = (int)item.Key;
        //                 PlayerPrefs.SetInt(filePath + "_" + row + "_" + col + "_key", key);
        //                 for (int i = 0; i < item.Value.Length; i++)
        //                 {
        //                     PlayerPrefs.SetInt(filePath + "_" + row + "_" + col + "_value_" + i, item.Value[i] ? 1 : 0);
        //                 }
        //             }
        //         }
        //     }

        //     Debug.Log("SaveTable!");
        //     PlayerPrefs.Save();
        // }
        // public static Dictionary<HexagonTileCompatibilitySide, bool[]>[,] LoadTable(string filePath)
        // {
        //     int rows = PlayerPrefs.GetInt(filePath + "_rows");
        //     int columns = PlayerPrefs.GetInt(filePath + "_columns");
        //     Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table = new Dictionary<HexagonTileCompatibilitySide, bool[]>[rows, columns];

        //     for (int i = 0; i < rows; i++)
        //     {
        //         for (int j = 0; j < columns; j++)
        //         {
        //             int keyCount = PlayerPrefs.GetInt(filePath + "_" + i + "_" + j + "_keyCount");
        //             Dictionary<HexagonTileCompatibilitySide, bool[]> dict = new Dictionary<HexagonTileCompatibilitySide, bool[]>();
        //             for (int k = 0; k < keyCount; k++)
        //             {
        //                 int dictKey = PlayerPrefs.GetInt(filePath + "_" + i + "_" + j + "_key_" + k);
        //                 int arrayLength = PlayerPrefs.GetInt(filePath + "_" + i + "_" + j + "_arrayLength_" + k);
        //                 bool[] array = new bool[arrayLength];
        //                 for (int l = 0; l < arrayLength; l++)
        //                 {
        //                     array[l] = PlayerPrefs.GetInt(filePath + "_" + i + "_" + j + "_array_" + k + "_" + l) == 1 ? true : false;
        //                 }
        //                 dict.Add((HexagonTileCompatibilitySide)dictKey, array);
        //             }
        //             table[i, j] = dict;
        //         }
        //     }
        //     return table;
        // }
        // public static void SaveTable(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table, string filePath = "Assets/")
        // {
        //     string json = JsonConvert.SerializeObject(table, Formatting.Indented);

        //     File.WriteAllText(filePath, json);

        //     Debug.Log("SaveTable!");
        // }
        // public static void SaveData(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] table, string filePath)
        // {
        //     // Convert the 2D dictionary to a 1D list
        //     List<Dictionary<HexagonTileCompatibilitySide, bool[]>> list = new List<Dictionary<HexagonTileCompatibilitySide, bool[]>>();
        //     for (int i = 0; i < table.GetLength(0); i++)
        //     {
        //         for (int j = 0; j < table.GetLength(1); j++)
        //         {
        //             list.Add(table[i, j]);
        //         }
        //     }

        //     // Serialize the list to a JSON string
        //     string json = JsonUtility.ToJson(list);

        //     // Write the JSON string to the file
        //     System.IO.File.WriteAllText(filePath, json);
        // }

        // [SerializeField] private string directory = "C:/temp";



    }

}