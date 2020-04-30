using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D textureFromColorMap(Color[] colorMap, int mapWidth, int mapHeight) {
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D textureHeightMap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength(0);//get the first line of the array
        int height = heightMap.values.GetLength(1);
        
        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {//set color between black and white using the float in the noiseMap
                float noiseSample = Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]);
                colorMap[x + y * width] = Color.Lerp(Color.black, Color.white, noiseSample);
                //
                //colorMap[x + y * width] = new Color(noiseMap[x, y] * 0.7f, noiseMap[x, y] * 0.9f, noiseMap[x, y]);
            }
        }

        return textureFromColorMap(colorMap, width, height);
    }
}
