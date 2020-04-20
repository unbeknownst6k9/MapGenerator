using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData generateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int levelDetail)
    {
        AnimationCurve reference = new AnimationCurve(meshHeightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        int simplificationIncrement = (levelDetail == 0? 1:levelDetail * 2);
        int verticesPerLine = (width - 1) / simplificationIncrement + 1;
        /*these two variables help the map spawns in the middle*/
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y<height; y+= simplificationIncrement)
        {
            for (int x = 0; x<width; x+= simplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3((topLeftX + (float)x), reference.Evaluate(heightMap[x, y]) * heightMultiplier, (topLeftZ - (float)y));
                meshData.UVS[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if(x < width-1 && y < height - 1)
                {
                    meshData.addTriangles(vertexIndex, vertexIndex+ verticesPerLine + 1, vertexIndex+ verticesPerLine);
                    meshData.addTriangles(vertexIndex, vertexIndex + 1, vertexIndex + verticesPerLine + 1);
                }
;               vertexIndex++;
            }
        }
        //return meshData for multi-threading so the game doesn't freeze when it loads
        //**we can't create Mesh inside the thread, so you have to do it outside the threading method
        return meshData;
    }
    
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    private int triangleIndex = 0;
    public Vector2[] UVS;


    public MeshData(int mapWidth, int mapHeight)
    {
        vertices = new Vector3[mapWidth * mapHeight];
        UVS = new Vector2[mapWidth * mapHeight];
        triangles = new int[(mapWidth - 1) * (mapHeight - 1) * 6];
    }

    public void addTriangles(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
        
    }

    public Mesh createMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVS;
        mesh.RecalculateNormals();//this is to adjust the lighting

        return mesh;
    }
}
