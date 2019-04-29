﻿namespace BovineLabs.Entities.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeHashMapImposter<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        public NativeHashMapDataImposter* Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle Safety;

        [NativeSetClassTypeToNullOnSchedule]
        public DisposeSentinel DisposeSentinel;
#endif

        public Allocator AllocatorLabel;

        public static implicit operator NativeHashMapImposter<TKey, TValue>(NativeHashMap<TKey, TValue> hashMap)
        {
            var ptr = UnsafeUtility.AddressOf(ref hashMap);
            UnsafeUtility.CopyPtrToStructure(ptr, out NativeHashMapImposter<TKey, TValue> imposter);
            return imposter;
        }


        internal static bool TryReplaceValue(NativeHashMapDataImposter* data, TKey key, TValue item, bool isMultiHashMap)
        {
            if (isMultiHashMap)
            {
                return false;
            }

            return TryReplaceFirstValueAtomic(data, key, item, out _);
        }

        private static bool TryReplaceFirstValueAtomic(NativeHashMapDataImposter* data, TKey key,
            TValue item, out NativeMultiHashMapIteratorImposter<TKey> it)
        {
            it.key = key;
            if (data->AllocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)data->Buckets;
            int bucket = key.GetHashCode() & data->BucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryReplaceNextValueAtomic(data, item, ref it);
        }

        private static bool TryReplaceNextValueAtomic(NativeHashMapDataImposter* data, TValue item, ref NativeMultiHashMapIteratorImposter<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            if (entryIdx < 0 || entryIdx >= data->Capacity)
            {
                return false;
            }

            int* nextPtrs = (int*)data->Next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->Keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->Capacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Write the value
            UnsafeUtility.WriteArrayElement(data->Keys, entryIdx, it.key);
            UnsafeUtility.WriteArrayElement(data->Values, entryIdx, item);
            return true;
        }
    }
}