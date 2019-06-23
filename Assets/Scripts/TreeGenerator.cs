using Unity.Mathematics;
using Unity.Collections;

public struct TreeGenerator
{
    public Random random;
    public WorleyNoise worley;

    BowyerWatsonTriangulation triangulation;
    DirichletTessellation tessellation;

    public void Generate(int2 cellIndex)
    {
        Tree(cellIndex);
    }

    void Tree(int2 cellIndex)
    {
        var points = new NativeList<float2>(Allocator.Persistent);

        float3 cellPosition = float3.zero;

        for(int x = -1; x < 2; x++)
            for(int z = -1; z < 2; z++)
            {
                int2 index = new int2(x, z) + cellIndex;
                float3 position = worley.GetCellData(index).position;
                points.Add(new float2(position.x, position.z));

                if(index.Equals(cellIndex))
                    cellPosition = position;
            }

        NativeArray<float2x4> delaunay = triangulation.Triangulate(points);

        NativeList<float2> edgeVertices = tessellation.Tessalate(delaunay, cellPosition, UnityEngine.Color.red);

        delaunay.Dispose();
        edgeVertices.Dispose();
    }

    //  Draw cell mesh
    //  Draw tree trunk with one edge per cell edge
    //  Draw leaf cells
}