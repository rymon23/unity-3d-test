using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class HexTileBuilder
    {
        Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;
        Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell = null;
        List<List<SurfaceBlock>> surfaceBlockClusters = null;
        public SurfaceBlock[,,] surfaceBlocksGrid = null;

        public void Generate_Structure(
            Bounds gridStructureBounds,
            List<Bounds> structureBounds,
            float baseBlockSize,
            float baseElevation,
            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize,
            List<HexagonCellPrototype> allBaseCells,
            List<HexagonCellPrototype> baseEdges,
            float cellSize,
            int cellLayerOffset,
            int cellLayersMax,
            Vector3 blockGrid_cluster_LWH_Min,
            int blockGrid_clustersMax = 2
        )
        {
            if (surfaceBlocksGrid == null)
            {
                (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridStructureBounds, baseBlockSize, baseElevation, ((cellLayersMax - 1) * cellLayerOffset));
                surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);

                SurfaceBlock.GetViableEntryways(surfaceBlocksGrid, baseEdges, 3);

                surfaceBlocksByCell = SurfaceBlock.GetSurfaceBlocksByCell(surfaceBlocksGrid, cellLookup_ByLayer_BySize[(int)cellSize], cellLayerOffset);
                surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);

                SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

                List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
                            SurfaceBlockState.Entry,
                            // SurfaceBlockState.Corner,
                        };

                surfaceBlockClusters = SurfaceBlock.GetConsecutiveClusters(
                    surfaceBlocksGrid,
                    blockGrid_clustersMax,
                    filterOnStates,
                    blockGrid_cluster_LWH_Min,
                    CellSearchPriority.SideNeighbors
                );

                if (surfaceBlockClusters != null && surfaceBlockClusters.Count > 0)
                {
                    foreach (var cluster in surfaceBlockClusters)
                    {
                        foreach (var item in cluster)
                        {
                            item.SetIgnored(true);
                        }
                    }
                }
                tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

                // Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles =
                //     SurfaceBlock.Generate_CellTileSocketProfiles(tileInnerEdgesByCellSide);
            }
        }

        public void DrawGrid()
        {
            SurfaceBlock.DrawGrid(surfaceBlocksGrid);

            // if (surfaceBlocksByCell != null)
            // {
            //     Gizmos.color = Color.magenta;
            //     if (tileInnerEdgesByCellSide != null)
            //     {
            //         foreach (var cell in tileInnerEdgesByCellSide.Keys)
            //         {

            //             foreach (var side in tileInnerEdgesByCellSide[cell].Keys)
            //             {
            //                 foreach (var block in tileInnerEdgesByCellSide[cell][side])
            //                 {
            //                     Gizmos.DrawSphere(block.Position, 0.3f);
            //                 }
            //             }

            //         }
            //     }

            // }
        }

        // public void Generate_Structure(
        //     SurfaceBlock[,,] surfaceBlocksGrid,
        //     Bounds gridStructureBounds,
        //     List<Bounds> structureBounds,
        //     float baseBlockSize,
        //     float baseElevation,
        //     Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize,
        //     List<HexagonCellPrototype> baseEdges,
        //     float cellSize,
        //     int cellLayerOffset,
        //     int cellLayersMax,
        //     Vector3 blockGrid_cluster_LWH_Min,
        //     int blockGrid_clustersMax = 2
        // )
        // {
        //     if (surfaceBlocksGrid == null)
        //     {
        //         (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridStructureBounds, baseBlockSize, baseElevation, ((cellLayersMax - 1) * cellLayerOffset));
        //         surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);

        //         SurfaceBlock.GetViableEntryways(surfaceBlocksGrid, baseEdges, 3);

        //         surfaceBlocksByCell = SurfaceBlock.GetSurfaceBlocksByCell(surfaceBlocksGrid, cellLookup_ByLayer_BySize[(int)cellSize], cellLayerOffset);
        //         surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);

        //         SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

        //         List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
        //                     SurfaceBlockState.Entry,
        //                     // SurfaceBlockState.Corner,
        //                 };

        //         surfaceBlockClusters = SurfaceBlock.GetConsecutiveClusters(
        //             surfaceBlocksGrid,
        //             blockGrid_clustersMax,
        //             filterOnStates,
        //             blockGrid_cluster_LWH_Min,
        //             CellSearchPriority.SideNeighbors
        //         );

        //         if (surfaceBlockClusters != null && surfaceBlockClusters.Count > 0)
        //         {
        //             foreach (var cluster in surfaceBlockClusters)
        //             {
        //                 foreach (var item in cluster)
        //                 {
        //                     item.SetIgnored(true);
        //                 }
        //             }
        //         }
        //         tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

        //         Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles =
        //             SurfaceBlock.Generate_CellTileSocketProfiles(tileInnerEdgesByCellSide);
        //     }
        // }
    }

}