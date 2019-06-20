using Unity.Mathematics;

public struct VectorUtil
{
    const float rad2Deg = 57.29578f;

    public float RotationFromUp(float2 position, float2 center)
    {
        float2 direction = math.normalize(position - center);
        float2 up = new float2(0, 1);
        return SignedAngle(direction, up);
    }

    public float SignedAngle(float2 from, float2 to)
    {
        float unsigned_angle = Angle(from, to);
        float sign = math.sign(from.x * to.y - from.y * to.x);

        return sign < 0 ? 360 - unsigned_angle : unsigned_angle;
    }

    public float Angle(float2 from, float2 to)
    {
        float denominator = (float)math.sqrt(math.length(from) * math.length(to));
        float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
        return ((float)math.acos(dot)) * rad2Deg;
    }
}
