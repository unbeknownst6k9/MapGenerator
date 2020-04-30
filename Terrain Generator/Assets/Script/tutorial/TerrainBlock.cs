using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;

public class TerrainBlock
{
    const float colliderDstThreshold = 5;
    public event System.Action<TerrainBlock, bool> OnVisibilityChanged;
    public Vector2 coord;
    GameObject meshObject;
    Vector2 sampleCentre;
    Bounds bounds;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;//this is for collider
    LODInfo[] detaillevels;
    LODMesh[] lodMeshes;
    int colliderLODInd;
    HeightMap heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDst;

    HeightMapSetting heightMapSettings;
    MeshSettings meshSettings;
    Transform viewer;

    public TerrainBlock(Vector2 coord, HeightMapSetting heightMapSetting, MeshSettings meshSettings, LODInfo[] detaillevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.detaillevels = detaillevels;
        heightMapSettings = heightMapSetting;
        this.meshSettings = meshSettings;
        this.coord = coord;
        this.viewer = viewer;

        colliderLODInd = colliderLODIndex;
        sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        meshObject = new GameObject("Terrain Block");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshRenderer.material = material;

        meshObject.transform.parent = parent;
        SetVisible(false);
        lodMeshes = new LODMesh[detaillevels.Length];

        for (int i = 0; i < lodMeshes.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detaillevels[i].lod);
            lodMeshes[i].UpdateCallBack += UpdateTerrainChunk;
            if (i == colliderLODInd)
            {
                lodMeshes[i].UpdateCallBack += updateCollisionMesh;
            }
        }
        maxViewDst = detaillevels[detaillevels.Length - 1].visibleDstThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.verticesNumPerLine, meshSettings.verticesNumPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
    }

    //void OnMeshDataRecieved(MeshData meshData)
    //{
    //meshFilter.mesh = meshData.createMesh();
    //}

    void OnHeightMapReceived(object heightMapObject)
    {
        //print("map data recieved!!");
        //mapGenerator.requestMeshData(OnMeshDataRecieved, mapData);
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;
        UpdateTerrainChunk();

    }

    Vector2 viewerPosition
    {
        get{
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (heightMapReceived)
        {
            float viewerNearestViewDst = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool wasVisible = IsVisible();
            bool isVisible = viewerNearestViewDst <= maxViewDst;
            if (isVisible)
            {
                int indLevel = 0;
                for (int i = 0; i < detaillevels.Length - 1; i++)
                {
                    if (viewerNearestViewDst > detaillevels[i].visibleDstThreshold)
                    {
                        indLevel = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                if (indLevel != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[indLevel];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = indLevel;
                        meshFilter.mesh = lodMesh.mesh;
                        //meshCollider.sharedMesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.requestMesh(heightMap, meshSettings);

                    }
                }
            }
            if (wasVisible != isVisible)
            {
                
                /*
                if (isVisible)
                {
                    terrainBlocksVisibleListUpdate.Add(this);
                }
                else
                {
                    terrainBlocksVisibleListUpdate.Remove(this);
                }*/
                SetVisible(isVisible);
                //if (OnVisibilityChanged != null)
                //{
                OnVisibilityChanged?.Invoke(this, isVisible);
                //}
            }
        }
    }

    public void updateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float squareDstfromEdge = bounds.SqrDistance(viewerPosition);
            if (squareDstfromEdge < detaillevels[colliderLODInd].squrVisibleDstThreshold)
            {
                if (!lodMeshes[colliderLODInd].hasRequestedMesh)
                {
                    lodMeshes[colliderLODInd].requestMesh(heightMap, meshSettings);
                }
            }

            if (squareDstfromEdge < colliderDstThreshold * colliderDstThreshold)
            {
                if (lodMeshes[colliderLODInd].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODInd].mesh;
                    hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action UpdateCallBack;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataRecieved(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).createMesh();
        hasMesh = true;

        UpdateCallBack();
    }

    public void requestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataRecieved);
    }
}
