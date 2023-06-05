using UnityEditor;
using UnityEngine;
using WFCSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


[CustomEditor(typeof(TileSocketController))]
public class TileSocketControllerEditor : Editor
{
    TileSocketController socketController;
    private string[] socketOptions;

    private Dictionary<string, int> socketNamesToIndex;
    private string[] socketOptions_side;
    private string[] socketOptions_layered;
    private string[] socketOptions_allAssigned;
    private string[] socketOptions_allSwappable;

    // private GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
    // private GUIStyle fontStyleSocket_globals = new GUIStyle(GUI.skin.label);
    // private GUIStyle fontStyleSocket_sides= new GUIStyle(GUI.skin.label);
    // private GUIStyle fontStyleSocket_layers= new GUIStyle(GUI.skin.label);
    // private GUIStyle fontStyleSocket_hover= new GUIStyle(GUI.skin.label);

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

        if (socketNamesToIndex == null || socketNamesToIndex.Count == 0) SetupSocketNameIndexDictionary(socketOptions);

        if (GUI.changed || socketOptions_allAssigned == null || socketOptions_allAssigned.Length == 0 || socketOptions_side == null || socketOptions_layered == null)
        {
            FilterSocketOptions(socketOptions);
        }

        DrawDefaultInspector();

        int resetToSocket = DrawSocketOptionDropdown_Filtered(socketController.resetToSocket, "Reset To: ", OptionType.AllAssigned);

        int sectionSpacing = 4;
        int sectionInnerSpacing = 3;


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Socket Options", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        // Add dropdowns for each specified field
        EditorGUILayout.LabelField("Side Bottom Sockets", EditorStyles.boldLabel);
        socketController.sideBtmFrontA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontA, "Side Bottom Front A", OptionType.Sides);
        socketController.sideBtmFrontB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontB, "Side Bottom Front B", OptionType.Sides);
        socketController.sideBtmFrontRightA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontRightA, "Side Bottom Front Right A", OptionType.Sides);
        socketController.sideBtmFrontRightB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontRightB, "Side Bottom Front Right B", OptionType.Sides);
        socketController.sideBtmBackRightA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackRightA, "Side Bottom Back Right A", OptionType.Sides);
        socketController.sideBtmBackRightB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackRightB, "Side Bottom Back Right B", OptionType.Sides);
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.sideBtmBackA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackA, "Side Bottom Back A", OptionType.Sides);
        socketController.sideBtmBackB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackB, "Side Bottom Back B", OptionType.Sides);
        socketController.sideBtmBackLeftA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackLeftA, "Side Bottom Back Left A", OptionType.Sides);
        socketController.sideBtmBackLeftB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmBackLeftB, "Side Bottom Back Left B", OptionType.Sides);
        socketController.sideBtmFrontLeftA = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontLeftA, "Side Bottom Front Left A", OptionType.Sides);
        socketController.sideBtmFrontLeftB = DrawSocketOptionDropdown_Filtered(socketController.sideBtmFrontLeftB, "Side Bottom Front Left B", OptionType.Sides);

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        EditorGUILayout.LabelField("Side Top Sockets", EditorStyles.boldLabel);
        socketController.sideTopFrontA = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontA, "Side Top Front A", OptionType.Sides);
        socketController.sideTopFrontB = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontB, "Side Top Front B", OptionType.Sides);
        socketController.sideTopFrontRightA = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontRightA, "Side Top Front Right A", OptionType.Sides);
        socketController.sideTopFrontRightB = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontRightB, "Side Top Front Right B", OptionType.Sides);
        socketController.sideTopBackRightA = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackRightA, "Side Top Back Right A", OptionType.Sides);
        socketController.sideTopBackRightB = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackRightB, "Side Top Back Right B", OptionType.Sides);
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.sideTopBackA = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackA, "Side Top Back A", OptionType.Sides);
        socketController.sideTopBackB = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackB, "Side Top Back B", OptionType.Sides);
        socketController.sideTopBackLeftA = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackLeftA, "Side Top Back Left A", OptionType.Sides);
        socketController.sideTopBackLeftB = DrawSocketOptionDropdown_Filtered(socketController.sideTopBackLeftB, "Side Top Back Left B", OptionType.Sides);
        socketController.sideTopFrontLeftA = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontLeftA, "Side Top Front Left A", OptionType.Sides);
        socketController.sideTopFrontLeftB = DrawSocketOptionDropdown_Filtered(socketController.sideTopFrontLeftB, "Side Top Front Left B", OptionType.Sides);

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing * 2));
        EditorGUILayout.LabelField("Bottom Edge Sockets", EditorStyles.boldLabel);
        socketController.bottomFrontA = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontA, "Bottom Front A", OptionType.Layers);
        socketController.bottomFrontB = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontB, "Bottom Front B", OptionType.Layers);
        socketController.bottomFrontRightA = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontRightA, "Bottom Front Right A", OptionType.Layers);
        socketController.bottomFrontRightB = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontRightB, "Bottom Front Right B", OptionType.Layers);
        socketController.bottomBackRightA = DrawSocketOptionDropdown_Filtered(socketController.bottomBackRightA, "Bottom Back Right A", OptionType.Layers);
        socketController.bottomBackRightB = DrawSocketOptionDropdown_Filtered(socketController.bottomBackRightB, "Bottom Back Right B", OptionType.Layers);
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.bottomBackA = DrawSocketOptionDropdown_Filtered(socketController.bottomBackA, "Bottom Back A", OptionType.Layers);
        socketController.bottomBackB = DrawSocketOptionDropdown_Filtered(socketController.bottomBackB, "Bottom Back B", OptionType.Layers);
        socketController.bottomBackLeftA = DrawSocketOptionDropdown_Filtered(socketController.bottomBackLeftA, "Bottom Back Left A", OptionType.Layers);
        socketController.bottomBackLeftB = DrawSocketOptionDropdown_Filtered(socketController.bottomBackLeftB, "Bottom Back Left B", OptionType.Layers);
        socketController.bottomFrontLeftA = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontLeftA, "Bottom Front Left A", OptionType.Layers);
        socketController.bottomFrontLeftB = DrawSocketOptionDropdown_Filtered(socketController.bottomFrontLeftB, "Bottom Front Left B", OptionType.Layers);

        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionSpacing));
        EditorGUILayout.LabelField("Top Edge Sockets", EditorStyles.boldLabel);
        socketController.topFrontA = DrawSocketOptionDropdown_Filtered(socketController.topFrontA, "Top Front A", OptionType.Layers);
        socketController.topFrontB = DrawSocketOptionDropdown_Filtered(socketController.topFrontB, "Top Front B", OptionType.Layers);
        socketController.topFrontRightA = DrawSocketOptionDropdown_Filtered(socketController.topFrontRightA, "Top Front Right A", OptionType.Layers);
        socketController.topFrontRightB = DrawSocketOptionDropdown_Filtered(socketController.topFrontRightB, "Top Front Right B", OptionType.Layers);
        socketController.topBackRightA = DrawSocketOptionDropdown_Filtered(socketController.topBackRightA, "Top Back Right A", OptionType.Layers);
        socketController.topBackRightB = DrawSocketOptionDropdown_Filtered(socketController.topBackRightB, "Top Back Right B", OptionType.Layers);
        EditorGUILayout.LabelField(" ", GUILayout.Height(sectionInnerSpacing));
        socketController.topBackA = DrawSocketOptionDropdown_Filtered(socketController.topBackA, "Top Back A", OptionType.Layers);
        socketController.topBackB = DrawSocketOptionDropdown_Filtered(socketController.topBackB, "Top Back B", OptionType.Layers);
        socketController.topBackLeftA = DrawSocketOptionDropdown_Filtered(socketController.topBackLeftA, "Top Back Left A", OptionType.Layers);
        socketController.topBackLeftB = DrawSocketOptionDropdown_Filtered(socketController.topBackLeftB, "Top Back Left B", OptionType.Layers);
        socketController.topFrontLeftA = DrawSocketOptionDropdown_Filtered(socketController.topFrontLeftA, "Top Front Left A", OptionType.Layers);
        socketController.topFrontLeftB = DrawSocketOptionDropdown_Filtered(socketController.topFrontLeftB, "Top Front Left B", OptionType.Layers);
        // //TEMP
        // testOption_A = DrawSocketOptionDropdown_Filtered(testOption_Side, "TEST FILTER OPTION", false);
        // testOption_B = DrawSocketOptionDropdown_Filtered(testOption_L, "TEST FILTER OPTION - Layered", true);

        // EditorGUILayout.Space();
        // EditorGUILayout.LabelField("Invertable Side Sockets", EditorStyles.boldLabel);
        // socketController.swappableSideSocketA = DrawSocketOptionDropdown_Filtered(socketController.swappableSideSocketA, "Socket A", OptionType.Sides);
        // socketController.swappableSideSocketB = DrawSocketOptionDropdown_Filtered(socketController.swappableSideSocketB, "Socket B", OptionType.Sides);


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

    int testOption_A = 0;
    int testOption_B = 0;
    // int testOption_Side = 0;
    // int testOption_L = 0;

    // Draws a dropdown for a socket option and returns the selected value
    private int DrawSocketOptionDropdown(int socketIndex, string label)
    {
        int selectedOption = EditorGUILayout.Popup(label, socketIndex, socketOptions);
        return selectedOption;
    }

    enum OptionType { AllAssigned = 0, Sides, Layers, SideSwappable }
    private int DrawSocketOptionDropdown_Filtered(int socketIndex, string label, OptionType optionType)
    {
        string socketName = socketOptions[socketIndex];
        string[] options = socketOptions_allAssigned;

        if (optionType == OptionType.Sides)
        {
            options = socketOptions_side;
        }
        else if (optionType == OptionType.Layers)
        {
            options = socketOptions_layered;
        }

        int filteredIndex = options.ToList().FindIndex(e => e == socketName);

        int selectedOptionIX = EditorGUILayout.Popup(label, filteredIndex, options);
        string selectedName = options[selectedOptionIX];

        if (socketNamesToIndex.ContainsKey(selectedName))
        {
            return socketNamesToIndex[selectedName];
        }
        else
        {
            return socketIndex;
        }
    }

    private void SetupSocketNameIndexDictionary(string[] allSocketOptions)
    {
        Dictionary<string, int> _socketNamesToIndex = new Dictionary<string, int>();
        for (var i = 0; i < allSocketOptions.Length; i++)
        {
            string socketName = allSocketOptions[i];
            _socketNamesToIndex.Add(socketName, i);
        }
        socketNamesToIndex = _socketNamesToIndex;
    }

    private void FilterSocketOptions(string[] allSocketOptions)
    {
        string[] globalSockets = Enum.GetNames(typeof(GlobalSockets));
        List<string> new_socketOptions_side = new List<string>();
        List<string> new_socketOptions_layered = new List<string>();
        List<string> new_socketOptions_allAssigned = new List<string>();
        List<string> new_socketOptions_allSwappable = new List<string>();

        for (var i = 0; i < allSocketOptions.Length; i++)
        {
            string socketName = allSocketOptions[i];
            if (i < globalSockets.Length)
            {
                // Dont include the sockets for Unassigned 
                if (HexagonSocketDirectory.IsGlobalUnassignedSocket(i)) continue;

                new_socketOptions_side.Add(socketName);
                new_socketOptions_layered.Add(socketName);
                new_socketOptions_allAssigned.Add(socketName);
            }
            else
            {
                if (socketName.Contains(HexagonSocketDirectory.GetSocketPrefix_Blank())) continue;

                new_socketOptions_allAssigned.Add(socketName);

                if (socketName.Contains(HexagonSocketDirectory.GetSocketPrefix_Layered()))
                {
                    new_socketOptions_layered.Add(socketName);
                }
                else
                {
                    new_socketOptions_side.Add(socketName);
                }
            }
        }

        socketOptions_side = new_socketOptions_side.ToArray();
        socketOptions_layered = new_socketOptions_layered.ToArray();
        socketOptions_allAssigned = new_socketOptions_allAssigned.ToArray();

        new_socketOptions_allSwappable.AddRange(new_socketOptions_side.FindAll(s => s != socketOptions[0]));
        socketOptions_allSwappable = new_socketOptions_allSwappable.ToArray();
    }
}
