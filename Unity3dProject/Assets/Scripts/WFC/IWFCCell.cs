using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;

interface IWFCCell
{
    public int GetId();

    public void RecalculateEdgePoints();

    public int GetNeighborsRelativeSide(int side);

    public int[] GetNeighborTileSockets();

    public void SetNeighborsBySide(float offset = 0.33f);
    public List<HexagonCell> GetEdgeCells(List<HexagonCell> cells);
    public void SetEdgeCell(bool enable);
}

