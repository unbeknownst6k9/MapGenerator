using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    
    const float viewerUpdateThresholdDst = 25f;//set a threshold update distance so the map does noy have to update per frame, but instead update when player moves 
    const float sqrViewerUpdateThresholdDst = viewerUpdateThresholdDst * viewerUpdateThresholdDst;
    

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    //public static float MaxViewDest;

    public MeshSettings meshSettings;
    public HeightMapSetting heightSettings;
    public TextureData textureSettings;

    public Transform viewer;
    public Material mapMaterial;
    private Vector2 viewPosition;
    private Vector2 viewPositionOld;
    static MapGenerator mapGenerator;
    float meshWorldSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainBlock> terrainBlockDictionary = new Dictionary<Vector2, TerrainBlock>();
    List<TerrainBlock> terrainBlocksVisibleListUpdate = new List<TerrainBlock>();

    void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeight(mapMaterial, heightSettings.minHeight, heightSettings.maxHeight);
        //mapGenerator = FindObjectOfType<MapGenerator>();
        float MaxViewDest = detailLevels[detailLevels.Length-1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(MaxViewDest / meshWorldSize);
        viewPositionOld = viewPosition;//get the beginning posiiton of the player move
        UpdateVisibleChunks();//get the first one to be drawn
    }

    private void Update()
    {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z);
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
        int currentCoorX = Mathf.RoundToInt(viewPosition.x / meshWorldSize);
        int currentCoorY = Mathf.RoundToInt(viewPosition.y / meshWorldSize);

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
                        TerrainBlock newBlock = new TerrainBlock(viewedChunkCoor, heightSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainBlockDictionary.Add(viewedChunkCoor, newBlock);
                        newBlock.OnVisibilityChanged += OnTerrainBlockVisibilityChange;
                        newBlock.Load();
                    }
                }
            }
        }
    }
    void OnTerrainBlockVisibilityChange(TerrainBlock terrainBlock, bool isVisible)
    {
        if (isVisible)
        {
            terrainBlocksVisibleListUpdate.Add(terrainBlock);
        }
        else
        {
            terrainBlocksVisibleListUpdate.Remove(terrainBlock);
        }
    }
}
[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numOfSupportedLOD - 1)]
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
