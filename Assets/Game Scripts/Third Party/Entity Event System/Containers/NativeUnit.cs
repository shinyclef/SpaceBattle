namespace BovineLabs.Entities.Containers
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine.Assertions;

    /// <summary>
    /// A single value native container to allow values to be passed between jobs.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="NativeUnit{T}"/>.</typeparam>
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainer]
    public unsafe struct NativeUnit<T> : IDisposable
        where T : struct
    {
#pragma warning disable SA1308 // Variable names should not be prefixed
        [NativeDisableUnsafePtrRestriction]
        private void* m_Buffer;

        private Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#pragma warning restore SA1308 // Variable names should not be prefixed
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeUnit{T}"/> struct.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> of the <see cref="NativeUnit{T}"/>.</param>
        /// <param name="options">The default memory state.</param>
        public NativeUnit(Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            IsBlittableAndThrow();

            var size = UnsafeUtility.SizeOf<T>();
            m_Buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), allocator);
            m_AllocatorLabel = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, allocator);
#endif

            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_Buffer, UnsafeUtility.SizeOf<T>());
            }
        }

        /// <summary>
        /// Gets or sets the value of the unit.
        /// </summary>
        public T Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);
            }

            [WriteAccessRequired]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                UnsafeUtility.WriteArrayElement(m_Buffer, 0, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="NativeUnit{T}"/> has been initialized.
        /// </summary>
        public bool IsCreated => (IntPtr)m_Buffer != IntPtr.Zero;

        /// <inheritdoc/>
        [WriteAccessRequired]
        public void Dispose()
        {
            Assert.IsTrue(UnsafeUtility.IsValidAllocator(m_AllocatorLabel));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        [BurstDiscard]
        private static void IsBlittableAndThrow()
        {
            if (!UnsafeUtility.IsBlittable<T>())
            {
                throw new ArgumentException($"{typeof(T)} used in NativeArray<{typeof(T)}> must be blittable");
            }
        }
    }
}