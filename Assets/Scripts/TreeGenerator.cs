using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using System.Collections.Generic;

public struct TreeGenerator
{
    public WorleyNoise worley;
    public GameObject meshPrefab;
    public Material material;
    public SimplexNoise simplex;

    WorleyNoise.CellProfile parentCell;

    NativeList<float3> vertices;
    NativeList<int> triangles;

    Unity.Mathematics.Random random;
    VectorUtil vectorUtil;

    Leaves leaves;
    Trunk trunk;

    float baseHeight;

    public void Generate(int2 cellIndex)
    {
        parentCell = worley.GetCellProfile(cellIndex);

        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        
        random = new Unity.Mathematics.Random((uint)(parentCell.data.value * 1000));

        leaves = new Leaves(vertices, triangles);
        trunk = new Trunk(vertices, triangles, random);

        baseHeight = simplex.GetSimplex(parentCell.data.position.x, parentCell.data.position.z) * 15;

        DrawChildCells(worley.frequency*2);
        //leaves.Draw(parentCellProfile, 0);

        trunk.DrawTrunk(parentCell);

        DrawCellSegments(parentCell);
        DrawCellLines(parentCell);

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
    }

    void DrawChildCells(float2 frequency)
    {
        WorleyNoise childWorley = worley;
        childWorley.frequency = frequency;

        float3 meanPointWorld = parentCell.data.position + vectorUtil.MeanPoint(parentCell.vertices);
        WorleyNoise.CellData startChild = childWorley.GetCellData(meanPointWorld);

        var checkNext = new NativeQueue<WorleyNoise.CellData>(Allocator.Temp);
        var alreadyChecked = new NativeList<int2>(Allocator.Temp);

        checkNext.Enqueue(startChild);
        alreadyChecked.Add(startChild.index);

        var children = new NativeList<WorleyNoise.CellProfile>(Allocator.Temp);

        while(checkNext.Count > 0)
        {
            WorleyNoise.CellData childData = checkNext.Dequeue();

            WorleyNoise.CellData dataFromParent = worley.GetCellData(childData.position);
            bool childIsInParent = dataFromParent.index.Equals(parentCell.data.index);

            if(!childIsInParent)
                continue;

            WorleyNoise.CellProfile childProfile = childWorley.GetCellProfile(childData);
            float3 positionInParent = childProfile.data.position - parentCell.data.position;
            positionInParent.y += baseHeight;

            leaves.Draw(childProfile, positionInParent);

            children.Add(childProfile);

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

        meshObject.transform.Translate(parentCell.data.position);
    }

    void DrawCellLines(WorleyNoise.CellProfile cell)
    {
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            int next = i == cell.vertices.Length-1 ? 0 : i+1;
            UnityEngine.Debug.DrawLine(cell.vertices[i]+cell.data.position, cell.vertices[next]+cell.data.position, UnityEngine.Color.green, 100);
        }
    }

    void DrawCellSegments(WorleyNoise.CellProfile cell)
    {
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            UnityEngine.Debug.DrawLine(cell.vertices[i]+cell.data.position, cell.data.position, UnityEngine.Color.red, 100);
        }
    }
}