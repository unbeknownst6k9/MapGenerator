using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise 
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistent, float lacunarity, Vector2 offSet)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffSets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offSetX = prng.Next(-10000,10000) + offSet.x;
            float offSetY = prng.Next(-10000, 10000) + offSet.y;
            octaveOffSets[i] = new Vector2(offSetX, offSetY);
        }

        if(scale <= 0)
        {
            scale = 0.0001f;
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
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffSets[i].x * frequency;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffSets[i].y * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)* 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    //Debug.Log("height is "+noiseHeight);
                    amplitude *= persistent;
                    frequency *= lacunarity;
                }
                
                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }else if(noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {//inverselerp will return a value from 1 to 0 base on max and min given in the first 2 parameters 
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x,y]);
            }
        }
        return noiseMap;
    }
}
