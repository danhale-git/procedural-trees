using Unity.Mathematics;

struct ClockwiseVertex : System.IComparable<ClockwiseVertex>
{
    public readonly float2 vertex;
    public readonly float2 clockCenter;

    VectorUtil vectorUtil;

    public ClockwiseVertex(float2 vertex, float2 clockCenter)
    {
        this.vertex = vertex;
        this.clockCenter = clockCenter;
    }

    public int CompareTo(ClockwiseVertex other)
    {
        float thisAngle = vectorUtil.RotationFromUp(vertex, clockCenter);
        float otherAngle = vectorUtil.RotationFromUp(other.vertex, other.clockCenter);
        return thisAngle.CompareTo(otherAngle);
    }
}