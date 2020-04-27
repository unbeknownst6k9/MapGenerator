using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData generateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int levelDetail)
    {
        AnimationCurve reference = new AnimationCurve(meshHeightCurve.keys);
        int simplificationIncrement = (levelDetail == 0 ? 1 : levelDetail * 2);
        int borderedSize = heightMap.GetLength(0);
        //int borderedSize = heightMap.GetLength(1);
        int meshSize = borderedSize - 2 * simplificationIncrement;
        int meshSizeUnsimplifies = borderedSize - 2;
        
        int verticesPerLine = (meshSize - 1) / simplificationIncrement + 1;
        
        /*these two variables help the map spawns in the middle*/
        float topLeftX = (meshSizeUnsimplifies - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplifies - 1) / 2f;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;
        //int vertexIndex = 0;

        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize-1 || x ==0 || x == borderedSize-1;

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y<borderedSize; y+= simplificationIncrement)
        {
            for (int x = 0; x<borderedSize; x+= simplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector2 percent = new Vector2((x - simplificationIncrement) / (float)meshSize, (y - simplificationIncrement) / (float)meshSize);
                float height = reference.Evaluate(heightMap[x, y]) * heightMultiplier;//this defines the height of all vertices
                Vector3 vertexPosition = new Vector3((topLeftX + percent.x * meshSizeUnsimplifies), height, (topLeftZ - percent.y * meshSizeUnsimplifies));

                meshData.addVertex(vertexPosition, percent, vertexIndex);
                if(x < borderedSize-1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + simplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + simplificationIncrement];
                    int d = vertexIndicesMap[x+simplificationIncrement, y+simplificationIncrement];

                    meshData.addTriangles(a,d,c);
                    meshData.addTriangles(d,a,b);
                }
;               //vertexIndex++;
            }
        }
        meshData.BakeNormals();
        //return meshData for multi-threading so the game doesn't freeze when it loads
        //**we can't create Mesh inside the thread, so you have to do it outside the threading method
        return meshData;
    }
    
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] UVS;
    Vector3[] borderVertices;
    Vector3[] bakedNormals;
    int[] borderTriangles;

    int triangleIndex = 0;
    int borderTriangleIndex;

    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        UVS = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void addVertex(Vector3 vertexPosition, Vector2 uvs, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            UVS[vertexIndex] = uvs;
        }
    }


    public void addTriangles(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        //this is because the triangles[].length is the number of vertices by dividing 3 we get the number of triangles
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++)
        {//this is the index for the triangles[]
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex+1];
            int vertexIndexC = triangles[normalTriangleIndex+2];

            Vector3 vertexIndexNormal = surfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += vertexIndexNormal;
            vertexNormals[vertexIndexB] += vertexIndexNormal;
            vertexNormals[vertexIndexC] += vertexIndexNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {//this is the index for the triangles[]
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];
            /*fix: by getting rid of the normals for the borders the shading line between each mesh disappear
             this is because the border triangles are not supposed to be seen, hence, neither should they have normal shade.
             this fix is temporary for the tutorial ep12*/
            
            //Vector3 vertexIndexNormal = surfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                //vertexNormals[vertexIndexA] += vertexIndexNormal;
            }
            if (vertexIndexB >= 0)
            {
                //vertexNormals[vertexIndexB] += vertexIndexNormal;
            }
            if (vertexIndexC >= 0)
            {
                //vertexNormals[vertexIndexC] += vertexIndexNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }
    //get the normal vector of the triangle
    Vector3 surfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? vertices[-indexA-1]:vertices[indexA];
        Vector3 pointB = (indexB < 0) ? vertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? vertices[-indexC - 1] : vertices[indexC];

        Vector3 vectorAB = pointB - pointA;
        Vector3 vectorAC = pointC - pointA;
        return Vector3.Cross(vectorAB, vectorAC).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public Mesh createMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVS;
        mesh.normals = bakedNormals;//this is to adjust the lighting

        return mesh;
    }
}
