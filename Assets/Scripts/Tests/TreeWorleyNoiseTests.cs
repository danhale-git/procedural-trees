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
            WorleyNoise.CellData adjacentPlaceholder;
            float dist2EdgePlaceholder;

            WorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);

            bool somethingWasGenerated = cell.value != 0;
            bool adjacentNotGenerated = adjacentPlaceholder.value == 0;

            Assert.IsTrue(somethingWasGenerated && adjacentNotGenerated);
        }

        [Test]
        public void GetWorleyNoiseNoDistance()
        {
            WorleyNoise.CellData adjacentPlaceholder;
            float dist2EdgePlaceholder;

            WorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2EdgePlaceholder, false, false);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotGenerated = dist2EdgePlaceholder == 999999;

            Assert.IsTrue(somethingWasGenerated && distanceNotGenerated);
        }

        [Test]
        public void GetWorleyDistanceNotZero()
        {
            WorleyNoise.CellData adjacentPlaceholder;
            float dist2Edge;

            WorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2Edge, false, true);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotZero = dist2Edge != 0;

            Assert.IsTrue(somethingWasGenerated && distanceNotZero);
        }

        [Test]
        public void GetWorleyDistanceNotNines()
        {
            WorleyNoise.CellData adjacentPlaceholder;
            float dist2Edge;

            WorleyNoise.CellData cell = GetWorleyDataHelper(out adjacentPlaceholder, out dist2Edge, false, true);

            bool somethingWasGenerated = cell.value != 0;
            bool distanceNotNines = dist2Edge != 999999;

            Assert.IsTrue(somethingWasGenerated && distanceNotNines);
        }

        [Test]
        public void AdjacentCellIsDifferent()
        {
            WorleyNoise.CellData adjacent;
            float dist2EdgePlaceholder;

            WorleyNoise.CellData cell = GetWorleyDataHelper(out adjacent, out dist2EdgePlaceholder, true, false);

            bool notZero = adjacent.value != 0;
            bool different = !adjacent.index.Equals(cell.index);
            Assert.IsTrue(notZero && different, "adjacent is zero: "+notZero);
        }

        WorleyNoise.CellData GetWorleyDataHelper(out WorleyNoise.CellData adjacentPlaceholder, out float dist2EdgePlaceholder, bool getAdjacent, bool getDistance)
        {
            WorleyNoise worley = GetWorleyGenerator();
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

        WorleyNoise GetWorleyGenerator()
        {
            return new WorleyNoise()
            {
                frequency = 0.01f,
                seed = Random().NextInt(),
                perterbAmp = 0,
                cellularJitter = 0.4f,
                distanceFunction = WorleyNoise.DistanceFunction.Euclidean,
                cellularReturnType = WorleyNoise.CellularReturnType.Distance2
            };
        }

        WorleyDatas GetWorleyDatas()
        {
            WorleyNoise worley = GetWorleyGenerator();

            float3 randomPosition = Random().NextFloat3();

            WorleyNoise.CellData cellFromIndex;
            WorleyNoise.CellData cellFromPosition;

            cellFromPosition = worley.GetCellData(randomPosition);
            cellFromIndex = worley.GetCellData(cellFromPosition.index);

            return new WorleyDatas(cellFromIndex, cellFromPosition);
        }

        struct WorleyDatas
        {
            public readonly WorleyNoise.CellData cellFromIndex;
            public readonly WorleyNoise.CellData cellFromPosition;
            public WorleyDatas(WorleyNoise.CellData cellFromIndex, WorleyNoise.CellData cellFromPosition)
            {
                this.cellFromIndex = cellFromIndex;
                this.cellFromPosition = cellFromPosition;
            }
        }
    }
}
