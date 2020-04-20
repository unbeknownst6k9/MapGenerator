using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float MaxViewDest = 600;
    public Transform viewer;

    public static Vector2 viewPosition;
    int chunkSize;
    int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainBlock> terrainBlockDictionary = new Dictionary<Vector2, TerrainBlock>();
    List<TerrainBlock> terrainBlocksVisibleListUpdate = new List<TerrainBlock>();
    void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt(MaxViewDest / chunkSize);

    }

    private void Update()
    {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for (int i=0;i< terrainBlocksVisibleListUpdate.Count; i++)
        {
            terrainBlocksVisibleListUpdate[i].SetVisible(false);
        }

        terrainBlocksVisibleListUpdate.Clear();
        int currentCoorX = Mathf.RoundToInt(viewPosition.x / chunkSize);
        int currentCoorY = Mathf.RoundToInt(viewPosition.y / chunkSize);

        for(int yOffSet = -chunkVisibleInViewDst; yOffSet<= chunkVisibleInViewDst; yOffSet++)
        {
            for(int xOffSet = -chunkVisibleInViewDst; xOffSet<= chunkVisibleInViewDst; xOffSet++)
            {
                Vector2 viewedChunkCoor = new Vector2(currentCoorX + xOffSet, currentCoorY + yOffSet);
                if (terrainBlockDictionary.ContainsKey(viewedChunkCoor))
                {
                    //make it visible
                    terrainBlockDictionary[viewedChunkCoor].UpdateTerrainChunk();
                    if (terrainBlockDictionary[viewedChunkCoor].IsVisible())
                    {
                        terrainBlocksVisibleListUpdate.Add(terrainBlockDictionary[viewedChunkCoor]);
                    }
                }
                else
                {
                    terrainBlockDictionary.Add(viewedChunkCoor, new TerrainBlock(viewedChunkCoor, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainBlock
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        
        public TerrainBlock(Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size/10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            float viewerNearestViewDst = Mathf.Sqrt(bounds.SqrDistance(viewPosition));
            bool isVisible = viewerNearestViewDst <= MaxViewDest;
            SetVisible(isVisible);
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
}
