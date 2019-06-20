using Unity.Collections;
using Unity.Mathematics;

public class DirichletTessellation
{
    NativeList<ClockwiseVertex> edgeVertices;

    public void Tessalate(NativeArray<float2x4> triangles, float3 cellPosition)
    {
        edgeVertices = new NativeList<ClockwiseVertex>(Allocator.TempJob);

        for(int i = 0; i < triangles.Length; i++)
        {
            float2x4 triangle = triangles[i];
            GatherCellVertices(triangle, cellPosition);
        }

        SortEdgeVerticesClockwise();
        RemoveDuplicateVertices();

        for(int i = 0; i < edgeVertices.Length; i++)
        {
            int nextIndex = i == edgeVertices.Length-1 ? 0 : i+1;
            DrawLineFloat2(edgeVertices[i].vertex, edgeVertices[nextIndex].vertex, UnityEngine.Color.green);//DEBUG
        }

        edgeVertices.Dispose();
    }

    void GatherCellVertices(float2x4 triangle, float3 pos)
    {
        float2 cellPosition = new float2(pos.x, pos.z);

        bool triangleInCell = false;
        int cellPositionIndex = 0;

        for(int i = 0; i < 3; i++)
            if(triangle[i].Equals(cellPosition))
            {
                triangleInCell = true;
                cellPositionIndex = i;
            }

        if(!triangleInCell)
            return;

        float2 circumcenter = triangle[3];
        for(int i = 0; i < 3; i++)
            if(i != cellPositionIndex)
            {
                ClockwiseVertex vertex = new ClockwiseVertex(
                    circumcenter,
                    cellPosition
                );

                edgeVertices.Add(vertex);
            }
    }

    void SortEdgeVerticesClockwise()
    {
        NativeArray<ClockwiseVertex> sortedVertices = new NativeArray<ClockwiseVertex>(edgeVertices.Length, Allocator.Temp);
        sortedVertices.CopyFrom(edgeVertices);
        sortedVertices.Sort();
        sortedVertices.CopyTo(edgeVertices);
    }

    void RemoveDuplicateVertices()
    {
        NativeArray<ClockwiseVertex> edgeVerticesCopy = new NativeArray<ClockwiseVertex>(edgeVertices.Length, Allocator.Temp);
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
    //DEBUG
}
