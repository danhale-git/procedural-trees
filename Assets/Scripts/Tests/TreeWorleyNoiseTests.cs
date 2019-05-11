using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;

namespace Tests
{
    public class TreeWorleyNoiseTests
    {
        Unity.Mathematics.Random Random()
        {
            uint randomSeed = (uint)UnityEngine.Random.Range(0, 10000);
            return new Unity.Mathematics.Random(randomSeed);
        }
        
        [Test]
        public void RandomNotZero()
        {
            Unity.Mathematics.Random random = Random();
            bool notZero = random.NextInt() != 0 && !random.NextFloat3().Equals(float3.zero);
            Assert.IsTrue(notZero);
        }

        [Test]
        public void IndexesMatch()
        {
            WorleyDatas datas = GetWorleyDatas();
            bool match =    datas.cellFromIndex.index.Equals(datas.cellFromPosition.index) &&
                            datas.cellFromIndex.index.Equals(datas.pointFromPosition.currentCellIndex
                        );
            Assert.IsTrue(match);
        }

        [Test]
        public void PositionsMatch()
        {
            WorleyDatas datas = GetWorleyDatas();
            bool match =    datas.cellFromIndex.position.Equals(datas.cellFromPosition.position) &&
                            datas.cellFromIndex.position.Equals(datas.pointFromPosition.currentCellPosition
                        );
            Assert.IsTrue(match);
        }
        [Test]
        public void ValuesMatch()
        {
            WorleyDatas datas = GetWorleyDatas();
            bool match =    datas.cellFromIndex.value.Equals(datas.cellFromPosition.value) &&
                            datas.cellFromIndex.value.Equals(datas.pointFromPosition.currentCellValue
                        );
            Assert.IsTrue(match);
        }

        WorleyDatas GetWorleyDatas()
        {
            TreeWorleyNoise worley = new TreeWorleyNoise(
                Random().NextInt(),//seed
                0,//peterb,
                0.4f,//cellularJitter
                TreeWorleyNoise.DistanceFunction.Euclidean,//distance function
                TreeWorleyNoise.CellularReturnType.Distance2//  cellular return type
            );
            float frequency = 0.01f;

            float3 randomPosition = Random().NextFloat3();

            TreeWorleyNoise.PointData pointFromPosition;
            TreeWorleyNoise.CellData cellFromIndex;
            TreeWorleyNoise.CellData cellFromPosition;

            pointFromPosition = worley.GetPointDataFromPosition(randomPosition.x, randomPosition.z, frequency);
            cellFromIndex = worley.GetCellDataFromIndex(pointFromPosition.currentCellIndex, frequency);
            cellFromPosition = worley.GetCellDataFromPosition(cellFromIndex.position.x, cellFromIndex.position.z, frequency);

            return new WorleyDatas(pointFromPosition, cellFromIndex, cellFromPosition);
        }

        struct WorleyDatas
        {
            public readonly TreeWorleyNoise.PointData pointFromPosition;
            public readonly TreeWorleyNoise.CellData cellFromIndex;
            public readonly TreeWorleyNoise.CellData cellFromPosition;
            public WorleyDatas(TreeWorleyNoise.PointData pointFromPosition, TreeWorleyNoise.CellData cellFromIndex, TreeWorleyNoise.CellData cellFromPosition)
            {
                this.pointFromPosition = pointFromPosition;
                this.cellFromIndex = cellFromIndex;
                this.cellFromPosition = cellFromPosition;
            }
        }
    }
}
