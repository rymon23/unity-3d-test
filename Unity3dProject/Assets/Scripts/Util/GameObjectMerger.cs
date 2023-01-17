using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectMerger : MonoBehaviour
{
    public static GameObject MergeNestedGameObjects(GameObject parent)
    {
        // Create a new gameobject to serve as the parent for the merged mesh
        var combinedObject = new GameObject("Combined Mesh");

        // Add a MeshFilter and MeshRenderer to the new gameobject
        var filter = combinedObject.AddComponent<MeshFilter>();
        var renderer = combinedObject.AddComponent<MeshRenderer>();

        // Create a list to store all the combine instances from the child gameobjects
        var combineInstances = new List<CombineInstance>();

        // Get all child gameobjects recursively
        RecursiveGetChildMeshes(parent, combineInstances);

        // Combine the meshes into a single mesh
        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

        // Assign the combined mesh to the MeshFilter
        filter.sharedMesh = combinedMesh;

        // Assign the material from the first child gameobject to the MeshRenderer
        renderer.sharedMaterial = parent.GetComponent<MeshRenderer>().sharedMaterial;

        // Remove the child gameobjects
        for (int i = 0; i < parent.transform.childCount; i++)
            DestroyImmediate(parent.transform.GetChild(i).gameObject);

        // Make the new gameobject a child of the original parent
        combinedObject.transform.parent = parent.transform;
        return combinedObject;
    }

    private static void RecursiveGetChildMeshes(GameObject parent, List<CombineInstance> combineInstances)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i).gameObject;
            var meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = meshFilter.sharedMesh;
                ci.transform = meshFilter.transform.localToWorldMatrix;
                combineInstances.Add(ci);
            }
            if (child.transform.childCount > 0)
                RecursiveGetChildMeshes(child, combineInstances);
        }
    }
}


// public static void MergeNestedGameObjects(GameObject parent)
// {
//     // Get all child gameobjects
//     var children = new List<GameObject>();
//     for (int i = 0; i < parent.transform.childCount; i++)
//         children.Add(parent.transform.GetChild(i).gameObject);

//     // Create a new gameobject to serve as the parent for the merged mesh
//     var combinedObject = new GameObject("Combined Mesh");

//     // Add a MeshFilter and MeshRenderer to the new gameobject
//     var filter = combinedObject.AddComponent<MeshFilter>();
//     var renderer = combinedObject.AddComponent<MeshRenderer>();

//     // Create a list to store all the combine instances from the child gameobjects
//     var combineInstances = new List<CombineInstance>();

//     // Add the meshes from each child gameobject to the list
//     for (int i = 0; i < children.Count; i++)
//     {
//         var meshFilter = children[i].GetComponent<MeshFilter>();
//         if (meshFilter != null)
//         {
//             CombineInstance ci = new CombineInstance();
//             ci.mesh = meshFilter.sharedMesh;
//             ci.transform = meshFilter.transform.localToWorldMatrix;
//             combineInstances.Add(ci);
//         }
//     }

//     // Combine the meshes into a single mesh
//     var combinedMesh = new Mesh();
//     combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

//     // Assign the combined mesh to the MeshFilter
//     filter.sharedMesh = combinedMesh;

//     // Assign the material from the first child gameobject to the MeshRenderer
//     renderer.sharedMaterial = children[0].GetComponent<MeshRenderer>().sharedMaterial;

//     // Remove the child gameobjects
//     for (int i = 0; i < children.Count; i++)
//         DestroyImmediate(children[i]);

//     // Make the new gameobject a child of the original parent
//     combinedObject.transform.parent = parent.transform;
// }


