using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatsonTriangulation
{
    public NativeList<float2> points;

    NativeList<Triangle> triangles;
    NativeList<Edge> edges;

    Triangle superTriangle;

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
            this.circumcircle = new Circumcircle(a, b, c);
        }
        public readonly float2 a, b, c;
        public Circumcircle circumcircle;

        public float2 this[int i]
        {
            get
            {
                switch(i)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;    
                    default: throw new System.IndexOutOfRangeException("Index "+i+" out of range 2");
                }
            }
        }
    }

    struct Circumcircle
	{
        public Circumcircle(float2 a, float2 b, float2 c)
        {
            float dA, dB, dC, aux1, aux2, div;
	
            dA = a.x * a.x + a.y * a.y;
            dB = b.x * b.x + b.y * b.y;
            dC = c.x * c.x + c.y * c.y;
        
            aux1 = (dA*(c.y - b.y) + dB*(a.y - c.y) + dC*(b.y - a.y));
            aux2 = -(dA*(c.x - b.x) + dB*(a.x - c.x) + dC*(b.x - a.x));
            div = (2*(a.x*(c.y - b.y) + b.x*(a.y-c.y) + c.x*(b.y - a.y)));
        
            this.center = new float2(aux1/div, aux2/div);
            this.radius = math.sqrt((center.x - a.x)*(center.x - a.x) + (center.y - a.y)*(center.y - a.y));
        }
		public float2 center;
		public float radius;
	}

    public void Triangulate()
    {
        triangles = new NativeList<Triangle>(Allocator.Persistent);
        superTriangle = SuperTriangle();
        triangles.Add(superTriangle);

        for(int i = 0; i < points.Length; i++)
        {
            edges = new NativeList<Edge>(Allocator.Temp);
            float2 point = points[i];

            RemoveIntersectingTriangles(point);

            AddNewTriangles(point);

            DrawPoint(point, UnityEngine.Color.blue);//DEBUG

            edges.Dispose();
        }

        RemoveExternalTriangles();
        
        DrawTriangles();

        points.Dispose();
        triangles.Dispose();
    }

    void RemoveIntersectingTriangles(float2 point)
    {
        NativeArray<Triangle> trianglesCopy = new NativeArray<Triangle>(triangles.Length, Allocator.Persistent);
        trianglesCopy.CopyFrom(triangles.ToArray());
        triangles.Clear();

        for(int i = 0; i < trianglesCopy.Length; i++)
        {
            Triangle triangle = trianglesCopy[i];
            float distanceFromCircumcircle = math.distance(point, triangle.circumcircle.center);
            bool pointIsInCircumcircle = distanceFromCircumcircle < triangle.circumcircle.radius;

            if(pointIsInCircumcircle)
            {
                AddOrRemoveEdge(new Edge(trianglesCopy[i].a,trianglesCopy[i].b));
                AddOrRemoveEdge(new Edge(trianglesCopy[i].b,trianglesCopy[i].c));
                AddOrRemoveEdge(new Edge(trianglesCopy[i].c,trianglesCopy[i].a));
            }
            else
            {
                triangles.Add(trianglesCopy[i]);
            }
        }

        trianglesCopy.Dispose();
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
                otherIndex = i;
                return true;
            }

        return false;
    }

    void AddNewTriangles(float2 point)
    {
        for(int i = 0; i < edges.Length; i++)
        {
            Triangle triangle = new Triangle(
                edges[i].a,
                edges[i].b,
                point
            );

            triangles.Add(triangle);
        }
    }

    void RemoveExternalTriangles()
    {
        NativeArray<Triangle> trianglesCopy = new NativeArray<Triangle>(triangles.Length, Allocator.Persistent);
        trianglesCopy.CopyFrom(triangles.ToArray());
        triangles.Clear();

        for(int i = 0; i < trianglesCopy.Length; i++)
        {
            Triangle triangle = trianglesCopy[i];
            if(!SharesVertexWithSupertriangle(triangle))
                triangles.Add(triangle);
        }

        trianglesCopy.Dispose();
    }

    bool SharesVertexWithSupertriangle(Triangle triangle)
    {
        for(int t = 0; t < 3; t++)
            for(int s = 0; s < 3; s++)
                if(triangle[t].Equals(superTriangle[s]))
                    return true;

        return false;
    }

    Triangle SuperTriangle()
    {
        float2 center = MeanPoint();
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
        DrawTriangle(triangle, UnityEngine.Color.red);//DEBUG

        return triangle;
    }

    public float2 MeanPoint()
    {
        float2 sum = float2.zero;
        for(int i = 0; i < points.Length; i++)
            sum += points[i];

        return sum /= points.Length;
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
    void DrawTriangles()
    {
        for(int i = 0; i < triangles.Length; i++)
        {
            DrawTriangle(triangles[i], UnityEngine.Color.green);
            DrawPoint(triangles[i].circumcircle.center, UnityEngine.Color.red);
        }
    }
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
