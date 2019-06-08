using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DeselectionSys))]
public class SelectionSys : ComponentSystem
{
    private const bool DebugMode = false;

    private BuildPhysicsWorld buildPhysicsWorldSys;
    private bool hasLeftClicked;

    public static Entity SelectedEntity { get; private set; }

    protected override void OnCreate()
    {
        buildPhysicsWorldSys = World.GetOrCreateSystem<BuildPhysicsWorld>();
        Game.RegisterForUpdate(RenderUpdate);
        hasLeftClicked = false;
    }

    public void RenderUpdate()
    {
        hasLeftClicked = hasLeftClicked || GInput.GetMouseButtonUpQuick(0);
    }

    protected override void OnUpdate()
    {
        if (SelectedEntity != Entity.Null && !EntityManager.Exists(SelectedEntity))
        {
            SelectedEntity = Entity.Null;
        }

        if (!hasLeftClicked)
        {
            return;
        }

        CollisionWorld collisionWorld = buildPhysicsWorldSys.PhysicsWorld.CollisionWorld;
        UnityEngine.Ray screenRay = Game.MainCam.ScreenPointToRay(GInput.MousePos);
        RaycastInput input = new RaycastInput
        {
            Start = screenRay.origin,
            End = screenRay.origin + screenRay.direction * 1000,
            Filter = new CollisionFilter()
            {
                BelongsTo = 1u << (int)PhysicsLayer.RayCast,
                CollidesWith = 1u << (int)PhysicsLayer.Ships,
                GroupIndex = 0
            }
        };

        #pragma warning disable 0162
        if (DebugMode)
        {
            UnityEngine.Debug.DrawRay(input.Start, input.End - input.Start, UnityEngine.Color.yellow, 1f);
        }
        #pragma warning restore 0162

        bool haveHit = collisionWorld.CastRay(input, out RaycastHit hit);
        if (haveHit)
        {
            Entity e = collisionWorld.Bodies[hit.RigidBodyIndex].Entity;
            EntityManager.AddComponent(e, typeof(IsSelectedTag));
            SelectedEntity = e;
            Logger.LogIf(DebugMode, "Have hit!");
        }
        else
        {
            SelectedEntity = Entity.Null;
        }

        hasLeftClicked = false;
    }
}