using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace WFCSystem
{
    public enum TileSocketEnvironment
    {
        Any,
        Interior,
        Exterior
    }
    public enum TileSocketFragType
    {
        Unset,
        Generic,
        Part,
        End,
        Outlet,
        Start,
    }

    public enum MicroTileSocket
    {
        Any = 0,
        Edge = 1,
        EmptySpace = 2,
        UnassignedEdge = 3,
        UnassignedInner = 4,
        EntranceGeneric,
        EntranceSide,
        InnerCellGeneric,
        WallGeneric,
        WallGenericPart,
        WallGenericCorner,
        WallGenericEnd,
        GenericStructureEnd,
        FloorGenericPart,
        FloorGenericEnd,

        DoorWay,
        DoorWayPart,
        DoorWaySide,

        BalconyEntry,

        Floor_Part_A,
        Floor_End_A,
        Floor_Corner_A,
        FloorWall_Part_A,
        FloorWall_End_A,
    }

    public enum MicroTileSocket_Vertical
    {
        Any = 0,
        Edge = 1,
        EmptySpace = 2,
        Unassigned_Edge = 3,
        Unassigned_Inner = 4,
        InnerCell = 5,

        Wall_BTM,
        Wall_End,
        Wall_Corner90,

        RoofGeneric,
        RoofGenericPart,
        CeilingGeneric,

        StairwayGenericTop,
        StairwayGenericBottom,
        StairwayGenericConnector,

        VerticalCorner_M,
        VerticalCorner_F,
    }

    [System.Serializable]

    [CreateAssetMenu(fileName = "New Micro Tile Socket Directory", menuName = "Micro Tile Socket Directory")]
    public class MicroTileSocketDirectory : ScriptableObject, ITileSocketDirectory
    {
        [SerializeField] private bool revaluate = true;
        [SerializeField] private bool defaultCompatibility = true;
        public List<SocketCompatibility> compatibilityTable;
        [SerializeField] private List<Color> generatedColors = new List<Color>();
        [SerializeField] private string[] enums = Enum.GetNames(typeof(MicroTileSocket));
        public bool[,] matrix;


        private void OnValidate()
        {
            enums = Enum.GetNames(typeof(MicroTileSocket));

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

                    if (index == (int)MicroTileSocket.Any || relationIX == (int)MicroTileSocket.Any)
                    {
                        compatibility = true;
                    }
                    else if (resetState != ITileSocketDirectory.ResetCompatibilityState.Unset)
                    {
                        compatibility = (resetState == ITileSocketDirectory.ResetCompatibilityState.All) ? true : false;
                    }

                    compatibilityTable[index].relations[relationIX] = new SocketRelation("ID: " + relationIX + " - " + enums[relationIX], compatibility);

                    if (relationIX < compatibilityTable.Count && relationIX < compatibilityTable[index].relations.Count)
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
    }

    [System.Serializable]
    public struct SocketCompatibility
    {
        public string name;
        public Color color;
        public ITileSocketDirectory.ResetCompatibilityState resetCompatibility;
        public List<SocketRelation> relations;
        public SocketCompatibility(string _name, List<SocketRelation> _relations, Color _color)
        {
            this.name = _name;
            this.relations = _relations;
            this.color = _color;
            this.resetCompatibility = ITileSocketDirectory.ResetCompatibilityState.Unset;
        }
    }
    [System.Serializable]
    public struct SocketEntry
    {
        public string name;
        public string description;
        public Color color;
    }
    [System.Serializable]
    public struct SocketRelation
    {
        public string name;
        public bool isCompatible;

        public SocketRelation(string name, bool isCompatible)
        {
            this.name = name;
            this.isCompatible = isCompatible;
        }
    }
}