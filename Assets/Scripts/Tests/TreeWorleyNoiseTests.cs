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
        public void GetWorleyNoiseNoAdjacent()
        {
            TreeWorleyNoise.CellData adjacentPlaceholder;
            float dist2EdgePlaceholder;

            TreeWorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);

            bool somethingWasGenerated = cell.value != 0;
            bool adjacentNotGenerated = adjacentPlaceholder.value == 0;

            Assert.IsTrue(somethingWasGenerated && adjacentNotGenerated);
        }

        [Test]
        public void GetWorleyNoiseNoDistance()
        {
            TreeWorleyNoise.CellData adjacentPlaceholder;
            float dist2EdgePlaceholder;

            TreeWorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotGenerated = dist2EdgePlaceholder == 999999;

            Assert.IsTrue(somethingWasGenerated && distanceNotGenerated);
        }

        [Test]
        public void GetWorleyDistanceNotZero()
        {
            TreeWorleyNoise.CellData adjacentPlaceholder;
            float dist2Edge;

            TreeWorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2Edge, false, true);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotZero = dist2Edge != 0;

            Assert.IsTrue(somethingWasGenerated && distanceNotZero);
        }

        [Test]
        public void GetWorleyDistanceNotNines()
        {
            TreeWorleyNoise.CellData adjacentPlaceholder;
            float dist2Edge;

            TreeWorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2Edge, false, true);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotNines = dist2Edge != 999999;

            Assert.IsTrue(somethingWasGenerated && distanceNotNines);
        }

        TreeWorleyNoise.CellData GetWorleyDataHelper(out TreeWorleyNoise.CellData adjacentPlaceholder, out float dist2EdgePlaceholder, bool getAdjacent, bool getDistance)
        {
            TreeWorleyNoise worley = GetWorleyGenerator();
            float3 randomPosition = Random().NextFloat3();

            return worley.GetWorleyData(
                randomPosition.x,
                randomPosition.y,
                0.01f,
                out adjacentPlaceholder,
                out dist2EdgePlaceholder,
                getAdjacent,
                getDistance
            );
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
            bool match = datas.cellFromIndex.index.Equals(datas.cellFromPosition.index);
            Assert.IsTrue(match);
        }

        [Test]
        public void PositionsMatch()
        {
            WorleyDatas datas = GetWorleyDatas();
            bool match = datas.cellFromIndex.position.Equals(datas.cellFromPosition.position);
            Assert.IsTrue(match);
        }
        [Test]
        public void ValuesMatch()
        {
            WorleyDatas datas = GetWorleyDatas();
            bool match = datas.cellFromIndex.value.Equals(datas.cellFromPosition.value);
            Assert.IsTrue(match);
        }

        TreeWorleyNoise GetWorleyGenerator()
        {
            return new TreeWorleyNoise()
            {
                seed = Random().NextInt(),
                perterbAmp = 0,
                cellularJitter = 0.4f,
                distanceFunction = TreeWorleyNoise.DistanceFunction.Euclidean,
                cellularReturnType = TreeWorleyNoise.CellularReturnType.Distance2
            };
        }

        WorleyDatas GetWorleyDatas()
        {
            TreeWorleyNoise worley = GetWorleyGenerator();
            float frequency = 0.01f;

            float3 randomPosition = Random().NextFloat3();

            TreeWorleyNoise.CellData cellFromIndex;
            TreeWorleyNoise.CellData cellFromPosition;

            cellFromPosition = worley.GetCellDataFromPosition(randomPosition.x, randomPosition.z, frequency);
            cellFromIndex = worley.GetCellDataFromIndex(cellFromPosition.index, frequency);

            return new WorleyDatas(cellFromIndex, cellFromPosition);
        }

        struct WorleyDatas
        {
            public readonly TreeWorleyNoise.CellData cellFromIndex;
            public readonly TreeWorleyNoise.CellData cellFromPosition;
            public WorleyDatas(TreeWorleyNoise.CellData cellFromIndex, TreeWorleyNoise.CellData cellFromPosition)
            {
                this.cellFromIndex = cellFromIndex;
                this.cellFromPosition = cellFromPosition;
            }
        }
    }
}
