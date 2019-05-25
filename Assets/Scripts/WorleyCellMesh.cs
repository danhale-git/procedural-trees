using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public struct WorleyCellMesh
{
    public TreeWorleyNoise worley;
    public int2 index;

    NativeList<Edge> edges;
    TreeWorleyNoise.CellData currentCell;

    NativeList<float3> vertices;

    public void Execute()
    {
        currentCell = worley.GetCellData(index, TreeManager.rootFrequency);

        GetEdges(currentCell.index, TreeManager.rootFrequency);

        RemoveSeparatedCells();
        DrawEdges();

        //DrawLines();
    }

    struct Edge
	{
        public float3 midPoint;
		public float3 edgeDirection;
		public TreeWorleyNoise.CellData adjacentCell;

        public float3 left;
        public float3 right;

        public Edge(TreeWorleyNoise.CellData currentCell, TreeWorleyNoise.CellData adjacentCell)
        {
			float3 offset = adjacentCell.position - currentCell.position;

            this.midPoint = currentCell.position + (offset * 0.5f);
            this.edgeDirection = math.cross(offset, new float3(0, 1, 0));
            this.adjacentCell = adjacentCell;
            this.left = midPoint + edgeDirection;
            this.right = midPoint - edgeDirection; 
        }
	}

    NativeList<Edge> GetEdges(int2 cellIndex, float2 frequency)
	{
		AdjacentIntOffsetsClockwise adjacentOffsets;

		edges = new NativeList<Edge>(Allocator.Temp);
		for(int i = 0; i < 8; i++)
		{
			TreeWorleyNoise.CellData adjacentCell = worley.GetCellData(adjacentOffsets[i] + currentCell.index, frequency);
			edges.Add(new Edge(currentCell, adjacentCell));
		}
        return edges;
	}

    void RemoveSeparatedCells()
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        NativeArray<Edge> edgesCopy = new NativeArray<Edge>(8, Allocator.Temp);
        edgesCopy.CopyFrom(edges);

		for(int i = 0; i < 8; i++)
		{
            int previousIndex = i > 0 ? i-1 : 7;
			int currentIndex = i;
			int nextIndex = i < 7 ? i+1 : 0;

			Edge nextEdge = edgesCopy[nextIndex];
			Edge edge = edgesCopy[currentIndex];
			Edge previousEdge = edgesCopy[previousIndex];

			bool leftIntersectionFound;
			float3 leftIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.left, previousEdge.midPoint, previousEdge.right, out leftIntersectionFound);

			bool rightIntersectionFound;
			float3 rightIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.right, nextEdge.midPoint, nextEdge.left, out rightIntersectionFound);

			if(leftIntersectionFound && rightIntersectionFound)
			{
                vertices.Add(leftIntersection);
            }
        }
    }

    void DrawEdges()
    {
        for(int i = 0; i < vertices.Length; i++)
        {
            int currentIndex = i;
			int nextIndex = i < vertices.Length-1 ? i+1 : 0;

            Draw(vertices[currentIndex], vertices[nextIndex], Color.red);
            Draw(currentCell.position, vertices[nextIndex], Color.white);
        }
    }

    /*void DrawLines()
    {
        int previousIndex = 7;
		for(int i = 0; i < 8; i++)
		{
			int currentIndex = i;
			int nextIndex = currentIndex < 7 ? currentIndex+1 : 0;

			Edge nextEdge = edges[nextIndex];

			Edge previousEdge = edges[previousIndex];

			Edge edge = edges[currentIndex];

			bool leftIntersectionFound;
			float3 leftIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.left, previousEdge.midPoint, previousEdge.right, out leftIntersectionFound);

			bool rightIntersectionFound;
			float3 rightIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.right, nextEdge.midPoint, nextEdge.left, out rightIntersectionFound);


			if(!rightIntersectionFound || !leftIntersectionFound)
				continue;

			previousIndex = currentIndex;

			//Draw(currentCell.position, leftIntersection, Color.yellow);
			
			//float colorFloat = (float)currentIndex / 8;
			float colorFloat = edge.adjacentCell.value;
			Color lineColor = new Color(colorFloat, colorFloat, colorFloat);

			Draw(rightIntersection, leftIntersection, Color.white);

			Draw(previousEdge.adjacentCell.position, leftIntersection, Color.green);

			Draw(nextEdge.adjacentCell.position, rightIntersection, Color.green);

			Draw(currentCell.position, leftIntersection, Color.red);
			
			//Draw(previousEdge.midPoint, leftIntersection, lineColor);
			//Draw(nextEdge.midPoint, rightIntersection, lineColor);
		}

		edges.Dispose();
    } */

	void Draw(float3 parentPosition, float3 childPosition, Color color)
    {
        Debug.DrawLine(parentPosition, childPosition, color, 1000);
    }

	public float3 GetIntersectionPointCoordinates(float3 A1, float3 A2, float3 B1, float3 B2, out bool found)
	{
		float tmp = (B2.x - B1.x) * (A2.z - A1.z) - (B2.z - B1.z) * (A2.x - A1.x);
	
		if(tmp == 0)
		{
			found = false;
			return float3.zero;
		}
	
		float mu = ((A1.x - B1.x) * (A2.z - A1.z) - (A1.z - B1.z) * (A2.x - A1.x)) / tmp;
	
		float3 point = new float3(
			B1.x + (B2.x - B1.x) * mu,
			0,
			B1.z + (B2.z - B1.z) * mu
		);

        float pointMagnitude = Magnitude(point - currentCell.position);

		float3 lineDirection = (math.normalize(A2 - A1));
		float3 pointDirection = (math.normalize(point - A1));

		float3 lineDirectionSign = math.sign(lineDirection);
		float3 pointDirectionSign = math.sign(pointDirection);

		if(!lineDirectionSign.Equals(pointDirectionSign))
		{
			found = false;
			return float3.zero;
		}
	
		found = true;
		return point;
	}

	float Magnitude(float3 o)
	{
		return math.sqrt(o.x*o.x + o.y*o.y + o.z*o.z);
	}
}
