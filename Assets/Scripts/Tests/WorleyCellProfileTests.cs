using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Collections;
using Unity.Mathematics;

namespace Tests
{
    public class WorleyCellProfileTests
    {
        Unity.Mathematics.Random Random()
        {
            uint randomSeed = (uint)UnityEngine.Random.Range(0, 10000);
            return new Unity.Mathematics.Random(randomSeed);
        }

        WorleyNoise GetWorleyNoise()
        {
            Random random = Random();
            return new WorleyNoise()
            {
                frequency = 0.075f,
                seed = random.NextInt(),
                //seed = -625141570,
                perterbAmp = 0,
                cellularJitter = 0.3f,
                distanceFunction = WorleyNoise.DistanceFunction.Euclidean,
                cellularReturnType = WorleyNoise.CellularReturnType.Distance2
            };
        }

        WorleyNoise.CellProfile GetRandomCell()
        {
            WorleyNoise worley = GetWorleyNoise();
            Random random = Random();
            return worley.GetCellProfile(random.NextInt2(-100, 100));
        } 

        [Test]
        public void AllVerticesAreUnique()
        {
            WorleyNoise.CellProfile cell = GetRandomCell();

            bool foundMatch = false;

            for(int v = 0; v < cell.vertices.Length; v++)
                for(int m = 0; m < cell.vertices.Length; m++)
                {
                    if(v != m && cell.vertices[v].Equals(cell.vertices[m]))
                        foundMatch = true;
                }
            
            Assert.IsFalse(foundMatch);
        }
    }
}
