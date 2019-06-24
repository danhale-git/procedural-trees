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
    NativeArray<float3> cellVertices;

    NativeList<float3> vertices;
    NativeList<int> triangles;
    int vertexIndex;

    /*struct Vertex
    {
        public readonly float3 position;
        public readonly int index;
        public Vertex(float3 position, int index)
        {
            this.position = position;
            this.index = index;
        }
    } */
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        vertexIndex = 0;
        
        cell = worley.GetCellData(cellIndex);
        cellVertices = worley.GetCellVertices(cellIndex);

        DrawCell(cellVertices, cell.position);
        Extrude(cellVertices, new float3(0, 10, 3));

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
        cellVertices.Dispose();
    }

    NativeArray<float3> Extrude(NativeArray<float3> extrudeFrom, float3 extrusion)
    {
        int vertCount = extrudeFrom.Length;
        NativeArray<float3> extrudeTo = new NativeArray<float3>(extrudeFrom.Length, Allocator.Temp);

        for(int i = 0; i < extrudeFrom.Length; i++)
            this.vertices.Add(extrudeFrom[i]);

        for(int i = 0; i < extrudeFrom.Length; i++)
        {
            float3 vertex = extrudeFrom[i] + extrusion;
            this.vertices.Add(vertex);
            extrudeTo[i] = vertex;
        }

        for(int i = 0; i < extrudeFrom.Length; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == extrudeFrom.Length-1 ? 0 : i+1);

            triangles.Add(currentEdge);
            triangles.Add(nextEdge);
            triangles.Add(currentEdge+vertCount);

            triangles.Add(currentEdge+vertCount);
            triangles.Add(nextEdge);
            triangles.Add(nextEdge+vertCount);
        }

        vertexIndex += vertCount*2;

        return extrudeTo;
    }

    void DrawCell(NativeArray<float3> worleyCellEdge, float3 cellCenterPosition)
    {
        int cellCenter = vertexIndex;
        vertices.Add(cellCenterPosition);
        vertexIndex++;

        for(int i = 0; i < worleyCellEdge.Length; i++)
            vertices.Add(worleyCellEdge[i]);

        for(int i = 0; i < worleyCellEdge.Length; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == worleyCellEdge.Length-1 ? 0 : i+1);

            triangles.Add(currentEdge);
            triangles.Add(nextEdge);
            triangles.Add(cellCenter);            
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
        //meshRenderer.material.color = new Color(randomColor.x, randomColor.y, randomColor.z);
        meshRenderer.material.color = new Color(.9f,.9f,.9f);
    }
}