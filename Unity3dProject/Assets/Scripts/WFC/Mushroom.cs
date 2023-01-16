using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Mushroom : MonoBehaviour
{
    public float size = 1;
    public float capHeight = 2;
    public float capWidth = 2;
    public float stemHeight = 1;
    public float stemWidth = 0.5f;
    public int segments = 10;

    private Vector3[] capPoints;
    private Vector3[] stemPoints;

    // private void Start()
    // {
    //     GenerateMushroom();
    // }
    // private void OnValidate()
    // {
    //     GenerateMushroom();
    // }

    Vector3[] GenerateMushroomPoints(float capHeight, float capWidth, float stemHeight, float stemWidth)
    {
        // create an array to store the points
        Vector3[] points = new Vector3[10];

        // generate the points for the cap of the mushroom
        points[0] = new Vector3(-capWidth / 2, 0, -capWidth / 2);
        points[1] = new Vector3(-capWidth / 2, 0, capWidth / 2);
        points[2] = new Vector3(capWidth / 2, 0, capWidth / 2);
        points[3] = new Vector3(capWidth / 2, 0, -capWidth / 2);
        points[4] = new Vector3(0, capHeight, 0);

        // generate the points for the stem of the mushroom
        points[5] = new Vector3(-stemWidth / 2, 0, -stemWidth / 2);
        points[6] = new Vector3(-stemWidth / 2, 0, stemWidth / 2);
        points[7] = new Vector3(stemWidth / 2, 0, stemWidth / 2);
        points[8] = new Vector3(stemWidth / 2, 0, -stemWidth / 2);
        points[9] = new Vector3(0, stemHeight, 0);

        // return the points
        return points;
    }

    private void OnDrawGizmos()
    {
        // generate the points for the mushroom
        Vector3[] mushroomPoints = GenerateMushroomPoints(capHeight, capWidth, stemHeight, stemWidth);

        // set the color for the lines
        Gizmos.color = Color.red;

        // draw lines connecting the points for the cap of the mushroom
        Gizmos.DrawLine(mushroomPoints[0], mushroomPoints[1]);
        Gizmos.DrawLine(mushroomPoints[1], mushroomPoints[2]);
        Gizmos.DrawLine(mushroomPoints[2], mushroomPoints[3]);
        Gizmos.DrawLine(mushroomPoints[3], mushroomPoints[0]);

        // draw lines connecting the points for the stem of the mushroom
        Gizmos.DrawLine(mushroomPoints[5], mushroomPoints[6]);
        Gizmos.DrawLine(mushroomPoints[6], mushroomPoints[7]);
        Gizmos.DrawLine(mushroomPoints[7], mushroomPoints[8]);
        Gizmos.DrawLine(mushroomPoints[8], mushroomPoints[5]);

        // draw the lines connecting the cap and the stem
        Gizmos.DrawLine(mushroomPoints[4], mushroomPoints[9]);
    }


    // private void GenerateMushroom()
    // {
    //     capPoints = new Vector3[segments];
    //     stemPoints = new Vector3[segments];

    //     // Generate points for mushroom cap
    //     for (int i = 0; i < segments; i++)
    //     {
    //         float angle = (float)i / (float)segments * Mathf.PI * 2;
    //         capPoints[i] = new Vector3(Mathf.Cos(angle) * size, capHeight, Mathf.Sin(angle) * size);
    //     }

    //     // Generate points for mushroom stem
    //     for (int i = 0; i < segments; i++)
    //     {
    //         float angle = (float)i / (float)segments * Mathf.PI * 2;
    //         stemPoints[i] = new Vector3(Mathf.Cos(angle) * stemWidth, 0, Mathf.Sin(angle) * stemWidth);
    //     }
    // }

    // private void OnDrawGizmos()
    // {
    //     if (capPoints != null)
    //     {
    //         for (int i = 0; i < segments; i++)
    //         {
    //             Gizmos.DrawLine(transform.position + capPoints[i], transform.position + capPoints[(i + 1) % segments]);
    //         }
    //     }
    //     if (stemPoints != null)
    //     {
    //         for (int i = 0; i < segments; i++)
    //         {
    //             Gizmos.DrawLine(transform.position + stemPoints[i], transform.position + stemPoints[(i + 1) % segments]);
    //             Gizmos.DrawLine(transform.position + capPoints[i], transform.position + stemPoints[i]);
    //         }
    //     }
    // }
}
