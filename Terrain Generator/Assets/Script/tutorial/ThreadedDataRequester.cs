using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour
{
    static ThreadedDataRequester instance;
    Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();
    //Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    private void Awake()
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    //fetch noise map to noise class
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            instance.DataThread(generateData, callback);
        };
        new Thread(threadStart).Start();
    }

    void DataThread(Func<object> generateData, Action<object> callback)
    {
        //HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.verticesNumPerLine, meshSettings.verticesNumPerLine, heightMapSettings, centre);
        object data = generateData();
        //textureData.ApplyToMaterial(terrainMaterial);
        //textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        //Debug.Log("the mesh max height is "+heightMap.maxValue);
        lock (dataQueue)
        {//when one thread reaches this point, no other queue can execute at the same time
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    /*
    public void requestMeshData(Action<MeshData> callback, int lod, HeightMap heightMap)
    {
        ThreadStart threadStart = delegate
        {
            meshDataThread(callback, lod, heightMap);
        };
        new Thread(threadStart).Start();
    }

    void meshDataThread(Action<MeshData> callback, int lod, HeightMap heightMap)
    {
        MeshData meshData = MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }*/

    private void Update()
    {
        if (dataQueue.Count > 0)
        {
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
        /*
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }*/

    struct ThreadInfo
    {//hold heightMap variable and callback variable
        public readonly Action<object> callback;
        public readonly object parameter;
        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
