using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using ProceduralBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;


namespace WFCSystem
{
    public class WorldSaveLoadUtil : MonoBehaviour
    {

        public static bool LoadFilePath(string directoryPath, string fileName, out string filePath)
        {
            filePath = Path.Combine(directoryPath, fileName + ".json");
            if (!File.Exists(filePath))
            {
                Debug.LogError("Error while loading data: File not found: " + fileName);
                return false;
            }
            return true;
        }

        public static Dictionary<Vector2, TerrainChunkData> LoadData_WorldAreaTerrainChunkData(string directoryPath, string fileName)
        {
            if (WorldSaveLoadUtil.LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter() },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };

            Dictionary<Vector2, TerrainChunkData> dictionary = null;
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, TerrainChunkJsonData>>(json, settings);
                dictionary = new Dictionary<Vector2, TerrainChunkData>();

                foreach (var item in result)
                {
                    var key = Vector2.zero;

                    if (new Vector2Converter().TryConvertFrom(item.Key, out key))
                    {
                        // TerrainChunkJsonData jsonData = item.Value;
                        TerrainChunkData terrainChunkData = new TerrainChunkData(item.Value);
                        dictionary.Add(terrainChunkData.chunkLookup, terrainChunkData);
                    }
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }
            return dictionary;
        }

        public static Dictionary<Vector2, HexagonCellPrototype> LoadData_CellDictionary(string directoryPath, string fileName, int cellSize)
        {
            if (WorldSaveLoadUtil.LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter() },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };
            Dictionary<Vector2, HexagonCellPrototype> dictionary = null;
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, WorldCellData>>(json, settings);
                dictionary = new Dictionary<Vector2, HexagonCellPrototype>();

                foreach (var item in result)
                {
                    var key = Vector2.zero;

                    if (new Vector2Converter().TryConvertFrom(item.Key, out key))
                    {
                        WorldCellData cellData = item.Value;

                        Vector2 lookup = cellData.lookup.ToVector2();
                        Vector3 center = cellData.center.ToVector3();
                        HexagonCellPrototype cell = new HexagonCellPrototype(center, cellSize, true);
                        cellData.PastToCell(cell);

                        dictionary.Add(lookup, cell);
                    }
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }
            return dictionary;
        }
        // public static Dictionary<Vector2, (Vector2, int)> LoadData_WorldAreaTerrainChunkIndex_ByLookup(string directoryPath, string fileName)
        // {
        //     if (WorldSaveLoadUtil.LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

        //     string json = File.ReadAllText(filePath);
        //     var settings = new JsonSerializerSettings
        //     {
        //         Converters = new List<JsonConverter> { new Vector2Converter() },
        //         Error = (sender, args) =>
        //         {
        //             Debug.LogError(args.ErrorContext.Error.Message);
        //             args.ErrorContext.Handled = true;
        //         }
        //     };

        //     Dictionary<Vector2, (Vector2, int)> dictionary = null;
        //     try
        //     {
        //         var result = JsonConvert.DeserializeObject<Dictionary<string, TerrainChunkLookupData>>(json, settings);
        //         dictionary = new Dictionary<Vector2, (Vector2, int)>();

        //         foreach (var item in result)
        //         {
        //             var key = Vector2.zero;

        //             if (new Vector2Converter().TryConvertFrom(item.Key, out key))
        //             {
        //                 TerrainChunkLookupData terrainChunkLookupData = item.Value;
        //                 Vector2 areaLookup = terrainChunkLookupData.areaLookup.ToVector2();
        //                 Vector2 chunkLookup = terrainChunkLookupData.chunkLookup.ToVector2();
        //                 int index = terrainChunkLookupData.index;

        //                 dictionary.Add(chunkLookup, (areaLookup, index));
        //             }
        //         }

        //         Debug.Log("Loaded, file: " + fileName);
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"Error while deserializing JSON: {e.Message}");
        //     }

        //     return dictionary;
        // }

        public static Dictionary<Vector2, Vector2> LoadData_Vector2Dictionary(string directoryPath, string fileName)
        {
            if (LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;
            // string filePath = Path.Combine(directoryPath, fileName + ".json");
            // if (!File.Exists(filePath))
            // {
            //     Debug.LogError("Error while loading data: File not found: " + fileName);
            //     return null;
            // }
            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter() },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };
            Dictionary<Vector2, Vector2> dictionary = null;
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, Vector2Serialized>>(json, settings);
                dictionary = new Dictionary<Vector2, Vector2>();

                foreach (var item in result)
                {
                    var key = Vector2.zero;
                    if (new Vector2Converter().TryConvertFrom(item.Key, out key)) dictionary[key] = item.Value.ToVector2();
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }
            return dictionary;
        }




        public static Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> LoadData_WorldCell_ByParentCell(string directoryPath, string fileName, int cellSize)
        {
            if (WorldSaveLoadUtil.LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter() },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };

            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> dictionary = null;
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, WorldCellData>>(json, settings);
                dictionary = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();

                foreach (var item in result)
                {
                    var key = Vector2.zero;

                    if (new Vector2Converter().TryConvertFrom(item.Key, out key))
                    {
                        WorldCellData cellData = item.Value;

                        Vector2 lookup = cellData.lookup.ToVector2();
                        Vector3 center = cellData.center.ToVector3();
                        HexagonCellPrototype cell = new HexagonCellPrototype(center, cellSize, true);
                        cellData.PastToCell(cell);

                        if (dictionary.ContainsKey(cell.parentLookup) == false) dictionary.Add(cell.parentLookup, new Dictionary<Vector2, HexagonCellPrototype>());

                        dictionary[cell.parentLookup].Add(lookup, cell);
                    }
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }
            return dictionary;
        }


        public static Dictionary<Vector2, Dictionary<int, List<Vector3>>> LoadData(string directoryPath, string fileName)
        {
            if (LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

            // string filePath = Path.Combine(directoryPath, fileName + ".json");
            // if (!File.Exists(filePath))
            // {
            //     Debug.LogError("Error while loading data: File not found.");
            //     return null;
            // }
            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter(), new StringEnumConverter() },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };

            Dictionary<Vector2, Dictionary<int, List<Vector3>>> dictionary = null;
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<Vector3Serialized>>>>(json, settings);
                dictionary = new Dictionary<Vector2, Dictionary<int, List<Vector3>>>();

                foreach (var item in result)
                {
                    var key = Vector2.zero;

                    if (new Vector2Converter().TryConvertFrom(item.Key, out key))
                    {
                        var subDictionary = new Dictionary<int, List<Vector3>>();

                        foreach (var subItem in item.Value)
                        {
                            if (int.TryParse(subItem.Key, out int subKey))
                            {
                                var vector3List = new List<Vector3>();

                                foreach (var vector3Serialized in subItem.Value)
                                {
                                    vector3List.Add(vector3Serialized.ToVector3());
                                }

                                subDictionary[subKey] = vector3List;
                            }
                        }

                        dictionary[key] = subDictionary;
                    }
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }

            return dictionary;
        }

        public static void SaveData_WorldAreaTerrainChunkData_ByLookup(Dictionary<Vector2, TerrainChunkData> terrainChunkData_byLookup, string directoryPath, string fileName)
        {
            Dictionary<Vector2, TerrainChunkJsonData> dict = new Dictionary<Vector2, TerrainChunkJsonData>();
            foreach (var pair in terrainChunkData_byLookup)
            {
                Vector2 chunkLookup = pair.Key;
                dict.Add(chunkLookup, pair.Value.ConvertToJson());
                // dict.Add(chunkLookup, new TerrainChunkJsonData(pair.Value));
            }
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

        public static void SaveData_TerrainChunkVertexData_ByLookup(Dictionary<Vector2, TerrainChunkVertexData> terrainChunkVertexData_ByLookup, string directoryPath, string fileName)
        {
            Dictionary<Vector2, TerrainChunkVertexJson> dict = new Dictionary<Vector2, TerrainChunkVertexJson>();
            foreach (var pair in terrainChunkVertexData_ByLookup)
            {
                Vector2 chunkLookup = pair.Key;
                dict.Add(chunkLookup, pair.Value.ConvertToJson());
            }
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



        public static void SaveData_Vector2Dictionary(Dictionary<Vector2, Vector2> dictionary, string directoryPath, string fileName)
        {
            Dictionary<Vector2, Vector2Serialized> dict = new Dictionary<Vector2, Vector2Serialized>();
            // Convert Vector2 points to Vector2Serialized that only stores x and y values
            foreach (var pair in dictionary)
            {
                Vector2 coord = pair.Key;
                dict.Add(coord, new Vector2Serialized(pair.Value));
            }

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

        public static void SaveData_GridByLookup(Dictionary<Vector2, Vector2[,]> gridByLookup, string directoryPath, string fileName)
        {
            Dictionary<Vector2, Vector2Serialized[,]> dict = new Dictionary<Vector2, Vector2Serialized[,]>();

            // Convert Vector2[,] grid data to Vector2Serialized[,] that only stores x and y values
            foreach (var pair in gridByLookup)
            {
                Vector2 coord = pair.Key;
                Vector2[,] gridData = pair.Value;

                Vector2Serialized[,] serializedGrid = new Vector2Serialized[gridData.GetLength(0), gridData.GetLength(1)];

                for (int i = 0; i < gridData.GetLength(0); i++)
                {
                    for (int j = 0; j < gridData.GetLength(1); j++)
                    {
                        serializedGrid[i, j] = new Vector2Serialized(gridData[i, j]);
                    }
                }

                dict.Add(coord, serializedGrid);
            }

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



        public static void Save_WorldAreaData(Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldAreas_ByRegion, string directoryPath, string fileName)
        {
            Dictionary<Vector2, WorldCellData> dict = new Dictionary<Vector2, WorldCellData>();
            // Convert Vector2 points to Vector2Serialized that only stores x and y values
            foreach (Vector2 key in worldAreas_ByRegion.Keys)
            {
                foreach (var kvp in worldAreas_ByRegion[key])
                {
                    Vector2 lookupCoord = kvp.Key;
                    HexagonCellPrototype cell = kvp.Value;

                    WorldCellData cellData = new WorldCellData();
                    cellData.CopyFromCell(cell, lookupCoord);


                    dict.Add(lookupCoord, cellData);
                }
            }
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

        public static void SaveData_WorldCell_ByParentCell(Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldCell_ByParentCell, string directoryPath, string fileName)
        {
            Dictionary<Vector2, WorldCellData> dict = new Dictionary<Vector2, WorldCellData>();
            // Convert Vector2 points to Vector2Serialized that only stores x and y values
            foreach (Vector2 key in worldCell_ByParentCell.Keys)
            {
                foreach (var kvp in worldCell_ByParentCell[key])
                {
                    Vector2 lookupCoord = kvp.Key;
                    HexagonCellPrototype cell = kvp.Value;

                    WorldCellData cellData = new WorldCellData();
                    cellData.CopyFromCell(cell, lookupCoord);

                    dict.Add(lookupCoord, cellData);
                }
            }
            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);

            try
            {
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                string filePath = Path.Combine(directoryPath, fileName + ".json");
                File.WriteAllText(filePath, json);

                Debug.Log("SaveData!: \n" + filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while saving data: " + ex.Message);
            }
        }



        public static void SaveData_WorldRegion(Dictionary<Vector2, HexagonCellPrototype> worldRegionCellsByCoordinate, string directoryPath, string fileName)
        {
            Dictionary<Vector2, WorldCellData> dict = new Dictionary<Vector2, WorldCellData>();
            foreach (var kvp in worldRegionCellsByCoordinate)
            {
                Vector2 lookupCoord = kvp.Key;
                HexagonCellPrototype cell = kvp.Value;

                WorldCellData cellData = new WorldCellData();
                cellData.CopyFromCell(cell, lookupCoord);

                dict.Add(lookupCoord, cellData);
            }

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

        public static void SaveData_CellGrid_ByParentWorldSpace_ByWorldArea(
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> _cellLookup_ByLayer_BySize_ByWorldSpace,
            string directoryPath,
            string fileName
        )
        {
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, WorldCellData>>>> dict = new Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, WorldCellData>>>>();
            // Convert Vector2 points to Vector2Serialized that only stores x and y values
            foreach (Vector2 worldspaceLookup in _cellLookup_ByLayer_BySize_ByWorldSpace.Keys)
            {
                if (dict.ContainsKey(worldspaceLookup) == false) dict.Add(worldspaceLookup, new Dictionary<int, Dictionary<int, Dictionary<Vector2, WorldCellData>>>());

                foreach (int currentSize in _cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].Keys)
                {
                    if (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4) continue;

                    if (dict[worldspaceLookup].ContainsKey(currentSize) == false) dict[worldspaceLookup].Add(currentSize, new Dictionary<int, Dictionary<Vector2, WorldCellData>>());

                    foreach (int currentLayer in _cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].Keys)
                    {
                        if (dict[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false) dict[worldspaceLookup][currentSize].Add(currentLayer, new Dictionary<Vector2, WorldCellData>());

                        foreach (var kvp in _cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer])
                        {
                            Vector2 lookupCoord = kvp.Key;

                            if (dict[worldspaceLookup][currentSize][currentLayer].ContainsKey(lookupCoord)) continue;

                            HexagonCellPrototype cell = kvp.Value;

                            WorldCellData cellData = new WorldCellData();
                            cellData.CopyFromCell(cell, lookupCoord);

                            dict[worldspaceLookup][currentSize][currentLayer].Add(lookupCoord, cellData);
                        }

                    }
                }
            }
            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);

            try
            {
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                string filePath = Path.Combine(directoryPath, fileName + ".json");
                File.WriteAllText(filePath, json);

                Debug.Log("SaveData!: \n" + filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error while saving data: " + ex.Message);
            }
        }



    }

    #region Json Converters 

    // [System.Serializable]
    // public class TerrainChunkLookupData
    // {
    //     public Vector2Serialized chunkLookup;
    //     public Vector2Serialized areaLookup;
    //     public int index;
    // }

    [System.Serializable]
    public class WorldCellData
    {
        public Vector2Serialized parentLookup;
        public Vector2Serialized worldspacelookup;
        public Vector2Serialized lookup;
        public Vector3Serialized center;

        public Vector2Serialized[] neighborLookups;
        public Vector2Serialized[] neighborWorldspaceLookups;
        public Vector2Serialized[] layerNeighborLookups;
        public Vector2Serialized[] layerNeighborWorldspaceLookups;


        public void CopyFromCell(HexagonCellPrototype cell, Vector2 lookupCoord)
        {
            this.lookup = new Vector2Serialized(lookupCoord);
            this.center = new Vector3Serialized(cell.center);
            this.parentLookup = new Vector2Serialized(cell.parentLookup);
            this.worldspacelookup = new Vector2Serialized(cell.worldspaceLookup);

            List<Vector2Serialized> neighborLookups = new List<Vector2Serialized>();
            List<Vector2Serialized> neighborWorldSpaceLookups = new List<Vector2Serialized>();
            foreach (HexagonCellPrototype neighbor in cell.neighbors)
            {
                if (neighbor == null || neighbor.IsSameLayer(cell) == false) continue;

                neighborLookups.Add(new Vector2Serialized(neighbor.GetLookup()));
                neighborWorldSpaceLookups.Add(new Vector2Serialized(neighbor.worldspaceLookup));
            }
            List<Vector2Serialized> layerNeighborLookups = new List<Vector2Serialized>();
            List<Vector2Serialized> layerneighborWorldSpaceLookups = new List<Vector2Serialized>();
            foreach (HexagonCellPrototype neighbor in cell.layerNeighbors)
            {
                if (neighbor == null || neighbor.IsSameLayer(cell)) continue;

                layerNeighborLookups.Add(new Vector2Serialized(neighbor.GetLookup()));
                layerneighborWorldSpaceLookups.Add(new Vector2Serialized(neighbor.worldspaceLookup));
            }

            this.neighborLookups = neighborLookups.ToArray();
            this.neighborWorldspaceLookups = neighborWorldSpaceLookups.ToArray();

            this.layerNeighborLookups = layerNeighborLookups.ToArray();
            this.layerNeighborWorldspaceLookups = layerneighborWorldSpaceLookups.ToArray();
        }

        public void PastToCell(HexagonCellPrototype cell)
        {
            // cell.SetWorldCoordinate(coordinate.ToVector2());
            cell.SetParentLookup(parentLookup.ToVector2());
            cell.SetWorldSpaceLookup(worldspacelookup.ToVector2());

            List<CellWorldData> neighborWorldData = new List<CellWorldData>();
            for (int i = 0; i < this.neighborLookups.Length; i++)
            {
                Vector2Serialized lookupCoord = this.neighborLookups[i];
                Vector2Serialized worldSpaceLookup = this.neighborWorldspaceLookups[i];

                CellWorldData neigborData = new CellWorldData();

                neigborData.layer = cell.layer;
                neigborData.lookup = lookupCoord.ToVector2();
                neigborData.worldspaceLookup = worldspacelookup.ToVector2();
                neigborData.parentLookup = parentLookup.ToVector2();
                neighborWorldData.Add(neigborData);
            }

            for (int i = 0; i < this.layerNeighborLookups.Length; i++)
            {
                Vector2Serialized lookupCoord = this.layerNeighborLookups[i];
                Vector2Serialized worldSpaceLookup = this.layerNeighborWorldspaceLookups[i];

                CellWorldData neigborData = new CellWorldData();

                neigborData.layer = i == 0 ? cell.layer - 1 : cell.layer + 1;
                neigborData.lookup = lookupCoord.ToVector2();
                neigborData.worldspaceLookup = worldspacelookup.ToVector2();
                neigborData.parentLookup = parentLookup.ToVector2();
                neighborWorldData.Add(neigborData);
            }
            cell.neighborWorldData = neighborWorldData.ToArray();
        }
    }


    [System.Serializable]
    public class Vector3Serialized
    {
        public float x;
        public float y;
        public float z;
        public Vector3Serialized(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    public class Vector2Serialized
    {
        public float x;
        public float y;

        public Vector2Serialized(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }


    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteValue($"({value.x},{value.y})");
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string[] values = reader.Value.ToString().Replace("(", "").Replace(")", "").Split(',');
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                {
                    return new Vector2(x, y);
                }
            }
            throw new JsonSerializationException("Unable to convert value to Vector2.");
        }

        public bool TryConvertFrom(string input, out Vector2 result)
        {
            result = Vector2.zero;
            if (input.StartsWith("(") && input.EndsWith(")"))
            {
                input = input.Substring(1, input.Length - 2);
                string[] values = input.Split(',');
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                {
                    result = new Vector2(x, y);
                    return true;
                }
            }
            return false;
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteValue($"({value.x},{value.y},{value.z})");
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string[] values = reader.Value.ToString().Replace("(", "").Replace(")", "").Split(',');
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y) && float.TryParse(values[2], out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
            throw new JsonSerializationException("Unable to convert value to Vector3.");
        }

        public bool TryConvertFrom(string input, out Vector3 result)
        {
            result = Vector3.zero;
            if (input.StartsWith("(") && input.EndsWith(")"))
            {
                input = input.Substring(1, input.Length - 2);
                string[] values = input.Split(',');
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y) && float.TryParse(values[2], out float z))
                {
                    result = new Vector3(x, y, z);
                    return true;
                }
            }
            return false;
        }
    }

    #endregion


}