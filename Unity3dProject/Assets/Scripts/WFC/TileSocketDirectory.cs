using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Socket Directory", menuName = "Socket Directory")]
public class TileSocketDirectory : ScriptableObject
{
    [System.Serializable]
    public struct SocketEntry
    {
        public string name;
        public string description;
        public Color color;
    }

    public SocketEntry[] sockets;
    private int socketsLength = 0;

    private void OnValidate()
    {
        // Only generate new colors if the size of the array has changed
        if (sockets != null && sockets.Length != socketsLength)
        {
            socketsLength = sockets.Length;

            // Create a hash set to store the generated colors
            HashSet<Color> generatedColors = new HashSet<Color>();

            // Generate a random color for each socket
            for (int i = 0; i < sockets.Length; i++)
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
                sockets[i].color = randomColor;
            }
        }
    }
}
