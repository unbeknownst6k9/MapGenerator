using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FallOffGenerator
{
    public static float[,] fallOffMap(int size) {
        float[,] map = new float[size,size];

        for(int y = 0; y < size; y++)
        {
            for(int x =0; x<size; x++)
            {
                float height = y / (float)size * 2f -1f;
                float width = x / (float)size * 2f - 1f;

                float value = Mathf.Max(Mathf.Abs(height), Mathf.Abs(width));
                map[x, y] = Evaluate(value);
            }
        }
        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow((b - b * value), a));
    }
}
