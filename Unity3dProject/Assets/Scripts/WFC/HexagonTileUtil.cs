using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFCSystem
{
    public enum CornerSocketSetType { SideBottom, SideTop, Bottom, Top }
    public enum GridExclusionRule { Unset = 0, EdgeOnly, InnerCellOnly, GridEdgesOnly, InnerGrid_NoEdges, InnerGrid_Any }
    public enum TileContext { Default = 0, Micro = 1, Meta, }

    public enum TileCategory
    {
        Unset = 0,
        Building,
        Road,
        Bridge,
        Wall,
        Gate,
        Interior,
        Misc,
    }

    public enum TileType
    {
        Unset = 0,
        ExteriorWallSmall,
        ExteriorWallLarge,
        TowerSmall,
        TowerLarge,
        InteriorWall,
        BuildingSmall,
        BuildingMedium,
        BuildingLarge,
        RoadSmall,
        RoadLarge,
    }

    public enum HexagonSide
    {
        Front = 0,
        FrontRight,
        BackRight,
        Back,
        BackLeft,
        FrontLeft,
    }

    public enum MirroredSideState
    {
        Unset = 0,
        SymmetricalRightAndLeft = 1,
        Mirror_FrontAndBack,
        Mirror_AllSides,
    }
    public enum SymmetricalHexagonSide
    {
        Front = 0,
        FrontRight = HexagonSide.FrontLeft,
        BackRight = HexagonSide.BackLeft,
        Back = HexagonSide.Back,
        BackLeft = BackRight,
        FrontLeft = FrontRight
    }

    public enum HexagonCorner
    {
        FrontA = 0,
        FrontB,
        FrontRightA,
        FrontRightB,
        BackRightA,
        BackRightB,

        BackA,
        BackB,
        BackLeftA,
        BackLeftB,
        FrontLeftA,
        FrontLeftB,
    }

    public enum HexagonSideEdge
    {
        Bottom = 0,
        Right,
        Top,
        Left,
    }


    public static class HexagonTileUtil
    {


        public static HashSet<int> GetTileSizes(List<HexagonTileCore> tiles)
        {
            HashSet<int> sizesFound = new HashSet<int>();
            foreach (var tile in tiles)
            {
                sizesFound.Add(tile.GetSize());
            }
            return sizesFound;
        }


    }

}