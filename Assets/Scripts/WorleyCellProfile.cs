using Unity.Mathematics;
using Unity.Collections;

public struct WorleyCellProfile
{
    float3 cellPosition;
    NativeList<BowyerWatson.Triangle> triangles;

    BowyerWatson bowyerWatson;
    VectorUtil vectorUtil;

    //TODO: rename script/struct and add small segment removal option
    public WorleyNoise.CellProfile GetCellProfile(NativeArray<WorleyNoise.CellData> nineCells, WorleyNoise.CellData cell)
    {
        this.cellPosition = cell.position;
        this.triangles = new NativeList<BowyerWatson.Triangle>(Allocator.Temp);

        this.bowyerWatson = new BowyerWatson();

        var allTriangles = bowyerWatson.BowyerWatsonTriangulation(nineCells, cell);
        AddEligibleTriangles(allTriangles);

        SortTrianglesClockwise();

        var cellProfile = new WorleyNoise.CellProfile();
        cellProfile.data = cell;
        cellProfile.vertices = GatherCellEdgeVertices(out cellProfile.adjacentCells);
        cellProfile.meanPoint = vectorUtil.MeanPoint(cellProfile.vertices);

        return cellProfile;
    }

    void AddEligibleTriangles(NativeList<BowyerWatson.Triangle> trianglesCopy)
    {
        for(int i = 0; i < trianglesCopy.Length; i++)
        {
            BowyerWatson.Triangle triangle = trianglesCopy[i];
            if(!SharesVertexWithSupertriangle(triangle))
                triangles.Add(triangle);
        }

        trianglesCopy.Dispose();
    }

    bool SharesVertexWithSupertriangle(BowyerWatson.Triangle triangle)
    {
        for(int t = 0; t < 3; t++)
            for(int s = 0; s < 3; s++)
                if(triangle[t].Equals(bowyerWatson.superTriangle[s]))
                    return true;

        return false;
    }

    void SortTrianglesClockwise()
    {
        var sortedTriangles = new NativeArray<BowyerWatson.Triangle>(triangles.Length, Allocator.Temp);
        sortedTriangles.CopyFrom(triangles);
        sortedTriangles.Sort();
        sortedTriangles.CopyTo(triangles);
        sortedTriangles.Dispose();
    }

    NativeArray<float3> GatherCellEdgeVertices(out NativeArray<WorleyNoise.CellDataX2> adjacentCellsArray)
    {
        var edgeVertices = new NativeList<float3>(Allocator.Temp);
        var adjacentCells = new NativeList<WorleyNoise.CellDataX2>(Allocator.Temp);

        for(int t = 0; t < triangles.Length; t++)
        {
            BowyerWatson.Triangle triangle = triangles[t];
            bool triangleInCell = false;

            int floatIndex = 0;
            var adjacentCellPair = new WorleyNoise.CellDataX2();

            for(int i = 0; i < 3; i++)
                if(triangle[i].pos.Equals(cellPosition))
                    triangleInCell = true;
                else if(floatIndex > 1)
                    continue;
                else
                {
                    adjacentCellPair[floatIndex] = triangle[i].cell;
                    floatIndex++;
                }

            if(triangleInCell)
            {
                edgeVertices.Add(triangle.circumcircle.center);
                adjacentCells.Add(SortCellPairClockwise(adjacentCellPair));
            }
        }

        var vertexArray = new NativeArray<float3>(edgeVertices.Length, Allocator.Temp);
        adjacentCellsArray = new NativeArray<WorleyNoise.CellDataX2>(edgeVertices.Length, Allocator.Temp);
        
        vertexArray.CopyFrom(edgeVertices);
        adjacentCellsArray.CopyFrom(adjacentCells);

        return vertexArray;
    }

    WorleyNoise.CellDataX2 SortCellPairClockwise(WorleyNoise.CellDataX2 original)
    {
        bool wrongWay = vectorUtil.RotationFromUp(original.c0.position, cellPosition) >
                        vectorUtil.RotationFromUp(original.c1.position, cellPosition);

        if(wrongWay)
        {
            var result = new WorleyNoise.CellDataX2();
            result.c0 = original.c1;
            result.c1 = original.c0;
            return result;
        }
        else
            return original;
    }


    void DrawTriangle(BowyerWatson.Triangle triangle, UnityEngine.Color color)
    {
        UnityEngine.Debug.DrawLine(triangle.a.pos, triangle.b.pos, color, 100);
        UnityEngine.Debug.DrawLine(triangle.a.pos, triangle.c.pos, color, 100);
        UnityEngine.Debug.DrawLine(triangle.c.pos, triangle.b.pos, color, 100);
    }
}
