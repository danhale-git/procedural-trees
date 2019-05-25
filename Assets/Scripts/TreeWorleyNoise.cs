using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public struct TreeWorleyNoise
{
	public enum DistanceFunction {Natural, Manhatten, Euclidean}
	public enum CellularReturnType {Distance2, Distance2Add, Distance2Sub, Distance2Mul, Distance2Div}

	public int seed;
	
	public float perterbAmp;
	public float cellularJitter;
	public DistanceFunction distanceFunction;
	public CellularReturnType cellularReturnType;
	
    CELL_2D cell_2D;
    const int X_PRIME = 1619;
	const int Y_PRIME = 31337;

	public void SetSeed(int newSeed)
	{
		seed = math.abs(newSeed);
	}

	public struct CellData
	{
		public int CompareTo(CellData other)
		{ return value.CompareTo(other.value); }

		public float value;
		public int2 index;
		public float3 position;
	}

	public void GetCellVertices(int2 cellIndex, float2 frequency)
	{
		CellData currentCell = GetCellData(cellIndex, frequency);

		AdjacentIntOffsetsClockwise adjacentOffsets;

		int initialIndex = 0;
		float smallestOffset = 999999;

		NativeList<Edge> edges = new NativeList<Edge>(Allocator.Temp);
		for(int i = 0; i < 8; i++)
		{
			CellData adjacentCell = GetCellData(adjacentOffsets[i] + currentCell.index, frequency);

			float3 offset = adjacentCell.position - currentCell.position;

			float3 midPoint = currentCell.position + (offset * 0.5f);

			/*TreeManager.CreateCube(midPoint + new float3(0,10,0), new float4(adjacentCell.value, adjacentCell.value, adjacentCell.value, 1));
			Debug.Log(midPoint);
			Debug.Log(adjacentCell.index);
			Debug.Log("---------------"); */

			float3 rightAngle = math.cross(offset, new float3(0, 1, 0));
			
			edges.Add(new Edge{
				midPoint = midPoint,
				edgeDirection = rightAngle,
				adjacentCell = adjacentCell
			});

			float offsetMagnitude =Magnitude(offset);
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

	struct Edge
	{
		public float3 midPoint;
		public float3 edgeDirection;
		public CellData adjacentCell;

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

	public CellData GetCellData(int2 cellIndex, float2 frequency)
    {
        float2 vec = cell_2D[Hash2D(seed, cellIndex.x, cellIndex.y) & 255];

        float cellX = cellIndex.x + vec.x * cellularJitter;
        float cellY = cellIndex.y + vec.y * cellularJitter;
		
		CellData cell = new CellData();

        cell.index = cellIndex;
        cell.position = new float3(cellX / frequency.x, 0, cellY / frequency.y);
		cell.value =  To01(ValCoord2D(seed, cellIndex.x, cellIndex.y));
		
		return cell;
    }

	public CellData GetCellData(float x, float y, float2 frequency)
	{
		CellData adjacentPlaceholder;
		float dist2EdgePlaceholder;
		CellData cell = GetWorleyData(x, y, frequency, out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);
		return cell;
	}
	public CellData GetCellData(float x, float y, float2 frequency, out float distanceToEdge)
	{
		CellData adjacentPlaceholder;
		CellData cell = GetWorleyData(x, y, frequency, out adjacentPlaceholder, out distanceToEdge, false, true);
		return cell;
	}
	public CellData GetCellData(float x, float y, float2 frequency, out CellData adjacent, out float distanceToEdge)
	{
		CellData cell = GetWorleyData(x, y, frequency, out adjacent, out distanceToEdge, true, true);
		return cell;
	}
	
	public CellData GetCellData(float3 position, float2 frequency)
	{
		CellData adjacentPlaceholder;
		float dist2EdgePlaceholder;
		CellData cell = GetWorleyData(position.x, position.z, frequency, out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);
		return cell;
	}
	public CellData GetCellData(float3 position, float2 frequency, out float distanceToEdge)
	{
		CellData adjacentPlaceholder;
		CellData cell = GetWorleyData(position.x, position.z, frequency, out adjacentPlaceholder, out distanceToEdge, false, true);
		return cell;
	}
	public CellData GetCellData(float3 position, float2 frequency, out CellData adjacent, out float distanceToEdge)
	{
		CellData cell = GetWorleyData(position.x, position.z, frequency, out adjacent, out distanceToEdge, true, true);
		return cell;
	}

	public CellData GetWorleyData(float x, float z, float2 frequency, out CellData adjacent, out float distanceToEdge, bool getAdjacent = true, bool getDistance = true)
	{
		if(perterbAmp > 0)SingleGradientPerturb(seed, perterbAmp, frequency, ref x, ref z);

		x *= frequency.x;
		z *= frequency.y;

		int xr = FastRound(x);
		int yr = FastRound(z);

		CellProcessing currentCell = new CellProcessing();
		CellProcessing adjacentCell = new CellProcessing();
		currentCell.distance = 999999;
		adjacentCell.distance = 999999;

		distanceToEdge = 999999;

		for (int newX = xr - 1; newX <= xr + 1; newX++)
			for (int newY = yr - 1; newY <= yr + 1; newY++)
			{
				float2 vec = cell_2D[Hash2D(seed, newX, newY) & 255];
				float vecX = newX - x + vec.x * cellularJitter;
				float vecY = newY - z + vec.y * cellularJitter;

				float cellX = newX + vec.x * cellularJitter;
				float cellY = newY + vec.y * cellularJitter;

				CellProcessing newCell = new CellProcessing();
				newCell.index = new int2(newX, newY);
				newCell.distance = ApplyDistanceFunction(vecX, vecY);
				newCell.position = new float3(cellX, 0, cellY);
				
				if(newCell.distance <= adjacentCell.distance)
				{
					if(newCell.distance >= currentCell.distance)
						adjacentCell = newCell;
					else
						adjacentCell = currentCell;
				}

				if(newCell.distance <= currentCell.distance)
					currentCell = newCell;

				if(getDistance)
				{
					float newDistanceToEdge = ApplyDistanceType(currentCell.distance, newCell.distance);
					if(newDistanceToEdge < distanceToEdge)
						distanceToEdge = newDistanceToEdge;
				}
			}

		CellData current = new CellData();
		adjacent = new CellData();

		current.index = currentCell.index;
		current.position = new float3(currentCell.position.x / frequency.x, 0, currentCell.position.z / frequency.y);
		current.value = To01(ValCoord2D(seed, currentCell.index.x, currentCell.index.y));

		if(getAdjacent)
		{
			adjacent.index = adjacentCell.index;
			adjacent.position = new float3(adjacentCell.position.x / frequency.x, 0, adjacentCell.position.z / frequency.y);
			adjacent.value = To01(ValCoord2D(seed, adjacentCell.index.x, adjacentCell.index.y));
		}

		return current;
	}

	struct CellProcessing
	{
		public float distance;
		public int2 index;
		public float3 position;
	}

	float ApplyDistanceType(float distance, float otherDistance)
	{
		switch (cellularReturnType)
		{
			case CellularReturnType.Distance2:
				return otherDistance;
			case CellularReturnType.Distance2Add:
				return otherDistance + distance;
			case CellularReturnType.Distance2Sub:
				return otherDistance - distance;
			case CellularReturnType.Distance2Mul:
				return otherDistance * distance;
			case CellularReturnType.Distance2Div:
				return distance / otherDistance;
			default:
				throw new System.Exception("Unrecognised cellular return type function");
		}
	}

	float ApplyDistanceFunction(float vecX, float vecY)
	{
		switch(distanceFunction)
		{
			case DistanceFunction.Natural:
				return (math.abs(vecX) + math.abs(vecY)) + (vecX * vecX + vecY * vecY);
			case DistanceFunction.Manhatten:
				return math.abs(vecX) + math.abs(vecY);
			case DistanceFunction.Euclidean:
				return vecX * vecX + vecY * vecY;
			default:
				throw new System.Exception("Unrecognised cellular distance function");
		}
	}

    float ValCoord2D(int seed, int x, int y)
	{
		int n = seed;
		n ^= X_PRIME * x;
		n ^= Y_PRIME * y;

		return (n * n * n * 60493) / (float)2147483648.0;
	}

	int Hash2D(int seed, int x, int y)
	{
		int hash = seed;
		hash ^= X_PRIME * x;
		hash ^= Y_PRIME * y;

		hash = hash * hash * hash * 60493;
		hash = (hash >> 13) ^ hash;

		return hash;
	}

    int FastRound(float f) { return (f >= 0) ? (int)(f + (float)0.5) : (int)(f - (float)0.5); }
	int FastFloor(float f) { return (f >= 0 ? (int)f : (int)f - 1); }

    float To01(float value)
	{
		return (value * 0.5f) + 0.5f;
	}

	void SingleGradientPerturb(int seed, float perturbAmp, float2 frequency, ref float x, ref float y)
	{
		float xf = x * frequency.x;
		float yf = y * frequency.y;

		int x0 = FastFloor(xf);
		int y0 = FastFloor(yf);
		int x1 = x0 + 1;
		int y1 = y0 + 1;

		float xs, ys;
		
		xs = InterpQuinticFunc(xf - x0);
		ys = InterpQuinticFunc(yf - y0);

		float2 vec0 = cell_2D[Hash2D(seed, x0, y0) & 255];
		float2 vec1 = cell_2D[Hash2D(seed, x1, y0) & 255];

		float lx0x = math.lerp(vec0.x, vec1.x, xs);
		float ly0x = math.lerp(vec0.y, vec1.y, xs);

		vec0 = cell_2D[Hash2D(seed, x0, y1) & 255];
		vec1 = cell_2D[Hash2D(seed, x1, y1) & 255];

		float lx1x = math.lerp(vec0.x, vec1.x, xs);
		float ly1x = math.lerp(vec0.y, vec1.y, xs);

		x += Lerp(lx0x, lx1x, ys) * perturbAmp;
		y += Lerp(ly0x, ly1x, ys) * perturbAmp;
	}
	private static float InterpQuinticFunc(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }
	float Lerp(float a, float b, float t) { return a + t * (b - a); }
}