using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap, Mesh, FallOffMap}
    public DrawMode drawMode;

    public NoiseData noiseData;//get reference of noise and terrain data
    public TerrainData terrainData;
    public TextureData textureData;
    public Material terrainMaterial;

    public int mapChunkSize = 239;
    [Range(0,6)]
    public int previewLevelOfDetail;

    public bool autoUpdate;
    
    public float[,] fallOffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

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
        MapData mapData = generateMapData(Vector2.zero);
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.noiseMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(mapData.heightMap);
            mapDisplay.drawTexture(texture);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            mapDisplay.drawMesh(MeshGenerator.generateTerrainMesh(mapData.heightMap, terrainData.meshHeight, terrainData.meshHeightCurve, previewLevelOfDetail));
        }
        else if(drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(fallOffMap);
            mapDisplay.drawTexture(texture);
        }
    }

    //fetch noise map to noise class
    public void requestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            mapDataThread(centre, callback);
        };
        new Thread(threadStart).Start();
    }

    void mapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = generateMapData(centre);
        lock (mapDataThreadInfoQueue)
        {//when one thread reaches this point, no other queue can execute at the same time
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void requestMeshData(Action<MeshData> callback,int lod, MapData mapData)
    {
        ThreadStart threadStart = delegate
        {
            meshDataThread(callback,lod,mapData);
        };
        new Thread(threadStart).Start();
    }

    void meshDataThread(Action<MeshData> callback,int lod, MapData mapData)
    {
        MeshData meshData = MeshGenerator.generateTerrainMesh(mapData.heightMap, terrainData.meshHeight, terrainData.meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
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

    MapData generateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize+2, mapChunkSize+2, noiseData.seed, noiseData.noiseScale, noiseData.octave, noiseData.persistent, noiseData.lacunarity, centre + noiseData.offSet, noiseData.normalizeMode);
        if (terrainData.useFallOff)
        {
            if(fallOffMap == null)
            {
                fallOffMap = FallOffGenerator.fallOffMap(mapChunkSize+2);
            }
            for (int y = 0; y < mapChunkSize+2; y++)
            {
                for (int x = 0; x < mapChunkSize+2; x++)
                {
                    if (terrainData.useFallOff)
                    {
                        noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - fallOffMap[x, y], 0, 1);
                    }
                    float currentHeight = noiseMap[x, y];

                }
            }
        }

        return new MapData(noiseMap);
    }

    private void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValueUpdated -= OnValueUpdated;//unsubscribe to the OnValueUpdated object **this is to avoid the OnValueUpdated method subscribe to too many things
            terrainData.OnValueUpdated += OnValueUpdated;//subscribe to the OnValueUpdated object
            //the other way is to use the invocation list to check the event is in the list
        }
        if (noiseData != null)
        {
            noiseData.OnValueUpdated -= OnValueUpdated;
            noiseData.OnValueUpdated += OnValueUpdated;
        }
        if(textureData != null)
        {
            textureData.OnValueUpdated -= OnTextureValuesUpdated;
            textureData.OnValueUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T>
    {//hold mapdata variable and callback variable
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}


public struct MapData
{
    public float[,] heightMap;

    public MapData(float[,] hMap)
    {
        this.heightMap = hMap;

    }
}

