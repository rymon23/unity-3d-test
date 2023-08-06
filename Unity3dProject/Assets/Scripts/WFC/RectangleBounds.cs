using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    public enum BlockSide { Front = 0, Right, Back, Left, Top, Bottom, }
    public class RectangleBounds
    {
        public RectangleBounds(Vector3 position, float _size, int _rotation, Vector3 _dimensions, RotationType _rotationType = RotationType.Hexogonal)
        {
            Position = position;
            size = _size;
            rotation = _rotation;
            rotationType = _rotationType;
            dimensions = _dimensions;
            corners = CreateCorners(position, size, rotation, dimensions, rotationType);
            Center = VectorUtil.Calculate_CenterPositionFromPoints(corners);
        }
        public Vector3 Position { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public float size;
        public Vector3 dimensions { get; private set; }
        public int rotation;
        public RotationType rotationType;

        public Vector3[] corners { get; private set; } = new Vector3[8];
        public Vector3[] GetCornersOnSide(SurfaceBlockSide side)
        {
            Vector3[] sideCorners = new Vector3[4];
            switch (side)
            {
                case SurfaceBlockSide.Front:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[5];
                    break;
                case SurfaceBlockSide.Back:
                    sideCorners[0] = corners[2];
                    sideCorners[1] = corners[3];
                    sideCorners[2] = corners[6];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Right:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[3];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Left:
                    sideCorners[0] = corners[1];
                    sideCorners[1] = corners[2];
                    sideCorners[2] = corners[5];
                    sideCorners[3] = corners[6];
                    break;
                case SurfaceBlockSide.Top:
                    sideCorners[0] = corners[4];
                    sideCorners[1] = corners[5];
                    sideCorners[2] = corners[6];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Bottom:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[2];
                    sideCorners[3] = corners[3];
                    break;
            }
            return sideCorners;
        }

        public static Vector3[] CreateCorners(Vector3 centerPos, float size, int rotation, Vector3 dimensions, RotationType rotationType = RotationType.Hexogonal)
        {
            Vector3[] new_corners = new Vector3[8];
            // Calculate half size for convenience
            float halfSize = size * 0.5f;
            centerPos.y += dimensions.y;

            Vector3 direction = rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rotation) : VectorUtil.GetDirectionFromRotation_90Degree(rotation);
            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            //Bottom corners
            new_corners[0] = centerPos - right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * halfSize * dimensions.z;
            new_corners[1] = centerPos - right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * halfSize * dimensions.z;
            new_corners[2] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y + direction * halfSize * dimensions.z;
            new_corners[3] = centerPos + right * halfSize * dimensions.x - up * halfSize * dimensions.y - direction * halfSize * dimensions.z;

            //Top corners
            new_corners[4] = centerPos - right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * halfSize * dimensions.z;
            new_corners[5] = centerPos - right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * halfSize * dimensions.z;
            new_corners[6] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y + direction * halfSize * dimensions.z;
            new_corners[7] = centerPos + right * halfSize * dimensions.x + up * halfSize * dimensions.y - direction * halfSize * dimensions.z;
            return new_corners;
        }

        public static Vector3[] CreateCorners(Vector3 centerPos, float size, int rotation, RotationType rotationType = RotationType.Hexogonal)
        {
            Vector3[] new_corners = new Vector3[8];
            // Calculate half size for convenience
            float halfSize = size * 0.5f;

            Vector3 direction = rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rotation) : VectorUtil.GetDirectionFromRotation_90Degree(rotation);
            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;

            // Calculate the corner points of the cube
            new_corners[0] = centerPos - right * halfSize - up * halfSize - direction * halfSize;
            new_corners[1] = centerPos - right * halfSize - up * halfSize + direction * halfSize;
            new_corners[2] = centerPos + right * halfSize - up * halfSize + direction * halfSize;
            new_corners[3] = centerPos + right * halfSize - up * halfSize - direction * halfSize;
            new_corners[4] = centerPos - right * halfSize + up * halfSize - direction * halfSize;
            new_corners[5] = centerPos - right * halfSize + up * halfSize + direction * halfSize;
            new_corners[6] = centerPos + right * halfSize + up * halfSize + direction * halfSize;
            new_corners[7] = centerPos + right * halfSize + up * halfSize - direction * halfSize;
            return new_corners;
        }

        public static bool IsPointWithinBounds(Vector3 point, Vector3[] corners)
        {
            float minX = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float minY = Mathf.Infinity;
            float maxY = Mathf.NegativeInfinity;
            float minZ = Mathf.Infinity;
            float maxZ = Mathf.NegativeInfinity;

            // Find the minimum and maximum values in each axis
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
                minZ = Mathf.Min(minZ, corner.z);
                maxZ = Mathf.Max(maxZ, corner.z);
            }

            // Check if the point is within the bounds
            if (point.x >= minX && point.x <= maxX &&
                point.y >= minY && point.y <= maxY &&
                point.z >= minZ && point.z <= maxZ)
            {
                return true;
            }

            return false;
        }

        public static Vector3 Generate_NeighborCenter(SurfaceBlockSide side, RectangleBounds rectangleBounds)
        {
            Vector3 neighborCenter;

            if (side == SurfaceBlockSide.Top || side == SurfaceBlockSide.Bottom)
            {
                Vector3[] neighborCenters = Generate_NeighborCenters(rectangleBounds);
                neighborCenter = neighborCenters[Mathf.Abs((int)side)];

                return neighborCenter;
            }

            int rotMod;
            if (rectangleBounds.rotationType == RotationType.Rectangle)
            {
                rotMod = Mathf.Abs((int)rectangleBounds.rotation + 1) % 4;
            }
            else rotMod = Mathf.Abs((int)rectangleBounds.rotation + 1) % 6;

            Vector3 direction = rectangleBounds.rotationType == RotationType.Hexogonal ? VectorUtil.GetDirectionFromRotation(rotMod)
                                                                   : VectorUtil.GetDirectionFromRotation_90Degree(rotMod);
            // Calculate the axis vectors based on the provided direction
            Vector3 right = Vector3.Cross(Vector3.forward, direction).normalized;
            neighborCenter = rectangleBounds.Center + right + (direction * rectangleBounds.size);
            neighborCenter.y = rectangleBounds.Center.y;
            return neighborCenter;
        }

        public static Vector3[] Generate_NeighborCenters(RectangleBounds rectangleBounds)
        {
            Vector3[] neighborCenters = new Vector3[6];
            Vector3 center = rectangleBounds.Position;
            float halfSize = rectangleBounds.size; // / 2f; // Half the size of the cube
            // Front neighbor center
            neighborCenters[(int)SurfaceBlockSide.Front] = center + new Vector3(0f, 0f, halfSize);
            // Back neighbor center
            neighborCenters[(int)SurfaceBlockSide.Back] = center + new Vector3(0f, 0f, -halfSize);
            // Right neighbor center
            neighborCenters[(int)SurfaceBlockSide.Left] = center + new Vector3(halfSize, 0f, 0f);
            // Left neighbor center
            neighborCenters[(int)SurfaceBlockSide.Right] = center + new Vector3(-halfSize, 0f, 0f);
            // Top neighbor center
            neighborCenters[(int)SurfaceBlockSide.Top] = center + new Vector3(0f, halfSize, 0f);
            // Bottom neighbor center
            neighborCenters[(int)SurfaceBlockSide.Bottom] = center + new Vector3(0f, -halfSize, 0f);
            return neighborCenters;
        }

        public void Draw()
        {
            // Gizmos.color = Color.white;
            for (var i = 0; i < 6; i++)
            {
                // Debug.Log("No neighbor on side: " + (SurfaceBlockSide)i);
                Vector3[] sideCorners = GetCornersOnSide((SurfaceBlockSide)i);
                Gizmos.DrawLine(sideCorners[0], sideCorners[1]);
                Gizmos.DrawLine(sideCorners[1], sideCorners[3]);
                Gizmos.DrawLine(sideCorners[3], sideCorners[2]);
                Gizmos.DrawLine(sideCorners[2], sideCorners[0]);
                Gizmos.DrawLine(sideCorners[0], sideCorners[3]);
                Gizmos.DrawLine(sideCorners[2], sideCorners[1]);
            }
            Gizmos.DrawWireSphere(Center, 0.1f);
            Gizmos.DrawWireSphere(Position, 0.15f);
        }

        public void DrawFace(SurfaceBlockSide side = SurfaceBlockSide.Front)
        {
            Vector3[] sideCorners = GetCornersOnSide(side);
            for (var i = 0; i < sideCorners.Length; i++)
            {
                Gizmos.DrawSphere(sideCorners[i], 0.1f);
            }

            Vector3 neighborCenter = Generate_NeighborCenter(side, this);
            Gizmos.DrawWireSphere(neighborCenter, 0.2f);
        }
    }
}