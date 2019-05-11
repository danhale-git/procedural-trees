using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public struct TreeGenerator
{
    public float rootFrequency;
    public float3 rootPosition;
    public int layerHeight;
    public TreeWorleyNoise worley;
    public int seed;

    public int branchLevels;
    public int trunkLevels;
    public bool allowBranchesToPassTrunk;

    public Unity.Mathematics.Random random;

    TreeWorleyNoise rootWorley;

    Color color;

    public void Execute()
    {
        rootWorley = worley;
        color = Color.green;

        TreeWorleyNoise.CellData cell = worley.GetCellDataFromPosition(rootPosition.x, rootPosition.z, rootFrequency);
        Node root = new Node();
        root.frequency = rootFrequency;
        root.height = rootPosition.y;
        root.cell = cell;

        rootPosition = root.Position();

        Node trunk = Trunk(root, root);
        Draw(rootPosition, trunk.Position(), trunk);
        Branch(branchLevels, trunk);

        worley.SetSeed(random.NextInt());
        color = Color.red;

        Node trunk2 = Trunk(trunk, root);
        Draw(trunk.Position(), trunk2.Position(), trunk2);
        Branch(branchLevels, trunk2);
    }

    Node Trunk(Node parent, Node root)
    {
        Node trunk = new Node();
        trunk.frequency = root.frequency;
        trunk.height = parent.height + layerHeight;
        trunk.cell = root.cell;

        return trunk;
    }

    void Branch(int levels, Node root)
    {
        NativeList<Node> drawNext = new NativeList<Node>(Allocator.Temp);
        drawNext.Add(root);
        bool first = true;

        while(levels > 0)
        {
            levels--;

            NativeArray<Node> copy = new NativeArray<Node>(drawNext.Length, Allocator.Temp);
            copy.CopyFrom(drawNext);
            drawNext.Clear();

            for(int d = 0; d < copy.Length; d++)
            {
                int multiplier = first ? 3 : 2;
                if(first) first = false;

                Node parent = copy[d];
                NativeList<Node> children = GetChildren(parent, root, multiplier);

                for(int i = 0; i < children.Length; i++)
                {
                    drawNext.Add(children[i]);
                    Draw(parent.Position(), children[i].Position(), children[i]);
                }
            }
        }
    }

    void Draw(float3 parentPosition, float3 childPosition, Node child)
    {
        Debug.DrawLine(parentPosition, childPosition, color, 1000);
        //CreateNodeDebug(child);
    }

    void DrawDebug(float3 parentPosition, float3 childPosition)
    {
        Debug.DrawLine(parentPosition, childPosition, new Color(color.r, color.g, color.b, 0.3f), 1000);
    }

    NativeList<Node> GetChildren(Node parent, Node root, int multiplier)
    {
        float frequency = parent.frequency * multiplier;

        NativeList<Node> children = new NativeList<Node>(Allocator.Temp);
        NativeQueue<TreeWorleyNoise.CellData> toCheck = new NativeQueue<TreeWorleyNoise.CellData>(Allocator.Temp);

        //  cell at position of parent
        TreeWorleyNoise.CellData initialCell = worley.GetCellDataFromPosition(parent.cell.position.x, parent.cell.position.z, frequency);
        toCheck.Enqueue(initialCell);

        NativeList<int2> checkedIndices = new NativeList<int2>(Allocator.Temp);
        checkedIndices.Add(initialCell.index);

        while(toCheck.Count > 0)
        {
            TreeWorleyNoise.CellData cell = toCheck.Dequeue();

            float distanceToEdge;
         
            TreeWorleyNoise.CellData cellPointInParent = worley.GetCellDataFromPositionWithDist2Edge(cell.position.x, cell.position.z, parent.frequency, out distanceToEdge);
            TreeWorleyNoise.CellData cellPointInRoot = worley.GetCellDataFromPosition(cell.position.x, cell.position.z, root.frequency);

            if(distanceToEdge == 0) Debug.Log("broke");

            float height = parent.height;
            height += (distanceToEdge - 0.2f)*layerHeight;
            
            if( !PointIsInParent(cellPointInParent, parent) ||
                !PointIsInParent(cellPointInRoot, root) )
            {
                DrawDebug(parent.Position(), cell.position + new float3(0, height, 0));
                continue;
            }

            
            
            Node newNode = new Node{
                height = height,
                frequency = frequency,
                parent = parent.cell,
                cell = cell
            };
            
            children.Add(newNode);

            for(int x = -1; x <= 1; x++)
                for(int z = -1; z <= 1; z++)
                {
                    int2 index = new int2(x, z);
                    if(checkedIndices.Contains(index))
                        continue;

                    TreeWorleyNoise.CellData cellToCheck = worley.GetCellDataFromIndex(cell.index + index, frequency);
                    toCheck.Enqueue(cellToCheck);
                    checkedIndices.Add(index);
                }
        }

        checkedIndices.Dispose();
        toCheck.Dispose();

        return children;
    }

    bool PointIsEligible(TreeWorleyNoise.CellData point, Node parent, TreeWorleyNoise.CellData child)
    {
        return  PointIsInParent(point, parent) &&
                !PointPassesParent(parent, child);
    }

    bool PointPassesParent(Node parent, TreeWorleyNoise.CellData child)
    {
        if(allowBranchesToPassTrunk)
            return false;

        float pointToRoot = Distance2D(child.position - rootPosition);
        float pointToParent = Distance2D(child.position - parent.cell.position);

        return pointToRoot < pointToParent;
    }

    bool PointIsInParent(TreeWorleyNoise.CellData point, Node parent)
    {
        if(!point.index.Equals(parent.cell.index))
            return false;

        return true;
    }

    float Distance(float3 v)
    {
        return math.abs(math.sqrt(v.x*v.x + v.y*v.y + v.z*v.z));
    }

    float Distance2D(float3 v)
    {
        return math.abs(math.sqrt(v.x*v.x + v.z*v.z));
    }
     
    struct Node
    {
        public float frequency;
        public float height;

        public TreeWorleyNoise.CellData parent;
        public TreeWorleyNoise.CellData cell;

        public float3 Position()
        {
            return cell.position + new float3(0, height, 0);
        }
    }

    struct RootNode
    {
        public Node node;
        public TreeWorleyNoise worley;
        public float frequency;
    }

    void CreateNodeDebug(Node node)
    {
        GameObject gObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gObj.AddComponent<BoxCollider>();
        NodeDebug nodeDebug = gObj.AddComponent<NodeDebug>();
        nodeDebug.node = node;

        gObj.transform.Translate(node.Position());
        gObj.transform.localScale = new float3(0.2f);
    }

    class NodeDebug : MonoBehaviour
    {
        public Node node;
        void OnMouseDown()
        {
            Debug.Log(node.cell.index);
        }
    }
}
