using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise 
{
    public enum NormalizeMode {Local, Global};
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSetting settings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffSets = new Vector2[settings.octave];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        for (int i = 0; i < settings.octave; i++)
        {
            float offSetX = prng.Next(-10000,10000) + settings.offSet.x + sampleCentre.x;
            float offSetY = prng.Next(-10000, 10000) - settings.offSet.y - sampleCentre.y;
            octaveOffSets[i] = new Vector2(offSetX, offSetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistent;
        }

        /*do not use float.max or float.min because it will give a very large number and 
         divide it will give almost 0 to any number
         */
        float minNoiseHeight = 0;
        float maxNoiseHeight = 0;

        /*zoom in to the center*/
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for(int y= 0; y<mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < settings.octave; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffSets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffSets[i].y) / settings.scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)* 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    //Debug.Log("height is "+noiseHeight);
                    amplitude *= settings.persistent;
                    frequency *= settings.lacunarity;
                }

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    noiseHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight/0.9f);
                    noiseMap[x, y] = Mathf.Clamp(noiseHeight, 0, int.MaxValue);
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
            }
        }

        settings.minNoiseHeight = minNoiseHeight;
        settings.maxNoiseHeight = maxNoiseHeight;
        if (settings.normalizeMode == NormalizeMode.Local) {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {//inverselerp will return a value from 1 to 0 base on max and min given in the first 2 parameters 
                     noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSetting
{
    public Noise.NormalizeMode normalizeMode;
    public float scale = 60f;

    public int octave = 4;
    [Range(0, 1)]
    public float persistent = 0.3f;
    public float lacunarity = 5f;

    public int seed;
    public Vector2 offSet;

    [HideInInspector]
    public float minNoiseHeight;
    [HideInInspector]
    public float maxNoiseHeight;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.001f);//this cannot reach 0
        octave = Mathf.Max(octave, 1);
        lacunarity = Mathf.Max(lacunarity, 1f);
        persistent = Mathf.Clamp01(persistent);
    }
}