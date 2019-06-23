using Unity.Collections;
using Unity.Mathematics;

public class DirichletTessellation
{
    NativeList<float2> edgeVertices;
    float2 centerPoint;
    
    VectorUtil vectorUtil;

    public void Tessalate(NativeArray<float2x4> triangles, float3 point)
    {
        this.edgeVertices = new NativeList<float2>(Allocator.TempJob);
        this.centerPoint = new float2(point.x, (float)point.z);

        for(int i = 0; i < triangles.Length; i++)
            GatherCellEdgeVertices(triangles[i], centerPoint);
        
        SortVerticesClockwise(centerPoint);
        RemoveDuplicateVertices();

        DrawEdges();//DEBUG

        edgeVertices.Dispose();
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

    struct VertexAngle : System.IComparable<VertexAngle>
    {
        public readonly float2 vertex;
        public readonly float angle;

        public VertexAngle(float2 vertex, float angle)
        {
            this.vertex = vertex;
            this.angle = angle;
        }

        public int CompareTo(VertexAngle otherVertAngle)
        {
            return angle.CompareTo(otherVertAngle.angle);
        }
    }

    NativeArray<float2> SortVerticesClockwise(float2 center)
    {
        VectorUtil vectorUtil;

        NativeArray<VertexAngle> sorter = new NativeArray<VertexAngle>(edgeVertices.Length, Allocator.Temp);
        for(int i = 0; i < edgeVertices.Length; i++)
        {
            float angle = vectorUtil.RotationFromUp(edgeVertices[i], center);
            sorter[i] = new VertexAngle(edgeVertices[i], angle);
        }

        sorter.Sort();

        for(int i = 0; i < edgeVertices.Length; i++)
            edgeVertices[i] = sorter[i].vertex;

        return edgeVertices;                
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

    void DrawEdges()
    {
        for(int i = 0; i < edgeVertices.Length; i++)//DEBUG
        {
            int nextIndex = i == edgeVertices.Length-1 ? 0 : i+1;
            DrawLineFloat2(edgeVertices[i], edgeVertices[nextIndex], UnityEngine.Color.green);
        }//DEBUG
    }
    //DEBUG
}
