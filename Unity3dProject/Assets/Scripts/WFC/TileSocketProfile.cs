using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class TileSocketProfile
    {
        public TileSocketProfile(Dictionary<Vector3, SocketFace> _socketFaceByLookup)
        {
            socketFaceByLookup = new Dictionary<Vector3, SocketFace>();
            // socketFacesByLookupByRotation = new Dictionary<int, Dictionary<Vector3, SocketFace>>();
            // socketFacesByLookupByRotation.Add(0, new Dictionary<Vector3, SocketFace>());
            foreach (var kvp in _socketFaceByLookup)
            {
                socketFaceByLookup.Add(kvp.Key, kvp.Value);
                // socketFacesByLookupByRotation[0].Add(kvp.Key, kvp.Value);
            }
        }

        // public TileSocketProfile(Dictionary<Vector3, SocketFace> _socketFaceByLookup, bool calculateRotations, Vector3 rotationCenter)
        // {
        //     // socketFaceByLookup = new Dictionary<Vector3, SocketFace>();
        //     socketFacesByLookupByRotation = new Dictionary<int, Dictionary<Vector3, SocketFace>>();
        //     socketFacesByLookupByRotation.Add(0, new Dictionary<Vector3, SocketFace>());
        //     foreach (var kvp in _socketFaceByLookup)
        //     {
        //         // socketFaceByLookup.Add(kvp.Key, kvp.Value);
        //         socketFacesByLookupByRotation[0].Add(kvp.Key, kvp.Value);
        //     }
        //     if (calculateRotations && socketFacesByLookupByRotation[0].Count > 0)
        //     {
        //         for (int rot = 1; rot < 6; rot++)
        //         {
        //             Dictionary<Vector3, SocketFace> rotatedSocketFaces = GenerateRotatedSocketFaces(socketFacesByLookupByRotation[0], rot, rotationCenter);
        //             socketFacesByLookupByRotation.Add(rot, rotatedSocketFaces);
        //         }
        //     }
        // }

        public Dictionary<Vector3, SocketFace> socketFaceByLookup { get; private set; } = null;
        // public Dictionary<int, Dictionary<Vector3, SocketFace>> socketFacesByLookupByRotation { get; private set; } = null; // Used for TOP & BOTTOM
        // public Dictionary<Vector3, SocketFace> GetSocketFaces(int rotation = 0)
        // {
        //     if (socketFacesByLookupByRotation.ContainsKey(rotation)) return socketFacesByLookupByRotation[rotation];
        //     return null;
        // }

        public TileSocketProfile(int _defaultID) { defaultID = _defaultID; }
        public TileSocketProfile(GlobalSockets _defaultID) { defaultID = (int)_defaultID; }

        public static TileSocketProfile Generate_TileSocketProfile(List<SurfaceBlock> tileInnerEdgeblocks, HexagonCellPrototype foreignCellOwner)
        {
            Dictionary<Vector3, SocketFace> _socketFaceByLookup = new Dictionary<Vector3, SocketFace>();

            foreach (var block in tileInnerEdgeblocks)
            {
                for (int _blockSide = 0; _blockSide < block.neighbors.Length; _blockSide++)
                {
                    SurfaceBlock neighbor = block.neighbors[_blockSide];

                    if (neighbor != null && neighbor.owner != block.owner && neighbor.owner == foreignCellOwner)
                    {
                        (Vector3 lookup, SocketFace socketFace) = SurfaceBlock.GetFaceProfie(block, neighbor);

                        //temp
                        (Vector3 lookupB, SocketFace socketFaceB) = SurfaceBlock.GetFaceProfie(neighbor, block);
                        if (lookup != lookupB || SurfaceBlock.AreFacesCompatible(socketFace, socketFaceB) == false)
                        {
                            Debug.LogError("lookup != lookupB || SurfaceBlock.AreFacesCompatible(socketFace, socketFaceB) == false, lookup: " + lookup + ", lookupB: " + lookupB);
                        }

                        if (_socketFaceByLookup.ContainsKey(lookup) == false) _socketFaceByLookup.Add(lookup, socketFace);
                    }

                }
            }
            return new TileSocketProfile(_socketFaceByLookup);
        }

        public TileSocketProfile(HexagonCellPrototype cell)
        {
            // if (cell.IsEdge()) defaultID = (int)GlobalSockets.Empty_Space;
            defaultID = (int)GlobalSockets.InnerCell_Generic;
        }

        public int defaultID { get; private set; } = -1;
        public void SetDefaultID(int _id)
        {
            defaultID = _id;
        }
        public void AddValue(Vector3 lookup, Vector3 point) { }

        public static bool IsCompatible(TileSocketProfile profile, TileSocketProfile otherProfile, HexagonSocketDirectory socketDirectory)
        {
            if (profile.socketFaceByLookup == null || otherProfile.socketFaceByLookup == null)
            {
                Debug.Log("(profile.socketFaceByLookup() == null || otherProfile.socketFaceByLookup == null)");

                int profileDefaultID = profile.defaultID;
                int otherDefaultID = otherProfile.defaultID;

                if (profile.socketFaceByLookup != otherProfile.socketFaceByLookup)
                {
                    if (profileDefaultID == -1)
                    {
                        Debug.LogError("profile.defaultID == -1, using Empty_SPACE");
                        profileDefaultID = (int)GlobalSockets.Empty_Space;
                    }

                    if (otherDefaultID == -1)
                    {
                        Debug.LogError("otherProfile.defaultID == -1, using Empty_SPACE");
                        otherDefaultID = (int)GlobalSockets.Empty_Space;
                    }
                    // return false;
                }

                // Debug.Log("Checking compatibilityMatrix...");
                bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();
                bool compatible = (compatibilityMatrix[profileDefaultID, otherDefaultID]);
                if (!compatible)
                {
                    Debug.LogError("NOT compatible! profileDefaultID: " + (GlobalSockets)profileDefaultID + ", otherDefaultID: " + (GlobalSockets)otherDefaultID);
                }
                else
                {
                    Debug.Log("Compatible socketIds! profileDefaultID: " + (GlobalSockets)profileDefaultID + ", otherDefaultID: " + (GlobalSockets)otherDefaultID);
                }
                return compatible;
            }
            return IsCompatible(profile, otherProfile);
        }

        public static bool IsCompatible(TileSocketProfile profile, TileSocketProfile otherProfile)
        {
            if (profile.socketFaceByLookup.Count != otherProfile.socketFaceByLookup.Count)
            {
                Debug.LogError("profile.socketFaceByLookup.Count != otherProfile.socketFaceByLookup.Count");
                return false;
            }

            foreach (Vector3 socketLookup in profile.socketFaceByLookup.Keys)
            {
                if (otherProfile.socketFaceByLookup.ContainsKey(socketLookup) == false)
                {
                    Debug.LogError("otherProfile.socketFaceByLookup.ContainsKey(socketLookup) == false, socketLookup: " + socketLookup);
                    return false;
                }
                SocketFace profileFace = profile.socketFaceByLookup[socketLookup];
                SocketFace otherFace = otherProfile.socketFaceByLookup[socketLookup];
                if (SurfaceBlock.AreFacesCompatible(profileFace, otherFace) == false)
                {
                    Debug.LogError("SurfaceBlock.AreFacesCompatible(profileFace, otherFace) == false - profileFace: " + profileFace + ", otherFace: " + otherFace);
                    return false;
                }
            }
            return true;
        }

        public static Dictionary<HexagonTileSide, TileSocketProfile> GetNeighborTileSocketProfilesOnSides_X8(HexagonCellPrototype cell)
        {
            Dictionary<HexagonTileSide, TileSocketProfile> neighborTileSocketsBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();
            HexagonCellPrototype[] neighborTileSides = cell.GetNeighborTileSides();

            for (int _side = 0; _side < neighborTileSides.Length; _side++)
            {
                HexagonCellPrototype neighbor = neighborTileSides[_side];
                HexagonTileSide side = (HexagonTileSide)_side;

                // If no neighbor, socket is Edge socket value
                if (neighbor == null)
                {
                    neighborTileSocketsBySide.Add(side, new TileSocketProfile(GlobalSockets.Edge));
                    continue;
                }
                if (neighbor.currentTile_V2 != null)
                {
                    neighborTileSocketsBySide.Add(side, neighbor.GetSideNeighborRelativeTileSockets_V2(_side));
                }
                else
                {
                    GlobalSockets defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                    // if (!sideNeighbor.isEdgeCell && !sideNeighbor.isEntryCell)
                    // {
                    // }
                    // else defaultSocketId = IsGroundCell() ? GlobalSockets.Unassigned_EdgeCell : defaultSocketId = GlobalSockets.Empty_Space;
                    Debug.LogError("Neighbor has NO Tile on side: " + side + ", using defaultSocketId: " + defaultSocketId);
                    neighborTileSocketsBySide.Add(side, new TileSocketProfile(defaultSocketId));
                }
            }
            return neighborTileSocketsBySide;
        }

        public static Dictionary<HexagonTileSide, TileSocketProfile> GetNeighborTileSocketProfilesOnSides(HexagonCellPrototype cell)
        {
            Dictionary<HexagonTileSide, TileSocketProfile> neighborTileSocketsBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();

            for (int side = 0; side < 6; side++)
            {
                HexagonCellPrototype sideNeighbor = cell.neighborsBySide[side];
                // If no neighbor, socket is Edge socket value
                if (sideNeighbor == null)
                {
                    neighborTileSocketsBySide.Add((HexagonTileSide)side, new TileSocketProfile((int)GlobalSockets.Edge));
                    continue;
                }
                if (sideNeighbor.currentTile_V2 != null)
                {
                    neighborTileSocketsBySide.Add((HexagonTileSide)side, sideNeighbor.GetSideNeighborRelativeTileSockets_V2(side));
                }
                else
                {
                    GlobalSockets defaultSocketId = GlobalSockets.Unassigned_InnerCell;
                    // if (!sideNeighbor.isEdgeCell && !sideNeighbor.isEntryCell)
                    // {
                    // }
                    // else defaultSocketId = IsGroundCell() ? GlobalSockets.Unassigned_EdgeCell : defaultSocketId = GlobalSockets.Empty_Space;
                    Debug.LogError("NO Tile on side: " + (HexagonTileSide)side + ", using defaultSocketId: " + defaultSocketId);

                    neighborTileSocketsBySide.Add((HexagonTileSide)side, new TileSocketProfile((int)defaultSocketId));
                }
            }
            return neighborTileSocketsBySide;
        }


        public static Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> EvaluateRotatedSideSockets(Dictionary<HexagonTileSide, TileSocketProfile> socketProfileBySide)
        {
            Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> socketsBySideByRotation = new Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>>();
            int rotations = 6;
            // Add the initial socket profile by side for rotation 0
            socketsBySideByRotation.Add(0, new Dictionary<HexagonTileSide, TileSocketProfile>(socketProfileBySide));

            for (int rot = 1; rot < rotations; rot++)
            {
                socketsBySideByRotation.Add(rot, new Dictionary<HexagonTileSide, TileSocketProfile>());

                for (int _side = 0; _side < 6; _side++)
                {
                    HexagonTileSide rotatedSide = HexCoreUtil.GetRotatedSide((HexagonSide)_side, rot);
                    if (socketProfileBySide.TryGetValue(rotatedSide, out var profile))
                    {
                        socketsBySideByRotation[rot].Add((HexagonTileSide)_side, profile);
                    }
                }
            }

            return socketsBySideByRotation;
        }


        public static Dictionary<Vector3, SocketFace> GenerateRotatedSocketFaces(Dictionary<Vector3, SocketFace> _socketFaceByLookup, int rotation, Vector3 center)
        {
            Dictionary<Vector3, SocketFace> rotatedSocketFaces = new Dictionary<Vector3, SocketFace>();

            float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };
            float rotationAngle = rotationValues[rotation % 6];

            Quaternion rotationQuaternion = Quaternion.Euler(0f, rotationAngle, 0f);

            foreach (var kvp in _socketFaceByLookup)
            {
                Vector3 rotatedKey = rotationQuaternion * (kvp.Key - center) + center;
                rotatedSocketFaces.Add(rotatedKey, kvp.Value);
            }

            return rotatedSocketFaces;
        }

        public static TileSocketProfile GetRotatedSideSockets(
            Dictionary<HexagonTileSide, TileSocketProfile> socketProfileBySide,
            HexagonTileSide side,
            int rotation,
            bool inverted = false
        )
        {
            // if (inverted) else
            if (socketProfileBySide == null)
            {
                Debug.LogError("socketProfileBySide == null");
                return null;
            }

            if (rotation == 0)
            {
                // Debug.Log("Default rotation");
                if (socketProfileBySide.ContainsKey(side))
                {
                    return socketProfileBySide[side];
                }
                else
                {
                    Debug.LogError("socketProfileBySide does not have side: " + side + ",  rotation: " + rotation);
                    return null;
                }
            }

            Dictionary<int, Dictionary<HexagonTileSide, TileSocketProfile>> socketProfileBySideByRotation = EvaluateRotatedSideSockets(socketProfileBySide);

            if (socketProfileBySideByRotation == null)
            {
                Debug.LogError("socketProfileBySideByRotation == null");
                return null;
            }
            Debug.Log("rotation: " + rotation + ", side: " + side);
            if (socketProfileBySideByRotation.ContainsKey(rotation) == false)
            {
                Debug.LogError("rotation key not found: " + rotation);
                return null;
            }
            if (socketProfileBySideByRotation[rotation].ContainsKey(side) == false)
            {
                Debug.LogError("side key not found: " + side + ", rotation: " + rotation);
                return null;
            }

            return socketProfileBySideByRotation[rotation][side];
        }




        // public static void SaveSideSocketProfileData(Dictionary<HexagonTileSide, TileSocketProfile> socketProfileBySide, string directoryPath, string fileName)
        // {
        //     Dictionary<Vector2, WorldCellData> dict = new Dictionary<Vector2, WorldCellData>();
        //     foreach (var kvp in socketProfileBySide)
        //     {
        //         Vector2 lookupCoord = kvp.Key;
        //         HexagonCellPrototype cell = kvp.Value;

        //         WorldCellData cellData = new WorldCellData();
        //         cellData.CopyFromCell(cell, lookupCoord);

        //         dict.Add(lookupCoord, cellData);
        //     }

        //     string json = JsonConvert.SerializeObject(dict, Formatting.Indented);

        //     try
        //     {
        //         if (!Directory.Exists(directoryPath))
        //         {
        //             Directory.CreateDirectory(directoryPath);
        //         }

        //         string filePath = Path.Combine(directoryPath, fileName + ".json");
        //         File.WriteAllText(filePath, json);

        //         Debug.Log("SaveData!: \n" + filePath);
        //     }
        //     catch (System.Exception ex)
        //     {
        //         Debug.LogError("Error while saving data: " + ex.Message);
        //     }
        // }
    }

    // public class TileSocketsData
    // {
    //     public Vector2Serialized parentLookup;
    //     public Vector2Serialized worldspacelookup;
    //     public Vector2Serialized lookup;
    //     public Vector3Serialized center;
    //     public Vector2Serialized[] neighborLookups;
    //     public Vector2Serialized[] neighborWorldspaceLookups;
    //     public Vector2Serialized[] layerNeighborLookups;
    //     public Vector2Serialized[] layerNeighborWorldspaceLookups;
    //     public int worldCellStatus;
    //     public bool isEdgeCell;
    //     public int objectIndex = -1;
    // }
}