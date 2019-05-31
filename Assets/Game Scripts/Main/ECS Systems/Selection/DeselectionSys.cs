using Unity.Entities;

[UpdateInGroup(typeof(GameGroupLateSim))]
public class DeselectionSys : ComponentSystem
{
    private bool hasLeftClicked;
    private EntityQuery query;

    protected override void OnCreate()
    {
        Game.RegisterForUpdate(RenderUpdate);
        hasLeftClicked = false;
        query = GetEntityQuery(typeof(IsSelectedTag));
    }

    public void RenderUpdate()
    {
        hasLeftClicked = hasLeftClicked || GInput.GetMouseButtonUpQuick(0);
    }

    protected override void OnUpdate()
    {
        if (!hasLeftClicked)
        {
            return;
        }

        EntityManager.RemoveComponent(query, typeof(IsSelectedTag));
        hasLeftClicked = false;
    }
}