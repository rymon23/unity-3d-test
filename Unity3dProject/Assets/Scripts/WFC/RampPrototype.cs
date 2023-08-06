using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    public enum RampSide { Slope = 0, Back, Bottom, BlockLeft, BlockRight, BlockTop, BlockBottom }
    public enum RampTopSide { Back = 0, Right, Left }
    public enum RotationType { Hexogonal = 0, Rectangle }

    public class RampPrototype
    {
        public RampPrototype(Vector3 center, int _rotation, Vector3 _dimensions, RotationType _rotationType = RotationType.Hexogonal, float _size = 2)
        {
            Position = center;
            size = _size;
            rotation = _rotation;
            rotationType = _rotationType;
            dimensions = _dimensions;
            corners = CreateCorners(center, size, rotation, dimensions);
        }

        public RampPrototype(Vector3 center, int _rotation, Vector4 _dimensions, RotationType _rotationType = RotationType.Hexogonal, float _size = 2)
        {
            Position = center;
            size = _size;
            rotation = _rotation;
            rotationType = _rotationType;
            dimensions = new Vector3(_dimensions.x, _dimensions.y, _dimensions.z);
            corners = CreateCorners(center, size, rotation, dimensions, _dimensions.w);
        }

        public RampPrototype(Vector3 rampStartCenter, int _rotation, Vector4 _dimensions, RotationType _rotationType = RotationType.Hexogonal)
        {
            Position = rampStartCenter;
            size = 2;
            rotation = _rotation;
            rotationType = _rotationType;
            dimensions = new Vector3(_dimensions.x, _dimensions.y, _dimensions.z);
            corners = CreateCornersAtStartPoint(rampStartCenter, rotation, dimensions, _dimensions.w, rotationType, size);
        }
        public RampPrototype(RectangleBounds rect, int _rotation, Vector4 _dimensions, RotationType _rotationType = RotationType.Hexogonal)
        {
            // Vector3[] neighborCenters = RectangleBounds.Generate_NeighborCenters(rect);
            Vector3 rampCenter = RectangleBounds.Generate_NeighborCenter(SurfaceBlockSide.Front, rect);
            Position = rampCenter;
            backBlock = rect;
            size = 2;
            rotation = _rotation;
            rotationType = _rotationType;
            dimensions = new Vector3(_dimensions.x, _dimensions.y, _dimensions.z);
            corners = CreateCorners(rampCenter, rect, size, dimensions);
        }

        public Vector3 Position { get; private set; }
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public float size;
        public Vector3 dimensions { get; private set; }
        public int rotation = 0;
        public RotationType rotationType;
        public Vector3[] corners { get; private set; } = new Vector3[8];
        public RectangleBounds backBlock { get; private set; }

        public static Vector3[] CreateCornersAtStartPoint(
            Vector3 rampStartCenter,
            int rotation,
            Vector3 dimensions,
            float blockLength = 1f,
            RotationType rotationType = RotationType.Hexogonal,
            float size = 2
        )
        {
            Vector3[] new_corners = new Vector3[10];
            // Calculate half size for convenience
            float halfSize = size * 0.5f;
            float halfSizeB = 0;// size * 1f;
            rampStartCenter.y += dimensions.y;
            Vector3 direction = rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rotation) : VectorUtil.GetDirectionFromRotation_90Degree(rotation);

            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;

            //Bottom corners
            new_corners[0] = rampStartCenter - right * halfSizeB * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            new_corners[1] = rampStartCenter - right * halfSizeB * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[2] = rampStartCenter + right * halfSizeB * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[3] = rampStartCenter + right * halfSizeB * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);

            //Top corners
            new_corners[4] = rampStartCenter + right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[5] = rampStartCenter + right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);

            //Ramp Back Block Section
            dimensions.x += blockLength;
            //TOP
            new_corners[6] = rampStartCenter + right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[7] = rampStartCenter + right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            //BTM
            new_corners[8] = rampStartCenter + right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            new_corners[9] = rampStartCenter + right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            return new_corners;
        }

        public static Vector3[] CreateCorners(Vector3 centerPos, float size, int rotation, Vector3 dimensions, float blockLength = 1f, RotationType rotationType = RotationType.Hexogonal)
        {
            Vector3[] new_corners = new Vector3[10];
            // Calculate half size for convenience
            float halfSize = size * 0.5f;
            centerPos.y += dimensions.y;
            Vector3 direction = rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rotation) : VectorUtil.GetDirectionFromRotation_90Degree(rotation);

            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            // Calculate the corner points of the cube

            //Bottom corners
            new_corners[0] = centerPos - right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            new_corners[1] = centerPos - right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[2] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[3] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);

            //Top corners
            new_corners[4] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[5] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);

            //Ramp Back Block Section
            dimensions.x += blockLength;
            //TOP
            new_corners[6] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            new_corners[7] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            //BTM
            new_corners[8] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            new_corners[9] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            return new_corners;
        }
        public static Vector3[] CreateCorners(Vector3 centerPos, RectangleBounds rect, float size, Vector3 dimensions)
        {
            Vector3[] new_corners = new Vector3[10];
            // Calculate half size for convenience
            Vector3 centerBase = centerPos;
            centerBase.y = rect.Position.y;

            float halfSize = size * 0.5f;
            float halfSizeB = 0;
            centerPos.y += dimensions.y;
            Vector3 direction = rect.rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rect.rotation) : VectorUtil.GetDirectionFromRotation_90Degree(rect.rotation);
            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            // Calculate the corner points of the cube
            //Bottom corners
            //Front Ramp
            new_corners[0] = centerBase - right * halfSize * dimensions.x - up * halfSizeB * dimensions.y - direction * halfSize * dimensions.z;
            new_corners[1] = centerBase - right * halfSize * dimensions.x - up * halfSizeB * dimensions.y + direction * halfSize * dimensions.z;

            Vector3[] frontCorners = rect.GetCornersOnSide(SurfaceBlockSide.Front);
            Vector3[] topCorners = rect.GetCornersOnSide(SurfaceBlockSide.Front);

            //Back Ramp
            new_corners[2] = frontCorners[1];
            new_corners[3] = frontCorners[0];
            //Top corners
            new_corners[4] = frontCorners[3];
            new_corners[5] = frontCorners[2];

            new_corners[6] = topCorners[2];
            new_corners[7] = topCorners[3];

            //BTM
            new_corners[8] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * (halfSize * dimensions.z);
            new_corners[9] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * (halfSize * dimensions.z);
            return new_corners;
        }

        public Vector3[] GetBlockCorners()
        {
            Vector3[] blockSectionCorners = new Vector3[8];
            //BTM
            blockSectionCorners[0] = corners[2];
            blockSectionCorners[1] = corners[3];
            blockSectionCorners[2] = corners[8];
            blockSectionCorners[3] = corners[9];
            //TOP
            blockSectionCorners[4] = corners[4];
            blockSectionCorners[5] = corners[5];
            blockSectionCorners[6] = corners[7];
            blockSectionCorners[7] = corners[6];
            return blockSectionCorners;
        }

        public List<Vector3> Generate_SlopeCenterLine(int steps = 8)
        {
            Vector3[] slopeCorners = new Vector3[4];
            slopeCorners[0] = corners[0];
            slopeCorners[1] = corners[1];
            slopeCorners[2] = corners[4];
            slopeCorners[3] = corners[5];

            Vector3 topCenter = VectorUtil.GetPointBetween(slopeCorners[2], slopeCorners[3]);
            Vector3 bottomCenter = VectorUtil.GetPointBetween(slopeCorners[0], slopeCorners[1]);
            List<Vector3> slopeCenterLine = new List<Vector3>();
            slopeCenterLine.Add(bottomCenter);
            // slopeCenterLine.Add(topCenter);
            slopeCenterLine.AddRange(
             VectorUtil.GenerateDottedLineBetweenPoints_Diagonal(topCenter, bottomCenter, steps)
            );
            return slopeCenterLine;
        }

        public RectangleBounds Generate_NeighborBlock(RampTopSide side)
        {
            Vector3 neighborCenter;
            float halfSize = backBlock.size; // Half the size of the cube

            switch (side)
            {
                case RampTopSide.Left:
                    neighborCenter = backBlock.Position + new Vector3(-halfSize * backBlock.dimensions.x, 0f, 0f);
                    break;
                case RampTopSide.Right:
                    neighborCenter = backBlock.Position + new Vector3(halfSize * backBlock.dimensions.x, 0f, 0f);
                    break;
                default:
                    neighborCenter = backBlock.Position + new Vector3(0f, 0f, -halfSize * backBlock.dimensions.z);
                    break;
            }
            return new RectangleBounds(neighborCenter, backBlock.size, backBlock.rotation, backBlock.dimensions, backBlock.rotationType);
        }


        public Vector3[] GetCornersOnSide(RampSide side)
        {
            Vector3[] sideCorners = new Vector3[4];
            switch (side)
            {
                case RampSide.Slope:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[5];
                    break;
                case RampSide.Back:
                    sideCorners[0] = corners[6];
                    sideCorners[1] = corners[7];
                    sideCorners[2] = corners[8];
                    sideCorners[3] = corners[9];
                    break;
                case RampSide.Bottom:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[8];
                    sideCorners[3] = corners[9];
                    break;
                case RampSide.BlockTop:
                    sideCorners[0] = corners[4];
                    sideCorners[1] = corners[5];
                    sideCorners[2] = corners[6];
                    sideCorners[3] = corners[7];
                    break;
                case RampSide.BlockLeft:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[8];
                    sideCorners[2] = corners[5];
                    sideCorners[3] = corners[7];
                    break;
                case RampSide.BlockRight:
                    sideCorners[0] = corners[1];
                    sideCorners[1] = corners[9];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[6];
                    break;
                case RampSide.BlockBottom:
                    sideCorners[0] = corners[2];
                    sideCorners[1] = corners[3];
                    sideCorners[2] = corners[8];
                    sideCorners[3] = corners[9];
                    break;
            }
            return sideCorners;
        }

        public Vector3 GetTopPlatformSidePoint(RampTopSide side)
        {
            switch (side)
            {
                case RampTopSide.Left:
                    return VectorUtil.GetPointBetween(corners[5], corners[7]);
                case RampTopSide.Right:
                    return VectorUtil.GetPointBetween(corners[4], corners[6]);

                default:
                    return VectorUtil.GetPointBetween(corners[6], corners[7]);
            }
        }



        public bool IsPointWithinRamp(Vector3 point)
        {
            // Get the bounds of the ramp
            List<Vector3> rampBounds = new List<Vector3>();
            rampBounds.AddRange(GetBlockCorners());

            // Check if the point is within the bounds of the ramp
            return VectorUtil.IsPointWithinPolygon(point, rampBounds);
        }


        public void DrawBlockSection()
        {
            Vector3[] cubeCorners = GetBlockCorners();

            Gizmos.DrawLine(cubeCorners[0], cubeCorners[1]);
            Gizmos.DrawLine(cubeCorners[1], cubeCorners[2]);
            Gizmos.DrawLine(cubeCorners[2], cubeCorners[3]);
            Gizmos.DrawLine(cubeCorners[3], cubeCorners[0]);

            Gizmos.DrawLine(cubeCorners[4], cubeCorners[5]);
            Gizmos.DrawLine(cubeCorners[5], cubeCorners[6]);
            Gizmos.DrawLine(cubeCorners[6], cubeCorners[7]);
            Gizmos.DrawLine(cubeCorners[7], cubeCorners[4]);

            for (var i = 0; i < cubeCorners.Length; i++)
            {
                Gizmos.DrawSphere(cubeCorners[i], 0.3f);
            }
        }

        public void DrawSide(RampSide side)
        {
            Gizmos.color = Color.red;
            List<Vector3> sideCorners = GetCornersOnSide(side).ToList();
            for (var i = 0; i < sideCorners.Count; i++)
            {
                Gizmos.DrawSphere(sideCorners[i], 0.3f);
            }

            Gizmos.DrawLine(sideCorners[0], sideCorners[1]);
            Gizmos.DrawLine(sideCorners[1], sideCorners[3]);
            Gizmos.DrawLine(sideCorners[3], sideCorners[2]);
            Gizmos.DrawLine(sideCorners[2], sideCorners[0]);
            Gizmos.DrawLine(sideCorners[0], sideCorners[3]);
            Gizmos.DrawLine(sideCorners[2], sideCorners[1]);

        }

        public void Draw()
        {
            // DrawSide(RampSide.BlockLeft);

            // Gizmos.color = Color.yellow;
            // DrawBlockSection();

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Position, 0.12f);


            // Gizmos.color = Color.blue;
            // float halfSize = size * 0.1f;
            // Vector3 frontCenter = Position + new Vector3(0f, 0f, size + halfSize);
            // Vector3 blockDimensions = new Vector3(dimensions.x * 0.1f, 0.1f, dimensions.z);
            // // RectangleBounds frontBlock = new RectangleBounds(frontCenter, 2, 0, blockDimensions);
            // // frontBlock.Draw();
            // // Gizmos.DrawSphere(frontCenter, 0.12f);

            // List<Vector3> slopeCenterLine = Generate_SlopeCenterLine();

            // foreach (var item in slopeCenterLine)
            // {
            //     Gizmos.DrawSphere(item, 0.12f);
            //     RectangleBounds step = new RectangleBounds(item, 2, 0, blockDimensions);
            //     step.Draw();
            // }


            Gizmos.color = Color.red;
            Vector3 topSidePoint = GetTopPlatformSidePoint(RampTopSide.Right);
            Gizmos.DrawSphere(topSidePoint, 0.17f);


            Gizmos.color = Color.white;

            // //BTM
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);

            //RAMP BACK
            Gizmos.DrawLine(corners[4], corners[1]);
            Gizmos.DrawLine(corners[5], corners[0]);
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[4], corners[5]);

            // //BACK
            // Gizmos.DrawLine(corners[4], corners[6]);
            // Gizmos.DrawLine(corners[5], corners[7]);
            // Gizmos.DrawLine(corners[6], corners[7]);

            // Gizmos.DrawLine(corners[8], corners[7]);
            // Gizmos.DrawLine(corners[9], corners[6]);


            // //BLOCK BTM
            // Gizmos.DrawLine(corners[2], corners[3]);
            // Gizmos.DrawLine(corners[3], corners[8]);
            // Gizmos.DrawLine(corners[2], corners[9]);
            // Gizmos.DrawLine(corners[8], corners[9]);

            //SLOPE
            Gizmos.DrawLine(corners[4], corners[2]);
            Gizmos.DrawLine(corners[5], corners[3]);
            Gizmos.DrawLine(corners[2], corners[3]);

            for (var i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawSphere(corners[i], 0.1f);
            }
        }

        public (Mesh, Vector3[]) Generate_MeshOnSide(RampSide side, Transform transform)
        {
            Vector3[] sideCorners = GetCornersOnSide(side);
            int vetexLength = sideCorners.Length;
            Mesh surfaceMesh = new Mesh();
            if (transform)
            {
                surfaceMesh.vertices = VectorUtil.InversePointsToLocal_ToArray(sideCorners.ToList(), transform);
            }
            else surfaceMesh.vertices = sideCorners;

            if (
                side == RampSide.BlockTop
                || side == RampSide.Bottom
                || side == RampSide.BlockRight
                || side == RampSide.BlockLeft
            )
            {
                int[] triangles = MeshUtil.GenerateRectangularTriangles(vetexLength);
                surfaceMesh.triangles = triangles;

                // Reverse the winding order of the triangles
                if (side == RampSide.Bottom || side == RampSide.BlockLeft || side == RampSide.BlockTop)
                {
                    MeshUtil.ReverseNormals(surfaceMesh);
                    MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
                }
            }
            else
            {
                int[] triangles = MeshUtil.GenerateTriangles(vetexLength);
                surfaceMesh.triangles = triangles;
                if (side == RampSide.Back)
                {
                    MeshUtil.ReverseNormals(surfaceMesh);
                    MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
                }
            }

            return (surfaceMesh, sideCorners);
        }

        public Mesh Generate_Mesh(Transform transform = null)
        {
            List<Mesh> surfaceMeshes = new List<Mesh>();

            for (var i = 0; i < 6; i++)
            {
                RampSide side = (RampSide)i;

                (Mesh surfaceMesh, Vector3[] sideCorners) = Generate_MeshOnSide(side, transform);
                int vetexLength = sideCorners.Length;

                // Set the UVs to the mesh (you can customize this based on your requirements)
                Vector2[] uvs = new Vector2[vetexLength];
                for (int j = 0; j < vetexLength; j++)
                {
                    uvs[j] = new Vector2(sideCorners[j].x, sideCorners[j].y); // Use x and y as UV coordinates
                }
                surfaceMesh.uv = uvs;
                // Recalculate normals and bounds for the surface mesh
                surfaceMesh.RecalculateNormals();
                surfaceMesh.RecalculateBounds();

                surfaceMeshes.Add(surfaceMesh);

            }
            return MeshUtil.GenerateMeshFromVertexSurfaces(surfaceMeshes);
        }

        public static Vector3[] UpdateOutOfBoundsCorners(Vector3 center, Vector3[] blockCorners, List<Vector3> parentCorners)
        {
            // Move any block corner that is out of bounds into bounds towards the center
            for (int i = 0; i < blockCorners.Length; i++)
            {
                Vector3 corner = blockCorners[i];
                if (!VectorUtil.IsPointWithinPolygon(corner, parentCorners))
                {
                    // Find the nearest edge points of the bounds to the corner
                    Vector3 closestEdgePointA = Vector3.zero;
                    Vector3 closestEdgePointB = Vector3.zero;
                    float closestDistanceSqr = float.MaxValue;

                    for (int j = 0; j < parentCorners.Count; j++)
                    {
                        Vector3 edgePointA = parentCorners[j];
                        Vector3 edgePointB = parentCorners[(j + 1) % parentCorners.Count];
                        Vector3 closestPointOnEdge = GetClosestPointOnLineSegment(corner, edgePointA, edgePointB);
                        float distanceSqr = (corner - closestPointOnEdge).sqrMagnitude;

                        if (distanceSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            closestEdgePointA = edgePointA;
                            closestEdgePointB = edgePointB;
                        }
                    }

                    // Move the corner to the nearest point on the edge of the bounds
                    Vector3 nearestPoint = GetClosestPointOnLineSegment(corner, closestEdgePointA, closestEdgePointB);
                    blockCorners[i] = nearestPoint;
                }
            }

            return blockCorners;
        }
        public static Vector3 GetClosestPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLengthSqr = lineDirection.sqrMagnitude;

            if (lineLengthSqr == 0f)
                return lineStart;

            float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr);
            return lineStart + t * lineDirection;
        }

    }
}