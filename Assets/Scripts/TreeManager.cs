using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class TreeManager : MonoBehaviour
{
    public const float rootFrequency = 0.1f;
    Unity.Mathematics.Random random;
    TreeWorleyNoise worley;

    void Start()
    {
        random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, 10000));
        //random = new Unity.Mathematics.Random(2456235);

        worley = new TreeWorleyNoise()
        {
            //seed = random.NextInt(),
            seed = 1234,
            //seed = -587290213, // Broken worley mesh
            perterbAmp = 0,
            cellularJitter = 0.4f,
            distanceFunction = TreeWorleyNoise.DistanceFunction.Euclidean,
            cellularReturnType = TreeWorleyNoise.CellularReturnType.Distance2
        };

        Debug.Log("Seed: "+worley.seed);

//        GenerateTree(int2.zero);
        WorleyCellMesh worleyMesh;
        worleyMesh.worley = this.worley;
        worleyMesh.index = int2.zero;
        worleyMesh.Execute();

        DebugWorley(20);
    }

    void CreateTrees(int range)
    {
        for(int x = -range; x < range; x++)
            for(int z = -range; z < range; z++)
            {
                int2 index = new int2(x,z);
                GenerateTree(index); 
            } 
    }

    void DebugWorley(int range)
    {
        for(int x = -range; x < range; x++)
            for(int z = -range; z < range; z++)
            {
                /*float xf = 0.1f * ( (float)math.abs(x) / range );
                float zf = 0.1f * ( (float)math.abs(z) / range ); */

                float dist2Edge;
                TreeWorleyNoise.CellData cell = worley.GetCellData(x, z, rootFrequency, out dist2Edge);

                float colorFloat = cell.value;
                float4 color = new float4(colorFloat/* + dist2Edge */, colorFloat, colorFloat, 1);

                CreateCube(new float3(x, 0, z), color);
            } 
    }

    public static GameObject CreateCube(float3 position, float4 c)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cube.transform.Translate(position);
        cube.transform.Rotate(new Vector3(90, 0, 0));
        cube.GetComponent<MeshRenderer>().material.color = new Color(c.x, c.y, c.z, c.w);
        return cube;
    }

    void GenerateTree(int2 index)
    {
        GenerateTreeMeshJob generator = new GenerateTreeMeshJob{
            rootIndex = index,
            rootWorley = worley,
            rootFrequency = rootFrequency,
            crownHeight = 5,
            random = random
        };
        generator.Execute();
    }
}
