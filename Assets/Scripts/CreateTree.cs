using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class CreateTree : MonoBehaviour
{
    Unity.Mathematics.Random random;
    TreeWorleyNoise worley;

    void Start()
    {
        random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, 10000));
        //random = new Unity.Mathematics.Random(2456235);

        worley = new TreeWorleyNoise()
        {
            seed = random.NextInt(),
            perterbAmp = 0,
            cellularJitter = 0.4f,
            distanceFunction = TreeWorleyNoise.DistanceFunction.Euclidean,
            cellularReturnType = TreeWorleyNoise.CellularReturnType.Distance2
        };

        GenerateTree(float3.zero);

        DebugWorley(20);
    }

    void CreateTrees(int range)
    {
        for(int x = -range; x < range; x++)
            for(int z = -range; z < range; z++)
            {
                int2 index = new int2(x,z);
                float3 position = worley.GetCellDataFromIndex(index, 0.1f).position;
                GenerateTree(position); 
            } 
    }

    void DebugWorley(int range)
    {
        for(int x = -range; x < range; x++)
            for(int z = -range; z < range; z++)
            {
                
                float dist2Edge;
                TreeWorleyNoise.CellData cell = worley.GetCellDataFromPositionWithDist2Edge(x, z, 0.1f, out dist2Edge);

                float colorFloat = cell.value * dist2Edge;
                float4 color = new float4(colorFloat, colorFloat, colorFloat, 1);

                CreateCube(new float3(x, 0, z), color);

                /*int2 index = new int2(x,z);
                float3 position = worley.GetCellDataFromIndex(index, 0.1f).position;
                GenerateTree(position); */
            } 
    }

    GameObject CreateCube(float3 position, float4 c)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.Translate(position);
        cube.GetComponent<MeshRenderer>().material.color = new Color(c.x, c.y, c.z, c.w);
        return cube;
    }

    void GenerateTree(float3 rootPosition)
    {
        TreeGenerator treeGenerator = new TreeGenerator{
            rootFrequency = 0.1f,
            rootPosition = rootPosition,
            layerHeight = 5,
            worley = worley,

            branchLevels = 3,
            trunkLevels = 2,
            allowBranchesToPassTrunk = true,

            random = random
        };
        treeGenerator.Execute();
    }
}
