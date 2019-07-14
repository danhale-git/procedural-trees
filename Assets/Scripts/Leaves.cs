﻿using Unity.Mathematics;
using Unity.Collections;

public struct Leaves
{
    NativeList<float3> vertices;
    NativeList<int> triangles;
    float3 offset;
    WorleyNoise.CellProfile cell;

    VectorUtil vectorUtil;
    SimplexNoise simplex;

    const int minSegmentAngle = 30;

    public Leaves(NativeList<float3> vertices, NativeList<int> triangles, int seed)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.offset = float3.zero;
        this.cell = new WorleyNoise.CellProfile();
        this.simplex = new SimplexNoise(seed, 0.5f, negative: true);
    }

    public void Draw(WorleyNoise.CellProfile cellProfile, float3 offset)
    {
        this.offset = offset;
        this.cell = cellProfile;

        RemoveSmallSegments();

        float height = GetHeight();

        float3 center = vectorUtil.MeanPoint(this.cell.vertices);
        center.y += height;

        for(int i = 0; i < this.cell.vertices.Length; i++)
        {
            float3 currentEdge = this.cell.vertices[i];
            float3 nextEdge = this.cell.vertices[WrapVertIndex(i + 1)];

            DrawTriangle(currentEdge, nextEdge, center);
        }
    }

    void RemoveSmallSegments()
    {
        var newVertices = new NativeList<float3>(Allocator.Temp);
        var newAdjacentCells = new NativeList<WorleyNoise.CellDataX2>(Allocator.Temp);

        bool smallSegmentFound = false;
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            int next = WrapVertIndex(i+1);
            float3 currentVertex = cell.vertices[i];
            float3 nextVertex = cell.vertices[next];

            float segmentAngle = vectorUtil.Angle(currentVertex, nextVertex);

            if(segmentAngle > minSegmentAngle)
            {
                newVertices.Add(currentVertex);
                newAdjacentCells.Add(cell.adjacentCells[i]);
            }
            else if(!smallSegmentFound)
                smallSegmentFound = true;
        }

        if(smallSegmentFound)
        {
            cell.vertices = new NativeArray<float3>(newVertices, Allocator.Temp);
            cell.adjacentCells = new NativeArray<WorleyNoise.CellDataX2>(newAdjacentCells, Allocator.Temp);
        }
    }

    /*public void Draw(WorleyNoise.CellProfile cell, float3 offset)
    {
        this.offset = offset;
        this.cell = cell;

        float height = GetHeight();

        float3 center = vectorUtil.MeanPoint(cell.vertices);
        center.y += height;

        for(int i = 0; i < cell.vertices.Length; i++)
        {
            float3 currentEdge = cell.vertices[i];
            float3 nextEdge = cell.vertices[NextVertIndex(i)];


            float3 currentMid = vectorUtil.MidPoint(center, currentEdge, 0.6f);
            currentMid.y = height;
            currentMid *= 0.8f;

            float3 nextMid = vectorUtil.MidPoint(center, nextEdge, 0.6f);
            nextMid.y = height;
            nextMid *= 0.8f;

            float3 edgeMid = vectorUtil.MidPoint(currentEdge, nextEdge);
            edgeMid *= 1.1f;

            if(SegmentIsThin(currentEdge, nextEdge))
            {
                VertAndTri(nextMid);
                VertAndTri(currentMid);
                VertAndTri(currentEdge);
                
                VertAndTri(currentEdge);
                VertAndTri(nextEdge);
                VertAndTri(nextMid);
                
                VertAndTri(currentMid);
                VertAndTri(nextMid);
                VertAndTri(center);
            }
            else
            {
                VertAndTri(edgeMid);
                VertAndTri(currentMid);
                VertAndTri(currentEdge);

                VertAndTri(edgeMid);
                VertAndTri(nextMid);
                VertAndTri(currentMid);
                
                VertAndTri(nextEdge);
                VertAndTri(nextMid);
                VertAndTri(edgeMid);

                VertAndTri(currentMid);
                VertAndTri(nextMid);
                VertAndTri(center);
            }
        }
    }  */

    float GetHeight()
    {
        float farthest = 0;
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            float distance = math.length(cell.vertices[i]);
            if(distance > farthest)
                farthest = distance;
        }

        return farthest * 0.5f;
    }

    bool SegmentIsThin(float3 a, float3 b)
    {
        return vectorUtil.Angle(a, b) < minSegmentAngle;
    }

    int WrapVertIndex(int index)
    {
        if(index >= cell.vertices.Length)
            return index - cell.vertices.Length;
        else if(index < 0)
            return index + cell.vertices.Length;
        else
            return index;
    }

    void DrawTriangle(float3 a, float3 b, float3 c)
    {
        VertAndTri(a);
        VertAndTri(b);
        VertAndTri(c);
    }

    void VertAndTri(float3 vert)
    {
        //float jitter = simplex.GetSimplex(vert.x + cellProfile.data.position.x, vert.z + cellProfile.data.position.z);
        //vert += jitter * 0.5f;

        vertices.Add(vert + offset);
        triangles.Add(vertices.Length-1);
    }
}
