public enum FactType
{
    Constant,
    Noise,
    DistanceFromTargetMulti,
    DistanceFromTargetIn1SecMulti,
    AngleFromTargetMulti, // how 'easy' is a target? an easy target is in front, and in weapons range, and should remain so in the next second. TimeUntilTargetLost
    TimeSinceLastChoiceSelection,
    TimeSinceThisChoiceSelection
}