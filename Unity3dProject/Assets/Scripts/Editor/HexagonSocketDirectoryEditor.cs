using UnityEditor;
using UnityEngine;
using WFCSystem;
using System;

[CustomEditor(typeof(HexagonSocketDirectory))]
public class HexagonSocketDirectoryEditor : Editor
{
    private HexagonSocketDirectory socketDirectory;
    private SerializedProperty sockets;
    private SerializedProperty colors;
    private bool[,] matrix;

    private void OnEnable()
    {
        socketDirectory = (HexagonSocketDirectory)target;
        matrix = socketDirectory.matrix;
        sockets = serializedObject.FindProperty("sockets");
    }

    public override void OnInspectorGUI()
    {
        socketDirectory = (HexagonSocketDirectory)target;

        // if (socketDirectory.copyMatrixFromTileSocketDirectory)
        // {
        //     socketDirectory.copyMatrixFromTileSocketDirectory = false;

        //     if (socketDirectory.tileSocketDirectory != null)
        //     {
        //         string[] names = Enum.GetNames(typeof(TileSocketPrimitive));
        //         string[] newSockets = new string[52];
        //         for (int i = 0; i < newSockets.Length; i++)
        //         {
        //             if (i < names.Length) newSockets[i] = names[i];
        //         }

        //         bool[,] oldMatrix = socketDirectory.tileSocketDirectory.GetCompatibilityMatrix();
        //         bool[,] newMatrix = new bool[52, 52];

        //         int oldLength = oldMatrix.GetLength(0);

        //         Debug.Log("oldMatrix: " + oldLength);


        //         for (int i = 0; i < newMatrix.GetLength(0); i++)
        //         {
        //             for (int j = 0; j < newMatrix.GetLength(1); j++)
        //             {
        //                 if (i < oldLength && j < oldLength)
        //                 {
        //                     newMatrix[i, j] = oldMatrix[i, j];
        //                     newMatrix[j, i] = oldMatrix[i, j];
        //                 }
        //             }
        //         }
        //         socketDirectory.sockets = newSockets;
        //         socketDirectory.matrix = newMatrix;

        //         matrix = newMatrix;
        //     }

        //     serializedObject.Update();

        //     serializedObject.ApplyModifiedProperties();
        // }


        matrix = socketDirectory.matrix;
        sockets = serializedObject.FindProperty("sockets");


        // DrawDefaultInspector();
        // if (matrix == null || sockets == null) return;

        GUILayout.Label("Hexagon Socket Directory");

        #region Socket Labels
        EditorGUILayout.LabelField("Sockets", EditorStyles.boldLabel);
        for (int i = 0; i < sockets.arraySize; i++)
        {
            SerializedProperty socket = sockets.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(socket);
        }
        #endregion

        // #region Socket Colors

        // colors = serializedObject.FindProperty("colors");

        // EditorGUILayout.LabelField("Socket Colors", EditorStyles.boldLabel);
        // for (int i = 0; i < colors.arraySize; i++)
        // {
        //     SerializedProperty color = colors.GetArrayElementAtIndex(i);
        //     EditorGUILayout.PropertyField(color);
        // }
        // #endregion

        GUILayout.BeginHorizontal();

        float horizontalOffset = 120;
        float verticalBuffer = (24f * sockets.arraySize) + horizontalOffset * 1.75f;
        // float verticalOffset = 20;

        GUILayout.Space(-verticalBuffer); // Add more space before the horizontal labels
        // GUILayout.Space(-1250); // Add more space before the horizontal labels

        GUILayout.BeginVertical();
        // Calculate vertical offset
        GUILayout.Space(-950); // Add more space before the horizontal labels

        // Display the socket names above the grid
        for (int x = 0; x < sockets.arraySize; x++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(horizontalOffset); // Add more space before the horizontal labels

            // Rotate the GUI matrix by -90 degrees
            GUIUtility.RotateAroundPivot(-90f, new Vector2(5, 0));

            // Display the socket name with a fixed width
            GUILayout.Label(sockets.GetArrayElementAtIndex(x).stringValue, GUILayout.Width(horizontalOffset), GUILayout.Height(21));
            // GUILayout.Space(50); // Add more space before the horizontal labels

            // Reset the GUI matrix
            GUIUtility.RotateAroundPivot(90f, new Vector2(5, 0));

            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();


        #region Compatibility Matrix

        GUILayout.BeginVertical();
        // Display the grid
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            GUILayout.BeginHorizontal();

            // Display the socket name beside the grid with a fixed width
            GUILayout.Label(sockets.GetArrayElementAtIndex(i).stringValue, GUILayout.Width(horizontalOffset));

            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                bool value = matrix[i, j];
                value = EditorGUILayout.Toggle(value, GUILayout.Width(20), GUILayout.Height(20));
                matrix[i, j] = value;
                matrix[j, i] = value;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        #endregion


        if (GUI.changed)
        {
            for (int i = 0; i < socketDirectory.matrix.GetLength(0); i++)
            {
                for (int j = 0; j < socketDirectory.matrix.GetLength(1); j++)
                {
                    // if (matrix[i, j] == true) Debug.Log("matrixProp: " + i + ", " + j + ": True");
                    socketDirectory.matrix[i, j] = matrix[i, j];
                    socketDirectory.matrix[j, i] = matrix[j, i];
                    socketDirectory.sockets[i] = sockets.GetArrayElementAtIndex(i).stringValue;
                }
            }
            socketDirectory.Save();
        }
        serializedObject.Update();

        serializedObject.ApplyModifiedProperties();
    }
}
