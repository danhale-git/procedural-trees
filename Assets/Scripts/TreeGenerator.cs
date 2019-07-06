using Unity.Mathematics;
using Unity.Collections;

using UnityEngine;
using System.Collections.Generic;

public struct TreeGenerator
{
    public WorleyNoise worley;
    public GameObject meshPrefab;
    public Material material;

    WorleyNoise.CellData cell;
    NativeArray<float3> cellVertices;

    NativeList<float3> vertices;
    NativeList<int> triangles;

    Unity.Mathematics.Random random;
    VectorUtil vectorUtil;
    
    public void Generate(int2 cellIndex)
    {
        vertices = new NativeList<float3>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        triangles = new NativeList<int>(Allocator.Temp);
        cell = worley.GetCellData(cellIndex);
        cellVertices = worley.GetCellVertices(cellIndex, UnityEngine.Color.blue);
        WorldToLocal(cellVertices);
        random = new Unity.Mathematics.Random((uint)(cell.value * 1000));

        //Draw other cell
        Crown(12, worley.frequency*1.75f);
        //DrawLeavesInSegment(8, worley.frequency*3);
        
        float3 min = new float3(-1, 0, -1);
        float3 max = new float3(1, 0, 1);
        NativeArray<int> extruded = TrunkVertices(0.2f);
        extruded = ExtrudeTrunk(extruded, new float3(0, 1, 0), 0.4f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded = ExtrudeTrunk(extruded, random.NextFloat3(min, max) + new float3(0, 3, 0), 0.7f);
        extruded.Dispose(); 

        MakeMesh();

        vertices.Dispose();
        triangles.Dispose();
        cellVertices.Dispose();
    }

    void WorldToLocal(NativeArray<float3> worldPositions)
    {
        for(int i = 0; i < worldPositions.Length; i++)
            worldPositions[i] = worldPositions[i] - cell.position;
    }

    NativeArray<float3> RemoveThinSegments(NativeArray<float3> originalVertices, float3 centre, int minAngle)
    {
        NativeList<float3> trimmed = new NativeList<float3>( Allocator.Temp);

        for(int i = 0; i < originalVertices.Length; i++)
        {
            int nextIndex = i == originalVertices.Length-1 ? 0 : i+1;
            
            float3 currentVertex = originalVertices[i] - centre;
            float3 nextVertex = originalVertices[nextIndex] - centre;

            if(vectorUtil.Angle(currentVertex, nextVertex) >= minAngle)
                trimmed.Add(originalVertices[i]);
        }

        NativeArray<float3> trimmedArray = new NativeArray<float3>(trimmed.Length, Allocator.Temp);
        trimmedArray.CopyFrom(trimmed);
        trimmed.Dispose();

        return trimmedArray;
    }

    NativeArray<int> TrunkVertices(float size)
    {
        NativeList<int> trunkIndices = new NativeList<int>(Allocator.Temp);
        NativeArray<float3> verticesTrimmed = RemoveThinSegments(cellVertices, float3.zero, 20);

        for(int i = 0; i < verticesTrimmed.Length; i++)
        {
            vertices.Add(verticesTrimmed[i] * size);
            trunkIndices.Add(vertices.Length-1);
        }

        NativeArray<int> indexArray = new NativeArray<int>(trunkIndices.Length, Allocator.Temp);
        indexArray.CopyFrom(trunkIndices);
        trunkIndices.Dispose();
        return indexArray;
    }

    NativeArray<int> ExtrudeTrunk(NativeArray<int> startIndices, float3 extrusion, float scale)
    {
        int edgeCount = startIndices.Length;

        NativeArray<int> endIndices = new NativeArray<int>(startIndices.Length, Allocator.Temp);

        for(int i = 0; i < edgeCount; i++)
        {
            float3 startVertex = vertices[startIndices[i]];
            float3 endVertex = (startVertex * scale) + extrusion;

            vertices.Add(endVertex);
            endIndices[i] = vertices.Length-1;
        }

        for(int i = 0; i < edgeCount; i++)
        {
            int currentEdge = i;
            int nextEdge = i == edgeCount-1 ? 0 : i+1;

            triangles.Add(startIndices[currentEdge]);
            triangles.Add(startIndices[nextEdge]);
            triangles.Add(endIndices[currentEdge]);

            triangles.Add(startIndices[nextEdge]);
            triangles.Add(endIndices[nextEdge]);
            triangles.Add(endIndices[currentEdge]);
        }

        return endIndices;
    }

    void Crown(float height, float2 frequency)
    {
        WorleyNoise newWorley = worley;
        newWorley.frequency = frequency;

        NativeList<WorleyNoise.CellData> children = GetCellChildren(newWorley);
        for(int i = 0; i < children.Length; i++)
        {
            NativeArray<float3> edgeVertices = newWorley.GetCellVertices(children[i].index, UnityEngine.Color.green);
            WorldToLocal(edgeVertices);
            NativeArray<float3> edgeVerticesTrimmed = RemoveThinSegments(edgeVertices, children[i].position - cell.position, 20);

            float3 meanPoint = vectorUtil.MeanPoint(edgeVerticesTrimmed);
            //TODO process vertices and center y axis point here
            DrawLeaves(edgeVerticesTrimmed, meanPoint, height);

            edgeVertices.Dispose();
            edgeVerticesTrimmed.Dispose();
        } 
        children.Dispose();
        /*float3 meanPoint = vectorUtil.MeanPoint(cellVertices);
        NativeArray<float3> verticesCopy = new NativeArray<float3>(cellVertices.Length, Allocator.Temp);
        verticesCopy.CopyFrom(cellVertices);
        DrawLeaves(verticesCopy, meanPoint, height); */
    }

    void DrawLeavesInSegment(float height, float2 frequency)
    {
        WorleyNoise newWorley = worley;
        newWorley.frequency = frequency;
        newWorley.seed = random.NextInt();

        NativeList<WorleyNoise.CellData> children = GetSegmentChildren(newWorley);
        for(int i = 0; i < children.Length; i++)
        {
            WorleyNoise.CellData childCell = children[i];
            DrawLeaves(newWorley.GetCellVertices(childCell.index, UnityEngine.Color.red), childCell.position, height);
        }
    }

    NativeList<WorleyNoise.CellData> GetSegmentChildren(WorleyNoise newWorley)
    {
        //TODO handle segment randomisation
        float3 positionInSegment = new float3(1, 0, 1) + cell.position;

        WorleyNoise.CellData startCell = newWorley.GetCellData(positionInSegment);

        var checkNext = new NativeQueue<WorleyNoise.CellData>(Allocator.Temp);
        var alreadyChecked = new NativeList<WorleyNoise.CellData>(Allocator.Temp);
        checkNext.Enqueue(startCell);
        alreadyChecked.Add(startCell);

        var children = new NativeList<WorleyNoise.CellData>(Allocator.Temp);
        while(checkNext.Count > 0)
        {
            WorleyNoise.CellData newCell = checkNext.Dequeue();

            WorleyNoise.CellData dataFromParent = worley.GetCellData(newCell.position);
            bool cellInParent = dataFromParent.index.Equals(cell.index);
            float rotation = vectorUtil.RotationFromUp(Float3To2(newCell.position), Float3To2(cell.position));
            //TODO Randomise segment, not just 90 from up
            bool cellIsInSegment = rotation < 90;

            if(!cellInParent || !cellIsInSegment)
                continue;

            children.Add(newCell);

            AdjacentIntOffsetsClockwise adjacentIndices;
            for(int i = 0; i < 8; i++)
            {
                WorleyNoise.CellData adjacentCell = newWorley.GetCellData(newCell.index + adjacentIndices[i]);
                if(!alreadyChecked.Contains(adjacentCell))
                {
                    checkNext.Enqueue(adjacentCell);
                    alreadyChecked.Add(adjacentCell);
                }
            }
        }

        checkNext.Dispose();
        alreadyChecked.Dispose();

        return children;
    }

    NativeList<WorleyNoise.CellData> GetCellChildren(WorleyNoise newWorley)
    {
        WorleyNoise.CellData startCell = newWorley.GetCellData(cell.position);

        var checkNext = new NativeQueue<WorleyNoise.CellData>(Allocator.Temp);
        var alreadyChecked = new NativeList<WorleyNoise.CellData>(Allocator.Temp);
        checkNext.Enqueue(startCell);
        alreadyChecked.Add(startCell);

        var children = new NativeList<WorleyNoise.CellData>(Allocator.Temp);
        while(checkNext.Count > 0)
        {
            WorleyNoise.CellData newCell = checkNext.Dequeue();

            WorleyNoise.CellData dataFromParent = worley.GetCellData(newCell.position);
            bool cellInParent = dataFromParent.index.Equals(cell.index);

            if(!cellInParent)
                continue;

            children.Add(newCell);

            AdjacentIntOffsetsClockwise adjacentIndices;
            for(int i = 0; i < 8; i++)
            {
                WorleyNoise.CellData adjacentCell = newWorley.GetCellData(newCell.index + adjacentIndices[i]);
                if(!alreadyChecked.Contains(adjacentCell))
                {
                    checkNext.Enqueue(adjacentCell);
                    alreadyChecked.Add(adjacentCell);
                }
            }
        }

        checkNext.Dispose();
        alreadyChecked.Dispose();

        return children;
    }

    void DrawCell(NativeArray<float3> cellEdgeVertexPositions, float3 centerPosition)
    {
        float3 centerVertex = centerPosition - cell.position;
        vertices.Add(centerVertex);

        int cellCenter = vertices.Length-1;
        int vertexIndex = vertices.Length;

        for(int i = 0; i < cellEdgeVertexPositions.Length; i++)
            vertices.Add(cellEdgeVertexPositions[i] - cell.position);
            
        for(int i = 0; i < cellEdgeVertexPositions.Length; i++)
        {
            int currentEdge = vertexIndex + i;
            int nextEdge = vertexIndex + (i == cellEdgeVertexPositions.Length-1 ? 0 : i+1);

            triangles.Add(currentEdge);
            triangles.Add(nextEdge);
            triangles.Add(cellCenter);            
        }

        cellEdgeVertexPositions.Dispose();
    }

    void DrawLeaves(NativeArray<float3> cellEdgeVertexPositions, float3 centerPosition, float height)
    {
        float drop = math.length(FarthestEdgeVertex(cellEdgeVertexPositions, centerPosition)) * 0.4f;

        float3 center = centerPosition + (drop * 0.5f);
        center.y += height;

        int cellCenter = vertices.Length-1;
        int vertexIndex = vertices.Length;

        const float jitter = 0.4f;

        float3 currentJitter = random.NextFloat3(-jitter, jitter);
        float3 currentMidJitter = random.NextFloat3(-jitter, jitter);
        float3 zeroJitter = currentJitter;
        float3 zeroMidJitter = currentMidJitter;
        for(int i = 0; i < cellEdgeVertexPositions.Length; i++)
        {
            bool final = i == cellEdgeVertexPositions.Length-1;
            
            float3 nextJitter = final ? zeroJitter : random.NextFloat3(-jitter, jitter);
            float3 nextMidJitter = final ? zeroMidJitter : random.NextFloat3(-jitter, jitter);

            int currentEdge = i;
            int nextEdge = final ? 0 : i+1;

            float3 current = cellEdgeVertexPositions[currentEdge] + currentJitter;
            float3 next = cellEdgeVertexPositions[nextEdge] + nextJitter;

            current.y += height;
            next.y += height;

            float3 currentMidPoint = vectorUtil.MidPoint(center, current, 0.6f) + currentMidJitter;
            float3 nextMidPoint = vectorUtil.MidPoint(center, next, 0.6f) + nextMidJitter;
            
            current.y -= drop;
            next.y -= drop;
            
            float3 edgeMidPoint = vectorUtil.MidPoint(current, next) + random.NextFloat3(-jitter, jitter);

            VertAndTri(current);
            VertAndTri(edgeMidPoint);
            VertAndTri(currentMidPoint);
            
            VertAndTri(edgeMidPoint);
            VertAndTri(next);
            VertAndTri(nextMidPoint);

            VertAndTri(currentMidPoint);
            VertAndTri(edgeMidPoint);
            VertAndTri(nextMidPoint);

            VertAndTri(currentMidPoint);
            VertAndTri(nextMidPoint);
            VertAndTri(center);

            float3 currentBottom = current;
            float3 nextBottom = next;
            currentBottom.y -= drop;
            nextBottom.y -= drop;

            VertAndTri(currentBottom);
            VertAndTri(edgeMidPoint);
            VertAndTri(current);
            
            VertAndTri(currentBottom);
            VertAndTri(nextBottom);
            VertAndTri(edgeMidPoint);

            VertAndTri(nextBottom);
            VertAndTri(next);
            VertAndTri(edgeMidPoint);

            currentJitter = nextJitter;
            currentMidJitter = nextMidJitter;
        }
    }

    void VertAndTri(float3 vert)
    {
        vertices.Add(vert);
        triangles.Add(vertices.Length-1);
    }

    float FarthestEdgeVertex(NativeArray<float3> cellEdgeVertexPositions, float3 centerPosition)
    {
        float longestDistance = 0;
        for(int i = 0; i < cellEdgeVertexPositions.Length; i++)
        {
            float distance = math.length(cellEdgeVertexPositions[i] - centerPosition);
            if(distance > longestDistance)
                longestDistance = distance;
        }
        return longestDistance;
    }

    void MakeMesh()
    {
        List<Vector3> vertexList = new List<Vector3>();
        for(int i = 0; i < vertices.Length; i++)
            vertexList.Add(vertices[i]);

        GameObject meshObject = GameObject.Instantiate(meshPrefab);
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertexList);
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshRenderer.material = this.material;
        meshFilter.mesh = mesh;

        float3 randomColor = random.NextFloat3();
        meshRenderer.material.color = new Color(randomColor.x, randomColor.y, randomColor.z);
        //meshRenderer.material.color = new Color(.9f,.9f,.9f);

        meshObject.transform.Translate(cell.position);
    }

    float2 Float3To2(float3 f)
    {
        return new float2(f.x, f.z);
    }
}