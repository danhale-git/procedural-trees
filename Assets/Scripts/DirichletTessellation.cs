using Unity.Collections;
using Unity.Mathematics;

public struct DirichletTessellation
{
    NativeList<float2> edgeVertices;
    NativeList<float2x2> adjacentCellPositions;
    float2 centerPoint;
    
    VectorUtil vectorUtil;

    public NativeList<float2> Tessalate(NativeArray<BowyerWatson.Triangle> triangles, float3 point, UnityEngine.Color debugColor, out NativeArray<float2x2> adjacentPositions)
    {
        this.edgeVertices = new NativeList<float2>(Allocator.Temp);
        this.adjacentCellPositions = new NativeList<float2x2>(Allocator.Temp);
        this.centerPoint = new float2(point.x, (float)point.z);

        SortTrianglesClockwise(triangles, centerPoint);

        GatherCellEdgeVertices(triangles, centerPoint);
        
        DrawEdges(debugColor);//DEBUG
        //DrawAdjacent(debugColor);//DEBUG

        adjacentPositions = adjacentCellPositions;

        return edgeVertices;
    }

    void GatherCellEdgeVertices(NativeArray<BowyerWatson.Triangle> triangles, float2 centerPoint)
    {
        for(int t = 0; t < triangles.Length; t++)
        {
            BowyerWatson.Triangle triangle = triangles[t];

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
                edgeVertices.Add(triangle.circumcircle.center);
                adjacentCellPositions.Add(adjacentCellPair);
            }
        }
    }

    struct VertexRotation : System.IComparable<VertexRotation>
    {
        public readonly BowyerWatson.Triangle triangle;
        public readonly float degrees;

        public VertexRotation(BowyerWatson.Triangle triangle, float angle)
        {
            this.triangle = triangle;
            this.degrees = angle;
        }

        public int CompareTo(VertexRotation otherVertAngle)
        {
            return degrees.CompareTo(otherVertAngle.degrees);
        }
    }

    public NativeArray<BowyerWatson.Triangle> SortTrianglesClockwise(NativeArray<BowyerWatson.Triangle> triangles, float2 center)
    {
        NativeArray<VertexRotation> sorter = new NativeArray<VertexRotation>(triangles.Length, Allocator.Temp);
        for(int i = 0; i < triangles.Length; i++)
        {
            float rotationInDegrees = vectorUtil.RotationFromUp(triangles[i].circumcircle.center, center);
            sorter[i] = new VertexRotation(triangles[i], rotationInDegrees);
        }

        sorter.Sort();

        for(int i = 0; i < triangles.Length; i++)
            triangles[i] = sorter[i].triangle;

        return triangles;                
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
