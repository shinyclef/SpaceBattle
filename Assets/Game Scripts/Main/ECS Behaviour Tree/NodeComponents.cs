using System;
using System.Collections.Generic;
using Unity.Entities;

public struct Node : IComponentData
{
    public NodeType NodeType;

    public NodeResult DoTask(Entity owner, params IComponentData[] compData)
    {
        switch (NodeType)
        {
            case NodeType.Sequence:
                SequenceNode seq = (SequenceNode)compData[2];
                return seq.DoTask(owner, compData);
        }

        return NodeResult.Pass;
    }
}

public struct SequenceNode : IComponentData
{
    List<Entity> children;

    public NodeResult DoTask(Entity bb, params IComponentData[] compData)
    {
        children = new List<Entity>();
        for (int i = 0; i < children.Count; i++)
        {

        }

        return NodeResult.Pass;
    }
}

public struct MoveNode : IComponentData
{
    public NodeResult DoTask(Entity bb, params IComponentData[] compData)
    {
        return NodeResult.Pass;
    }
}

[Serializable]
public struct NodeChildRef : IComponentData
{
    public Entity Child;
}

[InternalBufferCapacity(1)]
public struct NodeChildRefBuf : IBufferElementData
{
    public static implicit operator NodeChildRef(NodeChildRefBuf e) { return e.Value; }
    public static implicit operator NodeChildRefBuf(NodeChildRef e) { return new NodeChildRefBuf { Value = e }; }

    public NodeChildRef Value;
}