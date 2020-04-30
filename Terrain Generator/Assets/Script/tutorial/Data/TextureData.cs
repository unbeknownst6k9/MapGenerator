using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;//16 bit texture color format

    public Layer[] layers;
    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetFloatArray("baseBlend", layers.Select(x=>x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", layers.Select(x=>x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScale", layers.Select(x=>x.textureScale).ToArray());
        Texture2DArray textureArrayRef = generateTesture2DArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextureArray", textureArrayRef);
        UpdateMeshHeight(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeight(Material material, float  minHeight, float maxHeight)
    {
        //these two lines are just to let the shader keep the height data from resetting it each time there's a change
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        material.SetFloat("minHeight", savedMinHeight);
        material.SetFloat("maxHeight", savedMaxHeight + 10f);
        //Debug.Log("texture height is"+(savedMaxHeight + 10f));//46.9
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColor", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeight", layers.Select(x=> x.startHeight).ToArray());
    }

    Texture2DArray generateTesture2DArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
