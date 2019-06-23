/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public struct GenerateTreeMeshJob
{
    public int2 rootIndex;
    public WorleyNoise rootWorley;
    public float rootFrequency;

    public float crownHeight;
    

    public Unity.Mathematics.Random random;

    NativeList<Node> crowns;
    NativeList<Node> branches;

    Color color;

    public struct Node
    {
        public float frequency;
        public WorleyNoise.CellData cell;
        public float height;

        public float3 Position()
        {
            return cell.position + new float3(0, height, 0);
        }
    }

    public void Execute()
    {
        color = Color.green;
        Node root = RootNode();
        Branch(root);
    }

    Node RootNode()
    {
        return new Node{
            frequency = rootFrequency,
            cell = rootWorley.GetCellData(rootIndex, rootFrequency),
            height = crownHeight
        };
    }

    void Branch(Node root)
    {
        WorleyNoise branchWorley = rootWorley;
        branchWorley.SetSeed(random.NextInt());

        crowns = GetChildren(root, ref branchWorley, 3);

        float3 rootPosition = root.Position();
        for(int c = 0; c < crowns.Length; c++)
        {
            NativeList<Node> branches = GetChildren(crowns[c], ref branchWorley, 2);

            float3 averagePosition = float3.zero;
            for(int b = 0; b < branches.Length; b++)
                averagePosition += branches[b].Position();

            averagePosition += rootPosition;
            averagePosition /= branches.Length+1;

            Draw(root.Position(), averagePosition);

            for(int b = 0; b < branches.Length; b++)
            {
                Draw(averagePosition, branches[b].Position());
            }
        }
    }

    NativeList<Node> GetChildren(Node parent, ref WorleyNoise worley, int multiplier)
    {
        float frequency = parent.frequency * multiplier;

        NativeList<Node> children = new NativeList<Node>(Allocator.Temp);
        NativeQueue<WorleyNoise.CellData> toCheck = new NativeQueue<WorleyNoise.CellData>(Allocator.Temp);

        //  cell at position of parent
        WorleyNoise.CellData initialCell = worley.GetCellData(parent.cell.position, frequency);
        toCheck.Enqueue(initialCell);

        NativeList<int2> checkedIndices = new NativeList<int2>(Allocator.Temp);
        checkedIndices.Add(initialCell.index);

        while(toCheck.Count > 0)
        {
            WorleyNoise.CellData cell = toCheck.Dequeue();

            float distanceToEdgeParent;
         
            WorleyNoise.CellData cellPointInParent = worley.GetCellData(cell.position, parent.frequency, out distanceToEdgeParent);
            WorleyNoise.CellData cellPointInRoot = rootWorley.GetCellData(cell.position, rootFrequency);

            float height = parent.height;
            height += (distanceToEdgeParent - 0.2f)*crownHeight;
            
            if( !PointIsInParent(cellPointInParent, parent.cell.index) ||
                !PointIsInParent(cellPointInRoot, rootIndex) )
            {
                //DrawDebug(parent.Position(), cell.position + new float3(0, height, 0));
                continue;
            }

            Node newNode = new Node{
                height = height,
                frequency = frequency,
                cell = cell
            };
            
            children.Add(newNode);

            for(int x = -1; x <= 1; x++)
                for(int z = -1; z <= 1; z++)
                {
                    int2 index = new int2(x, z);
                    if(checkedIndices.Contains(index))
                        continue;

                    WorleyNoise.CellData cellToCheck = worley.GetCellData(cell.index + index, frequency);
                    toCheck.Enqueue(cellToCheck);
                    checkedIndices.Add(index);
                }
        }

        checkedIndices.Dispose();
        toCheck.Dispose();

        return children;
    }

    bool PointIsInParent(WorleyNoise.CellData point, int2 index)
    {
        if(!point.index.Equals(index))
            return false;

        return true;
    }

    void Draw(float3 parentPosition, float3 childPosition)
    {
        Debug.DrawLine(parentPosition, childPosition, color, 1000);
    }

    void DrawDebug(float3 parentPosition, float3 childPosition)
    {
        Debug.DrawLine(parentPosition, childPosition, new Color(color.r, color.g, color.b, 0.2f), 1000);
    }
}
 */