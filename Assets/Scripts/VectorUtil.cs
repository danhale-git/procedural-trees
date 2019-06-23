using Unity.Mathematics;
using Unity.Collections;

public struct VectorUtil
{
    const float rad2Deg = 57.29578f;

    
    struct VertexRotation : System.IComparable<VertexRotation>
    {
        public readonly float2 vertex;
        public readonly float degrees;

        public VertexRotation(float2 vertex, float angle)
        {
            this.vertex = vertex;
            this.degrees = angle;
        }

        public int CompareTo(VertexRotation otherVertAngle)
        {
            return degrees.CompareTo(otherVertAngle.degrees);
        }
    }

    public NativeArray<float2> SortVerticesClockwise(NativeArray<float2> vertices, float2 center)
    {
        NativeArray<VertexRotation> sorter = new NativeArray<VertexRotation>(vertices.Length, Allocator.Temp);
        for(int i = 0; i < vertices.Length; i++)
        {
            float rotationInDegrees = RotationFromUp(vertices[i], center);
            sorter[i] = new VertexRotation(vertices[i], rotationInDegrees);
        }

        sorter.Sort();

        for(int i = 0; i < vertices.Length; i++)
            vertices[i] = sorter[i].vertex;

        return vertices;                
    }

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
