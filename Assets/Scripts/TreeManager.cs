using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class TreeManager : MonoBehaviour
{
    Unity.Mathematics.Random random;
    WorleyNoise worley;

    BowyerWatson bowyerWatson;

    static GameObject textPrefab;
    public static void CreateText(float3 position, string text)
    {
        GameObject textObject = Instantiate<GameObject>(textPrefab, position, Quaternion.Euler(90,0,0));
        TextMesh textMesh = textObject.GetComponent<TextMesh>();
        textMesh.text = text;
    }

    void Start()
    {
        textPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/New Text.prefab");

        random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, 10000));
        //random = new Unity.Mathematics.Random(2456235);

        worley = new WorleyNoise()
        {
            frequency = 0.075f,
            seed = random.NextInt(),
            //seed = -625141570,
            perterbAmp = 0,
            cellularJitter = 0.3f,
            distanceFunction = WorleyNoise.DistanceFunction.Euclidean,
            cellularReturnType = WorleyNoise.CellularReturnType.Distance2
        };

        Debug.Log("Seed: "+worley.seed);

        TreeGenerator generator = new TreeGenerator
        {
            worley = this.worley,
            meshPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TreeMesh.prefab"),
            material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DefaultMat.mat")
        };

        bool one = true;
        int range = 1;

        if(!one)
            for(int x = -range; x <= range; x++)
                for(int z = -range; z <= range; z++)
                {
                    int2 index = new int2(x, z);
                    generator.Generate(index);
                }
        else
            generator.Generate(int2.zero);

        //DebugWorley(18);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit) &&  Input.GetMouseButtonDown(0))
        {
            float2 point = new float2(hit.point.x, hit.point.z);
            //var vert = new ClockwiseVertex(new float2(hit.point.x, hit.point.z), new float2(0,0));
            //Debug.Log(vert.GetAngle());
        }

    }

    void OnDrawGizmos()
    {
        /*if(worleyMesh.circles == null) return;

        foreach(WorleyCellMesh.Circumcircle circle in worleyMesh.circles)
        {
            Gizmos.DrawWireSphere(circle.center, circle.radius);
        } */
    }

    void CreateTrees(int range)
    {
        for(int x = -range; x < range; x++)
            for(int z = -range; z < range; z++)
            {
                int2 index = new int2(x,z);
                //GenerateTree(index); 
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
                WorleyNoise.CellData cell = worley.GetCellData(x, z, out dist2Edge);

                float colorFloat = cell.value;
                Color color = new Color(colorFloat/* + dist2Edge */, colorFloat, colorFloat, 1);

                CreateCube(new float3(x, 0, z), color);
            } 
    }

    public static GameObject CreateCube(float3 position, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cube.transform.Translate(position);
        cube.transform.Rotate(new Vector3(90, 0, 0));
        cube.GetComponent<MeshRenderer>().material.color = color;
        return cube;
    }
    public static GameObject CreateCube(float2 position, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cube.transform.Translate(new float3(position.x, 0, position.y));
        cube.transform.Rotate(new Vector3(90, 0, 0));
        cube.GetComponent<MeshRenderer>().material.color = color;
        return cube;
    }

    /*void GenerateTree(int2 index)
    {
        GenerateTreeMeshJob generator = new GenerateTreeMeshJob{
            rootIndex = index,
            rootWorley = worley,
            rootFrequency = rootFrequency,
            crownHeight = 5,
            random = random
        };
        generator.Execute();
    } */
}
