using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap, colormap, Mesh, FallOffMap}
    public Noise.NormalizeMode normalizeMode;
    public DrawMode drawMode;
    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int previewLevelOfDetail;
    //public int mapChunkSize;
    //public int mapChunkSize;
    public float noiseScale;

    public int octave;
    [Range(0,1)]
    public float persistent;
    public float lacunarity;

    public int seed;
    public Vector2 offSet;
    public bool autoUpdate;

    public bool useFallOff;

    public float meshHeight;
    public AnimationCurve meshHeightCurve;
    public float[,] fallOffMap;

    public TerrainType[] regions;
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        fallOffMap = FallOffGenerator.fallOffMap(mapChunkSize);
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
        else if (drawMode == DrawMode.colormap)
        {
            Texture2D texture = TextureGenerator.textureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize);
            mapDisplay.drawTexture(texture);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            Texture2D texture = TextureGenerator.textureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize);
            mapDisplay.drawMesh(MeshGenerator.generateTerrainMesh(mapData.heightMap, meshHeight, meshHeightCurve, previewLevelOfDetail), texture);
        }
        else if(drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.textureHeightMap(fallOffMap);
            mapDisplay.drawTexture(texture);
            //mapDisplay.drawMesh(MeshGenerator.generateTerrainMesh())
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
        MeshData meshData = MeshGenerator.generateTerrainMesh(mapData.heightMap, meshHeight, meshHeightCurve, lod);
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
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize+2, mapChunkSize+2, seed, noiseScale, octave, persistent, lacunarity, centre + offSet, normalizeMode);

        Color[] colorMap = new Color[noiseMap.Length];
        for (int y = 0; y < mapChunkSize; y++) {
            for(int x = 0; x < mapChunkSize; x++)
            {
                if (useFallOff)
                {
                    noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - fallOffMap[x, y], 0, 1);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    private void OnValidate()
    {/*
        if (mapChunkSize < 1)
        {
            //mapChunkSize = 1;
        }
        if (mapChunkSize < 1)
        {
            //mapChunkSize = 1;
        }*/
        if(octave < 1)
        {
            octave = 1;
        }
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }

        fallOffMap = FallOffGenerator.fallOffMap(mapChunkSize);
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
[System.Serializable]
public struct TerrainType {
    public string name;
    [Range(0,1)]
    public float height;
    public Color color;

}

public struct MapData
{
    public float[,] heightMap;
    public Color[] colorMap;

    public MapData(float[,] hMap, Color[] CMap)
    {
        this.heightMap = hMap;
        this.colorMap = CMap;
    }
}

