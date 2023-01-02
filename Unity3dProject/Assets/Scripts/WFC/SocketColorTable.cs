using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public enum SocketType
// {
//     None = 0,
//     Top,
//     Right,
//     Bottom,
//     Left
// }

// [System.Serializable]
// public struct SocketLookupEntry
// {
//     public SocketType Key;
//     public List<SocketType> Value;
// }
// // public class SocketLookupTable : MonoBehaviour
// // {
// //     [SerializeField]
// //     public Dictionary<SocketType, List<SocketType>> _lookupTable;
// // }
// // public class SocketLookupTable : MonoBehaviour
// // {
// //     public List<SocketLookupEntry> LookupTable;
// // }


// public class SocketLookupTable : MonoBehaviour
// {
//     [SerializeField]
//     private List<SocketLookupEntry> _lookupTable = new List<SocketLookupEntry>();

//     public Dictionary<SocketType, List<SocketType>> LookupTable
//     {
//         get
//         {
//             if (_lookupTableDict == null)
//             {
//                 _lookupTableDict = new Dictionary<SocketType, List<SocketType>>();
//                 foreach (SocketLookupEntry entry in _lookupTable)
//                 {
//                     _lookupTableDict.Add(entry.Key, entry.Value);
//                 }
//             }
//             return _lookupTableDict;
//         }
//     }

//     private Dictionary<SocketType, List<SocketType>> _lookupTableDict;

//     private void OnValidate()
//     {
//         _lookupTableDict = null;
//     }
// }

[CreateAssetMenu(fileName = "New Socket Color Table", menuName = "Socket Color Table")]
public class SocketColorTable : ScriptableObject
{
    public Color[] socketColor;

    private void OnValidate()
    {
        // Only generate new colors if the size of the array has changed
        if (socketColor.Length != socketColor.Length)
        {
            // Create a hash set to store the generated colors
            HashSet<Color> generatedColors = new HashSet<Color>();

            // Generate a random color for each socket
            for (int i = 0; i < socketColor.Length; i++)
            {
                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColors.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColors.Add(randomColor);

                // Assign the unique color to the socket
                socketColor[i] = randomColor;
            }
        }
    }
}
