﻿using Unity.Collections;
using Unity.Mathematics;

public struct DirichletTessellation
{
    NativeList<float2> edgeVertices;
    NativeList<float2x2> adjacentCellPositions;
    float2 centerPoint;
    
    VectorUtil vectorUtil;

    public NativeList<float2> Tessalate(NativeArray<float2x4> triangles, float3 point, UnityEngine.Color debugColor, out NativeArray<float2x2> adjacentPositions)
    {
        this.edgeVertices = new NativeList<float2>(Allocator.Temp);
        this.adjacentCellPositions = new NativeList<float2x2>(Allocator.Temp);
        this.centerPoint = new float2(point.x, (float)point.z);


        GatherCellEdgeVertices(triangles, centerPoint);
        
        vectorUtil.SortVerticesClockwise(edgeVertices, centerPoint);

        DrawEdges(debugColor);//DEBUG
        //DrawAdjacent(debugColor);//DEBUG

        adjacentPositions = adjacentCellPositions;

        return edgeVertices;
    }

    void GatherCellEdgeVertices(NativeArray<float2x4> triangles, float2 centerPoint)
    {
        for(int t = 0; t < triangles.Length; t++)
        {
            float2x4 triangle = triangles[t];
            float2 circumcenter = triangle[3];

            bool triangleInCell = false;
            int floatIndex = 0;
            float2x2 adjacentCellPair = float2x2.zero;

            for(int i = 0; i < 3; i++)
                if(triangle[i].Equals(centerPoint))
                {
                    triangleInCell = true;
                }
                else
                {
                    if(floatIndex > 1)
                        continue;

                    adjacentCellPair[floatIndex] = triangle[i];
                    floatIndex++;
                }

            if(triangleInCell)
            {
                edgeVertices.Add(circumcenter);
                adjacentCellPositions.Add(adjacentCellPair);
            }
        }
    }

    void RemoveDuplicateVertices(NativeList<float2> originalVertices)
    {
        NativeArray<float2> copy = new NativeArray<float2>(originalVertices.Length, Allocator.Temp);
        copy.CopyFrom(originalVertices);

        originalVertices.Clear();

        for(int i = 0; i < copy.Length;i += 2)
            originalVertices.Add(copy[i]);
    }

    //DEBUG
    void DrawLineFloat2(float2 a, float2 b, UnityEngine.Color color)
    {
        float3 a3 = new float3(a.x, 0, a.y);
        float3 b3 = new float3(b.x, 0, b.y);
        UnityEngine.Debug.DrawLine(a3, b3, color, 100);
    }
    void DrawPoint(float2 point, UnityEngine.Color color)
    {
        var offsets = new AdjacentIntOffsetsClockwise();
        for(int i = 0; i < 4; i++)
        {
            DrawLineFloat2(point + offsets[i], point-offsets[i], color);
        }
    }

    void DrawEdges(UnityEngine.Color color)
    {
        for(int i = 0; i < edgeVertices.Length; i++)//DEBUG
        {
            int nextIndex = i == edgeVertices.Length-1 ? 0 : i+1;
            DrawLineFloat2(edgeVertices[i], edgeVertices[nextIndex], color);
        }//DEBUG
    }
    void DrawAdjacent(UnityEngine.Color color)
    {
        for(int i = 0; i < adjacentCellPositions.Length; i++)
        {
            DrawLineFloat2(adjacentCellPositions[i].c0, centerPoint, color);
            DrawLineFloat2(adjacentCellPositions[i].c1, centerPoint, color);
        }
    }
    //DEBUG
}
