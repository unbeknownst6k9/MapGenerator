using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { noiseMap, Mesh, FallOffMap }
    public DrawMode drawMode;

    public HeightMapSetting heightMapSettings;//get reference of noise and terrain data
    public MeshSettings meshSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.numOfSupportedLOD - 1)]
    public int previewLevelOfDetail;

    public bool autoUpdate;


    public void DrawMapEditor()
    {
        //value might not be correct if we apply the falloffmap because it might falten down the highest point
        //textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight * meshSettings.meshScale);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.verticesNumPerLine, meshSettings.verticesNumPerLine, heightMapSettings, Vector2.zero);
        //MapPreview mapDisplay = FindObjectOfType<MapPreview>();
        if (drawMode == DrawMode.noiseMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(heightMap);
            drawTexture(texture);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            drawMesh(MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, previewLevelOfDetail));
            //Debug.Log("mesh height is " + heightMap.maxValue);
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(new HeightMap(FallOffGenerator.fallOffMap(meshSettings.verticesNumPerLine),0,1));
            drawTexture(texture);
        }

    }

    public void drawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1f, texture.height) / 10f;
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void drawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.createMesh();
        //meshFilter.transform.localScale = Vector3.one * meshSettings.meshScale;
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValueUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValueUpdated -= OnValueUpdated;//unsubscribe to the OnValueUpdated object **this is to avoid the OnValueUpdated method subscribe to too many things
            meshSettings.OnValueUpdated += OnValueUpdated;//subscribe to the OnValueUpdated object
            //the other way is to use the invocation list to check the event is in the list
        }
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValueUpdated -= OnValueUpdated;
            heightMapSettings.OnValueUpdated += OnValueUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValueUpdated -= OnTextureValuesUpdated;
            textureData.OnValueUpdated += OnTextureValuesUpdated;
        }
    }

}
