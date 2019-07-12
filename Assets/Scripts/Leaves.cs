using Unity.Mathematics;
using Unity.Collections;

public struct Leaves
{
    NativeList<float3> vertices;
    NativeList<int> triangles;
    WorleyNoise.CellProfile cell;
    Random random;

    VectorUtil vectorUtil;

    const int minSegmentAngle = 10;

    public Leaves(NativeList<float3> vertices, NativeList<int> triangles, Random random)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.random = random;
        cell = new WorleyNoise.CellProfile();
    }

    public void Draw(WorleyNoise.CellProfile cell)
    {
        float3 center = vectorUtil.MeanPoint(cell.vertices);

        this.cell = cell;

        //int vertexGroupSize = cell.vertices.Length / 3;
        //int remainder = cell.vertices.Length % 3;

        


        for(int i = 0; i < cell.vertices.Length; i++)
        {
            int next = i == cell.vertices.Length-1 ? 0 : i+1;

            bool skipNext = SegmentIsThin(i);
            if(skipNext) continue;

            float3 currentEdge = cell.vertices[i];
            float3 nextEdge = cell.vertices[next];

            float3 currentMid = vectorUtil.MidPoint(center, currentEdge, 0.6f);
            float3 nextMid = vectorUtil.MidPoint(center, nextEdge, 0.6f);
            float3 edgeMid = vectorUtil.MidPoint(currentEdge, nextEdge);

            VertAndTri(cell.vertices[i]);
            VertAndTri(cell.vertices[next]);
            VertAndTri(center);
        }
    }

    bool SegmentIsThin(int index)
    {
        int next = NextVertIndex(index);
        float3 vertA = cell.vertices[index];
        float3 vertB = cell.vertices[next];
        return vectorUtil.Angle(vertA-cell.data.position, vertB-cell.data.position) < minSegmentAngle;
    }

    int NextVertIndex(int index)
    {
        return index == cell.vertices.Length-1 ? 0 : index+1; 
    }

    void VertAndTri(float3 vert)
    {
        vertices.Add(vert);
        triangles.Add(vertices.Length-1);
    }
}
