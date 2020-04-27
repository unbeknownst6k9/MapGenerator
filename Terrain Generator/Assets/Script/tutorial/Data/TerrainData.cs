using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public float uniformScale = 5f;
    public bool useFallOff;

    public float meshHeight;
    public AnimationCurve meshHeightCurve;

    public float minHeight
    {
        get
        {
            return uniformScale * meshHeight * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return uniformScale * meshHeight * meshHeightCurve.Evaluate(1);
        }
    }
}
