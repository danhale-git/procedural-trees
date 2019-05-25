using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public struct WorleyCellMesh
{
    public TreeWorleyNoise worley;
    public int2 index;

    public void Execute()
    {
        TreeWorleyNoise.CellData cell = worley.GetCellData(index, TreeManager.rootFrequency);
        TreeManager.CreateCube(cell.position + new float3(0,10,0), new float4(1));
    }
}
