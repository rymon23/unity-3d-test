using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    public class StairPrototype
    {
        public StairPrototype(RampPrototype _rampPrototype)
        {
            rampPrototype = _rampPrototype;
            gridBounds = VectorUtil.CalculateBounds(rampPrototype.corners.ToList());

            // CreateSurfaceBlocks(gridBounds, 1f, 0, rampPrototype.dimensions.y);

        }
        public RampPrototype rampPrototype { get; private set; }
        Bounds gridBounds;
        Dictionary<Vector3, SurfaceBlock> blockCenterLookups = null;

        public void Generate_MeshObject(GameObject prefab, Transform transform = null)
        {
            Vector3 pos = rampPrototype.Position;
            GameObject new_GO = MeshUtil.InstantiatePrefabWithMesh(prefab, rampPrototype.Generate_Mesh(transform), pos);
            new_GO.transform.position = rampPrototype.Position;
        }


        public void DrawSide(RampSide side)
        {
            rampPrototype.DrawSide(side);
        }

        public void DrawBounds()
        {
            // VectorUtil.DrawRectangleLines(gridBounds);

            rampPrototype.Draw();

            // if (blockCenterLookups != null)
            // {
            //     SurfaceBlock.DrawGrid(blockCenterLookups);
            // }
        }

        public void CreateSurfaceBlocks(Bounds bounds, float blockSize, float baseElevation, float maxHeight)
        {
            int gridSizeX = Mathf.CeilToInt(bounds.size.x / blockSize);
            int gridSizeZ = Mathf.CeilToInt(bounds.size.z / blockSize);

            float spacingX = bounds.size.x / gridSizeX;
            float spacingZ = bounds.size.z / gridSizeZ;
            float _size = Mathf.Min(spacingX, spacingZ);
            _size = UtilityHelpers.RoundToNearestStep(_size, 0.2f);

            int gridSizeY = Mathf.FloorToInt((maxHeight - baseElevation) / _size) + 1;

            // Calculate the starting y position for the grid
            float startY = baseElevation + (_size * 0.5f); // Offset by half the spacing to center the points

            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();
            Dictionary<Vector3, SurfaceBlock> new_blockCenterLookups = new Dictionary<Vector3, SurfaceBlock>();
            // Debug.LogError("[gridSizeX, gridSizeY, gridSizeZ]: " + gridSizeX + ", " + gridSizeY + " , " + gridSizeZ + ", _size: " + _size + ", hexNodeGrid: " + hexNodeGrid.GetBaseLayerCells().Count);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        // Calculate the position of the current point
                        float xPos = bounds.min.x + (x * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        float yPos = startY + (y * _size);
                        float zPos = bounds.min.z + (z * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        // Set the point in the grid matrix
                        Vector3 position = new Vector3(xPos, yPos, zPos);

                        SurfaceBlock surfaceBlock = new SurfaceBlock(position, _size);

                        // Evaluate Node Cell bounds
                        bool inBounds = false;

                        if (rampPrototype.IsPointWithinRamp(position)) inBounds = true;

                        if (!inBounds) continue;

                        Vector3 lookup = VectorUtil.PointLookupDefault(position);
                        if (new_blockCenterLookups.ContainsKey(lookup) == false)
                        {
                            new_blockCenterLookups.Add(lookup, surfaceBlock);
                            neighborsToFill.Add(surfaceBlock);
                        }
                        else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                        // Generate corners for the current surfaceBlock
                        Vector3[] corners = SurfaceBlock.CreateCorners(position, _size);
                        for (var i = 0; i < corners.Length; i++)
                        {
                            Vector3 cornerLookup = VectorUtil.PointLookupDefault(corners[i]);
                            if (cornerLookups.ContainsKey(cornerLookup))
                            {
                                corners[i] = cornerLookups[cornerLookup];
                            }
                            else cornerLookups.Add(cornerLookup, corners[i]);
                        }

                        surfaceBlock.SetCorners(corners);
                    }
                }
            }

            foreach (SurfaceBlock item in neighborsToFill)
            {
                Vector3[] neighborCenters = SurfaceBlock.GenerateNeighborCenters(item.Position, _size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (new_blockCenterLookups.ContainsKey(lookup) && new_blockCenterLookups[lookup] != item)
                    {
                        item.neighbors[i] = new_blockCenterLookups[lookup];
                        found++;
                    }
                }
                item.SetEdge((found < 6));

                // if (!item.ignore && !item.IsRoof() && !item.IsFloor())
                // {
                //     List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(item.Position, 12);
                //     foreach (Vector2 currentLookup in nearestCellLookups)
                //     {
                //         if (boundsShapesByCellLookup.ContainsKey(currentLookup) == false) continue;
                //         bool exit = false;
                //         foreach (BoundsShapeBlock boundsShape in boundsShapesByCellLookup[currentLookup].Values)
                //         {
                //             if (BoundsShapeBlock.IsWithinBounds(boundsShape, item.Position))
                //             {
                //                 item.SetIgnored(true);
                //                 exit = true;
                //                 break;
                //             }
                //         }
                //         if (exit) break;
                //     }
                // }
                item.SetTileEdge(item.IsTileEdge());
                item.SetTileInnerEdge(item.IsTileInnerEdge());
            }
            // Debug.Log("surfaceBlocks: " + neighborsToFill.Count);

            blockCenterLookups = new_blockCenterLookups;
        }

    }
}