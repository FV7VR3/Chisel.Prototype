﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace Chisel.Core
{
    // TODO: move somewhere else
    [BurstCompile(CompileSynchronously = true)]
    struct StoreToCacheJob : IJob
    {
        // Read
        [NoAlias, ReadOnly] public NativeArray<IndexOrder> allTreeBrushIndexOrders;
        [NoAlias, ReadOnly] public NativeArray<ChiselAABB> brushTreeSpaceBoundCache;
        [NoAlias, ReadOnly] public NativeArray<ChiselBlobAssetReference<ChiselBrushRenderBuffer>> brushRenderBufferCache;

        // Read, Write
        [NoAlias] public NativeHashMap<CompactNodeID, ChiselAABB> brushTreeSpaceBoundLookup;
        [NoAlias] public NativeHashMap<CompactNodeID, ChiselBlobAssetReference<ChiselBrushRenderBuffer>> brushRenderBufferLookup;

        public void Execute()
        {
            brushTreeSpaceBoundLookup.Clear();
            brushRenderBufferLookup.Clear();
            for (int i = 0; i < allTreeBrushIndexOrders.Length; i++)
            {
                var nodeID = allTreeBrushIndexOrders[i].compactNodeID;
                brushTreeSpaceBoundLookup[nodeID] = brushTreeSpaceBoundCache[i];
                brushRenderBufferLookup[nodeID] = brushRenderBufferCache[i];
            }
        }
    }
}
