using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatsonTriangulation
{

    public NativeList<float2> points; 

    NativeList<Triangle> incompleteTriangles;
    NativeList<Triangle> completeTriangles;

    NativeList<Edge> edges;

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
            this.circumcircle = new Circumcircle();
        }
        public readonly float2 a, b, c;
        public Circumcircle circumcircle;
    }

    struct Circumcircle
	{
		public float2 center;
		public float radius;
	}

    public void Initialise()
    {

        NativeList<Triangle> incompleteTriangles = new NativeList<Triangle>(Allocator.Temp);
        NativeList<Triangle> completeTriangles = new NativeList<Triangle>(Allocator.Temp);
        NativeList<Edge> edges = new NativeList<Edge>(Allocator.Temp);

        for(int i = 0; i < points.Length; i++)//DEBUG
            DrawPoint(points[i], UnityEngine.Color.blue);
        DrawPoint(CenterPoint(), UnityEngine.Color.red);//DEBUG

        incompleteTriangles.Add(SuperTriangle());
    }

    public void Dispose()
    {
        points.Dispose();
        incompleteTriangles.Dispose();
        completeTriangles.Dispose();
        edges.Dispose();
    }

    Triangle SuperTriangle()
    {
        float2 center = CenterPoint();
        float radius = IncircleRadius(center);

        float2 topRight = center + new float2(radius, radius);
        float2 topLeft = center + new float2(-radius, radius);
        float2 bottom = center + new float2(0, -radius);

        float2 topIntersect = LineIntersection(
            topRight,
            topRight + new float2(-1, 1),
            topLeft,
            topLeft + new float2(1, 1)
        );

        float2 leftIntersect = LineIntersection(
            topLeft,
            topLeft + new float2(-1, -1),
            bottom,
            bottom + new float2(-1, 0)
        );

        float2 rightIntersect = LineIntersection(
            topRight,
            topRight + new float2(1, -1),
            bottom,
            bottom + new float2(1, 0)
        );

        Triangle triangle = new Triangle(topIntersect, rightIntersect, leftIntersect);
        triangle.circumcircle = TriangleCircumcircle(triangle);
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

    public float2 LineIntersection(float2 A1, float2 A2, float2 B1, float2 B2)
	{
		float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
		float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
	
		float2 point = new float2(
			B1.x + (B2.x - B1.x) * mu,
			B1.y + (B2.y - B1.y) * mu
		);

		return point;
	}

    Circumcircle TriangleCircumcircle(Triangle tri)
	{
		float dA, dB, dC, aux1, aux2, div;
	
		dA = tri.a.x * tri.a.x + tri.a.y * tri.a.y;
		dB = tri.b.x * tri.b.x + tri.b.y * tri.b.y;
		dC = tri.c.x * tri.c.x + tri.c.y * tri.c.y;
	
		aux1 = (dA*(tri.c.y - tri.b.y) + dB*(tri.a.y - tri.c.y) + dC*(tri.b.y - tri.a.y));
		aux2 = -(dA*(tri.c.x - tri.b.x) + dB*(tri.a.x - tri.c.x) + dC*(tri.b.x - tri.a.x));
		div = (2*(tri.a.x*(tri.c.y - tri.b.y) + tri.b.x*(tri.a.y-tri.c.y) + tri.c.x*(tri.b.y - tri.a.y)));
	
		Circumcircle circle = new Circumcircle();

		float2 center = new float2(
			aux1/div,
			aux2/div
		);
	
		circle.center = center;
		circle.radius = math.sqrt((center.x - tri.a.x)*(center.x - tri.a.x) + (center.y - tri.a.y)*(center.y - tri.a.y));

		return circle;
	}
    
    //DEBUG
    void DrawTriangle(Triangle triangle, UnityEngine.Color color)
    {
        DrawLineFloat2(triangle.a, triangle.b, color);
        DrawLineFloat2(triangle.b, triangle.c, color);
        DrawLineFloat2(triangle.c, triangle.a, color);
        DrawCircle(triangle.circumcircle, new UnityEngine.Color(0, 1, 1, 1));
    }
    void DrawLineFloat2(float2 a, float2 b, UnityEngine.Color color)
    {
        float3 a3 = new float3(a.x, 0, a.y);
        float3 b3 = new float3(b.x, 0, b.y);
        UnityEngine.Debug.DrawLine(a3, b3, color, 100);
    }
    void DrawPoint(float2 point, UnityEngine.Color color)
    {
        var offsets = new AdjacentIntOffsetsClockwise();
        for(int i = 0; i < 4; i++)
        {
            DrawLineFloat2(point + offsets[i], point-offsets[i], color);
        }
    }
    void DrawCircle(Circumcircle circle, UnityEngine.Color color)
    {
        float2 horizontal = new float2(circle.radius, 0);
        float2 vertical = new float2(0, circle.radius);

        DrawLineFloat2(circle.center-horizontal, circle.center+horizontal, color);
		DrawLineFloat2(circle.center-vertical, circle.center+vertical, color);
    }
    //DEBUG
}
