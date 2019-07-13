using Unity.Mathematics;
using Unity.Collections;

public struct Leaves
{
    NativeList<float3> vertices;
    NativeList<int> triangles;
    float3 offset;
    WorleyNoise.CellProfile cell;

    VectorUtil vectorUtil;
    SimplexNoise simplex;

    const int minSegmentAngle = 10;

    public Leaves(NativeList<float3> vertices, NativeList<int> triangles, int seed)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.offset = float3.zero;
        this.cell = new WorleyNoise.CellProfile();
        this.simplex = new SimplexNoise(seed, 0.5f, negative: true);
    }

    public void Draw(WorleyNoise.CellProfile cell, float3 offset)
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
    } 

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

    int NextVertIndex(int index)
    {
        return index >= cell.vertices.Length-1 ? index - (cell.vertices.Length-1) : index+1; 
    }

    void VertAndTri(float3 vert)
    {
        float jitter = simplex.GetSimplex(vert.x + cell.data.position.x, vert.z + cell.data.position.z);
        //vert += jitter * 0.5f;

        vertices.Add(vert + offset);
        triangles.Add(vertices.Length-1);
    }
}
