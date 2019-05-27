using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

using System.Collections.Generic;

public struct WorleyCellMesh
{
    public TreeWorleyNoise worley;
    public int2 index;

    List<Edge> edges;
    TreeWorleyNoise.CellData currentCell;

	public List<Circumcircle> circles;

    NativeList<float3> vertices;

	float2 frequency;

    public void Execute()
    {
		frequency = TreeManager.rootFrequency;
		circles = new List<Circumcircle>();

        currentCell = worley.GetCellData(index, TreeManager.rootFrequency);

        GetEdges(currentCell.index, TreeManager.rootFrequency);

		AddVertices();

		TestCirc();

        DrawEdges();

        //RemoveSeparatedCells();
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
            this.edgeDirection = math.normalize(math.cross(offset, new float3(0, 1, 0)));
            this.adjacentCell = adjacentCell;
            this.left = midPoint + edgeDirection;
            this.right = midPoint - edgeDirection; 
        }
	}

	struct Intersection
	{
		public Edge edge;
		public float3 rightIntersection;
		public float3 leftIntersection;
	}

    List<Edge> GetEdges(int2 cellIndex, float2 frequency)
	{
		AdjacentIntOffsetsClockwise adjacentOffsets;

		edges = new List<Edge>();
		for(int i = 0; i < 8; i++)
		{
			TreeWorleyNoise.CellData adjacentCell = worley.GetCellData(adjacentOffsets[i] + currentCell.index, frequency);
			edges.Add(new Edge(currentCell, adjacentCell));
			TreeManager.CreateText(adjacentCell.position+ new float3(0,0.5f,0), adjacentCell.index.x+","+adjacentCell.index.y);
		}
        return edges;
	}

	void TestCirc()
	{
		int2 debugIndex = new int2(0,1);

		for(int i = 0; i < edges.Count; i++)
		{
			Edge edge = edges[i];
			Edge previousEdge = edges[WrapEdgeIndex(i - 1)];
			Edge nextEdge = edges[WrapEdgeIndex(i + 1)];

			bool debug = edge.adjacentCell.index.Equals(new int2(-1,0));

			if(debug)
			{
				/*for(int e = 0; e < edges.Count; e++)
				{
					bool rightFound;
					float3 rightIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.right, edges[e].midPoint, edges[e].left, out rightFound);

					if(!rightFound) continue;

					float colorVal = edges[e].adjacentCell.value;

					Circumcircle circle;

					GetCircumcircle(
						edge.adjacentCell.position,
						edges[e].adjacentCell.position,
						currentCell.position,
						new Color(colorVal, colorVal, colorVal),
						out circle
					);

					
				}  */

				/*Edge nextEdgeButOne = edges[WrapEdgeIndex(i + 2)];

				
				if(rightFound) Draw(rightIntersection, currentCell.position, Color.green);

				float colorVal = nextEdgeButOne.adjacentCell.value;
				Circumcircle circle;
				GetCircumcircle(
					edge.adjacentCell.position,
					nextEdgeButOne.adjacentCell.position,
					currentCell.position,
					new Color(colorVal, colorVal, colorVal),
					out circle
				); */

			}
		}

		/*if(!foundStart)
			throw new System.Exception("No eligible starting edge!");
		
		int currentIndex = startIndex;
		int checkedCount = 0;
		while(currentIndex < edges.Count)
		{
			Edge edge = edges[currentIndex];
			Draw(edge.left, edge.right, Color.green);
			currentIndex++;
		} */
	}

	void AddVertices()
	{
		vertices = new NativeList<float3>(Allocator.Temp);

		
		int currentIndex = 0;
		while(currentIndex < edges.Count)
		{
			Edge edge = edges[currentIndex];
			Debug.Log(edge.adjacentCell.index+" =============================");
			bool DEBUG = edge.adjacentCell.index.Equals(new int2(-1,0));

			int neighboursChecked = 0;
			int nextIndex = WrapEdgeIndex(currentIndex + 1);

			bool eligibleIntersectionFound = false;

			List<Intersection> intersections = new List<Intersection>();

			while(neighboursChecked <= 4)
			{
				Edge nextEdge = edges[nextIndex];
				bool rightIntersectionFound;
				float3 rightIntersection = GetIntersectionPointCoordinates(edge.midPoint, edge.right, nextEdge.midPoint, nextEdge.left, out rightIntersectionFound);
				
				neighboursChecked++;

				eligibleIntersectionFound = rightIntersectionFound;//

				if(!eligibleIntersectionFound)
				{
					nextIndex = WrapEdgeIndex(nextIndex+1);

					Debug.Log(nextEdge.adjacentCell.index+" skipped");
					if(DEBUG)
					{
						TreeManager.CreateCube(nextEdge.adjacentCell.position+ new float3(0,1,0), Color.red);
					}
				}
				else
				{
					vertices.Add(rightIntersection);

					//Debug.Log("Intersection: "+worley.DistanceToEdge(rightIntersection, currentCell.index, nextEdge.adjacentCell.index));
					//Debug.Log("Next Midpoint: "+worley.DistanceToEdge(nextEdge.midPoint, currentCell.index, nextEdge.adjacentCell.index));

					Debug.Log("found "+  nextEdge.adjacentCell.index+" after "+neighboursChecked);
					if(DEBUG)
					{
						Draw(currentCell.position, edge.midPoint, Color.blue);
						TreeManager.CreateCube(edge.adjacentCell.position+ new float3(0,1,0), Color.white);
						TreeManager.CreateCube(nextEdge.adjacentCell.position+ new float3(0,1,0), Color.green);
					}

					break;
				}

			}

			if(eligibleIntersectionFound)
					currentIndex += neighboursChecked;
			else
			{
				Debug.Log("nothing found for "+edge.adjacentCell.index);
				currentIndex += 1; 
			}
		}
	} 

	int WrapEdgeIndex(int index)
	{
		while(index >= edges.Count)
			index -= edges.Count;

		while(index < 0)
			index += edges.Count;

		return index;
	}

    void DrawEdges()
    {
        for(int i = 0; i < vertices.Length; i++)
        {
            int currentIndex = i;
			int nextIndex = i < vertices.Length-1 ? i+1 : 0;

            Draw(vertices[currentIndex], vertices[nextIndex], new Color(1, 0, 0, 0.5f));
            Draw(currentCell.position, vertices[nextIndex], new Color(1, 1, 1, 0.5f));
        }
    }

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

		if(IsBehindLine(A1, A2, point) || IsBehindLine(B1, B2, point))
		{
			found = false;
			return float3.zero;
		}
	
		found = true;
		return point;
	}

	public bool OutsideCell(float3 intersection)
	{
		float3 testPosition = intersection + math.normalize(currentCell.position - intersection);
		int2 cellIndex = worley.GetCellData(testPosition, TreeManager.rootFrequency).index;

		Draw(testPosition, currentCell.position, Color.green);

		return !currentCell.index.Equals(cellIndex);
	}

	bool IsBehindLine(float3 lineStart, float3 linePoint, float3 checkPoint)
	{
		float3 lineDirection = (math.normalize(linePoint - lineStart));
		float3 pointDirection = (math.normalize(checkPoint - lineStart));

		//float angle = Angle(lineStart, linePoint);
		//float angle2 = Angle(lineStart, checkPoint);

		float3 lineDirectionSign = math.sign(lineDirection);
		float3 pointDirectionSign = math.sign(pointDirection);

		//return math.abs(angle - angle2) > 0.01f;		
		return !lineDirectionSign.Equals(pointDirectionSign);
		//TODO: Maybe this can be done by calculating the circumcircle of both midpoints and the cell position? What's more efficient?
	}

	bool GetCircumcircle(float3 p0, float3 p1, float3 p2, Color color, out Circumcircle circle)
	{
		float dA, dB, dC, aux1, aux2, div;
	
		dA = p0.x * p0.x + p0.z * p0.z;
		dB = p1.x * p1.x + p1.z * p1.z;
		dC = p2.x * p2.x + p2.z * p2.z;
	
		aux1 = (dA*(p2.z - p1.z) + dB*(p0.z - p2.z) + dC*(p1.z - p0.z));
		aux2 = -(dA*(p2.x - p1.x) + dB*(p0.x - p2.x) + dC*(p1.x - p0.x));
		div = (2*(p0.x*(p2.z - p1.z) + p1.x*(p0.z-p2.z) + p2.x*(p1.z - p0.z)));
	
		circle = new Circumcircle();

		if(div == 0){ 
			return false;
		}

		float3 center = new float3(
			aux1/div,
			0,
			aux2/div
		);
	
		circle.center = center;
		circle.radius = math.sqrt((center.x - p0.x)*(center.x - p0.x) + (center.z - p0.z)*(center.z - p0.z));

		/*Draw(circle.center, circle.center+ new float3(circle.radius,0,0), color);
		Draw(circle.center, circle.center+ new float3(-circle.radius,0,0), color);
		Draw(circle.center, circle.center+ new float3(0,0,circle.radius), color);
		Draw(circle.center, circle.center+ new float3(0,0,-circle.radius), color); */

		circles.Add(circle);
	
		return true;
	}

	public struct Circumcircle
	{
		public float3 center;
		public float radius;
	}

	float Magnitude(float3 o)
	{
		return math.sqrt(o.x*o.x + o.y*o.y + o.z*o.z);
	}

	float Angle(float3 a, float3 b)
	{
		return math.atan2(b.z - a.z, b.x - a.x);
	}
}
