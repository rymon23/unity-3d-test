using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using WFCSystem;
using System.IO;
using Newtonsoft.Json;

[System.Serializable]
[CreateAssetMenu(fileName = "New  Hexagon Socket Data Manager", menuName = "Hexagon Socket Data Manager")]
public class HexagonSocketDataManager : ScriptableObject
{
    [Header("Save / Load Settings")]
    [SerializeField] private string savedfilePath = "Assets/WFC/";
    [SerializeField] private string savefileName = "tile_socket_data";
    [SerializeField] private string[] _assetPaths = { "Assets/Prefabs/WFC" };
    [SerializeField] private float _currentVersion = 0f;

    public string GetFilePath() => savedfilePath;
    public string GetFileName() => savefileName;
    public float GetFileVersin() => _currentVersion;

    [Header(" ")]
    [SerializeField] private bool reevaluate;

    [Header("Generate")]
    [SerializeField] private bool generateNewFileVersion;

    public List<HexagonTileCore> tilePrefabs = new List<HexagonTileCore>();
    public List<string> tileUids = new List<string>();

    private void EvaluateTilePrefabs()
    {
        // Get Tiles
        List<HexagonTileCore> found1 = new List<HexagonTileCore>();
        string[] guids = AssetDatabase.FindAssets("t:GameObject", _assetPaths);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            HexagonTileCore tile = (go != null) ? go.GetComponent<HexagonTileCore>() : null;
            if (tile != null) found1.Add(tile);
        }
        tilePrefabs = found1;
    }

    private void EvaluateTileUIds()
    {
        List<string> foundTileUids = new List<string>();

        foreach (HexagonTileCore tile in tilePrefabs)
        {
            if (tile.HasUid())
            {
                string currentUid = tile.GetUid();
                if (foundTileUids.Contains(currentUid))
                {
                    Debug.LogError("Tile Uid: " + currentUid + " is being shared!\nCheck tile: " + tile.gameObject.name);
                }

                // Debug.Log("currentUid: " + currentUid);

                foundTileUids.Add(currentUid);
            }
        }

        tileUids = foundTileUids;
    }

    private bool GenerateNewDataFile(string directoryPath, string fileName)
    {
        string filePath = Path.Combine(directoryPath, fileName + ".json");

        // Load existing data from file
        Dictionary<string, List<int[]>> existingData = new Dictionary<string, List<int[]>>();
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            existingData = JsonConvert.DeserializeObject<Dictionary<string, List<int[]>>>(json);
        }

        // Remove uids that are missing from tileUids list
        Dictionary<string, List<int[]>> finalData = new Dictionary<string, List<int[]>>();
        int entries = 0;
        foreach (var kvp in existingData)
        {
            string _tileUid = kvp.Key;

            // Debug.Log(" _tileUid: " + _tileUid);

            if (tileUids.Contains(_tileUid))
            {
                finalData.Add(_tileUid, existingData[_tileUid]);
                entries++;
                // updatedData.Remove(kvp.Key);
            }
        }
        Debug.Log(" existingData entries: " + existingData.Keys.Count);

        Debug.Log("Final Data entries: " + entries);


        // Save updated data data to new file
        string newfilePath = Path.Combine(directoryPath, fileName + "_v" + (_currentVersion + 1) + ".json");
        string mergedJson = JsonConvert.SerializeObject(finalData, Formatting.Indented);
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(newfilePath, mergedJson);
            Debug.Log("SaveData!: \n" + newfilePath);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error while saving data: " + ex.Message);
            return false;
        }
    }


    private void OnValidate()
    {
        EvaluateTilePrefabs();
        EvaluateTileUIds();

        if (reevaluate)
        {
            reevaluate = false;

            EvaluateTilePrefabs();
            EvaluateTileUIds();
        }

        if (generateNewFileVersion)
        {
            generateNewFileVersion = false;

            bool sucess = GenerateNewDataFile(savedfilePath, savefileName);
        }
    }
}
