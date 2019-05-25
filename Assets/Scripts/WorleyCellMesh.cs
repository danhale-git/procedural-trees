﻿using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public struct WorleyCellMesh
{
    public TreeWorleyNoise worley;
    public int2 index;

    NativeList<Edge> edges;

    public void Execute()
    {
        TreeWorleyNoise.CellData cell = worley.GetCellData(index, TreeManager.rootFrequency);
        TreeManager.CreateCube(cell.position + new float3(0,10,0), new float4(1));

        GetEdges(cell.index, TreeManager.rootFrequency);
    }

    struct Edge
	{
		public float3 midPoint;
		public float3 edgeDirection;
		public TreeWorleyNoise.CellData adjacentCell;

		public float3 left{
			get{
				return midPoint + edgeDirection;
			}
		}
		public float3 right{
			get{
				return midPoint - edgeDirection;
			}
		} 
	}

    public void GetEdges(int2 cellIndex, float2 frequency)
	{
		TreeWorleyNoise.CellData currentCell = worley.GetCellData(cellIndex, frequency);

		AdjacentIntOffsetsClockwise adjacentOffsets;

		int initialIndex = 0;
		float smallestOffset = 999999;

		edges = new NativeList<Edge>(Allocator.Temp);
		for(int i = 0; i < 8; i++)
		{
			TreeWorleyNoise.CellData adjacentCell = worley.GetCellData(adjacentOffsets[i] + currentCell.index, frequency);

			float3 o = adjacentCell.position - currentCell.position;

			float3 midPoint = currentCell.position + (o * 0.5f);

			float3 rightAngle = math.cross(o, new float3(0, 1, 0));
			
			edges.Add(new Edge{
				midPoint = midPoint,
				edgeDirection = rightAngle,
				adjacentCell = adjacentCell
			});

			float offsetMagnitude = math.sqrt(o.x*o.x + o.y*o.y + o.z*o.z);
			if(offsetMagnitude < smallestOffset)
			{
				smallestOffset = offsetMagnitude;
				initialIndex = i;
			}
		}

		int previousIndex = initialIndex;
		int startIndex = initialIndex < edges.Length-1 ? initialIndex+1 : 0;

		TreeManager.CreateCube(edges[initialIndex].adjacentCell.position, new float4(1, 0, 0, 1));

		for(int i = startIndex; i < edges.Length+startIndex; i++)
		{
			int currentIndex = i < edges.Length ? i : i-edges.Length;
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

			previousEdge = edge;
		}

		edges.Dispose();

	}

	void Draw(float3 parentPosition, float3 childPosition, Color color)
    {
        Debug.DrawLine(parentPosition, childPosition, color, 1000);
    }

	public float3 GetIntersectionPointCoordinates(float3 A1, float3 A2, float3 B1, float3 B2, out bool found)
	{
		/*	A & B: the two lines,
			A_1, B_1: the arbitrary starting points of the two lines,
			A_2, B_2: the arbitrary points which tells the direction of the two lines,
			X: the intersection point,
			O: the origin point. */

		float tmp = (B2.x - B1.x) * (A2.z - A1.z) - (B2.z - B1.z) * (A2.x - A1.x);
	
		if (tmp == 0)
		{
			Debug.Log("skipped a line");
			// No solution!
			found = false;
			return float3.zero;
		}
	
		float mu = ((A1.x - B1.x) * (A2.z - A1.z) - (A1.z - B1.z) * (A2.x - A1.x)) / tmp;
	

		float3 point = new float3(
			B1.x + (B2.x - B1.x) * mu,
			0,
			B1.z + (B2.z - B1.z) * mu
		);

		float3 lineDirection = (math.normalize(A2 - A1));
		float3 pointDirection = (math.normalize(point - A1));

		float3 lineDirectionSign = math.sign(lineDirection);
		float3 pointDirectionSign = math.sign(pointDirection);

		if (!lineDirectionSign.Equals(pointDirectionSign))
		{
			Debug.Log("skipped a line");
			// No solution!
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
