using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatson
{
    WorleyNoise.CellData cell;
    WorleyNoise.CellProfile cellProfile;

    NativeList<float2> points;

    NativeList<Triangle> triangles;
    NativeList<Edge> edges;

    NativeList<float3> edgeVertices;

    Triangle superTriangle;

    VectorUtil vectorUtil;

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
            // Matching edges always have vertices in opposite order
            bool oppositeMatch = this.a.Equals(other.b) && this.b.Equals(other.a);
            if(oppositeMatch) return true;

            return false;
        }
    }

    public struct Triangle : System.IComparable<Triangle>
    {
        public float2 a, b, c;
        public Circumcircle circumcircle;
        public float degreesFromUp;

        public int CompareTo(Triangle other)
        {
            return degreesFromUp.CompareTo(other.degreesFromUp);
        }

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

    public struct Circumcircle
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

    public WorleyNoise.CellProfile GetCellProfile(NativeList<float2> points, WorleyNoise.CellData cell)
    {
        this.cellProfile = new WorleyNoise.CellProfile();
        this.cellProfile.cell = cell;

        this.points = points;
        this.cell = cell;
        this.triangles = new NativeList<Triangle>(Allocator.TempJob);
        this.edgeVertices = new NativeList<float3>(Allocator.Temp);
        
        BowyerWatsonTriangulation();
        
        GetWorleyCellVertices();

        return cellProfile;
    }

    void BowyerWatsonTriangulation()
    {
        triangles.Add(SuperTriangle());

        for(int i = 0; i < points.Length; i++)
        {
            edges = new NativeList<Edge>(Allocator.Temp);
            float2 point = points[i];

            RemoveIntersectingTriangles(point);

            AddNewTriangles(point);

            edges.Dispose();
        }
    }

    void GetWorleyCellVertices()
    {
        RemoveExternalTriangles();
        
        points.Dispose();

        SortTrianglesClockwise();

        float2 centerPoint = new float2(cell.position.x, (float)cell.position.z);

        GatherCellEdgeVertices(triangles, centerPoint);

        triangles.Dispose();

        var vertexArray = new NativeArray<float3>(edgeVertices.Length, Allocator.Temp);
        vertexArray.CopyFrom(edgeVertices);
        cellProfile.vertices = vertexArray;
    }

    void RemoveIntersectingTriangles(float2 point)
    {
        NativeArray<Triangle> trianglesCopy = CopyAndClearTrianglesArray();

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

    NativeArray<Triangle> CopyAndClearTrianglesArray()
    {
        NativeArray<Triangle> trianglesCopy = new NativeArray<Triangle>(triangles.Length, Allocator.Persistent);
        trianglesCopy.CopyFrom(triangles.ToArray());
        triangles.Clear();
        return trianglesCopy;
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
        NativeArray<float2> vertices = new NativeArray<float2>(3, Allocator.Temp);

        float2 cellPosition2D = new float2(cell.position.x, cell.position.z);

        for(int i = 0; i < edges.Length; i++)
        {
            vertices[0] = edges[i].a;
            vertices[1] = edges[i].b;
            vertices[2] = point;

            float2 triangleCenter = vectorUtil.MeanPoint(vertices);
            vectorUtil.SortVerticesClockwise(vertices, triangleCenter);

            Triangle triangle = new Triangle();
            triangle.a = vertices[0];
            triangle.b = vertices[1];
            triangle.c = vertices[2];
            triangle.circumcircle = new Circumcircle(vertices[0], vertices[1], vertices[2]);
            triangle.degreesFromUp = vectorUtil.RotationFromUp(triangle.circumcircle.center, cellPosition2D);

            triangles.Add(triangle);
        }
    }

    void RemoveExternalTriangles()
    {
        NativeArray<Triangle> trianglesCopy = CopyAndClearTrianglesArray();

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

    void SortTrianglesClockwise()
    {
        var sortedTriangles = new NativeArray<Triangle>(triangles.Length, Allocator.Temp);
        sortedTriangles.CopyFrom(triangles);
        sortedTriangles.Sort();
        sortedTriangles.CopyTo(triangles);
        sortedTriangles.Dispose();
    }

    void GatherCellEdgeVertices(NativeArray<BowyerWatson.Triangle> triangles, float2 centerPoint)
    {
        for(int t = 0; t < triangles.Length; t++)
        {
            BowyerWatson.Triangle triangle = triangles[t];

            bool triangleInCell = false;
            int floatIndex = 0;
            float2x2 adjacentCellPair = float2x2.zero;

            for(int i = 0; i < 3; i++)
                if(triangle[i].Equals(centerPoint))
                {
                    triangleInCell = true;
                }
                else
                {
                    if(floatIndex > 1)
                        continue;

                    adjacentCellPair[floatIndex] = triangle[i];
                    floatIndex++;
                }

            if(triangleInCell)
            {
                float2 c = triangle.circumcircle.center;
                edgeVertices.Add(new float3(c.x, 0, c.y));
            }
        }
    }

    Triangle SuperTriangle()
    {
        float2 center = vectorUtil.MeanPoint(points);
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

        Triangle triangle = new Triangle();
        triangle.a = topIntersect;
        triangle.b = rightIntersect;
        triangle.c = leftIntersect;
        triangle.circumcircle = new Circumcircle(triangle.a, triangle.b, triangle.c);

        return triangle;
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

    /*void DrawEdges(UnityEngine.Color color)
    {
        for(int i = 0; i < edgeVertices.Length; i++)//DEBUG
        {
            int nextIndex = i == edgeVertices.Length-1 ? 0 : i+1;
            DrawLineFloat2(edgeVertices[i], edgeVertices[nextIndex], color);
        }//DEBUG
    }
    void DrawAdjacent(UnityEngine.Color color)
    {
        for(int i = 0; i < adjacentCellPositions.Length; i++)
        {
            DrawLineFloat2(adjacentCellPositions[i].c0, centerPoint, color);
            DrawLineFloat2(adjacentCellPositions[i].c1, centerPoint, color);
        }
    } */
    //DEBUG
}
