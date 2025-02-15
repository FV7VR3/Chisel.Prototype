using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace Chisel.Core
{
    [BurstCompile(CompileSynchronously = true)]
    unsafe struct GatherBrushIntersectionPairsJob : IJob
    {
        // Read
        [NoAlias, ReadOnly] public NativeListArray<BrushIntersectWith> brushBrushIntersections;

        // Write
        [NativeDisableUnsafePtrRestriction]
        [NoAlias, WriteOnly] public UnsafeList*             brushIntersectionsWith;
        [NoAlias, WriteOnly] public NativeArray<int2>       brushIntersectionsWithRange;

        // Per thread scratch memory
        [NativeDisableContainerSafetyRestriction] NativeList<BrushPair> intersections;


        struct ListComparer : System.Collections.Generic.IComparer<BrushPair>
        {
            public int Compare(BrushPair x, BrushPair y)
            {
                var orderX = x.brushNodeOrder0;
                var orderY = y.brushNodeOrder0;
                var diff = orderX.CompareTo(orderY);
                if (diff != 0)
                    return diff;

                orderX = x.brushNodeOrder1;
                orderY = y.brushNodeOrder1;
                return orderX.CompareTo(orderY);
            }
        }


        public void Execute()
        {
            var minCount = brushBrushIntersections.Count * 16;

            NativeCollectionHelpers.EnsureCapacityAndClear(ref intersections, minCount);

            for (int i = 0; i < brushBrushIntersections.Count; i++)
            {
                if (!brushBrushIntersections.IsAllocated(i))
                    continue;
                var subArray = brushBrushIntersections[i];
                for (int j = 0; j < subArray.Count; j++)
                {
                    var intersectWith = subArray[j];
                    var pair = new BrushPair
                    {
                        brushNodeOrder0 = i,
                        brushNodeOrder1 = intersectWith.brushNodeOrder1,
                        type = intersectWith.type
                    };
                    intersections.Add(pair);
                    pair.Flip();
                    intersections.Add(pair);
                }
            }
            brushIntersectionsWith->Clear();
            if (intersections.Length == 0)
                return;

            intersections.Sort(new ListComparer());           

            var currentPair = intersections[0];
            int previousOrder = currentPair.brushNodeOrder0;
            brushIntersectionsWith->Add(new BrushIntersectWith
            {
                brushNodeOrder1 = currentPair.brushNodeOrder1,
                type            = currentPair.type,
            });
            int2 range = new int2(0, 1);
            for (int i = 1; i < intersections.Length; i++)
            {
                currentPair = intersections[i];
                int currentOrder = currentPair.brushNodeOrder0;
                brushIntersectionsWith->Add(new BrushIntersectWith
                {
                    brushNodeOrder1 = currentPair.brushNodeOrder1,
                    type            = currentPair.type,
                });
                if (currentOrder != previousOrder)
                {
                    //Debug.Log($"{previousOrder} {range}");
                    brushIntersectionsWithRange[previousOrder] = range;
                    previousOrder = currentOrder;
                    range.x = i;
                    range.y = 1;
                } else
                    range.y++;
            }
            brushIntersectionsWithRange[previousOrder] = range;
        }
    }
}
