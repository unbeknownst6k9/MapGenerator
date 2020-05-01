using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{

    public static MeshData generateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelDetail)
    {
        int skipIncrement = (levelDetail == 0 ? 1 : levelDetail * 2);
        int numVertsPerLine = meshSettings.verticesNumPerLine;
        //int borderedSize = heightMap.GetLength(0);
        //int borderedSize = heightMap.GetLength(1);
        //int meshSize = borderedSize - 2 * skipIncrement;
        //int meshSizeUnsimplifies = borderedSize - 2;
        
        //int verticesPerLine = (meshSize - 1) / skipIncrement + 1;

        /*these two variables help the map spawns in the middle*/
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement);

        int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;
        //int vertexIndex = 0;

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMesh = y == 0 || y == numVertsPerLine - 1 || x ==0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine-3 && ((x-2)% skipIncrement !=0||(y-2)%skipIncrement!=0);
                //for the last &&, if either of those is not evenly divided by the skipped increment then it has to be the main vertex because 
                if (isOutOfMesh)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if(!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y< numVertsPerLine; y++)
        {
            for (int x = 0; x< numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (!isSkippedVertex)
                {
                    bool isOutOfMesh = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1||x == numVertsPerLine - 2) && !isOutOfMesh;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y-2)%skipIncrement==0 && !isOutOfMesh&& !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMesh && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];
                    Vector2 percent = new Vector2((x - 1), (y - 1)) / (numVertsPerLine - 3);
                    float height = heightMap[x, y];//this defines the height of all vertices

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentAToB = dstToMainVertexA / (float)skipIncrement;
                        try
                        {
                            float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y : y - dstToMainVertexA];
                            float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y : y + dstToMainVertexB];
                            height = heightMainVertexA * (1 - dstPercentAToB) + heightMainVertexB * dstPercentAToB;
                        }
                        catch
                        {
                            //Debug.Log("out of range " + "x is "+ x + "y is "+ y);
                            /*the x and y will go over the boundry when passed into this method. reason: unknown*/
                        }

                        
                    }
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x,-percent.y) * meshSettings.meshWorldSize;
                    meshData.addVertex(new Vector3(vertexPosition2D.x,height, vertexPosition2D.y), percent, vertexIndex);
                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;//this should be change from 1 to LOD related number
                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.addTriangles(a, d, c);
                        meshData.addTriangles(d, a, b);
                    }
                }
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
    Vector3[] outOfMeshVertices;
    Vector3[] bakedNormals;
    int[] outOfMeshTriangles;

    int triangleIndex = 0;
    int outOfMeshTriangleIndex;

    public MeshData(int verticesPerLine, int skipIncrement)
    {//size of vertiecs array
        int numMeshEdgeVertices = (verticesPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (verticesPerLine - 5) / skipIncrement * 4;
        int mainVerticesPerLine = (verticesPerLine - 5) / skipIncrement + 1;
        int mainVertices = mainVerticesPerLine * mainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + mainVertices];
        UVS = new Vector2[vertices.Length];

        //size of triangle array
        int numMeshEdgeTriangles = ((verticesPerLine - 3) * 4 - 4)*2;
        int numMainTriangles = (verticesPerLine - 1) * (verticesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVertices = new Vector3[verticesPerLine * 4 - 4];
        outOfMeshTriangles = new int[((verticesPerLine-1)*4 - 4)*6];
    }

    public void addVertex(Vector3 vertexPosition, Vector2 uvs, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
            outOfMeshTriangles[outOfMeshTriangleIndex] = a;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
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

        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {//this is the index for the triangles[]
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];
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
