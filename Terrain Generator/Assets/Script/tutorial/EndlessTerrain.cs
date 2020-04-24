using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 5f;
    const float viewerUpdateThresholdDst = 25f;//set a threshold update distance so the map does noy have to update per frame, but instead update when player moves 
    const float sqrViewerUpdateThresholdDst = viewerUpdateThresholdDst * viewerUpdateThresholdDst;
    public LODInfo[] detailLevels;
    public static float MaxViewDest;
    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewPosition;
    private Vector2 viewPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainBlock> terrainBlockDictionary = new Dictionary<Vector2, TerrainBlock>();
    static List<TerrainBlock> terrainBlocksVisibleListUpdate = new List<TerrainBlock>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        MaxViewDest = detailLevels[detailLevels.Length-1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(MaxViewDest / chunkSize);
        viewPositionOld = viewPosition;//get the beginning posiiton of the player move
        UpdateVisibleChunks();//get the first one to be drawn
    }

    private void Update()
    {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        if((viewPositionOld - viewPosition).sqrMagnitude > sqrViewerUpdateThresholdDst) {
            viewPositionOld = viewPosition;//update the previous position for the next calculation
            UpdateVisibleChunks();
        }
        
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainBlocksVisibleListUpdate.Count; i++)
        {
            terrainBlocksVisibleListUpdate[i].SetVisible(false);
        }

        terrainBlocksVisibleListUpdate.Clear();
        int currentCoorX = Mathf.RoundToInt(viewPosition.x / chunkSize);
        int currentCoorY = Mathf.RoundToInt(viewPosition.y / chunkSize);

        for (int yOffSet = -chunkVisibleInViewDst; yOffSet <= chunkVisibleInViewDst; yOffSet++)
        {
            for (int xOffSet = -chunkVisibleInViewDst; xOffSet <= chunkVisibleInViewDst; xOffSet++)
            {
                Vector2 viewedChunkCoor = new Vector2(currentCoorX + xOffSet, currentCoorY + yOffSet);
                if (terrainBlockDictionary.ContainsKey(viewedChunkCoor))
                {
                    //make it visible
                    terrainBlockDictionary[viewedChunkCoor].UpdateTerrainChunk();

                }
                else
                {
                    terrainBlockDictionary.Add(viewedChunkCoor, new TerrainBlock(viewedChunkCoor, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainBlock
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;//this is for collider
        LODInfo[] detaillevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainBlock(Vector2 coord, int size, LODInfo[] detaillevels, Transform parent, Material material)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y) * scale;
            this.detaillevels = detaillevels;
            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject = new GameObject("Terrain Block");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * scale;
            meshRenderer.material = material;

            //meshObject.transform.localScale = Vector3.one * size/10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
            lodMeshes = new LODMesh[detaillevels.Length];

            for(int i =0; i< lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detaillevels[i].lod, UpdateTerrainChunk);
                if (detaillevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.requestMapData(position, OnMapDataRecieved);
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            //meshFilter.mesh = meshData.createMesh();
        }

        void OnMapDataRecieved(MapData mapData) {
            //print("map data recieved!!");
            //mapGenerator.requestMeshData(OnMeshDataRecieved, mapData);
            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();

            Texture2D texture = TextureGenerator.textureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerNearestViewDst = Mathf.Sqrt(bounds.SqrDistance(viewPosition));
                bool isVisible = viewerNearestViewDst <= MaxViewDest;
                if (isVisible)
                {
                    int indLevel = 0;
                    for (int i = 0; i < detaillevels.Length - 1; i++)
                    {
                        if (viewerNearestViewDst > detaillevels[i].visibleDstThreshold)
                        {
                            indLevel = i+1;
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
                            lodMesh.requestMesh(mapData);
                        }
                    }

                    if(indLevel == 0)
                    {
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        else if (!collisionLODMesh.hasRequestedMesh)
                        {
                            collisionLODMesh.requestMesh(mapData);
                        }
                    }
                    terrainBlocksVisibleListUpdate.Add(this);
                }
                SetVisible(isVisible);
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

    class LODMesh{
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action UpdateCallBack;

        public LODMesh(int lod, System.Action updateCallBack)
        {
            this.lod = lod;
            this.UpdateCallBack = updateCallBack;
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.createMesh();
            hasMesh = true;

            UpdateCallBack();
        }

        public void requestMesh (MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.requestMeshData(OnMeshDataRecieved, lod, mapData);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        [Range(0,6)]
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }
}
