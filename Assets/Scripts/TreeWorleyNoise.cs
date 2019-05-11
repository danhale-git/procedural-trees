﻿using Unity.Mathematics;
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

	public struct PointData
    {
		public int CompareTo(PointData other)
		{ return currentCellValue.CompareTo(other.currentCellValue); }

		public float distance2Edge, distance;

		public float3 currentCellPosition, adjacentCellPosition;
		public int2 currentCellIndex, adjacentCellIndex;
		public float currentCellValue, adjacentCellValue;
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
		if(perterbAmp > 0)SingleGradientPerturb(seed, perterbAmp, frequency, ref x, ref y);

		x *= frequency;
		y *= frequency;

		int xr = FastRound(x);
		int yr = FastRound(y);

		float distance0 = 999999;
		float distance1 = 999999;

		//	Store distance1 index
		int xc1 = 0, yc1 = 0;

		//	Store distance0 index in case it is assigned to distance1 later
		int xc0 = 0, yc0 = 0;

		float3 currentCellPosition = float3.zero;
		int2 currentCellIndex = int2.zero;

		float distance = 999999;

		for (int xi = xr - 1; xi <= xr + 1; xi++)
				{
					for (int yi = yr - 1; yi <= yr + 1; yi++)
					{
						float2 vec = cell_2D[Hash2D(seed, xi, yi) & 255];

						float vecX = xi - x + vec.x * cellularJitter;
						float vecY = yi - y + vec.y * cellularJitter;

						float cellX = xi + vec.x * cellularJitter;
						float cellY = yi + vec.y * cellularJitter;

						float newDistance;

						switch(distanceFunction)
						{
							case DistanceFunction.Natural:
								newDistance = (math.abs(vecX) + math.abs(vecY)) + (vecX * vecX + vecY * vecY);
								break;
							case DistanceFunction.Manhatten:
								newDistance = math.abs(vecX) + math.abs(vecY);
								break;
							case DistanceFunction.Euclidean:
								newDistance = newDistance = vecX * vecX + vecY * vecY;
								break;
							default:
								newDistance = 0;
								throw new System.Exception("Unrecognised cellular distance function");
						}
						
						if(newDistance < distance)
						{
							distance = newDistance;
						}

						if(newDistance <= distance1)
						{
							if(newDistance >= distance0)
							{
								distance1 = newDistance;
								xc1 = xi;
								yc1 = yi;
							}
							else
							{
								distance1 = distance0;
								xc1 = xc0;
								yc1 = yc0;
							}
						}

						if(newDistance <= distance0)
						{
							distance0 = newDistance;
							xc0 = xi;
							yc0 = yi;

							currentCellPosition = new float3(cellX, 0, cellY) / frequency;
							currentCellIndex = new int2(xi, yi);
						}			
					}
				}

		float currentCellValue = To01(ValCoord2D(seed, xc0, yc0));
		
		CellData cell = new CellData();

        cell.index = currentCellIndex;
        cell.position = currentCellPosition;
		cell.value =  currentCellValue;

		return cell;
	}

	public CellData GetWorleyData(float x, float z, float frequency, out CellData adjacent, out float distance2Edge, bool getAdjacent = false)
	{
		if(perterbAmp > 0)SingleGradientPerturb(seed, perterbAmp, frequency, ref x, ref z);

		x *= frequency;
		z *= frequency;

		int xr = FastRound(x);
		int yr = FastRound(z);

		float distance0 = 999999;
		float distance1 = 999999;

		//	Store distance1 index
		int xc1 = 0, yc1 = 0;

		//	Store distance0 index in case it is assigned to distance1 later
		int xc0 = 0, yc0 = 0;

		//	All adjacent cell indices and distances
		NineInts otherX = new NineInts();
		NineInts otherY = new NineInts();

		NineFloats otherCellX = new NineFloats();
		NineFloats otherCellY = new NineFloats();

		NineFloats otherDistance = new NineFloats();
		for(int i = 0; i < 9; i++)
		{
			otherDistance[i] = 999999;
		}

		int indexCount = 0;

		float3 currentCellPosition = float3.zero;
		int2 currentCellIndex = int2.zero;

		float distance = 999999;

		for (int xi = xr - 1; xi <= xr + 1; xi++)
				{
					for (int yi = yr - 1; yi <= yr + 1; yi++)
					{
						float2 vec = cell_2D[Hash2D(seed, xi, yi) & 255];

						float vecX = xi - x + vec.x * cellularJitter;
						float vecY = yi - z + vec.y * cellularJitter;

						float cellX = xi + vec.x * cellularJitter;
						float cellY = yi + vec.y * cellularJitter;

						float newDistance;

						switch(distanceFunction)
						{
							case DistanceFunction.Natural:
								newDistance = (math.abs(vecX) + math.abs(vecY)) + (vecX * vecX + vecY * vecY);
								break;
							case DistanceFunction.Manhatten:
								newDistance = math.abs(vecX) + math.abs(vecY);
								break;
							case DistanceFunction.Euclidean:
								newDistance = newDistance = vecX * vecX + vecY * vecY;
								break;
							default:
								newDistance = 0;
								throw new System.Exception("Unrecognised cellular distance function");
						}
						
						if(newDistance < distance)
						{
							distance = newDistance;
						}

						if(newDistance <= distance1)
						{
							if(newDistance >= distance0)
							{
								distance1 = newDistance;
								xc1 = xi;
								yc1 = yi;
							}
							else
							{
								distance1 = distance0;
								xc1 = xc0;
								yc1 = yc0;
							}
						}

						if(newDistance <= distance0)
						{
							distance0 = newDistance;
							xc0 = xi;
							yc0 = yi;

							currentCellPosition = new float3(cellX, 0, cellY) / frequency;
							currentCellIndex = new int2(xi, yi);
						}

						if(getAdjacent)
						{
							//	Store all adjacent cells
							otherCellX[indexCount] = cellX;
							otherCellY[indexCount] = cellY;
							otherX[indexCount] = xi;
							otherY[indexCount] = yi;
							otherDistance[indexCount] = newDistance;
							indexCount++;
						}
					}
				}

		float currentCellValue = To01(ValCoord2D(seed, xc0, yc0));

		CellData cell = new CellData();

        cell.index = currentCellIndex;
        cell.position = currentCellPosition;
		cell.value =  currentCellValue;

		distance2Edge = 999999;
		adjacent = new CellData();
		
		if(getAdjacent)
		{
			float adjacentCellValue = 0;
			int2 adjacentCellIndex = int2.zero;
			float3 adjacentCellPosition = float3.zero;

			for(int i = 0; i < 9; i++)
			{	
				float dist2Edge = ApplyDistanceType(distance0, otherDistance[i]);
				if(dist2Edge < distance2Edge)
				{
					int2 otherCellIndex = new int2(otherX[i], otherY[i]);
					distance2Edge = dist2Edge;
					adjacentCellValue = To01(ValCoord2D(seed, otherX[i], otherY[i]));
					adjacentCellIndex = otherCellIndex;
					adjacentCellPosition = new float3(otherCellX[i], 0, otherCellY[i]) / frequency;
				}
			}
			if(distance2Edge == 999999) distance2Edge = 0;
			
			adjacent.value = adjacentCellValue;
			adjacent.position = adjacentCellPosition;
			adjacent.index = adjacentCellIndex;
		}

		return cell;
	}

    public PointData GetPointDataFromPosition(float x, float z, float frequency)
	{
		if(perterbAmp > 0)SingleGradientPerturb(seed, perterbAmp, frequency, ref x, ref z);

		x *= frequency;
		z *= frequency;

		int xr = FastRound(x);
		int yr = FastRound(z);

		float distance0 = 999999;
		float distance1 = 999999;

		//	Store distance1 index
		int xc1 = 0, yc1 = 0;

		//	Store distance0 index in case it is assigned to distance1 later
		int xc0 = 0, yc0 = 0;

		//	All adjacent cell indices and distances
		NineInts otherX = new NineInts();
		NineInts otherY = new NineInts();

		NineFloats otherCellX = new NineFloats();
		NineFloats otherCellY = new NineFloats();

		NineFloats otherDistance = new NineFloats();
		for(int i = 0; i < 9; i++)
		{
			otherDistance[i] = 999999;
		}

		int indexCount = 0;

		float3 currentCellPosition = float3.zero;
		int2 currentCellIndex = int2.zero;

		float distance = 999999;

		for (int xi = xr - 1; xi <= xr + 1; xi++)
				{
					for (int yi = yr - 1; yi <= yr + 1; yi++)
					{
						float2 vec = cell_2D[Hash2D(seed, xi, yi) & 255];

						float vecX = xi - x + vec.x * cellularJitter;
						float vecY = yi - z + vec.y * cellularJitter;

						float cellX = xi + vec.x * cellularJitter;
						float cellY = yi + vec.y * cellularJitter;

						float newDistance;

						switch(distanceFunction)
						{
							case DistanceFunction.Natural:
								newDistance = (math.abs(vecX) + math.abs(vecY)) + (vecX * vecX + vecY * vecY);
								break;
							case DistanceFunction.Manhatten:
								newDistance = math.abs(vecX) + math.abs(vecY);
								break;
							case DistanceFunction.Euclidean:
								newDistance = newDistance = vecX * vecX + vecY * vecY;
								break;
							default:
								newDistance = 0;
								throw new System.Exception("Unrecognised cellular distance function");
						}
						
						if(newDistance < distance)
						{
							distance = newDistance;
						}

						if(newDistance <= distance1)
						{
							if(newDistance >= distance0)
							{
								distance1 = newDistance;
								xc1 = xi;
								yc1 = yi;
							}
							else
							{
								distance1 = distance0;
								xc1 = xc0;
								yc1 = yc0;
							}
						}

						if(newDistance <= distance0)
						{
							distance0 = newDistance;
							xc0 = xi;
							yc0 = yi;

							currentCellPosition = new float3(cellX, 0, cellY) / frequency;
							currentCellIndex = new int2(xi, yi);
						}

						//	Store all adjacent cells
						otherCellX[indexCount] = cellX;
						otherCellY[indexCount] = cellY;
						otherX[indexCount] = xi;
						otherY[indexCount] = yi;
						otherDistance[indexCount] = newDistance;
						indexCount++;			
					}
				}

		float currentCellValue = To01(ValCoord2D(seed, xc0, yc0));

		float distance2Edge = 999999;
		float adjacentCellValue = 0;
		int2 adjacentCellIndex = int2.zero;
		float3 adjacentCellPosition = float3.zero;

		for(int i = 0; i < 9; i++)
		{	
			float dist2Edge = ApplyDistanceType(distance0, otherDistance[i]);
			if(dist2Edge < distance2Edge)
			{
				int2 otherCellIndex = new int2(otherX[i], otherY[i]);
                distance2Edge = dist2Edge;
                adjacentCellValue = To01(ValCoord2D(seed, otherX[i], otherY[i]));
                adjacentCellIndex = otherCellIndex;
                adjacentCellPosition = new float3(otherCellX[i], 0, otherCellY[i]) / frequency;
			}
		}
		if(distance2Edge == 999999) distance2Edge = 0;
		
		PointData cell = new PointData();
		
		cell.distance2Edge = distance2Edge;
		cell.distance = distance;

		cell.currentCellValue = currentCellValue;
		cell.adjacentCellValue = adjacentCellValue;

		cell.currentCellPosition = currentCellPosition;
		cell.adjacentCellPosition = adjacentCellPosition;

		cell.currentCellIndex = currentCellIndex;
		cell.adjacentCellIndex = adjacentCellIndex;

		return cell;
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

	struct NineInts
	{
		int _0;
		int _1;
		int _2;
		int _3;
		int _4;
		int _5;
		int _6;
		int _7;
		int _8;
		
		public int this[int index]
		{
			get
			{
				switch(index)
				{
					case 0: return _0;
					case 1: return _1;
					case 2: return _2;
					case 3: return _3;
					case 4: return _4;
					case 5: return _5;
					case 6: return _6;
					case 7: return _7;
					case 8: return _8;

					default: throw new System.IndexOutOfRangeException();
				}
			}

			set
			{
				switch(index)
				{
					case 0: _0 = value; break;
					case 1: _1 = value; break;
					case 2: _2 = value; break;
					case 3: _3 = value; break;
					case 4: _4 = value; break;
					case 5: _5 = value; break;
					case 6: _6 = value; break;
					case 7: _7 = value; break;
					case 8: _8 = value; break;

					default: throw new System.IndexOutOfRangeException();
				}
			}
		}
	}

	struct NineFloats
	{
		float _0;
		float _1;
		float _2;
		float _3;
		float _4;
		float _5;
		float _6;
		float _7;
		float _8;
		
		public float this[int index]
		{
			get
			{
				switch(index)
				{
					case 0: return _0;
					case 1: return _1;
					case 2: return _2;
					case 3: return _3;
					case 4: return _4;
					case 5: return _5;
					case 6: return _6;
					case 7: return _7;
					case 8: return _8;

					default: throw new System.IndexOutOfRangeException();
				}
			}

			set
			{
				switch(index)
				{
					case 0: _0 = value; break;
					case 1: _1 = value; break;
					case 2: _2 = value; break;
					case 3: _3 = value; break;
					case 4: _4 = value; break;
					case 5: _5 = value; break;
					case 6: _6 = value; break;
					case 7: _7 = value; break;
					case 8: _8 = value; break;

					default: throw new System.IndexOutOfRangeException();
				}
			}
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