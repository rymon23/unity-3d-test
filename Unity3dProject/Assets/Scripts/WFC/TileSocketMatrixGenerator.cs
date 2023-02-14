using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WFCSystem
{
    public class TileSocketMatrixGenerator : MonoBehaviour
    {
        [SerializeField] private TileSocketDirectory tileSocketDirectory;
        [SerializeField] private bool defaultCompatibility = true;

        public bool[,] matrix;

        private void Awake()
        {
            InitializeMatrix();

        }

        private void OnValidate()
        {
            InitializeMatrix();
        }

        private void InitializeMatrix()
        {
            if (!tileSocketDirectory)
            {
                Debug.LogError("No tileSocketDirectory provided!");
                return;
            }
            matrix = tileSocketDirectory.GetCompatibilityMatrix();
        }

        public bool[,] GetCompatibilityMatrix()
        {
            return tileSocketDirectory.GetCompatibilityMatrix();
        }


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
        //     public List<SocketRelation> relations;
        // }
        // [SerializeField] private SocketCompatibility[] compatibilityTable;
        // public bool[,] GetCompatibilityMatrix()
        // {
        //     // Create a 2D array with the same dimensions as the compatibilityTable array
        //     bool[,] matrix = new bool[compatibilityTable.Length, compatibilityTable.Length];

        //     // Iterate through each element in the compatibilityTable array
        //     for (int i = 0; i < compatibilityTable.Length; i++)
        //     {
        //         // Copy the values from the isCompatible list into the matrix
        //         for (int j = 0; j < compatibilityTable[i].relations.Count; j++)
        //         {
        //             matrix[i, j] = compatibilityTable[i].relations[j].isCompatible;
        //         }
        //     }
        //     return matrix;
        // }

        // private void ResizeCompatibilityTable()
        // {
        //     int anySocketIX = 0;
        //     int outOfBoundsSocketIX = 1;

        //     // Iterate through each element in the compatibilityTable array
        //     for (int i = 0; i < compatibilityTable.Length; i++)
        //     {
        //         // If the relations list is too long, remove excess elements
        //         if (compatibilityTable[i].relations.Count > compatibilityTable.Length)
        //         {
        //             compatibilityTable[i].relations.RemoveRange(compatibilityTable.Length, compatibilityTable[i].relations.Count - compatibilityTable.Length);
        //         }
        //         // If the relations list is too short, add missing elements
        //         else if (compatibilityTable[i].relations.Count < compatibilityTable.Length)
        //         {

        //             for (int j = compatibilityTable[i].relations.Count; j < compatibilityTable.Length; j++)
        //             {
        //                 // Set the name of the socket relation
        //                 if (tileSocketDirectory.sockets.Length > j)
        //                 {
        //                     compatibilityTable[i].relations.Add(new SocketRelation("ID: " + j + " - " + tileSocketDirectory.sockets[j].name, defaultCompatibility));
        //                 }
        //                 // If there are no sockets in the TileSocketDirectory, use a default name
        //                 else
        //                 {
        //                     compatibilityTable[i].relations.Add(new SocketRelation("ID: " + j + " - Unknown Socket", defaultCompatibility));
        //                 }
        //             }
        //         }

        //         // Set the name of the socket and the name of the relations
        //         if (tileSocketDirectory.sockets.Length > i)
        //         {
        //             compatibilityTable[i].name = "ID: " + i + " - " + tileSocketDirectory.sockets[i].name;

        //             for (int j = 0; j < compatibilityTable[i].relations.Count; j++)
        //             {
        //                 if (tileSocketDirectory.sockets.Length > j)
        //                 {
        //                     bool compatibility = compatibilityTable[i].relations[j].isCompatible;
        //                     if (i == anySocketIX) compatibility = true;
        //                     // if (i == outOfBoundsSocketIX && j != anySocketIX) compatibility = false;
        //                     compatibilityTable[i].relations[j] = new SocketRelation("ID: " + j + " - " + tileSocketDirectory.sockets[j].name, compatibility);
        //                     if (i == anySocketIX || i == outOfBoundsSocketIX)
        //                     {
        //                         compatibilityTable[j].relations[i] = new SocketRelation("ID: " + i + " - " + tileSocketDirectory.sockets[i].name, compatibility);
        //                     }
        //                 }
        //             }
        //         }
        //     }

        // }

        // private void OnValidate()
        // {
        //     if (!tileSocketDirectory || compatibilityTable == null) return;

        //     ResizeCompatibilityTable();

        //     if (compatibilityTable.Length != tileSocketDirectory.sockets.Length)
        //     {
        //         Debug.LogError("compatibilityTable.Length != tileSocketDirectory.sockets.Length");
        //     }

        //     matrix = GetCompatibilityMatrix();
        // }
    }


}