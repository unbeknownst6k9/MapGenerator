using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap, Mesh, FallOffMap}
    public DrawMode drawMode;

    public HeightMapSetting heightMapSettings;//get reference of noise and terrain data
    public MeshSettings meshSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.numOfSupportedLOD-1)]
    public int previewLevelOfDetail;

    public bool autoUpdate;
    
    public float[,] fallOffMap;

    Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Start()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
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

    public void DrawMapEditor()
    {
        //value might not be correct if we apply the falloffmap because it might falten down the highest point
        //textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.verticesNumPerLine, meshSettings.verticesNumPerLine, heightMapSettings ,Vector2.zero);
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        textureData.UpdateMeshHeight(terrainMaterial, heightMap.minValue, heightMap.maxValue * meshSettings.meshScale);
        if (drawMode == DrawMode.noiseMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(heightMap.values);
            mapDisplay.drawTexture(texture);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            mapDisplay.drawMesh(MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, previewLevelOfDetail));
            print("mesh hight at max is:"+ heightMap.maxValue);
        }
        else if(drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(FallOffGenerator.fallOffMap(meshSettings.verticesNumPerLine));
            mapDisplay.drawTexture(texture);
        }
        
    }

    //fetch noise map to noise class
    public void requestheightMap(Vector2 centre, Action<HeightMap> callback)
    {
        ThreadStart threadStart = delegate
        {
            heightMapThread(centre, callback);
        };
        new Thread(threadStart).Start();
    }

    void heightMapThread(Vector2 centre, Action<HeightMap> callback)
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.verticesNumPerLine, meshSettings.verticesNumPerLine, heightMapSettings, centre);
        lock (heightMapThreadInfoQueue)
        {//when one thread reaches this point, no other queue can execute at the same time
            heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }

    public void requestMeshData(Action<MeshData> callback,int lod, HeightMap heightMap)
    {
        ThreadStart threadStart = delegate
        {
            meshDataThread(callback,lod,heightMap);
        };
        new Thread(threadStart).Start();
    }

    void meshDataThread(Action<MeshData> callback,int lod, HeightMap heightMap)
    {
        MeshData meshData = MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (heightMapThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < heightMapThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
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
        if(textureData != null)
        {
            textureData.OnValueUpdated -= OnTextureValuesUpdated;
            textureData.OnValueUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T>
    {//hold heightMap variable and callback variable
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}




