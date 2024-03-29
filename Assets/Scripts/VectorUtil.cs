﻿using Unity.Mathematics;
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

    public NativeArray<float3> SortVerticesClockwise(NativeArray<float3> vertices, float3 center)
    {
        var vertices2D = new NativeArray<float2>(vertices.Length, Allocator.Temp);
        for(int i = 0; i < vertices.Length; i++)
            vertices2D[i] = new float2(vertices[i].x, vertices[i].z);

        SortVerticesClockwise(vertices2D, new float2(center.x, center.z));

        for(int i = 0; i < vertices.Length; i++)
            vertices[i] = new float3(vertices2D[i].x, 0, vertices2D[i].y);

        return vertices;
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

    public float FarthestDistance(NativeArray<float3> checkFarthest, float3 fromPosition)
    {
        float farthestDistance = 0;
        for(int i = 0; i < checkFarthest.Length; i++)
        {
            float distance = math.length(checkFarthest[i] - fromPosition);
            if(distance > farthestDistance)
                farthestDistance = distance;
        }
        return farthestDistance;
    }

    public float2 MeanPoint(NativeArray<float2> points)
    {
        float2 sum = float2.zero;
        for(int i = 0; i < points.Length; i++)
            sum += points[i];

        return sum / points.Length;
    }

    public float2 MeanPoint(NineValues<float2> points)
    {
        float2 sum = float2.zero;
        for(int i = 0; i < points.Length; i++)
            sum += points[i];

        return sum / points.Length;
    }

    //TODO: correct operator in return line
    public float3 MeanPoint(NativeArray<float3> points)
    {
        float3 sum = float3.zero;
        for(int i = 0; i < points.Length; i++)
            sum += points[i];

        return sum / points.Length;
    }

    public float3 MeanPoint(NineValues<float3> points)
    {
        float3 sum = float3.zero;
        for(int i = 0; i < points.Length; i++)
            sum += points[i];

        return sum / points.Length;
    }

    public float2 MidPoint(float2 a, float2 b, float offset = 0.5f)
    {
        return a + ((b-a) * offset);
    }

    public float3 MidPoint(float3 a, float3 b, float offset = 0.5f)
    {
        return a + ((b-a) * offset);
    }

    public float RotationFromUp(float3 position, float3 center)
    {
        return RotationFromUp(new float2(position.x, position.z), new float2(center.x, center.z));
    }

    public float RotationFromUp(float2 position, float2 center)
    {
        float2 direction = position - center;
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
        from = math.normalize(from);
        to = math.normalize(to);
        float denominator = (float)math.sqrt(math.length(from) * math.length(to));
        float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
        return ((float)math.acos(dot)) * rad2Deg;
    }

    public float Angle(float3 from, float3 to)
    {
        from = math.normalize(from);
        to = math.normalize(to);
        float denominator = (float)math.sqrt(math.length(from) * math.length(to));
        float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
        return ((float)math.acos(dot)) * rad2Deg;
    }
}
