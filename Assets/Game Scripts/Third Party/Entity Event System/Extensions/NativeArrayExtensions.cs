﻿namespace BovineLabs.Entities.Extensions
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// The NativeArrayExtensions.
    /// </summary>
    public static class NativeArrayExtensions
    {
        public static unsafe NativeArray<TO> Reinterpret<TI, TO>(this NativeArray<TI> nativeArray)
            where TI : struct
            where TO : struct
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TO>(
                nativeArray.GetUnsafePtr(),
                nativeArray.Length,
                Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
#endif

            return array;
        }
    }
}