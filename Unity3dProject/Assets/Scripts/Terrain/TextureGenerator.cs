using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }
    // public static Texture2D TextureFromMaterialMap(Material[] materialMap, int width, int height)
    // {
    //     Texture2D texture = new Texture2D(width, height);
    //     texture.filterMode = FilterMode.Point;
    //     texture.wrapMode = TextureWrapMode.Clamp;

    //     Color[] colorMap = new Color[width * height];
    //     for (int y = 0; y < height; y++)
    //     {
    //         for (int x = 0; x < width; x++)
    //         {
    //             int index = y * width + x;
    //             Texture2D texture2D = (Texture2D)materialMap[index].mainTexture;
    //             colorMap[index] = texture2D.GetPixel(0, 0);
    //         }
    //     }
    //     texture.SetPixels(colorMap);
    //     texture.Apply();
    //     return texture;
    // }

    public static Texture2D CombineMaterials(Material[] materialMap, int width, int height)
    {
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int i = 0; i < materialMap.Length; i++)
        {
            Texture2D texture = materialMap[i].GetTexture("_MainTex") as Texture2D;
            if (texture != null)
            {
                Color[] colors = texture.GetPixels();
                result.SetPixels(i % width, i / width, 1, 1, colors);
            }
        }

        result.Apply();
        return result;
    }


    public static (Texture2D, Color[]) TextureFromMaterialMap(Material[] materialMap, int width, int height, Mesh mesh, MeshRenderer meshRenderer)
    {
        Texture2D texture = new Texture2D(materialMap.Length, materialMap.Length);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                colorMap[index] = materialMap[index].color;

                texture.SetPixel(x, y, materialMap[index].color);

            }
        }
        // for (int i = 0; i < colorMap.Length; i++)
        // {
        //     int x = Mathf.FloorToInt(mesh.vertices[i].x / width * (materialMap.Length - 1));
        //     int y = Mathf.FloorToInt(mesh.vertices[i].y / height * (materialMap.Length - 1));
        //     int index = y * materialMap.Length + x;
        //     colorMap[i] = materialMap[index].color;
        // }

        // for (int x = 0; x < materialMap.Length; x++)
        // {
        //     for (int y = 0; y < materialMap.Length; y++)
        //     {
        //         int index = y * materialMap.Length + x;
        //         texture.SetPixel(x, y, materialMap[index].color);
        //     }
        // }
        texture.Apply();
        return (texture, colorMap);
    }

    // public static void ApplyMaterialMap(Material[] materialMap, int width, int height, Mesh mesh, MeshRenderer meshRenderer)
    // {
    //     Texture2D texture = new Texture2D(materialMap.Length, materialMap.Length);

    //     Color[] colorMap = new Color[width * height];
    //     for (int y = 0; y < height; y++)
    //     {
    //         for (int x = 0; x < width; x++)
    //         {
    //             int index = y * width + x;

    //             colorMap[index] = materialMap[index].color;

    //             texture.SetPixel(x, y, materialMap[index].color);

    //         }
    //     }

    //     // for (int i = 0; i < colorMap.Length; i++)
    //     // {
    //     //     int x = Mathf.FloorToInt(mesh.vertices[i].x / width * (materialMap.Length - 1));
    //     //     int y = Mathf.FloorToInt(mesh.vertices[i].y / height * (materialMap.Length - 1));
    //     //     int index = y * materialMap.Length + x;
    //     //     colorMap[i] = materialMap[index].color;
    //     // }

    //     // for (int x = 0; x < materialMap.Length; x++)
    //     // {
    //     //     for (int y = 0; y < materialMap.Length; y++)
    //     //     {
    //     //         int index = y * materialMap.Length + x;
    //     //         texture.SetPixel(x, y, materialMap[index].color);
    //     //     }
    //     // }
    //     texture.Apply();

    //     mesh.colors = colorMap;
    //     meshRenderer.material.mainTexture = texture;
    //     textureRenderer.sharedMaterial.mainTexture = texture;
    // }



    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Debug.Log("TextureFromHeightMap, heightMap: " + heightMap[x, y]);


                colourMap[y * width + x] =
                    Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }
        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap, float maxHeight)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float mult = heightMap[x, y] / maxHeight;
                Debug.Log("TextureFromHeightMap, heightMap: " + mult);

                colourMap[y * width + x] =
                    Color.Lerp(Color.black, Color.white, mult);
            }
        }
        return TextureFromColourMap(colourMap, width, height);
    }
}
