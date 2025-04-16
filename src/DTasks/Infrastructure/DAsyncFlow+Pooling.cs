// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using DTasks.Utils;

namespace DTasks.Infrastructure;

// Code borrowed and adapted from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/PoolingAsyncValueTaskMethodBuilderT.cs
public sealed partial class DAsyncFlow
{
    private static readonly PaddedReference[] s_perCoreCache = new PaddedReference[Environment.ProcessorCount];
    [ThreadStatic] private static DAsyncFlow? t_tlsCache;

    private static DAsyncFlow RentFromCache(bool returnToCache)
    {
        DAsyncFlow? flow = t_tlsCache;
        if (flow is not null)
        {
            t_tlsCache = null;
        }
        else
        {
            ref DAsyncFlow? slot = ref PerCoreCacheSlot;
            if (slot is null || (flow = Interlocked.Exchange(ref slot, null)) is null)
            {
                flow = new DAsyncFlow();
            }
        }

        Debug.Assert(flow._state is FlowState.Idling);
        
        flow._state = FlowState.Pending;
        flow._returnToCache = returnToCache;
        return flow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnToCache()
    {
        Debug.Assert(_state is FlowState.Pending);
        
        _state = FlowState.Idling;
        
        if (t_tlsCache is null)
        {
            t_tlsCache = this;
        }
        else
        {
            ref DAsyncFlow? slot = ref PerCoreCacheSlot;
            if (slot is null)
            {
                Volatile.Write(ref slot, this);
            }
        }
    }

    private static ref DAsyncFlow? PerCoreCacheSlot
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(s_perCoreCache.Length == Environment.ProcessorCount, $"{s_perCoreCache.Length} != {Environment.ProcessorCount}");
            int i = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);

#if DEBUG
            object? transientValue = s_perCoreCache[i].Object;
            Debug.Assert(transientValue is null or DAsyncFlow, $"Expected null or {nameof(DAsyncFlow)}, got '{transientValue}'");
#endif
            return ref Unsafe.As<object?, DAsyncFlow?>(ref s_perCoreCache[i].Object);
        }
    }
}