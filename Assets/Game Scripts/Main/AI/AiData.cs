using Unity.Collections;

public struct AiData
{
    public NativeArray<Decision> Decisions;
    public NativeArray<Choice> Choices;
    public NativeArray<Consideration> Considerations;
    public NativeHashMap<int, float> RecordedScores;
}