using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    public class WorldCellData
    {
        public Vector2Serialized parentLookup;
        public Vector2Serialized worldspacelookup;
        public Vector2Serialized lookup;
        public Vector3Serialized center;
        public Vector2Serialized[] neighborLookups;
        public Vector2Serialized[] neighborWorldspaceLookups;
        public Vector2Serialized[] layerNeighborLookups;
        public Vector2Serialized[] layerNeighborWorldspaceLookups;
        public int worldCellStatus;
        public bool isEdgeCell;
        public int objectIndex = -1;

        public void CopyFromCell(HexagonCellPrototype cell, Vector2 lookupCoord)
        {
            this.lookup = new Vector2Serialized(lookupCoord);
            this.center = new Vector3Serialized(cell.center);
            this.parentLookup = new Vector2Serialized(cell.parentLookup);
            this.worldspacelookup = new Vector2Serialized(cell.worldspaceLookup);
            this.isEdgeCell = cell.IsEdge();
            this.worldCellStatus = (int)cell.worldCellStatus;
            this.objectIndex = cell.objectIndex;

            List<Vector2Serialized> neighborLookups = new List<Vector2Serialized>();
            List<Vector2Serialized> neighborWorldSpaceLookups = new List<Vector2Serialized>();
            foreach (HexagonCellPrototype neighbor in cell.neighbors)
            {
                if (neighbor == null || neighbor.IsSameLayer(cell) == false) continue;

                neighborLookups.Add(new Vector2Serialized(neighbor.GetLookup()));
                neighborWorldSpaceLookups.Add(new Vector2Serialized(neighbor.worldspaceLookup));
            }
            List<Vector2Serialized> layerNeighborLookups = new List<Vector2Serialized>();
            List<Vector2Serialized> layerneighborWorldSpaceLookups = new List<Vector2Serialized>();
            foreach (HexagonCellPrototype neighbor in cell.layerNeighbors)
            {
                if (neighbor == null || neighbor.IsSameLayer(cell)) continue;

                layerNeighborLookups.Add(new Vector2Serialized(neighbor.GetLookup()));
                layerneighborWorldSpaceLookups.Add(new Vector2Serialized(neighbor.worldspaceLookup));
            }

            this.neighborLookups = neighborLookups.ToArray();
            this.neighborWorldspaceLookups = neighborWorldSpaceLookups.ToArray();

            this.layerNeighborLookups = layerNeighborLookups.ToArray();
            this.layerNeighborWorldspaceLookups = layerneighborWorldSpaceLookups.ToArray();
        }

        public void PastToCell(HexagonCellPrototype cell)
        {
            // cell.SetWorldCoordinate(coordinate.ToVector2());
            cell.SetParentLookup(parentLookup.ToVector2());
            cell.SetWorldSpaceLookup(worldspacelookup.ToVector2());

            List<CellWorldData> neighborWorldData = new List<CellWorldData>();
            for (int i = 0; i < this.neighborLookups.Length; i++)
            {
                Vector2Serialized lookupCoord = this.neighborLookups[i];
                Vector2Serialized worldSpaceLookup = this.neighborWorldspaceLookups[i];

                CellWorldData neigborData = new CellWorldData();

                neigborData.layer = cell.layer;
                neigborData.lookup = lookupCoord.ToVector2();
                neigborData.worldspaceLookup = worldspacelookup.ToVector2();
                neigborData.parentLookup = parentLookup.ToVector2();
                neighborWorldData.Add(neigborData);
            }

            for (int i = 0; i < this.layerNeighborLookups.Length; i++)
            {
                Vector2Serialized lookupCoord = this.layerNeighborLookups[i];
                Vector2Serialized worldSpaceLookup = this.layerNeighborWorldspaceLookups[i];

                CellWorldData neigborData = new CellWorldData();

                neigborData.layer = i == 0 ? cell.layer - 1 : cell.layer + 1;
                neigborData.lookup = lookupCoord.ToVector2();
                neigborData.worldspaceLookup = worldspacelookup.ToVector2();
                neigborData.parentLookup = parentLookup.ToVector2();
                neighborWorldData.Add(neigborData);
            }
            cell.neighborWorldData = neighborWorldData.ToArray();

            cell.worldCellStatus = (WorldCellStatus)this.worldCellStatus;
            if (this.isEdgeCell) cell.SetEdgeCell(true);
            if (this.objectIndex > -1) cell.objectIndex = this.objectIndex;
        }
    }
}