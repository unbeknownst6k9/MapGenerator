﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSetting settings, Vector2 sampleCentre)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre, settings.useFallOff);
        AnimationCurve curve_threadSafe = new AnimationCurve(settings.heightCurve.keys);

        float minValue = int.MaxValue;
        float maxValue = int.MinValue;
        for(int i = 0; i < width; i++)
        {
            for(int j =0; j< height; j++)
            {
                values[i,j] = values[i, j] * curve_threadSafe.Evaluate(values[i, j]) * settings.heightMultiplier;
                
                if(values[i,j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if(values[i,j] < minValue)
                {
                    minValue = values[i, j];
                }
            }
        }
        
        return new HeightMap(values, minValue, maxValue);
    }
}

public struct HeightMap
{
    public float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}