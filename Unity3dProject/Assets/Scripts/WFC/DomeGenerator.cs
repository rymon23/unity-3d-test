using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralBase;

[System.Serializable]
public class DomeGenerator : MonoBehaviour
{
    [SerializeField] private float size = 1f;
    [SerializeField] private int resolution = 10;
    [SerializeField] private Vector3[] hexagonPoints;
    [SerializeField] private Vector3[] domePoints;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float domeRadius = 2f;

    private Vector3[] points;

    private void Start()
    {
        GenerateHalfDomePoints();
    }

    private void OnValidate()
    {
        GenerateHalfDomePoints();
    }


    public Vector3[] GenerateHalfDomeOverHexagon(Vector3[] hexagonPoints, float domeRadius)
    {
        Vector3[] domePoints = new Vector3[12];
        Vector3 center = hexagonPoints[0];
        Quaternion rotation = Quaternion.AngleAxis(30, Vector3.up);

        for (int i = 0; i < 6; i++)
        {
            Vector3 pointOnHexagon = hexagonPoints[i];
            Vector3 pointOnDome = center + Quaternion.Euler(0, i * 60, 0) * Vector3.forward * domeRadius;
            domePoints[i] = pointOnDome;
            domePoints[i + 6] = pointOnHexagon;
        }
        return domePoints;
    }


    // Vector3[] GenerateHalfDomeOverHexagon(Vector3[] hexagonPoints, float radius, float height)
    // {
    //     // create an empty list to store the points of the half dome
    //     List<Vector3> domePoints = new List<Vector3>();

    //     // calculate the center point of the hexagon
    //     Vector3 center = Vector3.zero;
    //     for (int i = 0; i < hexagonPoints.Length; i++)
    //     {
    //         center += hexagonPoints[i];
    //     }
    //     center /= hexagonPoints.Length;

    //     // generate the points of the half dome
    //     for (int i = 0; i < hexagonPoints.Length; i++)
    //     {
    //         // calculate the direction from the center to the current hexagon point
    //         Vector3 direction = (hexagonPoints[i] - center).normalized;
    //         // add the point on the outer shell of the half dome
    //         domePoints.Add(center + direction * radius);
    //         // add the peak point of the half dome
    //         domePoints.Add(center + direction * radius + Vector3.up * height);
    //     }

    //     // return the points of the half dome
    //     return domePoints.ToArray();
    // }

    // void OnDrawGizmos()
    // {
    //     hexagonPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, radius);

    //     Vector3[] halfDomePoints = GenerateHalfDomeOverHexagon(hexagonPoints, domeRadius);
    //     for (int i = 0; i < 11; i++)
    //     {
    //         Gizmos.DrawLine(halfDomePoints[i], halfDomePoints[i + 1]);
    //         if (i == 5) Gizmos.DrawLine(halfDomePoints[i], halfDomePoints[0]);
    //     }
    // }
    // void OnDrawGizmos()
    // {
    //     hexagonPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, radius);
    //     domePoints = GenerateHalfDomeOverHexagon(hexagonPoints, radius);

    //     for (int i = 0; i < domePoints.Length; i += 2)
    //     {
    //         // Draw line between outer shell point and peak point
    //         Gizmos.DrawLine(domePoints[i], domePoints[i + 1]);
    //     }
    //     for (int i = 0; i < domePoints.Length - 2; i += 2)
    //     {
    //         // Draw line between peak point and next peak point
    //         Gizmos.DrawLine(domePoints[i + 1], domePoints[i + 3]);
    //     }
    //     for (int i = 0; i < domePoints.Length - 2; i += 2)
    //     {
    //         // Draw line between outer shell point and next outer shell point
    //         Gizmos.DrawLine(domePoints[i], domePoints[i + 2]);
    //     }
    // }


    private void GenerateHalfDomePoints()
    {
        // Create an empty array to store the points
        points = new Vector3[(resolution + 1) * (resolution + 1)];

        // Generate the points of the outer shell of the half dome and center it over the hexagon shape
        int i = 0;
        for (float v = 0.5f; v <= 1f; v += 1f / resolution)
        {
            for (float u = 0f; u <= 1f; u += 1f / resolution)
            {
                float x = size * Mathf.Sin(Mathf.PI * u) * Mathf.Cos(2f * Mathf.PI * v);
                float y = size * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(2f * Mathf.PI * v);
                float z = size * Mathf.Cos(Mathf.PI * u);
                points[i++] = new Vector3(x, y, z) + transform.position; //hexagonCenter;
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (points == null) return;

        for (int i = 0; i < points.Length; i++)
        {
            if (i == (resolution + 1) * (resolution + 1) / 2) continue; // Skip the center point

            Vector3 currentPoint = points[i];
            Vector3 nextPoint;

            // Connect the point to the point on the right
            if (i % (resolution + 1) != resolution)
            {
                nextPoint = points[i + 1];
                Gizmos.DrawLine(currentPoint, nextPoint);
            }

            // Connect the point to the point below
            if (i < points.Length - (resolution + 1))
            {
                nextPoint = points[i + resolution + 1];
                Gizmos.DrawLine(currentPoint, nextPoint);
            }
        }
    }


    // private void GenerateDomePoints()
    // {
    //     // Create an empty array to store the points
    //     points = new Vector3[(resolution + 1) * (resolution + 1)];

    //     // Generate the points of the dome
    //     int i = 0;
    //     for (float v = 0f; v <= 1f; v += 1f / resolution)
    //     {
    //         for (float u = 0f; u <= 1f; u += 1f / resolution)
    //         {
    //             float x = size * Mathf.Sin(Mathf.PI * u) * Mathf.Cos(2f * Mathf.PI * v);
    //             float y = size * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(2f * Mathf.PI * v);
    //             float z = size * Mathf.Cos(Mathf.PI * u);
    //             points[i++] = new Vector3(x, y, z);
    //         }
    //     }
    // }

    // private void OnDrawGizmos()
    // {
    //     // GenerateHalfDomePoints();
    //     // Draw lines connecting the points
    //     for (int i = 0; i < points.Length; i++)
    //     {
    //         for (int j = i + 1; j < points.Length; j++)
    //         {
    //             Gizmos.DrawLine(transform.TransformPoint(points[i]), transform.TransformPoint(points[j]));
    //         }
    //     }
    // }
}
