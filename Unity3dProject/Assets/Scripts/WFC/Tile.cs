using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // public int id;
    [SerializeField] private TileSocketDirectory tileSocketDirectory;
    public bool isEdgeable; // can be placed on the edge / border or the grid
    [SerializeField] private int[] sideSocketIds = new int[4];

    // Rotated side socket data for the tile
    public int[][] rotatedSideSocketIds { get; private set; }
    [SerializeField] private float sideDisplayOffsetY = 6f;

    public int GetSideSocketId(TileSide side) => sideSocketIds[(int)side];
    public enum TileSide
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3,
    }
    public GameObject[] socketTextDisplay;
    [SerializeField] private bool showSocketColorMap = true;
    public bool showSocketLabels = true;
    private bool _showSocketLabels;

    private void OnValidate()
    {
        rotatedSideSocketIds = new int[4][];
        for (int i = 0; i < 4; i++)
        {
            rotatedSideSocketIds[i] = new int[4];
        }

        // Initialize rotatedSideSocketIds with the sideSocketIds of the unrotated tile
        for (int i = 0; i < 4; i++)
        {
            rotatedSideSocketIds[0][i] = sideSocketIds[i];
        }

        // Update rotatedSideSocketIds with the sideSocketIds of the rotated tiles
        for (int i = 1; i < 4; i++)
        {
            rotatedSideSocketIds[i][(int)TileSide.Front] = rotatedSideSocketIds[i - 1][(int)TileSide.Right];
            rotatedSideSocketIds[i][(int)TileSide.Right] = rotatedSideSocketIds[i - 1][(int)TileSide.Back];
            rotatedSideSocketIds[i][(int)TileSide.Back] = rotatedSideSocketIds[i - 1][(int)TileSide.Left];
            rotatedSideSocketIds[i][(int)TileSide.Left] = rotatedSideSocketIds[i - 1][(int)TileSide.Front];
        }

        EvaluateSocketLabels(true);
    }

    private void EvaluateSocketLabels(bool force = false)
    {
        if (force || _showSocketLabels != showSocketLabels)
        {
            _showSocketLabels = showSocketLabels;
            if (socketTextDisplay != null && socketTextDisplay.Length > 0)
            {
                for (int i = 0; i < socketTextDisplay.Length; i++)
                {
                    socketTextDisplay[i].SetActive(showSocketLabels);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        EvaluateSocketLabels(_showSocketLabels != showSocketLabels);

        if (!showSocketColorMap) return;

        // Set the color of the spheres to the color of the socket id
        Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[sideSocketIds[(int)TileSide.Front]].color : Color.white;
        Vector3 frontSpherePosition = transform.position + transform.forward * 0.5f * transform.localScale.z;
        Gizmos.DrawSphere(frontSpherePosition, 0.1f * transform.localScale.z);

        Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[sideSocketIds[(int)TileSide.Right]].color : Color.white;
        Vector3 rightSpherePosition = transform.position + transform.right * 0.5f * transform.localScale.x;
        Gizmos.DrawSphere(rightSpherePosition, 0.1f * transform.localScale.x);

        Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[sideSocketIds[(int)TileSide.Back]].color : Color.white;
        Vector3 backSpherePosition = transform.position - transform.forward * 0.5f * transform.localScale.z;
        Gizmos.DrawSphere(backSpherePosition, 0.1f * transform.localScale.z);

        Gizmos.color = tileSocketDirectory ? tileSocketDirectory.sockets[sideSocketIds[(int)TileSide.Left]].color : Color.white;
        Vector3 leftSpherePosition = transform.position - transform.right * 0.5f * transform.localScale.x;
        Gizmos.DrawSphere(leftSpherePosition, 0.1f * transform.localScale.x);

        // Update the transform position of each RectTransform in the socketTextDisplay array
        if (showSocketLabels && socketTextDisplay != null && socketTextDisplay.Length == 4)
        {
            socketTextDisplay[(int)TileSide.Front].GetComponent<RectTransform>().position = frontSpherePosition + new Vector3(0, sideDisplayOffsetY, 0);
            socketTextDisplay[(int)TileSide.Right].GetComponent<RectTransform>().position = rightSpherePosition + new Vector3(0, sideDisplayOffsetY, 0);
            socketTextDisplay[(int)TileSide.Back].GetComponent<RectTransform>().position = backSpherePosition + new Vector3(0, sideDisplayOffsetY, 0);
            socketTextDisplay[(int)TileSide.Left].GetComponent<RectTransform>().position = leftSpherePosition + new Vector3(0, sideDisplayOffsetY, 0);

            string[] sideNames = Enum.GetNames(typeof(TileSide));
            for (int i = 0; i < socketTextDisplay.Length; i++)
            {
                socketTextDisplay[i].GetComponent<RectTransform>().rotation = new Quaternion(0, 180, 0, 0);
                TextMesh textMesh = socketTextDisplay[i].GetComponent<TextMesh>();
                textMesh.color = tileSocketDirectory.sockets[sideSocketIds[i]].color;
                string str = "id_" + sideSocketIds[i] + " - " + tileSocketDirectory.sockets[sideSocketIds[i]].name + "\n" + sideNames[i];
                textMesh.text = str;
                textMesh.fontSize = 12;
            }
        }
    }
}
