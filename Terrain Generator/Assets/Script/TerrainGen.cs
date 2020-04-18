using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    [SerializeField]
    private int xSize;
    [SerializeField]
    private int zSize;
    [SerializeField]
    private float height;
    [Range(0f,1f)]
    [SerializeField]
    private float frequency;
    [SerializeField]
    private Vector3 startPoint = new Vector3(0, 0, 0);

    public bool GenerateAtStart = false;//set the default to false
    // Start is called before the first frame update
    void Start()
    {
        //generate these at the beginning of the level
        if (GenerateAtStart) {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            CreateTerrain();
            UpdateMesh();
        }
    }


    private void CreateTerrain()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        //so that the index is local to the for loop
        for (int index = 0, i = 0; i < zSize + 1; i++)
        {
            for (int j = 0; j < xSize + 1; j++)
            {
                float h = Mathf.PerlinNoise(i * frequency, j * frequency) * height;
                vertices[index] = new Vector3(i, h, j);
                index++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;
        //zSize and xSize don't do a + 1 because the vertices already did
        for (int i = 0; i < zSize; i++)
        {
            for (int j = 0; j < xSize; j++)
            {
                triangles[tris + 0] = vert + 1;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert;
                triangles[tris + 3] = vert + xSize + 2;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + 1;
                vert++;//vertice increase each time the two triangles are drawn 
                tris += 6;//input another 6 vertices for the two triangles
            }
            vert++;
        }
        
    }

    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
