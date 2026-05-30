using Content.Shared._Respiral.TimeWindow;

namespace Content._Respiral.NPC;

public static class MedievalNPCBlackboard
{
    public const string FearEndTime = "RespiralFearEndTime";
    public const string FearCoordinates = "RespiralFearCoordinates";
}

public static class MedievalNPCDefaultsSettings
{
    public static TimedWindow FearWindowDuration = new TimedWindow(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
}