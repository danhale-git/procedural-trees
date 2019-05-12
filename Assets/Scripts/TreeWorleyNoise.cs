using Unity.Mathematics;
using Unity.Collections;

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

	public CellData GetCellDataFromIndex(int2 cellIndex, float frequency)
    {
        float2 vec = cell_2D[Hash2D(seed, cellIndex.x, cellIndex.y) & 255];

        float cellX = cellIndex.x + vec.x * cellularJitter;
        float cellY = cellIndex.y + vec.y * cellularJitter;
		
		CellData cell = new CellData();

        cell.index = cellIndex;
        cell.position = new float3(cellX, 0, cellY) / frequency;
		cell.value =  To01(ValCoord2D(seed, cellIndex.x, cellIndex.y));
		
		return cell;
    }

	public CellData GetCellDataFromPosition(float x, float y, float frequency)
	{
		CellData adjacentPlaceholder;
		float dist2EdgePlaceholder;
		CellData cell = GetWorleyData(x, y, frequency, out adjacentPlaceholder, out dist2EdgePlaceholder, true, true);

		return cell;
	}
	public CellData GetCellDataFromPositionWithDist2Edge(float x, float y, float frequency, out float distanceToEdge)
	{
		CellData adjacentPlaceholder;
		CellData cell = GetWorleyData(x, y, frequency, out adjacentPlaceholder, out distanceToEdge, true, true);

		return cell;
	}

	public CellData GetWorleyData(float x, float z, float frequency, out CellData adjacent, out float distanceToEdge, bool getAdjacent = true, bool getDistance = true)
	{
		if(perterbAmp > 0)SingleGradientPerturb(seed, perterbAmp, frequency, ref x, ref z);

		x *= frequency;
		z *= frequency;

		int xr = FastRound(x);
		int yr = FastRound(z);

		float currentDistance = 999999;
		float adjacentDistance = 999999;

		int2 currentIndex = int2.zero;
		int2 adjacentIndex = int2.zero;

		float3 currentPosition = float3.zero;
		float3 adjacentPosition = float3.zero;

		CellData current = new CellData();
		adjacent = new CellData();
		distanceToEdge = 999999;

		for (int newX = xr - 1; newX <= xr + 1; newX++)
			for (int newY = yr - 1; newY <= yr + 1; newY++)
			{
				int2 newIndex = new int2(newX, newY);
				
				float2 vec = cell_2D[Hash2D(seed, newX, newY) & 255];
				float vecX = newX - x + vec.x * cellularJitter;
				float vecY = newY - z + vec.y * cellularJitter;
				float newDistance = ApplyDistanceFunction(vecX, vecY);

				float cellX = newX + vec.x * cellularJitter;
				float cellY = newY + vec.y * cellularJitter;
				float3 newPosition = new float3(cellX, 0, cellY);

				
				if(newDistance <= adjacentDistance)
				{
					if(newDistance >= currentDistance)
					{
						adjacentDistance 	= newDistance;
						adjacentIndex 		= newIndex;
						adjacentPosition 	= newPosition;
					}
					else
					{
						adjacentDistance 	= currentDistance;
						adjacentIndex 		= currentIndex;
						adjacentPosition 	= currentPosition;
					}
				}

				if(newDistance <= currentDistance)
				{
					currentDistance 	= newDistance;
					currentIndex 		= newIndex;
					currentPosition 	= newPosition;
				}

				

				if(getDistance)
				{
					float newDistanceToEdge = ApplyDistanceType(currentDistance, newDistance);
					if(newDistanceToEdge < distanceToEdge)
						distanceToEdge = newDistanceToEdge;
				}
			}

		current.index = currentIndex;
		current.position = currentPosition / frequency;
		current.value = To01(ValCoord2D(seed, currentIndex.x, currentIndex.y));

		if(getAdjacent)
		{
			adjacent.index = adjacentIndex;
			adjacent.position = adjacentPosition / frequency;
			adjacent.value = To01(ValCoord2D(seed, adjacentIndex.x, adjacentIndex.y));
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

	void SingleGradientPerturb(int seed, float perturbAmp, float frequency, ref float x, ref float y)
	{
		float xf = x * frequency;
		float yf = y * frequency;

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