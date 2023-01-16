using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PyramidGenerator : MonoBehaviour
{
    public float baseSize = 1f;
    public float height = 1f;

    [SerializeField] private Vector3[] points;

    private void Start()
    {
        GeneratePyramid();
    }

    private void OnValidate()
    {
        GeneratePyramid();
    }

    private void GeneratePyramid()
    {
        points = new Vector3[5];

        // Generate the base points
        points[0] = new Vector3(-baseSize / 2, 0, baseSize / 2);
        points[1] = new Vector3(baseSize / 2, 0, baseSize / 2);
        points[2] = new Vector3(baseSize / 2, 0, -baseSize / 2);
        points[3] = new Vector3(-baseSize / 2, 0, -baseSize / 2);

        // Generate the peak point
        Vector3 peak = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            peak += points[i];
        }
        peak /= 4;
        peak.y = height;
        points[4] = peak;
    }

    private void OnDrawGizmos()
    {
        if (points == null) return;

        // Draw lines connecting the points
        Gizmos.color = Color.white;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(points[i], points[(i + 1) % 4]);
            Gizmos.DrawLine(points[i], points[4]);
        }
    }
}
