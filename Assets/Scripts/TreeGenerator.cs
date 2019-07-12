using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using System.Collections.Generic;

public struct TreeGenerator
{
    public WorleyNoise worley;
    public GameObject meshPrefab;
    public Material material;

    WorleyNoise.CellProfile cellProfile;

    NativeList<float3> vertices;
    NativeList<int> triangles;

    Unity.Mathematics.Random random;
    VectorUtil vectorUtil;
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        
        cellProfile = worley.GetCellProfile(cellIndex);
        random = new Unity.Mathematics.Random((uint)(cellProfile.data.value * 1000));


        Leaves leaves = new Leaves(vertices, triangles, random);
        leaves.Draw(cellProfile);
        
        //DrawTrunk();

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
    }

    void WorldToLocal(NativeArray<float3> worldPositions)
    {
        for(int i = 0; i < worldPositions.Length; i++)
            worldPositions[i] = worldPositions[i] - cellProfile.data.position;
    }

    void DrawTrunk()
    {
        float3 min = new float3(-1, 0, -1);
        float3 max = new float3(1, 0, 1);
        NativeArray<int> extruded = TrunkVertices(0.2f);
        extruded = ExtrudeTrunk(extruded, new float3(0, 1, 0), 0.4f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded.Dispose(); 
    }

    NativeArray<int> TrunkVertices(float size)
    {
        NativeList<int> trunkIndices = new NativeList<int>(Allocator.Temp);

        for(int i = 0; i < cellProfile.vertices.Length; i++)
        {
            vertices.Add(cellProfile.vertices[i] * size);
            trunkIndices.Add(vertices.Length-1);
        }

        NativeArray<int> indexArray = new NativeArray<int>(trunkIndices.Length, Allocator.Temp);
        indexArray.CopyFrom(trunkIndices);
        trunkIndices.Dispose();
        return indexArray;
    }

    NativeArray<int> ExtrudeTrunk(NativeArray<int> startIndices, float3 extrusion, float scale)
    {
        int edgeCount = startIndices.Length;

        NativeArray<int> endIndices = new NativeArray<int>(startIndices.Length, Allocator.Temp);

        for(int i = 0; i < edgeCount; i++)
        {
            float3 startVertex = vertices[startIndices[i]];
            float3 endVertex = (startVertex * scale) + extrusion;

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

    //TODO world to local vertices and set cell position
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
        //meshRenderer.material.color = new Color(.4f,random.NextFloat(0.7f, 0.9f),.4f);

        //meshObject.transform.Translate(cellProfile.cell.position);
    }
}