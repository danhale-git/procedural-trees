using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatsonTriangulation
{
    NativeArray<float2> pointsToBeTriangulated; 

    NativeArray<Triangle> incompleteTriangles;
    NativeArray<Triangle> completeTriangles;

    NativeArray<Edge> edges;

    struct Edge
    {
        readonly float2 a, b; 
    }

    struct Triangle
    {
        readonly float2 a, b, c;
    }

    float GetAngle(float2 point, float2 vertex)
    {
        float2 vertexDirection = math.normalize(vertex - point);
        float2 up = new float2(0, 1);
        return SignedAngle(up, vertexDirection);
    }

    const float kEpsilonNormalSqrt = 1e-15F;
    const float rad2Deg = 57.29578f;

    // Returns the angle in degrees between /from/ and /to/. This is always the smallest
    float Angle(float2 from, float2 to)
    {
        float denominator = (float)math.sqrt(SquareMagnitude(from) * SquareMagnitude(to));
        if (denominator < kEpsilonNormalSqrt)
            return 0F;

        float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
        return ((float)math.acos(dot)) * rad2Deg;
    }

    float SignedAngle(float2 from, float2 to)
    {
        float unsigned_angle = Angle(from, to);
        float sign = math.sign(from.x * to.y - from.y * to.x);
        return unsigned_angle * sign;
    }

    float SquareMagnitude(float2 v)
    {
        return v.x * v.x + v.y * v.y;
    }

    public void TestClockwise()
    {
        float2 a = new float2(0, 1);
        float2 b = new float2(-1, 0);

        UnityEngine.Debug.Log(SignedAngle(a, b));
    }
}
