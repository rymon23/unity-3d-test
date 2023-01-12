using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public static Vector3[] GetLocationSubzonePoints(LocationPrototype locationPrototype)
        {
            Vector3[] subzonePoints = new Vector3[locationPrototype.subzonePrototypes.Count];
            for (int i = 0; i < locationPrototype.subzonePrototypes.Count; i++)
            {
                subzonePoints[i] = locationPrototype.subzonePrototypes[i].position;
            }
            return subzonePoints;
        }
        public static List<Vector3> GetLocationSubzonePointsList(LocationPrototype locationPrototype)
        {

            List<Vector3> subzonePoints = new List<Vector3>();
            foreach (SubzonePrototype item in locationPrototype.subzonePrototypes)
            {
                subzonePoints.Add(item.position);
            }
            return subzonePoints;
        }
        public static Vector3[] GetLocationZoneConnectors(LocationPrototype locationPrototype)
        {
            Vector3[] subzoneConnectors = new Vector3[locationPrototype.subzoneConnectors.Count];
            for (int i = 0; i < subzoneConnectors.Length; i++)
            {
                subzoneConnectors[i] = locationPrototype.subzoneConnectors[i].position;

            }
            return subzoneConnectors;
        }

        public static (List<SubzonePrototype>, List<ZoneConnector>) GenerateLocationZoneAndConnectorPrototypes(int number, Vector3 position, Vector2 radiusRange, Vector2 rangeY, float minDistance, float zoneRadius)
        {
            List<SubzonePrototype> subzonePrototypes = new List<SubzonePrototype>();
            List<ZoneConnector> zoneConnectors = new List<ZoneConnector>();
            Vector3[] points = new Vector3[number];

            for (int i = 0; i < number; i++)
            {
                float yMod = UnityEngine.Random.Range(-rangeY.x, rangeY.y);
                Vector3 lastPos = Vector3.zero;
                if (i == 0)
                {
                    // Generate the first point within the given radius range
                    (Vector3 newPoint, Vector3 connector) = ProceduralTerrainUtility.GenerateOverlappingPointAndMidPoint(position, radiusRange);

                    newPoint.y = position.y + yMod;
                    lastPos = newPoint;
                }
                else
                {
                    // Generate a point within the given radius range, ensuring it is at least minDistance away from all other points
                    bool validPointFound = false;
                    while (!validPointFound)
                    {
                        (Vector3 newPoint, Vector3 connector) = ProceduralTerrainUtility.GenerateOverlappingPointAndMidPoint(points[i - 1], radiusRange);
                        newPoint.y = points[i - 1].y + yMod;
                        lastPos = newPoint;

                        validPointFound = true;
                        for (int j = 0; j < i; j++)
                        {
                            if (Vector3.Distance(lastPos, points[j]) < minDistance)
                            {
                                validPointFound = false;
                                break;
                            }
                        }
                        if (validPointFound)
                        {
                            connector.y += yMod * 0.6f;

                            ZoneConnector newConnector = new ZoneConnector();
                            newConnector.position = connector;
                            newConnector.zones = new Vector3[2];
                            newConnector.zones[0] = points[i - 1];
                            newConnector.zones[1] = newPoint;

                            zoneConnectors.Add(newConnector);
                        }
                    }
                }
                Vector3[] borderCorners = ProceduralTerrainUtility.GenerateHexagonPoints(lastPos, zoneRadius * 0.88f);
                SubzonePrototype subzonePrototype = new SubzonePrototype();
                subzonePrototype.position = lastPos;
                subzonePrototype.borderCorners = borderCorners;
                subzonePrototype.radius = zoneRadius;
                subzonePrototypes.Add(subzonePrototype);

                points[i] = lastPos;
            }
            return (subzonePrototypes, zoneConnectors);
        }
    }
}
