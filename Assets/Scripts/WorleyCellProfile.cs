﻿using Unity.Mathematics;
using Unity.Collections;

public struct WorleyCellProfile
{
    float3 cellPosition;
    NativeList<BowyerWatson<WorleyNoise.CellData>.Triangle> triangles;

    BowyerWatson<WorleyNoise.CellData> bowyerWatson;
    VectorUtil vectorUtil;

    NativeList<float3> cellVertices;
    NativeList<WorleyNoise.CellDataX2> adjacentCells;

    public WorleyNoise.CellProfile GetCellProfile(NativeArray<WorleyNoise.CellData> nineCells, WorleyNoise.CellData cell)
    {
        this.cellPosition = cell.position;

        this.triangles = bowyerWatson.Triangulate(nineCells);

        SortTrianglesClockwise();

        this.cellVertices = new NativeList<float3>(Allocator.Temp);
        this.adjacentCells = new NativeList<WorleyNoise.CellDataX2>(Allocator.Temp);
        GetCellVerticesAndAdjacentCells();

        var cellProfile = new WorleyNoise.CellProfile();
        cellProfile.data = cell;
        cellProfile.meanPoint = vectorUtil.MeanPoint(cellProfile.vertices);
        cellProfile.vertices = new NativeArray<float3>(cellVertices, Allocator.Temp);
        cellProfile.adjacentCells = new NativeArray<WorleyNoise.CellDataX2>(adjacentCells, Allocator.Temp);

        triangles.Dispose();
        cellVertices.Dispose();
        adjacentCells.Dispose();

        return cellProfile;
    }

    void SortTrianglesClockwise()
    {
        for(int i = 0; i < triangles.Length; i++)
        {
            var triangle = triangles[i];
            triangle.degreesFromUp = vectorUtil.RotationFromUp(triangle.circumcircle.center, cellPosition);
            triangles[i] = triangle;
        }

        var sortedTriangles = new NativeArray<BowyerWatson<WorleyNoise.CellData>.Triangle>(triangles.Length, Allocator.Temp);
        sortedTriangles.CopyFrom(triangles);
        sortedTriangles.Sort();
        sortedTriangles.CopyTo(triangles);
        sortedTriangles.Dispose();
    }

    void GetCellVerticesAndAdjacentCells()
    {
        for(int t = 0; t < triangles.Length; t++)
        {
            var triangle = triangles[t];
            AddTriangleIfInCell(triangle);
        }
    }

    void AddTriangleIfInCell(BowyerWatson<WorleyNoise.CellData>.Triangle triangle)
    {
        bool triangleInCell = false;

        int floatIndex = 0;
        var adjacentCellPair = new WorleyNoise.CellDataX2();

        for(int i = 0; i < 3; i++)
            if(triangle[i].pos.Equals(cellPosition))
            {
                triangleInCell = true;
            }
            else
            {
                if(floatIndex > 1)
                    continue;

                adjacentCellPair[floatIndex] = triangle[i].pointObject;
                floatIndex++;
            }

        if(triangleInCell)
        {
            cellVertices.Add(triangle.circumcircle.center);
            adjacentCells.Add(SortCellPairClockwise(adjacentCellPair));
        }
    }

    WorleyNoise.CellDataX2 SortCellPairClockwise(WorleyNoise.CellDataX2 original)
    {
        bool wrongWay = vectorUtil.RotationFromUp(original.c0.position, cellPosition) >
                        vectorUtil.RotationFromUp(original.c1.position, cellPosition);

        if(wrongWay)
            return FlipCellDataX2(original);
        else
            return original;
    }

    WorleyNoise.CellDataX2 FlipCellDataX2(WorleyNoise.CellDataX2 original)
    {
        var result = new WorleyNoise.CellDataX2();
        result.c0 = original.c1;
        result.c1 = original.c0;
        return result;
    }
}
