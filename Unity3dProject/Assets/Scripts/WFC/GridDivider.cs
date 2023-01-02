using UnityEngine;

// This revised GridDivider class uses the OnDrawGizmos() method to draw dividing lines between the grid cells using the Gizmos.DrawLine() method.
// It also uses the Gizmos.DrawCube() method to draw a cube for each grid cell, using the material's color to set the color of the cube. The lineColor member allows you to specify the color of the dividing lines in the Unity editor or in code.

// public class GridDivider : MonoBehaviour
// {
//     // Input grid for dividing the plane
//     public int[,] inputGrid;
//     // Material to use for each grid cell
//     // public Material[] materials;
//     // Width and height of the input grid
//     private int width, height;
//     // Width and height of each grid cell
//     public float cellWidth, cellHeight;
//     // Color of the dividing lines
//     public Color lineColor;

//     private void Awake()
//     {
//         inputGrid = new int[10, 10];
//     }

//     private void OnDrawGizmos()
//     {
//         if (inputGrid == null || inputGrid.Length == 0) return;

//         // Get the dimensions of the input grid
//         width = inputGrid.GetLength(0);
//         height = inputGrid.GetLength(1);

//         // Iterate through the input grid and create a grid cell for each state
//         for (int x = 0; x < width; x++)
//         {
//             for (int y = 0; y < height; y++)
//             {
//                 // Calculate the position of the grid cell
//                 Vector3 pos = new Vector3(x * cellWidth, y * cellHeight, 0);
//                 // Set the material for the grid cell
//                 Gizmos.color = Color.green; // materials[inputGrid[x, y]].color;
//                 // Draw a quad for the grid cell
//                 Gizmos.DrawCube(pos, new Vector3(cellWidth, cellHeight, 0.1f));

//                 // Create dividing lines for the grid
//                 if (x < width - 1)
//                 {
//                     // Create a vertical dividing line
//                     Vector3 linePos = new Vector3((x + 1) * cellWidth, y * cellHeight, 0);
//                     Gizmos.color = lineColor;
//                     Gizmos.DrawLine(linePos, linePos + new Vector3(0, cellHeight, 0));
//                 }
//                 if (y < height - 1)
//                 {
//                     // Create a horizontal dividing line
//                     Vector3 linePos = new Vector3(x * cellWidth, (y + 1) * cellHeight, 0);
//                     Gizmos.color = lineColor;
//                     Gizmos.DrawLine(linePos, linePos + new Vector3(cellWidth, 0, 0));
//                 }
//             }
//         }
//     }
// }

using UnityEngine.UI;

public class GridDivider : MonoBehaviour
{
    // Input grid for dividing the plane
    public int[,] inputGrid;
    // Width and height of the input grid
    private int width, height;
    // Color of the dividing lines
    public Color lineColor;
    // Prefab for the grid numbers
    // public GameObject numberPrefab;
    private void Awake()
    {
        inputGrid = new int[10, 10];
    }
    private void OnDrawGizmos()
    {
        if (inputGrid == null || inputGrid.Length == 0) return;


        // Get the dimensions of the input grid
        width = inputGrid.GetLength(0);
        height = inputGrid.GetLength(1);

        // Calculate the position of the grid plane
        Vector3 planePos = new Vector3((width - 1) * transform.lossyScale.x / 2, 0, (height - 1) * transform.lossyScale.z / 2);
        // Set the transform position of the grid plane
        transform.position = planePos;

        // Iterate through the input grid and create a grid cell for each state
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Calculate the position of the grid cell
                Vector3 pos = new Vector3(x * transform.lossyScale.x, 0, y * transform.lossyScale.z);
                // Create a grid number for the current cell
                // GameObject number = Instantiate(numberPrefab, pos, Quaternion.identity);
                // number.GetComponent<Text>().text = inputGrid[x, y].ToString();
                // number.transform.parent = transform;

                // Create dividing lines for the grid
                if (x < width - 1)
                {
                    // Create a vertical dividing line
                    Vector3 linePos = new Vector3((x + 1) * transform.lossyScale.x, 0, y * transform.lossyScale.z);
                    Gizmos.color = lineColor;
                    Gizmos.DrawLine(linePos, linePos + new Vector3(0, 0, transform.lossyScale.z));
                }
                if (y < height - 1)
                {
                    // Create a horizontal dividing line
                    Vector3 linePos = new Vector3(x * transform.lossyScale.x, 0, (y + 1) * transform.lossyScale.z);
                    Gizmos.color = lineColor;
                    Gizmos.DrawLine(linePos, linePos + new Vector3(transform.lossyScale.x, 0, 0));
                }
            }
        }
    }
}
