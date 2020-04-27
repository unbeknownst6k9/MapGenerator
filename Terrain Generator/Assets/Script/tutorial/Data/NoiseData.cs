using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;
    public float noiseScale;

    public int octave;
    [Range(0, 1)]
    public float persistent;
    public float lacunarity;

    public int seed;
    public Vector2 offSet;

    protected override void OnValidate()
    {
        if (octave < 1)
        {
            octave = 1;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        base.OnValidate();
    }
}
