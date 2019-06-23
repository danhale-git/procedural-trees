using Unity.Collections;
using Unity.Mathematics;

public struct DirichletTessellation
{
    NativeList<float2> edgeVertices;
    float2 centerPoint;
    
    VectorUtil vectorUtil;

    public NativeList<float2> Tessalate(NativeArray<float2x4> triangles, float3 point, UnityEngine.Color debugColor)
    {
        this.edgeVertices = new NativeList<float2>(Allocator.TempJob);
        this.centerPoint = new float2(point.x, (float)point.z);

        for(int i = 0; i < triangles.Length; i++)
            GatherCellEdgeVertices(triangles[i], centerPoint);
        
        vectorUtil.SortVerticesClockwise(edgeVertices, centerPoint);
        RemoveDuplicateVertices();

        DrawEdges(debugColor);//DEBUG

        return edgeVertices;
    }

    void GatherCellEdgeVertices(float2x4 triangle, float2 centerPoint)
    {
        bool triangleInCell = false;
        int notAtEdge = 0;

        for(int i = 0; i < 3; i++)
            if(triangle[i].Equals(centerPoint))
            {
                triangleInCell = true;
                notAtEdge = i;
            }

        if(!triangleInCell)
            return;

        float2 circumcenter = triangle[3];
        for(int i = 0; i < 3; i++)
            if(i != notAtEdge)
                edgeVertices.Add(new float2(circumcenter));
    }

    void RemoveDuplicateVertices()
    {
        NativeArray<float2> edgeVerticesCopy = new NativeArray<float2>(edgeVertices.Length, Allocator.Temp);
        edgeVerticesCopy.CopyFrom(edgeVertices);

        edgeVertices.Clear();

        for(int i = 0; i < edgeVerticesCopy.Length;i += 2)
            edgeVertices.Add(edgeVerticesCopy[i]);
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
    //DEBUG
}
