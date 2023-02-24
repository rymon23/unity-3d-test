using UnityEditor;
using UnityEngine;
using WFCSystem;


[CustomEditor(typeof(TileSocketController))]
public class TileSocketControllerEditor : Editor
{
    TileSocketController socketController;
    private string[] socketOptions;

    private void OnEnable()
    {
        socketController = (TileSocketController)target;
    }


    private float _evaluateTimer = 1f;

    public override void OnInspectorGUI()
    {
        socketController = (TileSocketController)target;
        HexagonSocketDirectory socketDirectory = socketController.GetTileSocketDirectory();

        if (socketDirectory == null) return;

        socketOptions = socketDirectory.sockets;

        if (_evaluateTimer > 0f)
        {
            _evaluateTimer -= Time.fixedDeltaTime;
        }
        else
        {
            socketController.EvaluateAllSocketIDs();
            _evaluateTimer = 1f;
        }


        DrawDefaultInspector();

        int resetToSocket = DrawSocketOptionDropdown(socketController.resetToSocket, "Reset To: ");

        int sectionSpacing = 4;
        int sectionInnerSpacing = 3;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Socket Options", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        // Add dropdowns for each specified field
        EditorGUILayout.LabelField("Side Bottom Sockets", EditorStyles.boldLabel);
        socketController.sideBtmFrontA = DrawSocketOptionDropdown(socketController.sideBtmFrontA, "Side Bottom Front A");
        socketController.sideBtmFrontB = DrawSocketOptionDropdown(socketController.sideBtmFrontB, "Side Bottom Front B");
        socketController.sideBtmFrontRightA = DrawSocketOptionDropdown(socketController.sideBtmFrontRightA, "Side Bottom Front Right A");
        socketController.sideBtmFrontRightB = DrawSocketOptionDropdown(socketController.sideBtmFrontRightB, "Side Bottom Front Right B");
        socketController.sideBtmBackRightA = DrawSocketOptionDropdown(socketController.sideBtmBackRightA, "Side Bottom Back Right A");
        socketController.sideBtmBackRightB = DrawSocketOptionDropdown(socketController.sideBtmBackRightB, "Side Bottom Back Right B");
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.sideBtmBackA = DrawSocketOptionDropdown(socketController.sideBtmBackA, "Side Bottom Back A");
        socketController.sideBtmBackB = DrawSocketOptionDropdown(socketController.sideBtmBackB, "Side Bottom Back B");
        socketController.sideBtmBackLeftA = DrawSocketOptionDropdown(socketController.sideBtmBackLeftA, "Side Bottom Back Left A");
        socketController.sideBtmBackLeftB = DrawSocketOptionDropdown(socketController.sideBtmBackLeftB, "Side Bottom Back Left B");
        socketController.sideBtmFrontLeftA = DrawSocketOptionDropdown(socketController.sideBtmFrontLeftA, "Side Bottom Front Left A");
        socketController.sideBtmFrontLeftB = DrawSocketOptionDropdown(socketController.sideBtmFrontLeftB, "Side Bottom Front Left B");

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        EditorGUILayout.LabelField("Side Top Sockets", EditorStyles.boldLabel);
        socketController.sideTopFrontA = DrawSocketOptionDropdown(socketController.sideTopFrontA, "Side Top Front A");
        socketController.sideTopFrontB = DrawSocketOptionDropdown(socketController.sideTopFrontB, "Side Top Front B");
        socketController.sideTopFrontRightA = DrawSocketOptionDropdown(socketController.sideTopFrontRightA, "Side Top Front Right A");
        socketController.sideTopFrontRightB = DrawSocketOptionDropdown(socketController.sideTopFrontRightB, "Side Top Front Right B");
        socketController.sideTopBackRightA = DrawSocketOptionDropdown(socketController.sideTopBackRightA, "Side Top Back Right A");
        socketController.sideTopBackRightB = DrawSocketOptionDropdown(socketController.sideTopBackRightB, "Side Top Back Right B");
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.sideTopBackA = DrawSocketOptionDropdown(socketController.sideTopBackA, "Side Top Back A");
        socketController.sideTopBackB = DrawSocketOptionDropdown(socketController.sideTopBackB, "Side Top Back B");
        socketController.sideTopBackLeftA = DrawSocketOptionDropdown(socketController.sideTopBackLeftA, "Side Top Back Left A");
        socketController.sideTopBackLeftB = DrawSocketOptionDropdown(socketController.sideTopBackLeftB, "Side Top Back Left B");
        socketController.sideTopFrontLeftA = DrawSocketOptionDropdown(socketController.sideTopFrontLeftA, "Side Top Front Left A");
        socketController.sideTopFrontLeftB = DrawSocketOptionDropdown(socketController.sideTopFrontLeftB, "Side Top Front Left B");

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing * 2));
        EditorGUILayout.LabelField("Bottom Edge Sockets", EditorStyles.boldLabel);
        socketController.bottomFrontA = DrawSocketOptionDropdown(socketController.bottomFrontA, "Bottom Front A");
        socketController.bottomFrontB = DrawSocketOptionDropdown(socketController.bottomFrontB, "Bottom Front B");
        socketController.bottomFrontRightA = DrawSocketOptionDropdown(socketController.bottomFrontRightA, "Bottom Front Right A");
        socketController.bottomFrontRightB = DrawSocketOptionDropdown(socketController.bottomFrontRightB, "Bottom Front Right B");
        socketController.bottomBackRightA = DrawSocketOptionDropdown(socketController.bottomBackRightA, "Bottom Back Right A");
        socketController.bottomBackRightB = DrawSocketOptionDropdown(socketController.bottomBackRightB, "Bottom Back Right B");
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.bottomBackA = DrawSocketOptionDropdown(socketController.bottomBackA, "Bottom Back A");
        socketController.bottomBackB = DrawSocketOptionDropdown(socketController.bottomBackB, "Bottom Back B");
        socketController.bottomBackLeftA = DrawSocketOptionDropdown(socketController.bottomBackLeftA, "Bottom Back Left A");
        socketController.bottomBackLeftB = DrawSocketOptionDropdown(socketController.bottomBackLeftB, "Bottom Back Left B");
        socketController.bottomFrontLeftA = DrawSocketOptionDropdown(socketController.bottomFrontLeftA, "Bottom Front Left A");
        socketController.bottomFrontLeftB = DrawSocketOptionDropdown(socketController.bottomFrontLeftB, "Bottom Front Left B");

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        EditorGUILayout.LabelField("Top Edge Sockets", EditorStyles.boldLabel);
        socketController.topFrontA = DrawSocketOptionDropdown(socketController.topFrontA, "Top Front A");
        socketController.topFrontB = DrawSocketOptionDropdown(socketController.topFrontB, "Top Front B");
        socketController.topFrontRightA = DrawSocketOptionDropdown(socketController.topFrontRightA, "Top Front Right A");
        socketController.topFrontRightB = DrawSocketOptionDropdown(socketController.topFrontRightB, "Top Front Right B");
        socketController.topBackRightA = DrawSocketOptionDropdown(socketController.topBackRightA, "Top Back Right A");
        socketController.topBackRightB = DrawSocketOptionDropdown(socketController.topBackRightB, "Top Back Right B");
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.topBackA = DrawSocketOptionDropdown(socketController.topBackA, "Top Back A");
        socketController.topBackB = DrawSocketOptionDropdown(socketController.topBackB, "Top Back B");
        socketController.topBackLeftA = DrawSocketOptionDropdown(socketController.topBackLeftA, "Top Back Left A");
        socketController.topBackLeftB = DrawSocketOptionDropdown(socketController.topBackLeftB, "Top Back Left B");
        socketController.topFrontLeftA = DrawSocketOptionDropdown(socketController.topFrontLeftA, "Top Front Left A");
        socketController.topFrontLeftB = DrawSocketOptionDropdown(socketController.topFrontLeftB, "Top Front Left B");

        serializedObject.Update();


        if (GUI.changed)
        {
            Debug.Log("TileSocketControllerEditor: GUI changed");
            if (resetToSocket != socketController.resetToSocket)
            {
                socketController.resetToSocket = resetToSocket;
            }

            // serializedObject.ApplyModifiedProperties();
            // serializedObject.Update();

            socketController.Save();
        }

    }

    // Draws a dropdown for a socket option and returns the selected value
    private int DrawSocketOptionDropdown(int socketIndex, string label)
    {
        int selectedOption = EditorGUILayout.Popup(label, socketIndex, socketOptions);
        return selectedOption;
    }
}
