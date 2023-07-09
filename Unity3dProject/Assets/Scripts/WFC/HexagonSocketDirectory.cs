using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace WFCSystem
{

    public enum GlobalSockets
    {
        Any = 0,
        Edge = 1,
        Empty_Space = 2,
        Unassigned_EdgeCell = 3,
        Unassigned_InnerCell = 4,
        Entrance_Generic = 5,
        InnerCell_Generic = 6,
        Path_Generic = 7,
        Wall_Generic = 8,
        Leveled_Inner = 9,
        Leveled_Edge_Part = 10,
        Unset_Edge_Connector = 11,
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "New Hex Socket Directory", menuName = "Hex Socket Directory")]
    public class HexagonSocketDirectory : ScriptableObject
    {
        public static TileObjectType _tileObjectType { get; } = TileObjectType.SocketDirectory;

        [SerializeField] private string id;
        private void OnEnable()
        {
            if (id == null)
            {
                // Generate a new unique identifier for the object
                Guid guid = Guid.NewGuid();
                id = $"{GetInstanceID()}-{guid}";
            }
        }

        [SerializeField] public string[] sockets = new string[52];
        [SerializeField] public Color[] colors;
        public bool[,] matrix = new bool[52, 52];

        public bool[,] GetCompatibilityMatrix() => matrix;
        public bool copyMatrixFromTileSocketDirectory;
        public bool enableDebugLogs;


        private void InitialSetup()
        {
            if (sockets == null)
            {
                sockets = new string[52];

                sockets[32] = "__LAYERED__";
            }

            if (matrix == null) matrix = new bool[sockets.Length, sockets.Length];

            if (colors == null) colors = GenerateUniqueColors(sockets.Length);


            EvaluateGlobalSockets();

            EvaluateSocketConstants();
        }

        private void Awake()
        {
            InitialSetup();
        }

        private void OnValidate()
        {
            InitialSetup();

            if (copyMatrixFromTileSocketDirectory)
            {
                // copyMatrixFromTileSocketDirectory = false;

                // if (tileSocketDirectory != null)
                // {
                //     string[] names = Enum.GetNames(typeof(TileSocketPrimitive));
                //     string[] newSockets = new string[52];
                //     for (int i = 0; i < newSockets.Length; i++)
                //     {
                //         if (i < names.Length) newSockets[i] = names[i];
                //     }

                //     bool[,] oldMatrix = tileSocketDirectory.GetCompatibilityMatrix();
                //     bool[,] newMatrix = new bool[52, 52];

                //     int oldLength = oldMatrix.GetLength(0);

                //     Debug.Log("oldMatrix: " + oldLength);


                //     for (int i = 0; i < newMatrix.GetLength(0); i++)
                //     {
                //         for (int j = 0; j < newMatrix.GetLength(1); j++)
                //         {
                //             if (i < oldLength && j < oldLength)
                //             {
                //                 newMatrix[i, j] = oldMatrix[i, j];
                //             }
                //         }
                //     }
                //     sockets = newSockets;
                //     matrix = newMatrix;
                // }
            }

            if (savefileName != null)
            {
                Load();
            }
        }


        private void EvaluateGlobalSockets()
        {
            string[] globalSockets = Enum.GetNames(typeof(GlobalSockets));
            for (int i = 0; i < sockets.Length; i++)
            {
                if (i < globalSockets.Length)
                {
                    sockets[i] = globalSockets[i];
                }
                else
                {
                    if (sockets[i] == null || sockets[i] == "")
                    {
                        sockets[i] = GetSocketPrefix_Blank() + i;
                    }
                }
            }
        }

        private void EvaluateSocketConstants()
        {
            if (matrix.GetLength(0) > 2 && matrix.GetLength(1) > 2)
            {
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    for (int j = 0; j < matrix.GetLength(1); j++)
                    {
                        if (i == 0 || j == 0) matrix[i, j] = true;
                    }
                }
            }
        }

        public static string GetSocketPrefix_Layered() => "L_";
        public static string GetSocketPrefix_Blank() => "__EMPTY_";
        public static bool IsGlobalUnassignedSocket(int socektId) =>
            (GlobalSockets)socektId == GlobalSockets.Unassigned_EdgeCell
                || (GlobalSockets)socektId == GlobalSockets.Unassigned_InnerCell
                || (GlobalSockets)socektId == GlobalSockets.Unset_Edge_Connector;

        // public static Color[] GenerateUniqueColors(int length)
        // {
        //     Color[] generatedColors = new Color[length];

        //     float hueIncrement = 0.1f;
        //     float saturation = 0.7f;
        //     float value = 0.8f;

        //     for (int i = 0; i < length; i++)
        //     {
        //         float hue = (float)i / length + hueIncrement * i;
        //         generatedColors[i] = Color.HSVToRGB(hue, saturation, value);
        //     }

        //     return generatedColors;
        // }
        public static Color[] GenerateUniqueColors(int length)
        {
            Color[] generatedColors = new Color[length];

            for (int i = 0; i < length; i++)
            {
                float hue = (float)i / length;
                generatedColors[i] = Color.HSVToRGB(hue, 1f, 1f);
            }

            generatedColors[(int)GlobalSockets.Any] = Color.white;
            generatedColors[(int)GlobalSockets.Edge] = Color.black;

            return generatedColors;
        }

        public void Save()
        {
            EvaluateGlobalSockets();
            EvaluateSocketConstants();

            savefileName = filenameHead + id;
            Debug.Log("Save, savefileName: " + savefileName);

            SaveData(matrix, sockets, savedfilePath, savefileName);
        }

        public void Load()
        {
            (bool[,] loadedMatrix, string[] loadedSockets) = LoadData(savedfilePath, savefileName, name, enableDebugLogs);
            if (loadedMatrix != null) matrix = loadedMatrix;
            if (loadedSockets != null) sockets = loadedSockets;
        }

        [Header("Save / Load Settings")]
        [SerializeField] private string savedfilePath = "Assets/WFC/";
        [SerializeField] private string filenameHead = "socket_compatibility_Data";
        [SerializeField] private string savefileName;

        public static void SaveData(bool[,] matrix, string[] sockets, string directoryPath, string fileName)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("matrix", matrix);
            dict.Add("sockets", sockets);

            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string filePath = Path.Combine(directoryPath, fileName + ".json");
                File.WriteAllText(filePath, json);

                Debug.Log("SaveData!: \n" + filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while saving data: " + ex.Message);
            }
        }

        public static (bool[,], string[]) LoadData(string directoryPath, string fileName, string directoryName, bool enableDebugLogs = false)
        {
            try
            {
                string filePath = Path.Combine(directoryPath, fileName + ".json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    bool[,] matrix = JsonConvert.DeserializeObject<bool[,]>(dict["matrix"].ToString());
                    string[] sockets = JsonConvert.DeserializeObject<string[]>(dict["sockets"].ToString());

                    if (enableDebugLogs) Debug.Log("\n" + directoryName + " loaded socket directory file: " + fileName);
                    return (matrix, sockets);
                }
                else
                {
                    Debug.LogError("Error while loading data: file not found");
                    return (null, null);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while loading data: " + ex.Message);
                return (null, null);
            }

        }
    }
}