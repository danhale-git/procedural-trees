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

    VectorUtil vectorUtil;
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        vertexIndex = 0;
        
        cell = worley.GetCellData(cellIndex);
        cellVertices = worley.GetCellVertices(cellIndex);

        DrawCell(cellVertices, cell.position);
        NativeArray<float3> extruded;

        float3 min = new float3(-2, 0, -2);
        float3 max = new float3(2, 0, 2);
        
        extruded = ExtrudeTrunk(TrunkVertices(1), random.NextFloat3(min, max) + new float3(0, 4, 0));
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0));
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0));

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
        cellVertices.Dispose();
    }

    NativeArray<float3> TrunkVertices(float size)
    {
        NativeList<float3> trunkVertices = new NativeList<float3>(Allocator.Temp);
        for(int i = 0; i < cellVertices.Length; i++)
        {
            int nextIndex = i == cellVertices.Length-1 ? 0 : i+1;
            
            float3 currentVertex = math.normalize(cellVertices[i] - cell.position);
            float3 nextVertex = math.normalize(cellVertices[nextIndex] - cell.position);

            if(vectorUtil.Angle(currentVertex, nextVertex) < 20)
                continue;

            float3 trunkVertex = (cell.position + currentVertex) * size;
            trunkVertices.Add(trunkVertex);
        }

        NativeArray<float3> vertexArray = new NativeArray<float3>(trunkVertices.Length, Allocator.Temp);
        vertexArray.CopyFrom(trunkVertices);
        trunkVertices.Dispose();

        return vertexArray;
    }

    NativeArray<int> ExtrudeTrunk2(NativeArray<int> startIndices, float3 extrusion)
    {
        int edgeCount = startIndices.Length;

        NativeArray<int> endIndices = new NativeArray<int>(startIndices.Length, Allocator.Temp);

        NativeArray<float3> startVertices = new NativeArray<float3>(startIndices.Length, Allocator.Temp);
        NativeArray<float3> endVertices = new NativeArray<float3>(startIndices.Length, Allocator.Temp);

        for(int i = 0; i < edgeCount; i++)
        {
            float3 startVertex = vertices[startIndices[i]];
            startVertices[i] = startVertex;
            
            float3 endVertex = startVertex + extrusion;
            vertices.Add(endVertex);
            endIndices[i] = vertices.Length-1;
        }

        for(int i = 0; i < edgeCount; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == edgeCount-1 ? 0 : i+1);

            triangles.Add(startIndices[currentEdge]);
            triangles.Add(startIndices[nextEdge]);
            triangles.Add(endIndices[currentEdge]);

            triangles.Add(endIndices[currentEdge]);
            triangles.Add(endIndices[nextEdge]);
            triangles.Add(startIndices[nextEdge]);
        }

        return endIndices;
    }

    NativeArray<float3> ExtrudeTrunk(NativeArray<float3> extrudeFrom, float3 extrusion)
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