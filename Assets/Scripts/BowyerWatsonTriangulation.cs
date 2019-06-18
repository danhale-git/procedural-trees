using Unity.Mathematics;
using Unity.Collections;
using System;

public struct BowyerWatsonTriangulation
{

    NativeArray<float2> pointsToBeTriangulated; 
    NativeArray<Triangle> incompleteTriangles;
    NativeArray<Triangle> completeTriangles;

    NativeArray<Edge> edges;

    public struct Vertex : IComparable<Vertex>
    {
        const float rad2Deg = 57.29578f;
        
        readonly float2 v;

        public Vertex(float2 v)
        {
            this.v = v;
        }

        public int CompareTo(Vertex other)
        {
            return 0;
        }

        public float GetAngle(float2 point)
        {
            float2 vertexDirection = math.normalize(v - point);
            float2 up = new float2(0, 1);
            return SignedAngle(vertexDirection, up);
        }

        float Angle(float2 from, float2 up)
        {
            float denominator = (float)math.sqrt(Magnitude(from) * Magnitude(up));

            float dot = math.clamp(math.dot(from, up) / denominator, -1F, 1F);
            return ((float)math.acos(dot)) * rad2Deg;
        }

        float Magnitude(float2 v)
        {
            return v.x * v.x + v.y * v.y;
        }

        float SignedAngle(float2 from, float2 to)
        {
            float unsigned_angle = Angle(from, to);
            float sign = math.sign(from.x * to.y - from.y * to.x);

            return sign < 0 ? 360 - unsigned_angle : unsigned_angle;
        }
    }

    struct Edge
    {
        readonly float2 a, b; 
    }

    struct Triangle
    {
        readonly float2 a, b, c;
    }

}
