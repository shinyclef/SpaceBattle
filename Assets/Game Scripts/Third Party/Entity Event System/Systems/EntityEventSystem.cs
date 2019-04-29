namespace BovineLabs.Entities.Systems
{
    using BovineLabs.Entities.Jobs;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine.Assertions;
    using UnityEngine.Profiling;

    /// <summary>
    /// The BatchBarrierSystem.
    /// </summary>
    //[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public sealed class EntityEventSystem : ComponentSystem
    {
        private readonly Dictionary<Type, IEventBatch> types = new Dictionary<Type, IEventBatch>();

        private readonly Dictionary<KeyValuePair<Type, Type>, IEventBatch> bufferTypes =
            new Dictionary<KeyValuePair<Type, Type>, IEventBatch>();

        private JobHandle producerHandle = default;

        /// <summary>
        /// The interface for the batch systems.
        /// </summary>
        private interface IEventBatch : IDisposable
        {
            /// <summary>
            /// Updates the batch, destroy, create, set.
            /// </summary>
            /// <param name="entityManager">The <see cref="EntityManager"/>.</param>
            /// <returns>A <see cref="JobHandle"/>.</returns>
            JobHandle Update(EntityManager entityManager);

            /// <summary>
            /// Resets the batch for the next frame.
            /// </summary>
            void Reset();
        }

        /// <summary>
        /// Creates a queue where any added component added will be batch created as an entity event
        /// and automatically destroyed 1 frame later.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/>.</typeparam>
        /// <returns>A <see cref="NativeQueue{T}"/> which any component that is added will be turned into a single frame event.</returns>
        public NativeQueue<T> CreateEventQueue<T>()
            where T : struct, IComponentData
        {
            if (!types.TryGetValue(typeof(T), out var create))
            {
                create = types[typeof(T)] = new EventBatch<T>(EntityManager);
            }

            return ((EventBatch<T>)create).GetNew();
        }

        /// <summary>
        /// Creates a new event with a component and a buffer. These events are batch created
        /// will be automatically destroyed 1 frame later.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/>.</typeparam>
        /// <typeparam name="TB">The type of <see cref="IBufferElementData"/>.</typeparam>
        /// <param name="component">The event component.</param>
        /// <param name="buffer">The event buffer.</param>
        public void CreateBufferEvent<T, TB>(T component, NativeArray<TB> buffer)
            where T : struct, IComponentData
            where TB : struct, IBufferElementData
        {
            var key = new KeyValuePair<Type, Type>(typeof(T), typeof(TB));

            if (!bufferTypes.TryGetValue(key, out var create))
            {
                create = bufferTypes[key] = new EventBufferBatch<T, TB>();
            }

            ((EventBufferBatch<T, TB>)create).Enqueue(component, buffer);
        }

        /// <summary>
        /// Add a dependency handle.
        /// </summary>
        /// <param name="handle">The dependency handle.</param>
        public void AddJobHandleForProducer(JobHandle handle)
        {
            producerHandle = JobHandle.CombineDependencies(producerHandle, handle);
        }

        /// <inheritdoc />
        protected override void OnDestroyManager()
        {
            foreach (var t in types)
            {
                t.Value.Dispose();
            }

            types.Clear();

            foreach (var t in bufferTypes)
            {
                t.Value.Dispose();
            }

            bufferTypes.Clear();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            producerHandle.Complete();
            producerHandle = default;

            var handles = new NativeArray<JobHandle>(types.Count + bufferTypes.Count, Allocator.TempJob);

            int index = 0;

            foreach (var t in types)
            {
                handles[index++] = t.Value.Update(EntityManager);
            }

            foreach (var t in bufferTypes)
            {
                handles[index++] = t.Value.Update(EntityManager);
            }

            JobHandle.CompleteAll(handles);
            handles.Dispose();

            foreach (var t in types)
            {
                t.Value.Reset();
            }

            foreach (var t in bufferTypes)
            {
                t.Value.Reset();
            }
        }

        private class EventBatch<T> : EventBatchBase
            where T : struct, IComponentData
        {
            private readonly List<NativeQueue<T>> queues = new List<NativeQueue<T>>();
            private readonly EntityQuery query;

            private EntityArchetype archetype;

            public EventBatch(EntityManager entityManager)
            {
                query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            }

            /// <inheritdoc />
            protected override ComponentType[] ArchetypeTypes { get; } = { typeof(T) };

            public NativeQueue<T> GetNew()
            {
                // Having allocation leak warnings when using TempJob
                var queue = new NativeQueue<T>(Allocator.Persistent);
                queues.Add(queue);

                return queue;
            }

            public override void Reset()
            {
                base.Reset();

                foreach (var queue in queues)
                {
                    queue.Dispose();
                }

                queues.Clear();
            }

            protected override int GetCount()
            {
                var sum = 0;
                foreach (var i in queues)
                {
                    sum += i.Count;
                }

                return sum;
            }

            /// <inheritdoc />
            protected override JobHandle SetComponentData(EntityManager entityManager, NativeArray<Entity> entities)
            {
                var componentType = entityManager.GetArchetypeChunkComponentType<T>(false);

                var chunks = query.CreateArchetypeChunkArray(Allocator.TempJob);

                int startIndex = 0;

                var handles = new NativeArray<JobHandle>(queues.Count, Allocator.TempJob);

                // Create a job for each queue. This is designed so that these jobs can run simultaneously.
                for (var index = 0; index < queues.Count; index++)
                {
                    var queue = queues[index];
                    var job = new SetComponentDataJob
                    {
                        Chunks = chunks,
                        Queue = queue,
                        StartIndex = startIndex,
                        ComponentType = componentType,
                    };

                    startIndex += queue.Count;

                    handles[index] = job.Schedule();
                }

                var handle = JobHandle.CombineDependencies(handles);
                handles.Dispose();

                // Deallocate the chunk array
                handle = new DeallocateJob<NativeArray<ArchetypeChunk>>(chunks).Schedule(handle);

                return handle;
            }

            [BurstCompile]
            private struct SetComponentDataJob : IJob
            {
                public int StartIndex;

                public NativeQueue<T> Queue;

                [ReadOnly]
                public NativeArray<ArchetypeChunk> Chunks;

                [NativeDisableContainerSafetyRestriction]
                public ArchetypeChunkComponentType<T> ComponentType;

                /// <inheritdoc />
                public void Execute()
                {
                    GetIndexes(out var chunkIndex, out var entityIndex);

                    for (; chunkIndex < Chunks.Length; chunkIndex++)
                    {
                        var chunk = Chunks[chunkIndex];

                        var components = chunk.GetNativeArray(ComponentType);

                        while (Queue.TryDequeue(out var item) && entityIndex < components.Length)
                        {
                            components[entityIndex++] = item;
                        }

                        if (Queue.Count == 0)
                        {
                            return;
                        }

                        entityIndex = entityIndex < components.Length ? entityIndex : 0;
                    }
                }

                private void GetIndexes(out int chunkIndex, out int entityIndex)
                {
                    var sum = 0;

                    for (chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
                    {
                        var chunk = Chunks[chunkIndex];

                        var length = chunk.Count;

                        if (sum + length < StartIndex)
                        {
                            sum += length;
                            continue;
                        }

                        entityIndex = StartIndex - sum;
                        return;
                    }

                    throw new ArgumentOutOfRangeException(nameof(StartIndex));
                }
            }
        }

        private class EventBufferBatch<T, TB> : EventBatchBase
            where T : struct, IComponentData
            where TB : struct, IBufferElementData
        {
            private readonly List<KeyValuePair<T, NativeArray<TB>>> queue = new List<KeyValuePair<T, NativeArray<TB>>>();

            /// <inheritdoc />
            protected override ComponentType[] ArchetypeTypes { get; } = { typeof(T), typeof(TB) };

            public void Enqueue(T component, NativeArray<TB> buffer)
            {
                queue.Add(new KeyValuePair<T, NativeArray<TB>>(component, buffer));
            }

            /// <inheritdoc />
            public override void Reset()
            {
                base.Reset();

                foreach (var pending in queue)
                {
                    pending.Value.Dispose();
                }

                queue.Clear();
            }

            protected override int GetCount()
            {
                return queue.Count;
            }

            /// <inheritdoc />
            protected override JobHandle SetComponentData(EntityManager entityManager, NativeArray<Entity> entities)
            {
                Assert.AreEqual(queue.Count, entities.Length);

                for (var index = 0; index < queue.Count; index++)
                {
                    var pair = queue[index];
                    var entity = entities[index];

                    entityManager.SetComponentData(entity, pair.Key);

                    var buffer = entityManager.GetBuffer<TB>(entity);
                    buffer.AddRange(pair.Value);
                }

                return default;
            }
        }

        private abstract class EventBatchBase : IEventBatch
        {
            private EntityArchetype archetype;

            private NativeArray<Entity> entities;

            protected abstract ComponentType[] ArchetypeTypes { get; }

            /// <summary>
            /// Handles the destroying of entities.
            /// </summary>
            /// <param name="entityManager">The entity manager.</param>
            /// <returns>A default handle.</returns>
            public JobHandle Update(EntityManager entityManager)
            {
                DestroyEntities(entityManager);

                if (!CreateEntities(entityManager))
                {
                    return default;
                }

                return SetComponentData(entityManager, entities);
            }

            /// <summary>
            /// Optional reset method.
            /// </summary>
            public virtual void Reset()
            {
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (entities.IsCreated)
                {
                    entities.Dispose();
                }

                Reset();
            }

            protected abstract int GetCount();

            protected abstract JobHandle SetComponentData(EntityManager entityManager, NativeArray<Entity> entities);

            private void DestroyEntities(EntityManager entityManager)
            {
                Profiler.BeginSample("DestroyEntity");

                if (entities.IsCreated)
                {
                    entityManager.DestroyEntity(entities);
                    entities.Dispose();
                }

                Profiler.EndSample();
            }

            private bool CreateEntities(EntityManager entityManager)
            {
                var count = GetCount();

                if (count == 0)
                {
                    return false;
                }

                Profiler.BeginSample("CreateEntity");

                // Felt like Temp should be the allocator but gets disposed for some reason.
                entities = new NativeArray<Entity>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                if (!archetype.Valid)
                {
                    archetype = entityManager.CreateArchetype(ArchetypeTypes);
                }

                entityManager.CreateEntity(archetype, entities);

                Profiler.EndSample();
                return true;
            }
        }
    }
}