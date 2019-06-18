using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatsonTriangulation
{

    public NativeList<float2> points; 

    NativeArray<Triangle> incompleteTriangles;
    NativeArray<Triangle> completeTriangles;

    NativeArray<Edge> edges;

    struct Edge
    {
        readonly float2 a, b; 
    }

    struct Triangle
    {
        public Triangle(float2 a, float2 b, float2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public readonly float2 a, b, c;
    }

    public void Test()
    {
        DrawPoints();
        SuperTriangle();

    }
    void DrawPoints()
    {
        for(int i = 0; i < points.Length; i++)
        {
            TreeManager.CreateCube(points[i], UnityEngine.Color.black);
        }

        TreeManager.CreateCube(CenterPoint(), UnityEngine.Color.red);
    }

    Triangle SuperTriangle()
    {
        float2 center = CenterPoint();
        float radius = IncircleRadius(center);

        float2 topRight = center + new float2(radius, radius);
        float2 topLeft = center + new float2(-radius, radius);
        float2 bottom = center + new float2(0, -radius);

        float2 topIntersect = GetIntersectionPointCoordinates(
            topRight,
            topRight + new float2(-1, 1),
            topLeft,
            topLeft + new float2(1, 1)
        );

        float2 leftIntersect = GetIntersectionPointCoordinates(
            topLeft,
            topLeft + new float2(-1, -1),
            bottom,
            bottom + new float2(-1, 0)
        );

        float2 rightIntersect = GetIntersectionPointCoordinates(
            topRight,
            topRight + new float2(1, -1),
            bottom,
            bottom + new float2(1, 0)
        );

        Triangle triangle = new Triangle(topIntersect, rightIntersect, leftIntersect);
        DrawTriangle(triangle, UnityEngine.Color.red);

        return triangle;
    }

    public float2 CenterPoint()
    {
        float2 center = float2.zero;
        for(int i = 0; i < points.Length; i++)
            center += points[i];

        return center /= points.Length;
    }
    public float IncircleRadius(float2 center)
    {
        float largestDistance = 0;
        for(int i = 0; i < points.Length; i++)
        {
            float distance = math.distance(points[i], center);
            if(distance > largestDistance)
                largestDistance = distance;
        }
        
        return largestDistance + 1;
    }

    public float2 GetIntersectionPointCoordinates(float2 A1, float2 A2, float2 B1, float2 B2)
	{
		float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
		float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
	
		float2 point = new float2(
			B1.x + (B2.x - B1.x) * mu,
			B1.y + (B2.y - B1.y) * mu
		);

		return point;
	}
    
    //DEBUG
    void DrawTriangle(Triangle triangle, UnityEngine.Color color)
    {
        DrawLineFloat2(triangle.a, triangle.b, color);
        DrawLineFloat2(triangle.b, triangle.c, color);
        DrawLineFloat2(triangle.c, triangle.a, color);
    }
    void DrawLineFloat2(float2 a, float2 b, UnityEngine.Color color)
    {
        float3 a3 = new float3(a.x, 0, a.y);
        float3 b3 = new float3(b.x, 0, b.y);
        UnityEngine.Debug.DrawLine(a3, b3, color, 100);
    }
    //DEBUG
}
