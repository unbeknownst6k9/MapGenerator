using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode{ noiseMap, colormap, Mesh}
    public DrawMode drawMode;
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octave;
    [Range(0,1)]
    public float persistent;
    public float lacunarity;

    public int seed;
    public Vector2 offSet;
    public bool autoUpdate;
    //fetch noise map to noise class

    public TerrainType[] regions;
    public void generateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octave, persistent, lacunarity, offSet);

        Color[] colorMap = new Color[noiseMap.Length];
        for (int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.noiseMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(noiseMap);
            mapDisplay.drawTexture(texture);
        }else if(drawMode == DrawMode.colormap)
        {
            Texture2D texture = TextureGenerator.textureFromColorMap(colorMap, mapWidth, mapHeight);
            mapDisplay.drawTexture(texture);
        }else if(drawMode == DrawMode.Mesh)
        {
            Texture2D texture = TextureGenerator.textureFromColorMap(colorMap, mapWidth, mapHeight);
            mapDisplay.drawMesh(MeshGenerator.generateTerrainMesh(noiseMap), texture);
        }

    }

    private void OnValidate()
    {
        if (mapWidth < 1)
        {
            mapWidth = 1;
        }
        if (mapHeight < 1)
        {
            mapHeight = 1;
        }
        if(octave < 1)
        {
            octave = 1;
        }
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
    }
}
[System.Serializable]
public struct TerrainType {
    public string name;
    [Range(0,1)]
    public float height;
    public Color color;

}

