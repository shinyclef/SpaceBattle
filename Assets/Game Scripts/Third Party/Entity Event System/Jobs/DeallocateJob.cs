namespace BovineLabs.Entities.Jobs
{
    using JetBrains.Annotations;
    using System;
    using Unity.Collections;
    using Unity.Jobs;

    /// <summary>
    /// Simple job to deallocate at the end of a dependency chain.
    /// </summary>
    /// <typeparam name="T">The type to deallocate.</typeparam>
    public struct DeallocateJob<T> : IJob
        where T : struct, IDisposable
    {
        /// <summary>
        /// The item to deallocate.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public T Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeallocateJob{T}"/> struct.
        /// </summary>
        /// <param name="value">The item to deallocate.</param>
        public DeallocateJob(T value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public void Execute()
        {
            /* noop */
        }
    }
}
