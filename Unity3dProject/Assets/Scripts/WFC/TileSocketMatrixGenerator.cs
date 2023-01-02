using UnityEngine;
using System.Collections.Generic;

public class TileSocketMatrixGenerator : MonoBehaviour
{
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
    }

    [SerializeField] private SocketCompatibility[] compatibilityTable;
    [SerializeField] private TileSocketDirectory tileSocketDirectory;

    public bool[,] matrix;

    public bool[,] GetCompatibilityMatrix()
    {
        // Create a 2D array with the same dimensions as the compatibilityTable array
        bool[,] matrix = new bool[compatibilityTable.Length, compatibilityTable.Length];

        // Iterate through each element in the compatibilityTable array
        for (int i = 0; i < compatibilityTable.Length; i++)
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
        // Iterate through each element in the compatibilityTable array
        for (int i = 0; i < compatibilityTable.Length; i++)
        {
            // If the relations list is too long, remove excess elements
            if (compatibilityTable[i].relations.Count > compatibilityTable.Length)
            {
                compatibilityTable[i].relations.RemoveRange(compatibilityTable.Length, compatibilityTable[i].relations.Count - compatibilityTable.Length);
            }
            // If the relations list is too short, add missing elements
            else if (compatibilityTable[i].relations.Count < compatibilityTable.Length)
            {
                for (int j = compatibilityTable[i].relations.Count; j < compatibilityTable.Length; j++)
                {
                    // Set the name of the socket relation
                    if (tileSocketDirectory.sockets.Length > j)
                    {
                        compatibilityTable[i].relations.Add(new SocketRelation("ID: " + j + " - " + tileSocketDirectory.sockets[j].name, false));
                    }
                    // If there are no sockets in the TileSocketDirectory, use a default name
                    else
                    {
                        compatibilityTable[i].relations.Add(new SocketRelation("ID: " + j + " - Unknown Socket", false));
                    }
                }
            }

            // Set the name of the socket and the name of the relations
            if (tileSocketDirectory.sockets.Length > i)
            {
                compatibilityTable[i].name = "ID: " + i + " - " + tileSocketDirectory.sockets[i].name;
                for (int j = 0; j < compatibilityTable[i].relations.Count; j++)
                {
                    if (tileSocketDirectory.sockets.Length > j)
                    {
                        bool temp = compatibilityTable[i].relations[j].isCompatible;
                        compatibilityTable[i].relations[j] = new SocketRelation("ID: " + j + " - " + tileSocketDirectory.sockets[j].name, temp);

                        compatibilityTable[j].relations[i] = new SocketRelation("ID: " + i + " - " + tileSocketDirectory.sockets[i].name, temp);
                    }
                }
            }
        }

    }

    private void OnValidate()
    {
        if (!tileSocketDirectory || compatibilityTable == null) return;

        ResizeCompatibilityTable();

        matrix = GetCompatibilityMatrix();
        Debug.Log("Matrix Length: " + matrix.Length);
    }
}


