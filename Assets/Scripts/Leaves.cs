using Unity.Mathematics;
using Unity.Collections;

public struct Leaves
{
    NativeList<float3> vertices;
    NativeList<int> triangles;
    WorleyNoise.CellData parent;
    Random random;

    VectorUtil vectorUtil;

    public Leaves(NativeList<float3> vertices, NativeList<int> triangles, WorleyNoise.CellData parent, Random random)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.parent = parent;
        this.random = random;
    }

    public void Draw(NativeArray<float3> cellEdgeVertexPositions, float3 centerPosition, float height)
    {
        float drop = vectorUtil.FarthestDistance(cellEdgeVertexPositions, centerPosition) * 0.4f;

        float3 center = centerPosition + (drop * 0.5f);
        center.y += height;

        for(int i = 0; i < cellEdgeVertexPositions.Length; i++)
        {
            bool final = i == cellEdgeVertexPositions.Length-1;
            
            int currentEdge = i;
            int nextEdge = final ? 0 : i+1;

            float3 current = cellEdgeVertexPositions[currentEdge];
            float3 next = cellEdgeVertexPositions[nextEdge];
            current.y += height;
            next.y += height;

            float3 currentMid = vectorUtil.MidPoint(center, current, 0.6f);
            float3 nextMid = vectorUtil.MidPoint(center, next, 0.6f);
            float3 edge = vectorUtil.MidPoint(current, next);

            VertAndTri(current, drop);
            VertAndTri(edge, drop);
            VertAndTri(currentMid);
            
            VertAndTri(edge, drop);
            VertAndTri(next, drop);
            VertAndTri(nextMid);

            VertAndTri(currentMid);
            VertAndTri(edge, drop);
            VertAndTri(nextMid);

            VertAndTri(currentMid);
            VertAndTri(nextMid);
            VertAndTri(center);


            //Bottom 
            VertAndTri(current, drop*2);
            VertAndTri(edge, drop);
            VertAndTri(current, drop);
            
            VertAndTri(current, drop*2);
            VertAndTri(next, drop*2);
            VertAndTri(edge, drop);

            VertAndTri(next, drop*2);
            VertAndTri(next, drop);
            VertAndTri(edge, drop);
        }
    }

    void VertAndTri(float3 vert, float drop = 0)
    {
        vert.y -= drop;

        /*if(scale > 0)
        {
            vert.x *= scale;
            vert.z *= scale;
        } */

        vertices.Add(vert);
        triangles.Add(vertices.Length-1);
    }
}
