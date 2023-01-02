using UnityEngine;

public class GridDivider3D : MonoBehaviour
{
    // Width, height, and depth of each grid cell
    public float cellWidth, cellHeight, cellDepth;
    // Number of cells in the x and z directions
    public int numCellsX, numCellsZ;
    // Color of the dividing lines
    public Color lineColor;

    // private void OnDrawGizmos()
    // {
    //     // Get the dimensions of the grid plane
    //     float planeWidth = numCellsX * cellWidth;
    //     float planeHeight = numCellsZ * cellHeight;

    //     // Calculate the position of the grid plane
    //     Vector3 planePos = transform.position - new Vector3(planeWidth / 2, 0, planeHeight / 2);

    //     // Iterate through the input grid and create dividing lines for each cell
    //     for (int x = 0; x < numCellsX; x++)
    //     {
    //         for (int z = 0; z < numCellsZ; z++)
    //         {
    //             // Calculate the positions of the dividing lines
    //             Vector3 xPos = planePos + new Vector3((x + 1) * cellWidth, 0, z * cellHeight);
    //             Vector3 zPos = planePos + new Vector3(x * cellWidth, 0, (z + 1) * cellHeight);

    //             // Draw the dividing lines
    //             Gizmos.color = lineColor;
    //             Gizmos.DrawLine(xPos, xPos + new Vector3(0, 0, cellHeight));
    //             Gizmos.DrawLine(zPos, zPos + new Vector3(cellWidth, 0, 0));

    //             // Draw the depth lines if the cell depth is greater than 0
    //             if (cellDepth > 0)
    //             {
    //                 Vector3 yPos = planePos + new Vector3(x * cellWidth, -cellDepth / 2, z * cellHeight);
    //                 Gizmos.DrawLine(yPos, yPos + new Vector3(cellWidth, 0, 0));
    //                 Gizmos.DrawLine(yPos, yPos + new Vector3(0, 0, cellHeight));
    //             }
    //         }
    //     }
    // }

    public float planeHeight, planeWidth;

    private void OnDrawGizmos()
    {
        // Get the dimensions of the grid plane
        planeWidth = transform.lossyScale.x * 10;
        planeHeight = transform.lossyScale.z * 10;

        // Calculate the number of cells in the x and z directions
        numCellsX = Mathf.FloorToInt(planeWidth / cellWidth);
        numCellsZ = Mathf.FloorToInt(planeHeight / cellHeight);

        // Calculate the position of the grid plane
        Vector3 planePos = transform.position - new Vector3(planeWidth / 2, 0, planeHeight / 2);

        // Iterate through the input grid and create dividing lines for each cell
        for (int x = 0; x < numCellsX; x++)
        {
            for (int z = 0; z < numCellsZ; z++)
            {
                // Calculate the positions of the dividing lines
                Vector3 xPos = planePos + new Vector3((x + 1) * cellWidth, 0, z * cellHeight);
                Vector3 zPos = planePos + new Vector3(x * cellWidth, 0, (z + 1) * cellHeight);

                // Draw the dividing lines
                Gizmos.color = lineColor;
                Gizmos.DrawLine(xPos, xPos + new Vector3(0, 0, cellHeight));
                Gizmos.DrawLine(zPos, zPos + new Vector3(cellWidth, 0, 0));

                // Draw the depth lines if the cell depth is greater than 0
                if (cellDepth > 0)
                {
                    Vector3 yPos = planePos + new Vector3(x * cellWidth, -cellDepth / 2, z * cellHeight);
                    Gizmos.DrawLine(yPos, yPos + new Vector3(cellWidth, 0, 0));
                    Gizmos.DrawLine(yPos, yPos + new Vector3(0, 0, cellHeight));
                }
            }
        }
    }

}
