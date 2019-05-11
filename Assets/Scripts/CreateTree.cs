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

        /*for(int x = -3; x < 3; x++)
            for(int z = -3; z < 3; z++)
            {
                int2 index = new int2(x,z);
                float3 position = worley.GetCellDataFromIndex(index, 0.1f).position;
                GenerateTree(position);
            } */
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
