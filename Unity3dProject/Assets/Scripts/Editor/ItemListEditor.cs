using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemList))]
public class ItemListEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (((ItemList)target).modifyValues)
        {
            if (GUILayout.Button("Save changes"))
            {
                ((ItemList)target).DeserializeDictionary();
            }
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar);
        if (GUILayout.Button("Print Dictionary"))
        {
            ((ItemList)target).PrintDictionary();
        }
    }
}
