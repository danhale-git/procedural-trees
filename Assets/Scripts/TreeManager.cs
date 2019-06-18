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

    BowyerWatsonTriangulation bowyerWatson;

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

        worley = new TreeWorleyNoise()
        {
            seed = random.NextInt(),
            perterbAmp = 0,
            cellularJitter = 0.4f,
            distanceFunction = TreeWorleyNoise.DistanceFunction.Euclidean,
            cellularReturnType = TreeWorleyNoise.CellularReturnType.Distance2
        };

        Debug.Log("Seed: "+worley.seed);

        TestBW();
    }

    void TestBW()
    {
        float2 min = new float2(-10, -10);
        float2 max = new float2(10, 10);

        bowyerWatson = new BowyerWatsonTriangulation();
        bowyerWatson.points = new NativeList<float2>(Allocator.Persistent);

        for(int i = 0; i < 4; i++)
            bowyerWatson.points.Add(random.NextFloat2(min, max));

        bowyerWatson.Test();

        bowyerWatson.points.Dispose();

    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit) && Input.GetMouseButtonDown(0))
        {
            float2 point = new float2(hit.point.x, hit.point.z);
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
