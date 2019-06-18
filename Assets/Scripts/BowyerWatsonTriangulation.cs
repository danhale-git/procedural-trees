using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatsonTriangulation
{

    public NativeList<float2> points;
    int currentPoint;

    NativeList<Triangle> incompleteTriangles;
    //NativeList<Triangle> completeTriangles;

    NativeList<Edge> edges;

    struct Edge
    {
        public Edge(float2 a, float2 b)
        {
            this.a = a;
            this.b = b;
        }
        public readonly float2 a, b;

        public bool Equals(Edge other)
        {
            bool match = this.a.Equals(other.a) && this.b.Equals(other.b);
            bool oppositeMatch = this.a.Equals(other.b) && this.b.Equals(other.a);

            return (match || oppositeMatch);
        }
    }

    struct Triangle
    {
        public Triangle(float2 a, float2 b, float2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            float dA, dB, dC, aux1, aux2, div;
	
            dA = a.x * a.x + a.y * a.y;
            dB = b.x * b.x + b.y * b.y;
            dC = c.x * c.x + c.y * c.y;
        
            aux1 = (dA*(c.y - b.y) + dB*(a.y - c.y) + dC*(b.y - a.y));
            aux2 = -(dA*(c.x - b.x) + dB*(a.x - c.x) + dC*(b.x - a.x));
            div = (2*(a.x*(c.y - b.y) + b.x*(a.y-c.y) + c.x*(b.y - a.y)));
        
            Circumcircle circle = new Circumcircle();

            float2 center = new float2(
                aux1/div,
                aux2/div
            );
        
            circle.center = center;
            circle.radius = math.sqrt((center.x - a.x)*(center.x - a.x) + (center.y - a.y)*(center.y - a.y));

            this.circumcircle = circle;
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
        incompleteTriangles = new NativeList<Triangle>(Allocator.Persistent);

        DrawPoint(CenterPoint(), UnityEngine.Color.red);//DEBUG

        incompleteTriangles.Add(SuperTriangle());
    }

    public void Triangulate()
    {
        if(currentPoint == points.Length-1)
        {
            incompleteTriangles.Dispose();
            points.Dispose();
            return;
        }

        NativeArray<Triangle> incompleteTrianglesCopy = new NativeArray<Triangle>(incompleteTriangles.Length, Allocator.Persistent);
        incompleteTrianglesCopy.CopyFrom(incompleteTriangles.ToArray());
        edges = new NativeList<Edge>(Allocator.Temp);
        incompleteTriangles.Clear();
        
        float2 point = points[currentPoint];
        currentPoint++;

        for(int i = 0; i < incompleteTrianglesCopy.Length; i++)
        {
            Triangle triangle = incompleteTrianglesCopy[i];
            float distanceFromCircumcircle = math.distance(point, triangle.circumcircle.center);
            bool pointIsInCircumcircle = distanceFromCircumcircle < triangle.circumcircle.radius;

            if(pointIsInCircumcircle)
            {
                AddOrRemoveEdge(new Edge(incompleteTrianglesCopy[i].a,incompleteTrianglesCopy[i].b));
                AddOrRemoveEdge(new Edge(incompleteTrianglesCopy[i].b,incompleteTrianglesCopy[i].c));
                AddOrRemoveEdge(new Edge(incompleteTrianglesCopy[i].c,incompleteTrianglesCopy[i].a));
            }
            else
            {
                incompleteTriangles.Add(incompleteTrianglesCopy[i]);
            }
        }

        for(int i = 0; i < edges.Length; i++)
        {
            Triangle triangle = new Triangle(
                edges[i].a,
                edges[i].b,
                point
            );

            incompleteTriangles.Add(triangle);

            DrawTriangle(triangle, new UnityEngine.Color(0, 1, 0, 0.5f));
        }
        DrawPoint(point, UnityEngine.Color.blue);

        edges.Dispose();
        incompleteTrianglesCopy.Dispose();
    }

    void AddOrRemoveEdge(Edge edge)
    {
        int otherIndex;
        if(EdgeIsDuplicate(edge, out otherIndex))
            edges.RemoveAtSwapBack(otherIndex);
        else
            edges.Add(edge);
    }

    bool EdgeIsDuplicate(Edge check, out int otherIndex)
    {
        otherIndex = 0;
        if(edges.Length == 0) return false;

        for(int i = 0; i < edges.Length; i++)
            if(check.Equals(edges[i]))
            {
                DrawLineFloat2(check.a, check.b, new UnityEngine.Color(1, 0, 0, 0.5f));
                otherIndex = i;
                return true;
            }

        return false;
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
    
    //DEBUG
    void DrawTriangle(Triangle triangle, UnityEngine.Color color)
    {
        DrawLineFloat2(triangle.a, triangle.b, color);
        DrawLineFloat2(triangle.b, triangle.c, color);
        DrawLineFloat2(triangle.c, triangle.a, color);
        //DrawCircle(triangle.circumcircle, new UnityEngine.Color(0, 1, 1, 1));
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
