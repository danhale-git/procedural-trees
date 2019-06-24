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

    VectorUtil vectorUtil;
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        
        cell = worley.GetCellData(cellIndex);
        cellVertices = worley.GetCellVertices(cellIndex);

        DrawCell(cellVertices, cell.position);

        float3 min = new float3(-1, 0, -1);
        float3 max = new float3(1, 0, 1);

        NativeArray<int> extruded = TrunkVertices(1.5f);

        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 1, 0), 0.4f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded.Dispose();

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
        cellVertices.Dispose();
    }

    NativeArray<int> TrunkVertices(float size)
    {
        NativeList<int> trunkIndices = new NativeList<int>(Allocator.Temp);
        for(int i = 0; i < cellVertices.Length; i++)
        {
            int nextIndex = i == cellVertices.Length-1 ? 0 : i+1;
            
            float3 currentVertex = math.normalize(cellVertices[i] - cell.position);
            float3 nextVertex = math.normalize(cellVertices[nextIndex] - cell.position);

            if(vectorUtil.Angle(currentVertex, nextVertex) < 20)
                continue;

            float3 trunkVertex = currentVertex*size;

            vertices.Add(trunkVertex);
            trunkIndices.Add(vertices.Length-1);
        }

        NativeArray<int> vertexArray = new NativeArray<int>(trunkIndices.Length, Allocator.Temp);
        vertexArray.CopyFrom(trunkIndices);
        trunkIndices.Dispose();

        return vertexArray;
    }

    NativeArray<int> ExtrudeTrunk(NativeArray<int> startIndices, float3 extrusion, float scale)
    {
        int edgeCount = startIndices.Length;

        NativeArray<int> endIndices = new NativeArray<int>(startIndices.Length, Allocator.Temp);

        for(int i = 0; i < edgeCount; i++)
        {
            float3 startVertex = vertices[startIndices[i]];
            float3 distanceFromCell = startVertex;
            float3 endVertex = (distanceFromCell * scale) + extrusion;

            vertices.Add(endVertex);
            endIndices[i] = vertices.Length-1;
        }

        for(int i = 0; i < edgeCount; i++)
        {
            int currentEdge = i;
            int nextEdge = i == edgeCount-1 ? 0 : i+1;

            triangles.Add(startIndices[currentEdge]);
            triangles.Add(startIndices[nextEdge]);
            triangles.Add(endIndices[currentEdge]);

            triangles.Add(startIndices[nextEdge]);
            triangles.Add(endIndices[nextEdge]);
            triangles.Add(endIndices[currentEdge]);
        }

        return endIndices;
    }

    void DrawCell(NativeArray<float3> worleyCellEdge, float3 cellCenterPosition)
    {
        vertices.Add(float3.zero);
        int cellCenter = vertices.Length-1;
        int vertexIndex = vertices.Length;

        for(int i = 0; i < worleyCellEdge.Length; i++)
            vertices.Add(worleyCellEdge[i] - cell.position);

        for(int i = 0; i < worleyCellEdge.Length; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == worleyCellEdge.Length-1 ? 0 : i+1);

            triangles.Add(currentEdge);
            triangles.Add(nextEdge);
            triangles.Add(cellCenter);            
        }
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

        meshObject.transform.Translate(cell.position);
    }
}