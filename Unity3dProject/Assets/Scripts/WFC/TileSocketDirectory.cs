using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProceduralBase;
using UnityEditor;


namespace WFCSystem
{
    // public enum TileMetaSocket
    // {
    //     Any = 0,
    //     Edge = 1,
    //     Empty = 2,
    //     Entrance = 3,
    //     Path = 4,
    //     PathSide = 4,
    //     WallSide = 3,
    //     WallSideOuter = 3,
    //     WallSideInner = 4,
    //     WallPart = 5,
    //     InnerCell = 6, 
    //     InnerCuster = 7
    //     BuildingFront
    //     BuildingBack
    //     BuildingSide
    //     BuildingOutlet

    //     VerticalFront
    //     VerticalBack
    //     VerticalSide
    //     VerticalOutlet
    // }

    public enum TileSocketPrimitive
    {
        Any = 0,
        Edge = 1,
        InnerCell = 2, //Generic for any nonEdge cell

        EntranceOuter = 3,
        EntrancePart = 4,
        EntranceSide = 5,
        EntranceInner = 6,

        WallBegin,
        WallPart,
        OuterWall,
        InnerWall,
        WallEnd,


        Cluster,
        ClusterPart,

        PathGeneric = 14,
        PathBegin,
        PathFinish,
        PathPart,
        PathSide,

        LeveledInner,
        LeveledOuter,
        LeveledEdgePart,
        LeveledRampTop,
        LeveledRampSide,
        LeveledRampBottom,

        EmptySpace,

        BuildingFront,
        BuildingSide,
        BuildingBack,
        BuildingEntry,
        BuildingPart,
        BuildingLevelOutlet,
        BuildingLevelConnector,

        VerticalPart,
        VerticalPartFront,
        VerticalPartSide,
        VerticalPartBack,

    }

    // public enum TileSocketConstants
    // {
    //     Any = 0,
    //     Edge = 1,
    //     Entrance = 2,
    //     OuterWall = 3,
    //     InnerWall = 4,
    //     WallPart = 5,
    //     InnerCell = 6, //Generic for any nonEdge cell
    //     InnerCuster = 7 //Generic for any nonEdge cell
    // }


    [CreateAssetMenu(fileName = "New Socket Directory", menuName = "Socket Directory")]
    public class TileSocketDirectory : ScriptableObject, ITileSocketDirectory
    {
        [SerializeField] private bool revaluate = true;
        [SerializeField] private bool defaultCompatibility = true;
        public List<SocketCompatibility> compatibilityTable;
        [SerializeField] private List<Color> generatedColors = new List<Color>();
        [SerializeField] private string[] enums = Enum.GetNames(typeof(TileSocketPrimitive));
        public bool[,] matrix;


        private void OnValidate()
        {
            enums = Enum.GetNames(typeof(TileSocketPrimitive));

            // UpdateSocketLabels();
            if (compatibilityTable == null) return;

            ValidateTable();
            revaluate = false;

            if (compatibilityTable.Count != enums.Length)
            {
                Debug.LogError("compatibilityTable.Count != tileSocketDirectory.sockets.Length");
            }

            matrix = GetCompatibilityMatrix();
        }

        public bool[,] GetCompatibilityMatrix()
        {
            // Create a 2D array with the same dimensions as the compatibilityTable array
            bool[,] matrix = new bool[compatibilityTable.Count, compatibilityTable.Count];

            // Iterate through each element in the compatibilityTable array
            for (int i = 0; i < compatibilityTable.Count; i++)
            {
                // Copy the values from the isCompatible list into the matrix
                for (int j = 0; j < compatibilityTable[i].relations.Count; j++)
                {
                    matrix[i, j] = compatibilityTable[i].relations[j].isCompatible;
                }
            }
            return matrix;
        }
        public List<SocketCompatibility> GetCompatibilityTable()
        {
            return compatibilityTable;
        }
        private void ResizeCompatibilityTable()
        {
            if (compatibilityTable.Count < enums.Length)
            {
                // List<SocketCompatibility> newEntries = new List<SocketCompatibility>();

                for (int i = compatibilityTable.Count - 1; i < enums.Length; i++)
                {
                    SocketCompatibility newSocketCompatibility = new SocketCompatibility();
                    newSocketCompatibility.name = enums[i];
                    newSocketCompatibility.relations = new List<SocketRelation>();

                    compatibilityTable.Add(newSocketCompatibility);
                }
            }
            else if (compatibilityTable.Count > enums.Length)
            {
                while (compatibilityTable.Count > enums.Length)
                {
                    compatibilityTable.Remove(compatibilityTable[compatibilityTable.Count - 1]);
                }
            }
        }


        private void UpdateColors()
        {
            if (generatedColors.Count < enums.Length)
            {
                for (int i = generatedColors.Count - 1; i < enums.Length; i++)
                {
                    generatedColors.Add(Color.white);
                }
            }
            else if (generatedColors.Count > enums.Length)
            {
                while (generatedColors.Count > enums.Length)
                {
                    generatedColors.Remove(generatedColors[generatedColors.Count - 1]);
                }
            }

            // Create a hash set to store the generated colors
            HashSet<Color> generatedColorSet = new HashSet<Color>();

            // Generate a random color for each socket
            for (int i = 0; i < generatedColors.Count; i++)
            {
                if (generatedColors[i] != Color.white) continue;

                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = UnityEngine.Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = UnityEngine.Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = UnityEngine.Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColorSet.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColorSet.Add(randomColor);

                // Assign the unique color to the socket
                generatedColors.Add(randomColor);
            }
        }


        private void EvaluateRelations()
        {
            for (int index = 0; index < compatibilityTable.Count; index++)
            {
                // If the relations list is too long, remove excess elements
                if (compatibilityTable[index].relations.Count > compatibilityTable.Count)
                {
                    compatibilityTable[index].relations.RemoveRange(compatibilityTable.Count, compatibilityTable[index].relations.Count - compatibilityTable.Count);
                }

                // If the relations list is too short, add missing elements
                else if (compatibilityTable[index].relations.Count < compatibilityTable.Count)
                {
                    for (int j = compatibilityTable[index].relations.Count; j < compatibilityTable.Count; j++)
                    {
                        // Set the name of the socket relation
                        if (enums.Length > j)
                        {
                            compatibilityTable[index].relations.Add(new SocketRelation("ID: " + j + " - " + enums[j], defaultCompatibility));
                        }
                        // If there are no sockets in the TileSocketDirectory, use a default name
                        else
                        {
                            compatibilityTable[index].relations.Add(new SocketRelation("ID: " + j + " - Unknown Socket", defaultCompatibility));
                        }
                    }
                }
            }
        }

        private void UpdateRelationsData()
        {
            for (int index = 0; index < compatibilityTable.Count; index++)
            {
                ITileSocketDirectory.ResetCompatibilityState resetState = compatibilityTable[index].resetCompatibility;

                SocketCompatibility newSocketCompatibility = new SocketCompatibility(enums[index],
                                                                        compatibilityTable[index].relations,
                                                                        generatedColors[index]
                                                                        );
                compatibilityTable[index] = newSocketCompatibility;

                for (int relationIX = 0; relationIX < compatibilityTable[index].relations.Count; relationIX++)
                {
                    // Set the name of the socket and the name of the relations
                    bool compatibility = compatibilityTable[index].relations[relationIX].isCompatible;

                    if (index == (int)TileSocketPrimitive.Any || relationIX == (int)TileSocketPrimitive.Any)
                    {
                        compatibility = true;
                    }
                    else if (resetState != ITileSocketDirectory.ResetCompatibilityState.Unset)
                    {
                        compatibility = (resetState == ITileSocketDirectory.ResetCompatibilityState.All) ? true : false;
                    }

                    compatibilityTable[index].relations[relationIX] = new SocketRelation("ID: " + relationIX + " - " + enums[relationIX], compatibility);

                    if (relationIX < compatibilityTable.Count && relationIX < compatibilityTable[index].relations.Count
                        )// && index == (int)TileSocketPrimitive.Any || index == (int)TileSocketPrimitive.Edge)
                    {

                        compatibilityTable[relationIX].relations[index] = new SocketRelation("ID: " + index + " - " + enums[index], compatibility);
                    }
                }
            }
        }

        private void ValidateTable()
        {
            UpdateColors();

            ResizeCompatibilityTable();

            EvaluateRelations();

            UpdateRelationsData();
        }


        // [System.Serializable]
        // public struct SocketEntry
        // {
        //     public string name;
        //     public string description;
        //     public Color color;
        // }
        // [System.Serializable]
        // public struct SocketRelation
        // {
        //     public string name;
        //     public bool isCompatible;

        //     public SocketRelation(string name, bool isCompatible)
        //     {
        //         this.name = name;
        //         this.isCompatible = isCompatible;
        //     }
        // }
        // [System.Serializable]
        // public struct SocketCompatibility
        // {
        //     public string name;
        //     public Color color;
        //     public ResetCompatibilityState resetCompatibility;
        //     public List<SocketRelation> relations;
        //     public SocketCompatibility(string _name, List<SocketRelation> _relations, Color _color)
        //     {
        //         this.name = _name;
        //         this.relations = _relations;
        //         this.color = _color;
        //         this.resetCompatibility = ResetCompatibilityState.Unset;
        //     }
        // }
        // public enum ResetCompatibilityState { Unset = 0, None, All }

    }
}