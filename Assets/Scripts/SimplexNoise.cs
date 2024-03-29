﻿using Unity.Mathematics;
using Unity.Collections;

public struct SimplexNoise
{
    public enum FractalType { FBM, Billow, RigidMulti };

    GRAD_2D grad_2D;

    int X_PRIME;
    int Y_PRIME;

    const float F2 = (float)(1.0 / 2.0);
    const float G2 = (float)(1.0 / 4.0);

    int seed;
    float frequency;

    public bool fractal;
    public bool negative;

    float lacunarity;
    float fractalBounding;
    FractalType fractalType;
    int octaves;
    float gain;

    struct GRAD_2D
    {
        public float2 this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0: return new float2(-1,-1); 
                    case 1: return new float2( 1,-1); 
                    case 2: return new float2(-1, 1); 
                    case 3: return new float2( 1, 1);
                    case 4: return new float2( 0,-1); 
                    case 5: return new float2(-1, 0); 
                    case 6: return new float2( 0, 1); 
                    case 7: return new float2( 1, 0);
                    default: throw new System.IndexOutOfRangeException();
                }
            }
        }
    }

    public SimplexNoise(int seed, float frequency, float lacunarity, FractalType fractalType, int octaves, float gain, bool negative = false)
    {
        this.seed = seed;
        this.frequency = frequency;

        this.lacunarity = lacunarity;
        this.fractalBounding = 0;
        this.fractalType = fractalType;
        this.octaves = octaves;
        this.gain = gain;
        this.negative = negative;

        fractal = true;

        grad_2D = new GRAD_2D();

        X_PRIME = 1619;
        Y_PRIME = 31337;

        CalculateFractalBounding();
    }

    void CalculateFractalBounding()
	{
		float amp = gain;
		float ampFractal = 1;
		for (int i = 1; i < octaves; i++)
		{
			ampFractal += amp;
			amp *= gain;
		}
		fractalBounding = 1 / ampFractal;
	}

    public SimplexNoise(int seed, float frequency, bool negative = false)
    {
        this.seed = seed;
        this.frequency = frequency;
        this.negative = negative;

        lacunarity = 0;
        fractalBounding = 0;
        fractalType = 0;
        octaves = 0;
        gain = 0;

        fractal = false;

        grad_2D = new GRAD_2D();

        X_PRIME = 1619;
        Y_PRIME = 31337;
    }


    public float GetSimplex(float x, float y, float newFrequency)
    {
        float oldFrequency = frequency;
        frequency = newFrequency;

        float result = GetSimplex(x, y);

        frequency = oldFrequency;
        return result;
    }
        
    public float GetSimplex(float x, float y)
    {
        if(fractal)
            return GetSimplexFractal(x, y);
        else
            return SingleSimplex(seed, x * frequency, y * frequency);
    }

    float GetSimplexFractal(float x, float y)
	{
		x *= frequency;
		y *= frequency;

		switch (fractalType)
		{
			case FractalType.FBM:
				return SingleSimplexFractalFBM(x, y);
			case FractalType.Billow:
				return SingleSimplexFractalBillow(x, y);
			case FractalType.RigidMulti:
				return SingleSimplexFractalRigidMulti(x, y);
			default:
				return 0;
		}
	}

	float SingleSimplexFractalFBM(float x, float y)
	{
		float sum = SingleSimplex(seed, x, y);
		float amp = 1;

		for (int i = 1; i < octaves; i++)
		{
			x *= lacunarity;
			y *= lacunarity;

			amp *= gain;
			sum += SingleSimplex(++seed, x, y) * amp;
		}

		return sum * fractalBounding;
	}

	float SingleSimplexFractalBillow(float x, float y)
	{
		float sum = math.abs(SingleSimplex(seed, x, y)) * 2 - 1;
		float amp = 1;

		for (int i = 1; i < octaves; i++)
		{
			x *= lacunarity;
			y *= lacunarity;

			amp *= gain;
			sum += (math.abs(SingleSimplex(++seed, x, y)) * 2 - 1) * amp;
		}

		return sum * fractalBounding;
	}

	float SingleSimplexFractalRigidMulti(float x, float y)
	{
		float sum = 1 - math.abs(SingleSimplex(seed, x, y));
		float amp = 1;

		for (int i = 1; i < octaves; i++)
		{
			x *= lacunarity;
			y *= lacunarity;

			amp *= gain;
			sum -= (1 - math.abs(SingleSimplex(++seed, x, y))) * amp;
		}

		return sum;
	}

    float SingleSimplex(int seed, float x, float y)
    {
        float t = (x + y) * F2;
        int i = FastFloor(x + t);
        int j = FastFloor(y + t);

        t = (i + j) * G2;
        float X0 = i - t;
        float Y0 = j - t;

        float x0 = x - X0;
        float y0 = y - Y0;

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1; j1 = 0;
        }
        else
        {
            i1 = 0; j1 = 1;
        }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1 + F2;
        float y2 = y0 - 1 + F2;

        float n0, n1, n2;

        t = (float)0.5 - x0 * x0 - y0 * y0;
        if (t < 0) n0 = 0;
        else
        {
            t *= t;
            n0 = t * t * GradCoord2D(seed, i, j, x0, y0);
        }

        t = (float)0.5 - x1 * x1 - y1 * y1;
        if (t < 0) n1 = 0;
        else
        {
            t *= t;
            n1 = t * t * GradCoord2D(seed, i + i1, j + j1, x1, y1);
        }

        t = (float)0.5 - x2 * x2 - y2 * y2;
        if (t < 0) n2 = 0;
        else
        {
            t *= t;
            n2 = t * t * GradCoord2D(seed, i + 1, j + 1, x2, y2);
        }

        return To01(50 * (n0 + n1 + n2));
    }

    float GradCoord2D(int seed, int x, int y, float xd, float yd)
    {
        int hash = seed;
        hash ^= X_PRIME * x;
        hash ^= Y_PRIME * y;

        hash = hash * hash * hash * 60493;
        hash = (hash >> 13) ^ hash;

        float2 g = grad_2D[hash & 7];

        return xd * g.x + yd * g.y;
    }

    float To01(float value)
	{
		return negative ? value : (value * 0.5f) + 0.5f;
	}

    int FastFloor(float f) { return (f >= 0 ? (int)f : (int)f - 1); }
}