using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public Color[] baseColor;
    [Range(0,1)]
    public float[] baseStartHeight;
    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        
        UpdateMeshHeight(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeight(Material material, float  minHeight, float maxHeight)
    {
        //these two lines are just to let the shader keep the height data from resetting it each time there's a change
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        material.SetFloat("minHeight", savedMinHeight);
        material.SetFloat("maxHeight", savedMaxHeight);
        material.SetInt("baseColorCount", baseColor.Length);
        material.SetColorArray("baseColor", baseColor);
        material.SetFloatArray("baseStartHeight", baseStartHeight);
    }
}
