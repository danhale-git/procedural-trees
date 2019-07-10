using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatson
{
    WorleyNoise.CellProfile cellProfile;

    NativeList<float3> points;

    NativeList<Triangle> triangles;
    NativeList<Edge> edges;

    NativeList<float3> edgeVertices;

    Triangle superTriangle;

    VectorUtil vectorUtil;

    struct Edge
    {
        public Edge(float3 a, float3 b)
        {
            this.a = a;
            this.b = b;
        }
        public readonly float3 a, b;

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
        public float3 a, b, c;
        public Circumcircle circumcircle;
        public float degreesFromUp;

        public int CompareTo(Triangle other)
        {
            return degreesFromUp.CompareTo(other.degreesFromUp);
        }

        public float3 this[int i]
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
		public readonly float3 center;
		public readonly float radius;

        public Circumcircle(float3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
	}

    public WorleyNoise.CellProfile GetCellProfile(NativeList<float3> points, WorleyNoise.CellData cell)
    {
        this.cellProfile = new WorleyNoise.CellProfile();
        this.cellProfile.cell = cell;

        this.points = points;
        this.triangles = new NativeList<Triangle>(Allocator.TempJob);
        this.edgeVertices = new NativeList<float3>(Allocator.Temp);
        
        BowyerWatsonTriangulation();

        GetWorleyCellVertices();

        points.Dispose();
        triangles.Dispose();

        return cellProfile;
    }

    void BowyerWatsonTriangulation()
    {
        triangles.Add(SuperTriangle());

        for(int i = 0; i < points.Length; i++)
        {
            edges = new NativeList<Edge>(Allocator.Temp);
            float3 point = points[i];

            RemoveIntersectingTriangles(point);

            AddNewTriangles(point);

            edges.Dispose();
        }
    }

    void RemoveIntersectingTriangles(float3 point)
    {
        NativeArray<Triangle> trianglesCopy = CopyAndClearTrianglesArray();

        for(int i = 0; i < trianglesCopy.Length; i++)
        {
            Triangle triangle = trianglesCopy[i];
            float distanceFromCircumcircle = math.length(triangle.circumcircle.center - point);
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

    void AddNewTriangles(float3 point)
    {
        NativeArray<float3> vertices = new NativeArray<float3>(3, Allocator.Temp);

        float3 cellPosition2D = cellProfile.cell.position;

        for(int i = 0; i < edges.Length; i++)
        {
            vertices[0] = edges[i].a;
            vertices[1] = edges[i].b;
            vertices[2] = point;

            float3 triangleCenter = vectorUtil.MeanPoint(vertices);
            vectorUtil.SortVerticesClockwise(vertices, triangleCenter);

            Triangle triangle = new Triangle();
            triangle.a = vertices[0];
            triangle.b = vertices[1];
            triangle.c = vertices[2];
            triangle.circumcircle = GetCircumcircle(vertices[0], vertices[1], vertices[2]);
            triangle.degreesFromUp = vectorUtil.RotationFromUp(triangle.circumcircle.center, cellPosition2D);

            triangles.Add(triangle);
        }

        vertices.Dispose();
    }

    public Circumcircle GetCircumcircle(float3 a, float3 b, float3 c)
    {
        float dA, dB, dC, aux1, aux2, div;

        dA = a.x * a.x + a.z * a.z;
        dB = b.x * b.x + b.z * b.z;
        dC = c.x * c.x + c.z * c.z;
    
        aux1 = (dA*(c.z - b.z) + dB*(a.z - c.z) + dC*(b.z - a.z));
        aux2 = -(dA*(c.x - b.x) + dB*(a.x - c.x) + dC*(b.x - a.x));
        div = (2*(a.x*(c.z - b.z) + b.x*(a.z-c.z) + c.x*(b.z - a.z)));

        float3 center = new float3(aux1/div, 0, aux2/div);
        float radius = math.sqrt((center.x - a.x)*(center.x - a.x) + (center.z - a.z)*(center.z - a.z));

        return new Circumcircle(center, radius);
    }

    void GetWorleyCellVertices()
    {
        RemoveExternalTriangles();
        
        SortTrianglesClockwise();

        GatherCellEdgeVertices();

        var vertexArray = new NativeArray<float3>(edgeVertices.Length, Allocator.Temp);
        vertexArray.CopyFrom(edgeVertices);
        cellProfile.vertices = vertexArray;
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

    void GatherCellEdgeVertices()
    {
        float3 centerPoint = cellProfile.cell.position;

        for(int t = 0; t < triangles.Length; t++)
        {
            BowyerWatson.Triangle triangle = triangles[t];

            bool triangleInCell = false;
            int floatIndex = 0;
            float3x2 adjacentCellPair = float3x2.zero;

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
                float3 c = triangle.circumcircle.center;
                edgeVertices.Add(c);
            }
        }
    }

    Triangle SuperTriangle()
    {
        float3 center = vectorUtil.MeanPoint(points);
        float radius = IncircleRadius(center);

        float3 topRight = center + new float3(radius, 0, radius);
        float3 topLeft = center + new float3(-radius, 0, radius);
        float3 bottom = center + new float3(0, 0, -radius);

        float3 topIntersect = LineIntersection(
            topRight,
            topRight + new float3(-1, 0, 1),
            topLeft,
            topLeft + new float3(1, 0, 1)
        );

        float3 leftIntersect = LineIntersection(
            topLeft,
            topLeft + new float3(-1, 0, -1),
            bottom,
            bottom + new float3(-1, 0, 0)
        );

        float3 rightIntersect = LineIntersection(
            topRight,
            topRight + new float3(1, 0, -1),
            bottom,
            bottom + new float3(1, 0, 0)
        );

        Triangle triangle = new Triangle();
        triangle.a = topIntersect;
        triangle.b = rightIntersect;
        triangle.c = leftIntersect;
        triangle.circumcircle = GetCircumcircle(triangle.a, triangle.b, triangle.c);

        return triangle;
    }

    public float IncircleRadius(float3 center)
    {
        float largestDistance = 0;
        for(int i = 0; i < points.Length; i++)
        {
            float distance = math.length(center - points[i]);
            if(distance > largestDistance)
                largestDistance = distance;
        }
        
        return largestDistance + 1;
    }

    public float3 LineIntersection(float3 A1, float3 A2, float3 B1, float3 B2)
	{
		float tmp = (B2.x - B1.x) * (A2.z - A1.z) - (B2.z - B1.z) * (A2.x - A1.x);
		float mu = ((A1.x - B1.x) * (A2.z - A1.z) - (A1.z - B1.z) * (A2.x - A1.x)) / tmp;
	
		float3 point = new float3(
			B1.x + (B2.x - B1.x) * mu,
            0,
			B1.z + (B2.z - B1.z) * mu
		);

		return point;
	}

    //DEBUG
    void DrawLineFloat3(float3 a, float3 b, UnityEngine.Color color)
    {
        UnityEngine.Debug.DrawLine(a, b, color, 100);
    }
    /*void DrawPoint(float2 point, UnityEngine.Color color)
    {
        var offsets = new AdjacentIntOffsetsClockwise();
        for(int i = 0; i < 4; i++)
        {
            DrawLineFloat2(point + offsets[i], point-offsets[i], color);
        }
    } */

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
