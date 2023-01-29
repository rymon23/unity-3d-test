using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using ProceduralBase;

public class HeightmapConverter : MonoBehaviour
{
    public Terrain terrain; // path to the PNG file
    public string filePath = "Assets/Terrain/"; // path to the PNG file
    public float[,] heights;

    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private SubZone subZone;
    public HexagonCell[,] rectangularHexGrid;

    public float scale = 1f;
    public Sprite sprite;

    // void Start()
    // {
    //     byte[] fileData = File.ReadAllBytes(filePath);
    //     Texture2D heightmap = new Texture2D(1, 1);
    //     heightmap.LoadImage(fileData);

    //     float[,] heights = new float[heightmap.width, heightmap.height];
    //     for (int x = 0; x < heightmap.width; x++)
    //     {
    //         for (int y = 0; y < heightmap.height; y++)
    //         {
    //             Color pixel = heightmap.GetPixel(x, y);
    //             heights[x, y] = pixel.grayscale;
    //         }
    //     }
    // }

    [SerializeField] private bool convertHeightmap;
    private void OnValidate()
    {
        if (convertHeightmap)
        {
            convertHeightmap = false;

            // sprite = ScaleImage(filePath, scale);
            // heights = ConvertSpriteToHeightmap(sprite);
            // heights = Resize(heights, terrain);
            // terrain.terrainData.SetHeights(0, 0, heights);

            heights = ConverterFileToHeightmap(filePath);

            float[,] zoomedMap = ZoomIn(heights, scale, terrain);

            (float min, float max) = GetMinMax(zoomedMap);
            minHeight = min;
            maxHeight = max;

            // rectangularHexGrid = CreateRectangularGrid(subZone.hexagonTileCells);

            // float[,] resized = Resize(heights, terrain);

            // resized = ScaleHeights(resized, scale);

            // resized = BilinearInterpolation(resized, scale);

            terrain.terrainData.SetHeights(0, 0, zoomedMap);
            // terrain.terrainData.SetHeights(0, 0, heights);
        }
    }


    public static Sprite ScaleImage(string filePath, float scale)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(fileData);

        // Scale the texture
        int width = (int)(texture.width * scale);
        int height = (int)(texture.height * scale);
        Texture2D scaledTexture = new Texture2D(width, height);
        scaledTexture.Resize(width, height);
        scaledTexture.SetPixels(texture.GetPixels());
        scaledTexture.Apply();

        // Create a new sprite from the scaled texture
        Rect rect = new Rect(0, 0, scaledTexture.width, scaledTexture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite newSprite = Sprite.Create(scaledTexture, rect, pivot);
        return newSprite;
    }


    public static Sprite CreateSpriteFromTexture(Texture2D originalTexture, Rect rect)
    {
        int width = (int)Mathf.Min(rect.width, originalTexture.width);
        int height = (int)Mathf.Min(rect.height, originalTexture.height);
        Texture2D newTexture = new Texture2D(width, height);
        Color[] pixels = originalTexture.GetPixels((int)rect.x, (int)rect.y, width, height);
        newTexture.SetPixels(pixels);
        newTexture.Apply();
        return Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.zero);
    }


    public static float[,] ConvertSpriteToHeightmap(Sprite sprite)
    {
        Texture2D texture = sprite.texture;

        // check the dimensions of the sprite and the texture
        if (sprite.rect.width > texture.width || sprite.rect.height > texture.height)
        {
            Debug.LogError("Error: Sprite dimensions are larger than texture dimensions.");
            return null;
        }

        float[,] heights = new float[texture.width, texture.height];

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                Color pixel = texture.GetPixel(x, y);
                heights[x, y] = pixel.grayscale;
            }
        }
        return heights;
    }


    public static float[,] ConverterFileToHeightmap(string filePath)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D heightmap = new Texture2D(1, 1);
            heightmap.LoadImage(fileData);

            float[,] heights = new float[heightmap.width, heightmap.height];
            for (int x = 0; x < heightmap.width; x++)
            {
                for (int y = 0; y < heightmap.height; y++)
                {
                    Color pixel = heightmap.GetPixel(x, y);
                    heights[x, y] = pixel.grayscale;
                }
            }
            return heights;
        }
        catch (FileNotFoundException ex)
        {
            Debug.LogError("File not found: " + ex.Message);
            return null;
        }
    }

    public static float[,] Resize(float[,] array, Terrain terrain)
    {
        int newWidth = terrain.terrainData.heightmapWidth;
        int newHeight = terrain.terrainData.heightmapHeight;

        float[,] newArray = new float[newWidth, newHeight];
        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                float xRatio = (float)x / newWidth;
                float yRatio = (float)y / newHeight;
                int oldX = Mathf.RoundToInt(xRatio * array.GetLength(0));
                int oldY = Mathf.RoundToInt(yRatio * array.GetLength(1));
                newArray[x, y] = array[oldX, oldY];
            }
        }
        return newArray;
    }

    public static float[,] ScaleHeights(float[,] heights, float scale)
    {
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] *= scale;
            }
        }
        return heights;
    }


    public static float[,] ZoomIn(float[,] heightmap, float scale, Terrain terrain)
    {
        int rows = heightmap.GetLength(0);
        int cols = heightmap.GetLength(1);
        int terrain_rows = terrain.terrainData.heightmapResolution;
        int terrain_cols = terrain.terrainData.heightmapResolution;
        float[,] zoomedMap = new float[terrain_rows, terrain_cols];
        for (int i = 0; i < zoomedMap.GetLength(0); i++)
        {
            for (int j = 0; j < zoomedMap.GetLength(1); j++)
            {
                int x = (int)(i / scale);
                int y = (int)(j / scale);
                if (x >= rows || y >= cols)
                {
                    continue; // skip out of bounds values
                }
                zoomedMap[i, j] = heightmap[x, y];
            }
        }
        return zoomedMap;
    }


    public static (float, float) GetMinMax(float[,] heightmap)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < heightmap.GetLength(0); i++)
        {
            for (int j = 0; j < heightmap.GetLength(1); j++)
            {
                float value = heightmap[i, j];
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
        }
        return (min, max);
    }

}
