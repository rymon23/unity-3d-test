using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Directory", menuName = "Tile Directory")]
public class TileDirectory : ScriptableObject
{
    [System.Serializable]
    public struct TileEntry
    {
        public HexagonTile tilePrefab;
        public int id;
        public float probability;
        // public Color color;
    }

    [SerializeField] private TileEntry[] tileEntries;
    [SerializeField] private bool revaluate;
    private int currentSize = 0;
    private void OnValidate()
    {
        if (revaluate || tileEntries != null && tileEntries.Length != currentSize)
        {
            revaluate = false;
            currentSize = tileEntries.Length;

            for (int i = 0; i < tileEntries.Length; i++)
            {
                tileEntries[i].tilePrefab.id = i;
                tileEntries[i].id = i;
            }
        }
    }
}
