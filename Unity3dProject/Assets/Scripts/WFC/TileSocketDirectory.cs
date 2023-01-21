using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileSocketConstants
{
    Any = 0,
    Edge = 1,
    Entrance = 2,
    OuterWall = 3,
    InnerWall = 4,
    WallPart = 5,
    InnerCell = 6, //Generic for any nonEdge cell
    InnerCuster = 7 //Generic for any nonEdge cell
}


[CreateAssetMenu(fileName = "New Socket Directory", menuName = "Socket Directory")]
public class TileSocketDirectory : ScriptableObject
{
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
    [System.Serializable]
    public struct SocketCompatibility
    {
        public string name;
        public List<SocketRelation> relations;
        public SocketCompatibility(string name, List<SocketRelation> relations)
        {
            this.name = name;
            this.relations = relations;
        }
    }

    public SocketEntry[] sockets;
    private int socketsLength = 0;
    [SerializeField] private bool revaluate = true;
    [SerializeField] private bool defaultCompatibility = true;
    [SerializeField] private List<SocketCompatibility> compatibilityTable;
    public bool[,] matrix;



    // [System.Serializable]
    // public class Matrix2D
    // {
    //     public float[,] matrix = new float[9, 9];
    // }

    // public Matrix2D myMatrix = new Matrix2D();


    private void OnValidate()
    {
        // Only generate new colors if the size of the array has changed
        if (sockets != null && sockets.Length != socketsLength)
        {
            socketsLength = sockets.Length;

            // Create a hash set to store the generated colors
            HashSet<Color> generatedColors = new HashSet<Color>();

            // Generate a random color for each socket
            for (int i = 0; i < sockets.Length; i++)
            {
                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColors.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColors.Add(randomColor);

                // Assign the unique color to the socket
                sockets[i].color = randomColor;
            }
        }

        if (sockets == null || compatibilityTable == null) return;

        ValidateTable();
        revaluate = false;

        if (compatibilityTable.Count != sockets.Length)
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

    private void ResizeCompatibilityTable()
    {
        if (compatibilityTable.Count < sockets.Length)
        {
            List<SocketCompatibility> newEntries = new List<SocketCompatibility>();

            for (int i = compatibilityTable.Count - 1; i < sockets.Length; i++)
            {
                SocketCompatibility newSocketCompatibility = new SocketCompatibility();
                newSocketCompatibility.name = sockets[i].name;
                compatibilityTable.Add(newSocketCompatibility);
            }
        }
        else if (compatibilityTable.Count > sockets.Length)
        {
            while (compatibilityTable.Count > sockets.Length)
            {
                compatibilityTable.Remove(compatibilityTable[compatibilityTable.Count - 1]);
            }
            // // If the compatibilityTable list is too long, remove excess elements
            // compatibilityTable.RemoveRange(sockets.Length, compatibilityTable.Count - sockets.Length);
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
                    if (sockets.Length > j)
                    {
                        compatibilityTable[index].relations.Add(new SocketRelation("ID: " + j + " - " + sockets[j].name, defaultCompatibility));
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
        int anySocketIX = 0;
        int outOfBoundsSocketIX = 1;
        for (int index = 0; index < compatibilityTable.Count; index++)
        {
            SocketCompatibility newSocketCompatibility = new SocketCompatibility(sockets[index].name, compatibilityTable[index].relations);
            compatibilityTable[index] = newSocketCompatibility;

            for (int relationIX = 0; relationIX < compatibilityTable[index].relations.Count; relationIX++)
            {
                // Set the name of the socket and the name of the relations
                bool compatibility = compatibilityTable[index].relations[relationIX].isCompatible;
                if (index == anySocketIX) compatibility = true;

                compatibilityTable[index].relations[relationIX] = new SocketRelation("ID: " + relationIX + " - " + sockets[relationIX].name, compatibility);

                if (relationIX < compatibilityTable.Count && relationIX < compatibilityTable[index].relations.Count
                    )// && index == anySocketIX || index == outOfBoundsSocketIX)
                {
                    compatibilityTable[relationIX].relations[index] = new SocketRelation("ID: " + index + " - " + sockets[index].name, compatibility);
                }
            }
        }
    }

    private void ValidateTable()
    {


        ResizeCompatibilityTable();

        EvaluateRelations();

        UpdateRelationsData();

    }


}
