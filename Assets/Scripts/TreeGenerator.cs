using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using System.Collections.Generic;

public struct TreeGenerator
{
    public Unity.Mathematics.Random random;
    public WorleyNoise worley;
    public GameObject meshPrefab;
    public Material material;

    WorleyNoise.CellData cell;

    NativeList<float3> vertices;
    NativeList<int> triangles;
    int vertexIndex;
    
    public void Generate(int2 cellIndex)
    {
        cell = worley.GetCellData(cellIndex);

        Tree(cellIndex);
    }

    void Tree(int2 cellIndex)
    {
        NativeList<float2> edgeVertices = worley.GetCellVertices(cellIndex);

        DrawCell(edgeVertices, cell.position);

        edgeVertices.Dispose();

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
    }


    void DrawCell(NativeArray<float2> worleyCellEdge, float3 cellPosition)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        vertexIndex = 0;

        vertices.Add(cellPosition);

        for(int i = 0; i < worleyCellEdge.Length; i++)
        {
            float2 two = worleyCellEdge[i];
            float3 three = new float3(two.x, 0, two.y);
            vertices.Add(three);
        }

        for(int i = 1; i < worleyCellEdge.Length+1; i++)
        {
            int current = i + vertexIndex;
            int next = (i == worleyCellEdge.Length ? 1 : i+1) + vertexIndex;
            int cell = vertexIndex;

            triangles.Add(current);
            triangles.Add(next);
            triangles.Add(cell);            
        }

        vertexIndex += worleyCellEdge.Length;
    }

    void MakeMesh()
    {
        List<Vector3> vertexList = new List<Vector3>();
        for(int i = 0; i < vertices.Length; i++)
            vertexList.Add(vertices[i]);

        GameObject meshObject = GameObject.Instantiate(meshPrefab);
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertexList);
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshRenderer.material = this.material;
        meshFilter.mesh = mesh;

        float3 randomColor = random.NextFloat3();
        meshRenderer.material.color = new Color(randomColor.x, randomColor.y, randomColor.z);
    }

    //  Draw cell mesh
    //  Draw tree trunk with one edge per cell edge
    //  Draw leaf cells
}