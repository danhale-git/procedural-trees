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
        cell = worley.GetCellData(cellIndex);

        Tree(cellIndex);
    }

    void Tree(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        vertexIndex = 0;

        NativeList<float2> edgeVertices = worley.GetCellVertices(cellIndex);

        DrawCell(edgeVertices, cell.position);

        var currentVerts = new NativeArray<float3>(vertices.Length-1, Allocator.Temp);
        var currentVertIndices = new NativeArray<int>(vertices.Length-1, Allocator.Temp);
        for(int i = 1; i < vertices.Length; i++)
        {
            currentVerts[i-1] = vertices[i];
            currentVertIndices[i-1] = i;
        }

        Extrude(currentVerts, new float3(0, 10, 3));
        //ExtrudeIndex(currentVertIndices, new float3(0, 10, 3));

        currentVerts.Dispose();

        MakeMesh();

        edgeVertices.Dispose();
        vertices.Dispose();
        triangles.Dispose();
    }

    NativeArray<int> ExtrudeIndex(NativeArray<int> extrudeFrom, float3 extrusion)
    {
        int vertCount = extrudeFrom.Length;
        NativeArray<int> extrudeTo = new NativeArray<int>(extrudeFrom.Length, Allocator.Temp);

        for(int i = 0; i < extrudeFrom.Length; i++)
        {
            float3 vertex = extrudeFrom[i] + extrusion;
            this.vertices.Add(vertex);
            extrudeTo[i] = i + vertexIndex;
        }

        for(int i = 0; i < vertCount; i++)
        {
            int currentEdge = i;
            int nextEdge = i == extrudeFrom.Length-1 ? 0 : i+1;

            triangles.Add(extrudeFrom[currentEdge]);
            triangles.Add(extrudeFrom[nextEdge]);
            triangles.Add(extrudeTo[currentEdge]);

            triangles.Add(extrudeTo[currentEdge]);
            triangles.Add(extrudeFrom[nextEdge]);
            triangles.Add(extrudeTo[nextEdge]);
        }

        vertexIndex += vertCount*2;

        return extrudeTo;
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


    void DrawCell(NativeArray<float2> worleyCellEdge, float3 cellCenterPosition)
    {
        vertices.Add(cellCenterPosition);
        int cellCenter = vertexIndex;

        for(int i = 0; i < worleyCellEdge.Length; i++)
        {
            float2 two = worleyCellEdge[i];
            float3 three = new float3(two.x, 0, two.y);
            vertices.Add(three);
        }

        for(int i = 1; i < worleyCellEdge.Length+1; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == worleyCellEdge.Length ? 1 : i+1);

            triangles.Add(currentEdge);
            triangles.Add(nextEdge);
            triangles.Add(cellCenter);            
        }

        vertexIndex += worleyCellEdge.Length + 1;
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