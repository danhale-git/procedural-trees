using Unity.Mathematics;
using Unity.Collections;

public struct Leaves
{
    NativeList<float3> vertices;
    NativeList<int> triangles;
    float3 offset;
    WorleyNoise.CellProfile cell;

    VectorUtil vectorUtil;

    float3 center;
    float height;

    const int minCornerAngle = 90;
    const int minSegmentAngle = 30;

    public Leaves(NativeList<float3> vertices, NativeList<int> triangles)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.offset = float3.zero;
        this.cell = new WorleyNoise.CellProfile();

        this.center = float3.zero;
        this.height = 0;
    }

    public void Draw(WorleyNoise.CellProfile cellProfile, float3 offset)
    {
        this.offset = offset;
        this.cell = cellProfile;

        RemoveSmallSegments();
        SoftenAcuteCorners();

        this.height = FarthestVertexDistance();

        this.center = vectorUtil.MeanPoint(this.cell.vertices);
        center.y += height * 1.2f;

        DrawCell();
    }

    void DrawCell()
    {
        const float dropHorizontalMultiplier = 1.3f;

        for(int i = 0; i < cell.vertices.Length; i++)
        {
            float3 currentEdge = cell.vertices[i];
            float3 nextEdge = cell.vertices[WrapVertIndex(i + 1)];

            float3 currentDrop = math.lerp(currentEdge, nextEdge, 0.5f) * dropHorizontalMultiplier;
            float3 nextDrop = math.lerp(nextEdge, cell.vertices[WrapVertIndex(i+2)], 0.5f) * dropHorizontalMultiplier;

            currentEdge.y += height;
            nextEdge.y += height;

            DrawTriangle(currentEdge, nextEdge, center);

            DrawTriangle(currentDrop, nextEdge, currentEdge);
            DrawTriangle(currentDrop, nextDrop, nextEdge);
        }
    }

    void RemoveSmallSegments()
    {
        var newVertices = new NativeList<float3>(Allocator.Temp);
        var newAdjacentCells = new NativeList<WorleyNoise.CellDataX2>(Allocator.Temp);

        bool smallSegmentFound = false;
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            float3 currentVertex = cell.GetVertex(i);
            float3 nextVertex = cell.GetVertex(i+1);

            float segmentAngle = vectorUtil.Angle(currentVertex, nextVertex);

            if(segmentAngle > minSegmentAngle)
            {
                newVertices.Add(currentVertex);
                newAdjacentCells.Add(cell.adjacentCells[i]);
            }
            else if(!smallSegmentFound)
                    smallSegmentFound = true;
        }

        if(smallSegmentFound)
        {
            cell.vertices = new NineValues<float3>(newVertices);
            cell.adjacentCells = new NineValues<WorleyNoise.CellDataX2>(newAdjacentCells);
        }
    }

    void SoftenAcuteCorners()
    {
        for(int i = 0; i < cell.vertices.Length; i++)
        {
            float3 previousVertex = cell.vertices[WrapVertIndex(i-1)];
            float3 currentVertex = cell.vertices[i];
            float3 nextVertex = cell.vertices[WrapVertIndex(i+1)];
            
            float3 directionToPrevious = previousVertex - currentVertex;
            float3 directionToNext = nextVertex - currentVertex;

            float angle = vectorUtil.Angle(directionToNext, directionToPrevious);

            if(angle < minCornerAngle)
            {
                float lengthMultiplier = math.unlerp(0, 90, angle);
                cell.vertices[i] = math.lerp(center, currentVertex, lengthMultiplier);
            }
        }
    }

    float FarthestVertexDistance()
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

    int WrapVertIndex(int index)
    {
        if(index >= cell.vertices.Length)
            return index - cell.vertices.Length;
        else if(index < 0)
            return index + cell.vertices.Length;
        else
            return index;
    }

    void DrawTriangle(float3 a, float3 b, float3 c)
    {
        VertAndTri(a);
        VertAndTri(b);
        VertAndTri(c);
    }

    void VertAndTri(float3 vert)
    {
        vertices.Add(vert + offset);
        triangles.Add(vertices.Length-1);
    }
}
