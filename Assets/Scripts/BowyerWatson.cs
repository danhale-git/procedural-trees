using Unity.Mathematics;
using Unity.Collections;

public struct BowyerWatson
{
    float3 cellPosition;

    NativeList<Vertex> points;

    NativeList<Triangle> triangles;
    NativeList<Edge> edges;

    Triangle superTriangle;

    VectorUtil vectorUtil;

    public struct Vertex : System.IComparable<Vertex>
    {
        public readonly float3 pos;
        public readonly WorleyNoise.CellData cell;
        public float degreesFromUp;
        
        public Vertex(WorleyNoise.CellData cell)
        {
            this.cell = cell;
            this.pos = cell.position;
            this.degreesFromUp = 0;
        }

        public Vertex(float3 pos)
        {
            this.cell = new WorleyNoise.CellData();
            this.pos = pos;
            this.degreesFromUp = 0;
        }

        public int CompareTo(Vertex other)
        {
            return degreesFromUp.CompareTo(other.degreesFromUp);
        }
    }

    struct Edge
    {
        public readonly Vertex a, b;
        
        public Edge(Vertex a, Vertex b)
        {
            this.a = a;
            this.b = b;
        }

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
        public Vertex a, b, c;
        public Circumcircle circumcircle;
        public float degreesFromUp;

        public int CompareTo(Triangle other)
        {
            return degreesFromUp.CompareTo(other.degreesFromUp);
        }

        public Vertex this[int i]
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

    //TODO: rename script/struct and add small segment removal option
    public WorleyNoise.CellProfile GetCellProfile(NativeList<WorleyNoise.CellData> nineCells, WorleyNoise.CellData cell)
    {
        this.cellPosition = cell.position;

        this.triangles = new NativeList<Triangle>(Allocator.TempJob);
        this.points = new NativeList<Vertex>(Allocator.Temp);
        for(int i = 0; i < nineCells.Length; i++)
            points.Add(new Vertex(nineCells[i]));
        
        BowyerWatsonTriangulation();
        RemoveExternalTriangles();
        SortTrianglesClockwise();

        var cellProfile = new WorleyNoise.CellProfile();
        cellProfile.cell = cell;
        cellProfile.vertices = GatherCellEdgeVertices(out cellProfile.adjacentCells);
        cellProfile.meanPoint = vectorUtil.MeanPoint(cellProfile.vertices);

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
            Vertex point = points[i];

            RemoveIntersectingTriangles(point);

            AddNewTriangles(point);

            edges.Dispose();
        }
    }

    Triangle SuperTriangle()
    {
        float3 center = MeanPoint(points);
        float radius = IncircleRadius(center);

        float3 frontRight = center + new float3(radius, 0, radius);
        float3 frontLeft = center + new float3(-radius, 0, radius);
        float3 back = center + new float3(0, 0, -radius);

        float3 frontVertex = LineIntersection(
            frontRight,
            frontRight + new float3(-1, 0, 1),
            frontLeft,
            frontLeft + new float3(1, 0, 1)
        );

        float3 leftVertex = LineIntersection(
            frontLeft,
            frontLeft + new float3(-1, 0, -1),
            back,
            back + new float3(-1, 0, 0)
        );

        float3 rightVertex = LineIntersection(
            frontRight,
            frontRight + new float3(1, 0, -1),
            back,
            back + new float3(1, 0, 0)
        );
        
        Triangle triangle = new Triangle();
        triangle.a = new Vertex(frontVertex);
        triangle.b = new Vertex(rightVertex);
        triangle.c = new Vertex(leftVertex);
        triangle.circumcircle = GetCircumcircle(triangle.a.pos, triangle.b.pos, triangle.c.pos);

        this.superTriangle = triangle;

        return triangle;
    }

    public float IncircleRadius(float3 center)
    {
        float largestDistance = 0;
        for(int i = 0; i < points.Length; i++)
        {
            float distance = math.length(center - points[i].pos);
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

    void RemoveIntersectingTriangles(Vertex vert)
    {
        NativeArray<Triangle> trianglesCopy = CopyAndClearTrianglesArray();

        for(int i = 0; i < trianglesCopy.Length; i++)
        {
            Triangle triangle = trianglesCopy[i];
            float distanceFromCircumcircle = math.length(triangle.circumcircle.center - vert.pos);
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

    void AddNewTriangles(Vertex point)
    {
        NativeArray<Vertex> vertices = new NativeArray<Vertex>(3, Allocator.Temp);

        for(int i = 0; i < edges.Length; i++)
        {
            vertices[0] = edges[i].a;
            vertices[1] = edges[i].b;
            vertices[2] = point;

            Triangle triangle = new Triangle();
            triangle.a = vertices[0];
            triangle.b = vertices[1];
            triangle.c = vertices[2];
            triangle.circumcircle = GetCircumcircle(vertices[0].pos, vertices[1].pos, vertices[2].pos);
            triangle.degreesFromUp = vectorUtil.RotationFromUp(triangle.circumcircle.center, cellPosition);

            triangles.Add(triangle);
        }

        vertices.Dispose();
    }

    float3 MeanPoint(NativeArray<Vertex> vertices)
    {
        float3 sum = float3.zero;
        for(int i = 0; i < vertices.Length; i++)
        {
            sum += vertices[i].pos;
        }
        return sum / vertices.Length;
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

    NativeArray<float3> GatherCellEdgeVertices(out NativeArray<WorleyNoise.CellDataX2> adjacentCellsArray)
    {
        var edgeVertices = new NativeList<float3>(Allocator.Temp);
        var adjacentCells = new NativeList<WorleyNoise.CellDataX2>(Allocator.Temp);

        for(int t = 0; t < triangles.Length; t++)
        {
            BowyerWatson.Triangle triangle = triangles[t];
            bool triangleInCell = false;

            int floatIndex = 0;
            var adjacentCellPair = new WorleyNoise.CellDataX2();

            for(int i = 0; i < 3; i++)
                if(triangle[i].pos.Equals(cellPosition))
                    triangleInCell = true;
                else if(floatIndex > 1)
                    continue;
                else
                {
                    adjacentCellPair[floatIndex] = triangle[i].cell;
                    floatIndex++;
                }

            if(triangleInCell)
            {
                edgeVertices.Add(triangle.circumcircle.center);
                adjacentCells.Add(adjacentCellPair);

            }
        }

        var vertexArray = new NativeArray<float3>(edgeVertices.Length, Allocator.Temp);
        adjacentCellsArray = new NativeArray<WorleyNoise.CellDataX2>(edgeVertices.Length, Allocator.Temp);
        
        vertexArray.CopyFrom(edgeVertices);
        adjacentCellsArray.CopyFrom(adjacentCells);

        return vertexArray;
    }


    void DrawTriangle(Triangle triangle, UnityEngine.Color color)
    {
        UnityEngine.Debug.DrawLine(triangle.a.pos, triangle.b.pos, color, 100);
        UnityEngine.Debug.DrawLine(triangle.a.pos, triangle.c.pos, color, 100);
        UnityEngine.Debug.DrawLine(triangle.c.pos, triangle.b.pos, color, 100);
    }
}
