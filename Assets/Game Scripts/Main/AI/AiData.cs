using Unity.Collections;

public struct AiData
{
    public NativeArray<Decision> Decisions;
    public NativeArray<Choice> Choices;
    public NativeArray<ushort> ConsiderationIndecies;
    public NativeArray<Consideration> Considerations;
}