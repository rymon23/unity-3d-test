using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    public static class LocationUtility
    {

        public static List<HexagonTilePrototype> GetTilesWithinRadius(List<HexagonTilePrototype> tiles, Vector3 position, float radius)
        {
            List<HexagonTilePrototype> withinRadius = new List<HexagonTilePrototype>();
            foreach (HexagonTilePrototype tile in tiles)
            {
                bool isWithinRadius = true;
                foreach (Vector3 cornerPoint in tile.cornerPoints)
                {
                    float distance = Vector3.Distance(cornerPoint, position);
                    if (distance > radius)
                    {
                        isWithinRadius = false;
                        break;
                    }
                }
                if (isWithinRadius)
                {
                    withinRadius.Add(tile);
                }
            }
            return withinRadius;
        }

        public static List<HexagonTilePrototype> GetTilesWithinPositions(List<HexagonTilePrototype> tiles, List<Vector3> positions, float radius)
        {
            List<HexagonTilePrototype> withinPositions = new List<HexagonTilePrototype>();
            foreach (HexagonTilePrototype tile in tiles)
            {
                foreach (Vector3 position in positions)
                {
                    bool isWithinPosition = true;
                    foreach (Vector3 cornerPoint in tile.cornerPoints)
                    {
                        float distance = Vector3.Distance(cornerPoint, position);
                        if (distance > radius)
                        {
                            isWithinPosition = false;
                            break;
                        }
                    }
                    if (isWithinPosition)
                    {
                        withinPositions.Add(tile);
                        break; // Exit the inner loop since the tile is already within a position
                    }
                }
            }
            return withinPositions;
        }

    }
}
