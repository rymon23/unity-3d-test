using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFCSystem
{
    static class WFCUtilities
    {
        public static void ShuffleTiles(List<HexagonTile> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonTile temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }
        public static void ShuffleTiles(List<HexagonTileCluster> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonTileCluster temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }
        public static void ShuffleHexTiles(List<IHexagonTile> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                IHexagonTile temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }
        public static void ShuffleHexTiles(List<HexagonTileCore> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonTileCore temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }
    }
}