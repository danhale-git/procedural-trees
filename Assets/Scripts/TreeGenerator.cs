using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using System.Collections.Generic;

public struct TreeGenerator
{
    public WorleyNoise worley;
    public GameObject meshPrefab;
    public Material material;

    WorleyNoise.CellProfile parentCellProfile;

    NativeList<float3> vertices;
    NativeList<int> triangles;

    Unity.Mathematics.Random random;
    VectorUtil vectorUtil;

    Color randomColor;//DEBUG
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        
        parentCellProfile = worley.GetCellProfile(cellIndex);
        random = new Unity.Mathematics.Random((uint)(parentCellProfile.data.value * 1000));

        DrawChildCells(worley.frequency*2);
        //Leaves leaves = new Leaves(vertices, triangles, worley.seed);
        //leaves.Draw(parentCellProfile, 0);

        //DrawTrunk();

        DrawCellLines(parentCellProfile);

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();

        //DEBUG
        random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, 10000));
        float3 col = random.NextFloat3(0, 1);
        randomColor = new Color(col.x, col.y, col.z);
        //TreeManager.CreateTextMesh(parentCellProfile.data.position + new float3(0,12,0), parentCellProfile.data.index.ToString(), randomColor*0.5f);
        //DEBUG
    }

    void DrawChildCells(float2 frequency)
    {
        Leaves leaves = new Leaves(vertices, triangles, worley.seed);

        WorleyNoise childWorley = worley;
        childWorley.frequency = frequency;

        float3 meanPointWorld = parentCellProfile.data.position + vectorUtil.MeanPoint(parentCellProfile.vertices);
        WorleyNoise.CellData startChild = childWorley.GetCellData(meanPointWorld);

        var checkNext = new NativeQueue<WorleyNoise.CellData>(Allocator.Temp);
        var alreadyChecked = new NativeList<int2>(Allocator.Temp);

        checkNext.Enqueue(startChild);
        alreadyChecked.Add(startChild.index);

        while(checkNext.Count > 0)
        {
            WorleyNoise.CellData childData = checkNext.Dequeue();

            WorleyNoise.CellData dataFromParent = worley.GetCellData(childData.position);
            bool childIsInParent = dataFromParent.index.Equals(parentCellProfile.data.index);

            if(!childIsInParent)
                continue;

            WorleyNoise.CellProfile childProfile = childWorley.GetCellProfile(childData);
            float3 positionInParent = childProfile.data.position - parentCellProfile.data.position;

            leaves.Draw(childProfile, positionInParent);

            for(int i = 0; i < childProfile.vertices.Length; i++)
            {
                WorleyNoise.CellData adjacent = childProfile.adjacentCells[i].c0;
                if(!alreadyChecked.Contains(adjacent.index))
                {
                    checkNext.Enqueue(adjacent);
                    alreadyChecked.Add(adjacent.index);
                }
            }
        }

        checkNext.Dispose();
        alreadyChecked.Dispose();
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

        for(int i = 0; i < parentCellProfile.vertices.Length; i++)
        {
            vertices.Add(parentCellProfile.vertices[i] * size);
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

        //meshRenderer.material.color = randomColor;
        meshRenderer.material.color = UnityEngine.Color.white;
        //meshRenderer.material.color = new Color(.4f,random.NextFloat(0.7f, 0.9f),.4f);

        meshObject.transform.Translate(parentCellProfile.data.position);
    }

    void DrawCellLines(WorleyNoise.CellProfile cell)
    {
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            int next = i == cell.vertices.Length-1 ? 0 : i+1;
            UnityEngine.Debug.DrawLine(cell.vertices[i]+cell.data.position, cell.vertices[next]+cell.data.position, UnityEngine.Color.green, 100);
        }
    }
}