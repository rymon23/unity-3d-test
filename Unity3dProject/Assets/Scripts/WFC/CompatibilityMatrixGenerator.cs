using System.Collections.Generic;
using UnityEngine;

public class CompatibilityMatrixGenerator : MonoBehaviour
{
    [SerializeField]
    private SlotCompatibility[] compatibilityTable;

    public bool[,] matrix;

    public bool[,] GetCompatibilityMatrix()
    {
        // Create a 2D array with the same dimensions as the compatibilityTable array
        bool[,] matrix = new bool[compatibilityTable.Length, compatibilityTable.Length];

        // Iterate through each element in the compatibilityTable array
        for (int i = 0; i < compatibilityTable.Length; i++)
        {
            // Copy the values from the isCompatible list into the matrix
            for (int j = 0; j < compatibilityTable[i].isCompatible.Count; j++)
            {
                matrix[i, j] = compatibilityTable[i].isCompatible[j];
            }
        }
        return matrix;
    }


    private void ResizeCompatibilityTable()
    {
        // Iterate through each element in the compatibilityTable array
        for (int i = 0; i < compatibilityTable.Length; i++)
        {
            // If the isCompatible list is too long, remove excess elements
            if (compatibilityTable[i].isCompatible.Count > compatibilityTable.Length)
            {
                compatibilityTable[i].isCompatible.RemoveRange(compatibilityTable.Length, compatibilityTable[i].isCompatible.Count - compatibilityTable.Length);
            }
            // If the isCompatible list is too short, add missing elements
            else if (compatibilityTable[i].isCompatible.Count < compatibilityTable.Length)
            {
                for (int j = compatibilityTable[i].isCompatible.Count; j < compatibilityTable.Length; j++)
                {
                    compatibilityTable[i].isCompatible.Add(false);
                }
            }
        }
    }


    private void OnValidate()
    {
        ResizeCompatibilityTable();

        matrix = GetCompatibilityMatrix();
    }
}


[System.Serializable]
public struct SlotCompatibility
{
    public List<bool> isCompatible;
}