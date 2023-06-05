using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public static class HexGridUtil
    {
        // public static Dictionary<int, Dictionary<Vector2, Vector3>> GenerateHexGridCenterPoints_V3(
        //     Vector3 initialCenter,
        //     float smallestSize,
        //     int radius,
        //     IHexCell parentCell = null,
        //     Transform transform = null,
        //     bool logResults = false
        // )
        // {
        //     // Debug.Log("Radius: " + radius);

        //     List<int> cornersToUse = new List<int>()
        //         {
        //             (int)HexagonCorner.FrontA,
        //             (int)HexagonCorner.FrontB,
        //             (int)HexagonCorner.FrontLeftA,

        //             (int)HexagonCorner.BackA,
        //             (int)HexagonCorner.BackB,

        //             (int)HexagonCorner.BackRightA,
        //         };

        //     List<Vector3> divideCenterPoints = new List<Vector3>();
        //     Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>();
        //     List<Vector3> allNewCenterPoints = new List<Vector3>();

        //     int initialDivideSize = -1;
        //     int prevStepSize = radius;
        //     int currentStepSize = radius;

        //     while (smallestSize < currentStepSize)
        //     {
        //         currentStepSize = (prevStepSize / 3);

        //         List<Vector3> newCenterPoints = new List<Vector3>();

        //         if (divideCenterPoints.Count == 0)
        //         {
        //             newCenterPoints = HexCoreUtil.GenerateHexagonCenterPoints(initialCenter, currentStepSize, cornersToUse, true);
        //             initialDivideSize = currentStepSize;
        //         }
        //         else
        //         {
        //             foreach (Vector3 centerPoint in divideCenterPoints)
        //             {
        //                 List<Vector3> points = HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, cornersToUse, true);
        //                 newCenterPoints.AddRange(points);
        //             }
        //         }

        //         allNewCenterPoints.AddRange(newCenterPoints);
        //         // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);

        //         centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());
        //         foreach (Vector3 point in allNewCenterPoints)
        //         {
        //             Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
        //             if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
        //         }

        //         prevStepSize = currentStepSize;

        //         if (currentStepSize <= smallestSize)
        //         {
        //             break;
        //         }
        //         else
        //         {
        //             divideCenterPoints.Clear();
        //             divideCenterPoints.AddRange(newCenterPoints);
        //         }
        //     }

        //     return centerPointsByLookup_BySize;
        // }

        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_HexGridCenterPoints_BySize(
            Vector3 initialCenter,
            float smallestSize,
            int gridRadius,
            bool logResults = false
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            List<Vector3> divideCenterPoints = new List<Vector3>();
            List<Vector3> allNewCenterPoints = new List<Vector3>();

            int initialDivideSize = -1;
            int prevStepSize = gridRadius;
            int currentStepSize = gridRadius;

            while (smallestSize < currentStepSize)
            {
                currentStepSize = (prevStepSize / 3);

                List<Vector3> newCenterPoints = new List<Vector3>();

                if (divideCenterPoints.Count == 0)
                {
                    newCenterPoints = HexCoreUtil.GenerateHexCenterPoints_X13(initialCenter, currentStepSize);
                    initialDivideSize = currentStepSize;
                }
                else
                {
                    foreach (Vector3 centerPoint in divideCenterPoints)
                    {
                        newCenterPoints.AddRange(HexCoreUtil.GenerateHexCenterPoints_X13(centerPoint, currentStepSize));
                    }
                }

                // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);
                allNewCenterPoints.AddRange(newCenterPoints);
                centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());

                foreach (Vector3 point in allNewCenterPoints)
                {
                    Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
                    if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
                    {
                        centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
                        if (smallestSize == currentStepSize)
                        {
                            List<Vector3> points = HexCoreUtil.GenerateHexCenterPoints_X6(point, currentStepSize);
                            foreach (var item in points)
                            {
                                Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
                                if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);
                            }
                        }
                    }
                }

                prevStepSize = currentStepSize;

                if (currentStepSize <= smallestSize)
                {
                    break;
                }
                else
                {
                    divideCenterPoints.Clear();
                    divideCenterPoints.AddRange(newCenterPoints);
                }
            }
            return centerPointsByLookup_BySize;
        }


        public static Dictionary<Vector2, Vector3> Generate_HexagonGridCenterPoints(
            Vector3 initialCenter,
            float cellSize,
            int gridRadius,
            bool logResults = false
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = Generate_HexGridCenterPoints_BySize(
                    initialCenter,
                    cellSize,
                    gridRadius,
                    logResults
                );
            return centerPointsByLookup_BySize[(int)cellSize];

            // List<Vector3> divideCenterPoints = new List<Vector3>();
            // Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            // List<Vector3> allNewCenterPoints = new List<Vector3>();

            // int initialDivideSize = -1;
            // int prevStepSize = gridRadius;
            // int currentStepSize = gridRadius;

            // while (cellSize < currentStepSize)
            // {
            //     currentStepSize = (prevStepSize / 3);

            //     List<Vector3> newCenterPoints = new List<Vector3>();

            //     if (divideCenterPoints.Count == 0)
            //     {
            //         newCenterPoints = HexCoreUtil.GenerateHexCenterPoints_X13(initialCenter, currentStepSize);
            //         initialDivideSize = currentStepSize;
            //     }
            //     else
            //     {
            //         foreach (Vector3 centerPoint in divideCenterPoints)
            //         {
            //             newCenterPoints.AddRange(HexCoreUtil.GenerateHexCenterPoints_X13(centerPoint, currentStepSize));
            //         }
            //     }

            //     allNewCenterPoints.AddRange(newCenterPoints);
            //     // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);

            //     centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());
            //     foreach (Vector3 point in allNewCenterPoints)
            //     {
            //         Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
            //         if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
            //         {
            //             centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
            //             if (cellSize == currentStepSize)
            //             {
            //                 List<Vector3> points = HexCoreUtil.GenerateHexCenterPoints_X6(point, currentStepSize);
            //                 foreach (var item in points)
            //                 {
            //                     Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
            //                     if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);
            //                 }
            //             }
            //         }
            //     }

            //     prevStepSize = currentStepSize;

            //     if (currentStepSize <= cellSize)
            //     {
            //         break;
            //     }
            //     else
            //     {
            //         divideCenterPoints.Clear();
            //         divideCenterPoints.AddRange(newCenterPoints);
            //     }
            // }
            // return centerPointsByLookup_BySize[currentStepSize];
        }


        // public static List<Vector3> GenerateHexGridCenterPoints(
        //     Vector3 initialCenter,
        //     float size,
        //     int radius,
        //     IHexCell parentCell = null,
        //     Transform transform = null,
        //     List<int> useCorners = null,
        //     bool logResults = false)
        // {
        //     List<Vector3> spawnCenters = new List<Vector3>();
        //     List<Vector3> quatCenterPoints = new List<Vector3>();
        //     List<int> quadrantSizes = new List<int>();

        //     bool filterOutCorners = (useCorners == null || useCorners.Count == 0);

        //     int prevStepSize = radius;
        //     int currentStepSize = radius;
        //     while (size < currentStepSize)
        //     {
        //         currentStepSize = (prevStepSize / 3);

        //         List<Vector3> newCenterPoints = new List<Vector3>();
        //         if (quatCenterPoints.Count == 0)
        //         {
        //             newCenterPoints = (!filterOutCorners && currentStepSize <= size) ? HexCoreUtil.GenerateHexagonCenterPoints(initialCenter, currentStepSize, useCorners, true)
        //                 : HexCoreUtil.GenerateHexagonCenterPoints(initialCenter, currentStepSize, true, currentStepSize > size);
        //             // newCenterPoints = HexCoreUtil.GenerateHexagonCenterPoints(center, currentStepSize, true, currentStepSize > size);
        //         }
        //         else
        //         {
        //             foreach (Vector3 centerPoint in quatCenterPoints)
        //             {
        //                 HexagonCellPrototype quadrantPrototype = new HexagonCellPrototype(centerPoint, prevStepSize, false);
        //                 // List<Vector3> points = HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, true, true);

        //                 List<Vector3> points = (!filterOutCorners && currentStepSize <= size) ? HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, useCorners, true)
        //                     : HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, true, true);

        //                 newCenterPoints.AddRange(points);
        //                 // quadrantCenterPoints.Add(quadrantPrototype, points);
        //             }
        //         }
        //         // Debug.Log("GenerateHexGrid - newCenterPoints: " + newCenterPoints.Count + ", currentStepSize: " + currentStepSize + ", desired size: " + size);
        //         prevStepSize = currentStepSize;
        //         if (currentStepSize <= size)
        //         {
        //             spawnCenters.AddRange(newCenterPoints);
        //             break;
        //         }
        //         else
        //         {
        //             quatCenterPoints.Clear();
        //             quatCenterPoints.AddRange(newCenterPoints);
        //             // Debug.Log("Quadrants of size " + currentStepSize + ": " + quatCenterPoints.Count);
        //         }
        //     }

        //     List<Vector3> results = new List<Vector3>();
        //     Vector2 baseCenterPosXZ = new Vector2(initialCenter.x, initialCenter.z);
        //     int skipped = 0;
        //     for (int i = 0; i < spawnCenters.Count; i++)
        //     {
        //         Vector3 centerPoint = spawnCenters[i];
        //         // Vector3 centerPoint = VectorUtil.RoundVector3To1DecimalXZ(spawnCenters[i]);
        //         // Vector3 centerPoint = transform != null ? transform.TransformVector(spawnCenters[i]) : spawnCenters[i];

        //         // Filter out duplicate points & out of bounds
        //         float distance = Vector2.Distance(new Vector2(centerPoint.x, centerPoint.z), baseCenterPosXZ);
        //         // Debug.Log("GenerateHexGrid - centerPoint: " + centerPoint + ", size: " + size + ", distance: " + distance);

        //         if (filterOutCorners == false || (filterOutCorners && distance < radius))
        //         {
        //             bool skip = false;
        //             foreach (Vector3 point in results)
        //             {
        //                 if (Vector3.Distance(centerPoint, point) < 1f)
        //                 {
        //                     skip = true;
        //                     skipped++;
        //                     break;
        //                 }
        //             }
        //             if (!skip) results.Add(centerPoint);
        //         }
        //     }

        //     // Debug.Log("GenerateHexGrid - 01 results: " + results.Count + ", size: " + size);
        //     // if (filterOutCorners)
        //     // {
        //     //     HexagonCellPrototype parentHex = new HexagonCellPrototype(initialCenter, radius);
        //     //     results = GetPrototypesWithinHexagon(results, initialCenter, radius, parentHex.GetEdgePoints());
        //     // }
        //     return results;
        // }

        // public static Dictionary<int, List<Vector3>> GenerateHexGridCenterPoints_V2(
        //     Vector3 initialCenter,
        //     float smallestSize,
        //     int radius,
        //     IHexCell parentCell = null,
        //     Transform transform = null,
        //     bool logResults = false
        // )
        // {
        //     List<Vector3> quatCenterPoints = new List<Vector3>();
        //     List<int> quadrantSizes = new List<int>();

        //     List<int> cornersToUse = new List<int>()
        //         {
        //             (int)HexagonCorner.FrontA,
        //             (int)HexagonCorner.FrontB,
        //             (int)HexagonCorner.FrontLeftA,

        //             (int)HexagonCorner.BackA,
        //             (int)HexagonCorner.BackB,

        //             (int)HexagonCorner.BackRightA,
        //         };

        //     Dictionary<int, List<Vector3>> centerPointsBySize = new Dictionary<int, List<Vector3>>();

        //     List<Vector3> allNewCenterPoints = new List<Vector3>();

        //     int initialDivideSize = -1;
        //     int prevStepSize = radius;
        //     int currentStepSize = radius;

        //     while (smallestSize < currentStepSize)
        //     {
        //         currentStepSize = (prevStepSize / 3);

        //         List<Vector3> newCenterPoints = new List<Vector3>();
        //         if (quatCenterPoints.Count == 0)
        //         {
        //             newCenterPoints = HexagonCellPrototype.GenerateHexagonCenterPoints(initialCenter, currentStepSize, cornersToUse, true);
        //             initialDivideSize = currentStepSize;
        //         }
        //         else
        //         {
        //             foreach (Vector3 centerPoint in quatCenterPoints)
        //             {
        //                 List<Vector3> points = HexagonCellPrototype.GenerateHexagonCenterPoints(centerPoint, currentStepSize, cornersToUse, true);
        //                 // List<Vector3> points = HexagonCellPrototype.GenerateHexagonCenterPoints_WithCorners(centerPoint, currentStepSize, true);
        //                 // List<Vector3> points = (currentStepSize == 12 && currentStepSize > smallestSize) ?
        //                 //         HexagonCellPrototype.GenerateHexagonCenterPoints_WithCorners(centerPoint, currentStepSize, true) :
        //                 //              HexagonCellPrototype.GenerateHexagonCenterPoints(centerPoint, currentStepSize, cornersToUse, true);

        //                 newCenterPoints.AddRange(points);
        //                 // HexagonCellPrototype quadrantPrototype = new HexagonCellPrototype(centerPoint, prevStepSize);
        //             }
        //         }

        //         allNewCenterPoints.AddRange(newCenterPoints);

        //         List<Vector3> allSoFar = new List<Vector3>();
        //         allSoFar.AddRange(allNewCenterPoints);
        //         // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);

        //         // Debug.Log("GenerateHexGrid - newCenterPoints: " + newCenterPoints.Count + ", currentStepSize: " + currentStepSize + ", desired size: " + size);
        //         centerPointsBySize.Add(currentStepSize, allSoFar);

        //         // Add side points from first divided hexagons
        //         // if (currentStepSize == (smallestSize * 3))
        //         // {
        //         //     Debug.Log("GenerateHexGrid - add corners from currentStepSize: " + currentStepSize);
        //         //     foreach (Vector3 centerPoint in newCenterPoints)
        //         //     {
        //         //         allNewCenterPoints.AddRange(ProceduralTerrainUtility.GenerateHexagonPoints(centerPoint, currentStepSize));
        //         //     }
        //         // }
        //         // Add side points from first divided hexagons
        //         // if (currentStepSize == initialDivideSize)
        //         // {
        //         //     foreach (Vector3 centerPoint in newCenterPoints)
        //         //     {
        //         //         HexagonCellPrototype initialHex = new HexagonCellPrototype(centerPoint, prevStepSize);
        //         //         allNewCenterPoints.AddRange(initialHex.sidePoints);
        //         //     }
        //         // }

        //         // if (centerPointsBySize.ContainsKey(prevStepSize))
        //         // {
        //         //     centerPointsBySize[currentStepSize].AddRange(centerPointsBySize[prevStepSize]);
        //         //     Debug.Log("GenerateHexGrid - add prevStepSize: " + prevStepSize + " list to currentStepSize: " + currentStepSize);
        //         // }

        //         prevStepSize = currentStepSize;

        //         if (currentStepSize <= smallestSize)
        //         {
        //             break;
        //         }
        //         else
        //         {
        //             quatCenterPoints.Clear();
        //             quatCenterPoints.AddRange(newCenterPoints);
        //             // Debug.Log("Quadrants of size " + currentStepSize + ": " + quatCenterPoints.Count);
        //         }
        //     }

        //     Dictionary<int, List<Vector3>> finalPointsBySize = new Dictionary<int, List<Vector3>>();
        //     Vector2 baseCenterPosXZ = new Vector2(initialCenter.x, initialCenter.z);

        //     int skipped = 0;

        //     foreach (var kvp in centerPointsBySize)
        //     {
        //         int currentSize = kvp.Key;

        //         if (finalPointsBySize.ContainsKey(currentSize) == false) finalPointsBySize.Add(currentSize, new List<Vector3>());

        //         for (int i = 0; i < centerPointsBySize[currentSize].Count; i++)
        //         {
        //             Vector3 centerPoint = centerPointsBySize[currentSize][i];

        //             // Filter out duplicate points & out of bounds
        //             bool skip = false;
        //             foreach (Vector3 point in finalPointsBySize[currentSize])
        //             {
        //                 if (Vector3.Distance(centerPoint, point) < 1f)
        //                 {
        //                     skip = true;
        //                     skipped++;
        //                     break;
        //                 }
        //             }
        //             if (!skip) finalPointsBySize[currentSize].Add(centerPoint);
        //         }
        //     }

        //     return finalPointsBySize;
        // }
    }
}