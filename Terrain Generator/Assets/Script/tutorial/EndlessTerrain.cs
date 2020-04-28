using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    
    const float viewerUpdateThresholdDst = 25f;//set a threshold update distance so the map does noy have to update per frame, but instead update when player moves 
    const float sqrViewerUpdateThresholdDst = viewerUpdateThresholdDst * viewerUpdateThresholdDst;
    const float colliderDstThreshold = 5;

    public int colliderLODIndex;
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
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(MaxViewDest / chunkSize);
        viewPositionOld = viewPosition;//get the beginning posiiton of the player move
        UpdateVisibleChunks();//get the first one to be drawn
    }

    private void Update()
    {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
        if(viewPosition != viewPositionOld)
        {
            foreach(TerrainBlock block in terrainBlocksVisibleListUpdate)
            {
                block.updateCollisionMesh();
            }
        }
        if((viewPositionOld - viewPosition).sqrMagnitude > sqrViewerUpdateThresholdDst) {
            viewPositionOld = viewPosition;//update the previous position for the next calculation
            UpdateVisibleChunks();
        }
        
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> updatedBlocksCoord = new HashSet<Vector2>();
        for (int i = terrainBlocksVisibleListUpdate.Count-1; i >= 0; i--)
        {
            updatedBlocksCoord.Add(terrainBlocksVisibleListUpdate[i].coord);
            terrainBlocksVisibleListUpdate[i].UpdateTerrainChunk();
        }

        //terrainBlocksVisibleListUpdate.Clear();
        int currentCoorX = Mathf.RoundToInt(viewPosition.x / chunkSize);
        int currentCoorY = Mathf.RoundToInt(viewPosition.y / chunkSize);

        for (int yOffSet = -chunkVisibleInViewDst; yOffSet <= chunkVisibleInViewDst; yOffSet++)
        {
            for (int xOffSet = -chunkVisibleInViewDst; xOffSet <= chunkVisibleInViewDst; xOffSet++)
            {
                Vector2 viewedChunkCoor = new Vector2(currentCoorX + xOffSet, currentCoorY + yOffSet);
                if (!updatedBlocksCoord.Contains(viewedChunkCoor))
                {//if the updated the block coord is not in the array then update it into the array
                    if (terrainBlockDictionary.ContainsKey(viewedChunkCoor))
                    {
                        //make it visible
                        terrainBlockDictionary[viewedChunkCoor].UpdateTerrainChunk();

                    }
                    else
                    {
                        terrainBlockDictionary.Add(viewedChunkCoor, new TerrainBlock(viewedChunkCoor, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }

    public class TerrainBlock
    {
        public Vector2 coord;
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;//this is for collider
        LODInfo[] detaillevels;
        LODMesh[] lodMeshes;
        int colliderLODInd;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        bool hasSetCollider;

        public TerrainBlock(Vector2 coord, int size, LODInfo[] detaillevels, int colliderLODIndex, Transform parent, Material material)
        {
            this.coord = coord;
            colliderLODInd = colliderLODIndex;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y) * mapGenerator.terrainData.uniformScale;
            this.detaillevels = detaillevels;
            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject = new GameObject("Terrain Block");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            meshRenderer.material = material;

            //meshObject.transform.localScale = Vector3.one * size/10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
            lodMeshes = new LODMesh[detaillevels.Length];

            for(int i =0; i< lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detaillevels[i].lod);
                lodMeshes[i].UpdateCallBack += UpdateTerrainChunk;
                if (i == colliderLODInd)
                {
                    lodMeshes[i].UpdateCallBack += updateCollisionMesh;
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

        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerNearestViewDst = Mathf.Sqrt(bounds.SqrDistance(viewPosition));
                bool wasVisible = IsVisible();
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

                    
                    
                }

                if(wasVisible != isVisible)
                {
                    if (isVisible)
                    {
                        terrainBlocksVisibleListUpdate.Add(this);
                    }
                    else
                    {
                        terrainBlocksVisibleListUpdate.Remove(this);
                    }
                    SetVisible(isVisible);
                }
            }
        }

        public void updateCollisionMesh()
        {
            if (!hasSetCollider)
            {
                float squareDstfromEdge = bounds.SqrDistance(viewPosition);
                if (squareDstfromEdge < detaillevels[colliderLODInd].squrVisibleDstThreshold)
                {
                    if (!lodMeshes[colliderLODInd].hasRequestedMesh)
                    {
                        lodMeshes[colliderLODInd].requestMesh(mapData);
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

    class LODMesh{
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action UpdateCallBack;

        public LODMesh(int lod)
        {
            this.lod = lod;
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
        [Range(0,MeshGenerator.numOfSupportedLOD-1)]
        public int lod;
        public float visibleDstThreshold;
        //public bool useForCollider;

        public float squrVisibleDstThreshold
        {
            get
            {
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }
}
