using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Respiral.Blacksmithing;
using Content.Shared._Respiral.Blacksmithing.Components;
using Content.Shared.Burning.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Blacksmithing.Systems;

public sealed partial class QuenchingSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public bool TryQuench(EntityUid user, EntityUid ingot, EntityUid bucket, [NotNullWhen(true)] out EntityUid? result,
        HeatedComponent? heated = null, QuenchingComponent? quenching = null)
    {
        result = null;
        if (!Resolve(ingot, ref heated) || !Resolve(bucket, ref quenching))
            return false;

        if (heated.CurrentTemperature < 700) // Too cold
            return false;

        if (!_solution.TryGetSolution(bucket, quenching.Solution, out var bucketSln, out var sln))
            return false;

        if (sln.GetTotalPrototypeQuantity(quenching.CoolingReagent) < quenching.ReagentAmount)
            return false;

        var remQuantity = new ReagentQuantity(quenching.CoolingReagent, quenching.ReagentAmount);
        _solution.RemoveReagent(bucketSln.Value, remQuantity);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Respiral/Effects/Blacksmithing/quenching.ogg"), bucket);

        TryCompleteForgingProcess(ingot, user, heated, out result);
        return result != null;
    }

    private void TryCompleteForgingProcess(EntityUid ingot, EntityUid user, HeatedComponent heated, [NotNullWhen(true)] out EntityUid? result)
    {
        result = null;
        if (!TryComp<BlacksmithingProcessComponent>(ingot, out var process))
            return;

        var recipe = FindMatchingRecipe(ingot, process.StrikeSequence);

        if (recipe != null)
        {
            var analysis = AnalyzeStrikeSequence(process.StrikeSequence, recipe);
            if (analysis.IsSuccessful && CheckRecipeQuality(analysis, recipe))
            {
                result = Spawn(recipe.OutputEntity, Transform(ingot).Coordinates);
                ApplyQualityToResult(result.Value, analysis, recipe);
            }
            else
            {
                result = Spawn(recipe.FailedOutputEntity, Transform(ingot).Coordinates);
                _popup.PopupEntity(Loc.GetString("quenching-failed"), user, user);
            }
        }
        else
        {
            result = Spawn("ShapelessMetal", Transform(ingot).Coordinates);
        }

        if (TryComp<HeatedComponent>(result, out var resultHeated))
            resultHeated.CurrentTemperature = heated.CurrentTemperature * 0.2f;

        QueueDel(ingot);
    }

    private ForgingRecipePrototype? FindMatchingRecipe(EntityUid ingot, List<StrikeData> strikeSequence)
    {
        foreach (var recipeProto in _proto.EnumeratePrototypes<ForgingRecipePrototype>())
        {
            if (!TryComp(ingot, out MetaDataComponent? meta) || meta.EntityPrototype?.ID != recipeProto.InputEntity.ToString())
                continue;

            if (IsSequenceMatching(strikeSequence, recipeProto.RequiredSequence))
                return recipeProto;
        }

        return null;
    }

    private bool IsSequenceMatching(List<StrikeData> actual, List<StrikePattern> required)
    {
        var expandedSequence = ExpandStrikePatterns(required);
        if (actual.Count != expandedSequence.Count)
            return false;

        for (int i = 0; i < actual.Count; i++)
        {
            if (actual[i].Type != expandedSequence[i].Type)
                return false;
        }

        return true;
    }

    private List<ExpandedStrike> ExpandStrikePatterns(List<StrikePattern> patterns)
    {
        var result = new List<ExpandedStrike>();
        foreach (var pattern in patterns)
        {
            var strikeType = GetStrikeTypeFromPattern(pattern);
            var timeWindow = GetTimeWindowFromPattern(pattern);
            var rhythmBonus = GetRhythmBonusFromPattern(pattern);

            for (int i = 0; i < pattern.Amount; i++)
            {
                result.Add(new ExpandedStrike
                {
                    Type = strikeType,
                    TimeWindow = timeWindow,
                    RhythmBonus = rhythmBonus
                });
            }
        }

        return result;
    }

    private StrikeType GetStrikeTypeFromPattern(StrikePattern pattern)
    {
        if (pattern.StrikeId != null && _proto.TryIndex(pattern.StrikeId, out var strikeProto))
            return strikeProto.Type;

        return pattern.Type;
    }

    private (float Min, float Max) GetTimeWindowFromPattern(StrikePattern pattern)
    {
        if (pattern.TimeWindow != default)
            return pattern.TimeWindow;

        if (pattern.StrikeId != null && _proto.TryIndex(pattern.StrikeId, out var strikeProto))
            return strikeProto.TimeWindow;

        return (0.8f, 1.2f);
    }

    private float GetRhythmBonusFromPattern(StrikePattern pattern)
    {
        if (pattern.RhythmBonus != default)
            return pattern.RhythmBonus;

        if (pattern.StrikeId != null && _proto.TryIndex(pattern.StrikeId, out var strikeProto))
            return strikeProto.RhythmBonus;

        return 5f;
    }

    private SequenceAnalysis AnalyzeStrikeSequence(List<StrikeData> strikes, ForgingRecipePrototype recipe)
    {
        var expandedPatterns = ExpandStrikePatterns(recipe.RequiredSequence);
        var analysis = new SequenceAnalysis();
        int consecutiveGoodRhythms = 0;
        int heatingPausesUsed = 0;

        for (int i = 0; i < strikes.Count; i++)
        {
            var strike = strikes[i];
            var pattern = expandedPatterns[i];

            var tempQuality = EvaluateTemperatureQuality(strike.Temperature, recipe.IdealTemperature);
            analysis.TemperatureScores.Add(tempQuality);

            StrikeQuality rhythmQuality;

            if (i > 0)
            {
                var timeDiff = strike.TimeFromPrevious;
                bool isHeatingPause = timeDiff > 10;

                if (isHeatingPause)
                {
                    rhythmQuality = EvaluateHeatingPauseQuality(timeDiff);

                    if (rhythmQuality == StrikeQuality.Good)
                        heatingPausesUsed++;
                }
                else
                {
                    float rhythmForgiveness = 1f + consecutiveGoodRhythms * 0.15f;
                    var adaptedMin = pattern.TimeWindow.Min * Math.Max(0.6f, 1f - rhythmForgiveness * 0.1f);
                    var adaptedMax = pattern.TimeWindow.Max * (1f + rhythmForgiveness * 0.15f);

                    if (timeDiff > 2f && consecutiveGoodRhythms >= 2)
                        adaptedMax = Math.Max(adaptedMax, 5f);

                    rhythmQuality = EvaluateRhythmQuality(timeDiff, adaptedMin, adaptedMax);
                }

                if (rhythmQuality >= StrikeQuality.Good && !isHeatingPause)
                    consecutiveGoodRhythms++;
                else if (rhythmQuality == StrikeQuality.Bad)
                    consecutiveGoodRhythms = 0;

                analysis.RhythmScores.Add(rhythmQuality);

                if (rhythmQuality == StrikeQuality.Perfect)
                    analysis.RhythmBonus += pattern.RhythmBonus;
                else if (rhythmQuality == StrikeQuality.Good)
                    analysis.RhythmBonus += pattern.RhythmBonus * 0.5f;
            }
            else
            {
                analysis.RhythmScores.Add(StrikeQuality.Good);
                rhythmQuality = StrikeQuality.Good;
                consecutiveGoodRhythms = 1;
            }

            var strikeQuality = CombineQualities(tempQuality, rhythmQuality);

            switch (strikeQuality)
            {
                case StrikeQuality.Perfect:
                    analysis.PerfectStrikes++;
                    analysis.QualityScore += 2f;
                    break;
                case StrikeQuality.Good:
                    analysis.GoodStrikes++;
                    analysis.QualityScore += 1f;
                    break;
                case StrikeQuality.Bad:
                    analysis.BadStrikes++;
                    analysis.QualityScore += 0.3f;
                    break;
            }
        }

        if (heatingPausesUsed > 2)
        {
            analysis.QualityScore *= Math.Max(0.7f, 1f - (heatingPausesUsed - 2) * 0.1f);
        }

        return analysis;
    }

    private StrikeQuality EvaluateTemperatureQuality(float temperature, (float Min, float Max) idealRange)
    {
        return temperature switch
        {
            var t when t >= idealRange.Min && t <= idealRange.Max => StrikeQuality.Perfect,
            var t when t >= idealRange.Min - 100 && t <= idealRange.Max + 100 => StrikeQuality.Good,
            _ => StrikeQuality.Bad
        };
    }

    private StrikeQuality EvaluateHeatingPauseQuality(double timeDiff)
    {
        return timeDiff switch
        {
            <= 30f => StrikeQuality.Good,
            <= 60f => StrikeQuality.Good,
            _ => StrikeQuality.Bad
        };
    }

    private StrikeQuality EvaluateRhythmQuality(double timeDiff, float minTime, float maxTime)
    {
        if (timeDiff >= minTime && timeDiff <= maxTime)
            return StrikeQuality.Perfect;

        float center = (minTime + maxTime) / 2f;
        float deviation = Math.Abs((float)timeDiff - center);
        float range = maxTime - minTime;

        return deviation <= range ? StrikeQuality.Good : StrikeQuality.Bad;
    }

    private StrikeQuality CombineQualities(StrikeQuality tempQuality, StrikeQuality rhythmQuality)
    {
        return (StrikeQuality)Math.Min((byte)tempQuality, (byte)rhythmQuality);
    }

    private bool CheckRecipeQuality(SequenceAnalysis analysis, ForgingRecipePrototype recipe)
    {
        var effectiveBadStrikes = Math.Max(0, analysis.BadStrikes - analysis.TemperatureScores.Count(q => q == StrikeQuality.Perfect) / 3);

        return analysis.PerfectStrikes >= recipe.MinPerfectStrikes &&
               effectiveBadStrikes <= recipe.MaxBadStrikes;
    }

    private void ApplyQualityToResult(EntityUid result, SequenceAnalysis analysis, ForgingRecipePrototype recipe)
    {
        var finalQuality = CalculateFinalQuality(analysis, recipe);
        if (TryComp<EntityQualityComponent>(result, out var quality))
            quality.Quality = finalQuality;
    }

    private EntityQuality CalculateFinalQuality(SequenceAnalysis analysis, ForgingRecipePrototype recipe)
    {
        var perfectMultiplier = recipe.QualityMultipliers[StrikeQuality.Perfect];
        var goodMultiplier = recipe.QualityMultipliers[StrikeQuality.Good];
        var badMultiplier = recipe.QualityMultipliers[StrikeQuality.Bad];

        var totalScore = analysis.PerfectStrikes * perfectMultiplier +
                         analysis.GoodStrikes * goodMultiplier +
                         analysis.BadStrikes * badMultiplier +
                         analysis.RhythmBonus;

        int perfectTempCount = analysis.TemperatureScores.Count(q => q == StrikeQuality.Perfect);
        if (perfectTempCount == analysis.TemperatureScores.Count)
            totalScore += 15f;
        else if (perfectTempCount >= analysis.TemperatureScores.Count * 0.7f)
            totalScore += 8f;

        int maxConsecutiveGood = CalculateMaxConsecutiveGood(analysis.RhythmScores);
        if (maxConsecutiveGood >= 5)
            totalScore += 10f;
        else if (maxConsecutiveGood >= 3)
            totalScore += 5f;

        return totalScore switch
        {
            < 15 => EntityQuality.Rough,
            < 25 => EntityQuality.Normal,
            < 35 => EntityQuality.Good,
            < 45 => EntityQuality.Master,
            _ => EntityQuality.Masterpiece
        };
    }

    private int CalculateMaxConsecutiveGood(List<StrikeQuality> rhythmScores)
    {
        int max = 0;
        int current = 0;

        for (int i = 1; i < rhythmScores.Count; i++)
        {
            if (rhythmScores[i] >= StrikeQuality.Good)
            {
                current++;
                max = Math.Max(max, current);
            }
            else
            {
                current = 0;
            }
        }

        return max;
    }
}

public sealed class SequenceAnalysis
{
    public int PerfectStrikes { get; set; }
    public int GoodStrikes { get; set; }
    public int BadStrikes { get; set; }
    public float QualityScore { get; set; }
    public float RhythmBonus { get; set; }
    public List<StrikeQuality> TemperatureScores { get; } = new();
    public List<StrikeQuality> RhythmScores { get; } = new();
    public bool IsSuccessful => PerfectStrikes + GoodStrikes > BadStrikes * 2;
}

public sealed class ExpandedStrike
{
    public StrikeType Type { get; init; }
    public (float Min, float Max) TimeWindow { get; init; }
    public float RhythmBonus { get; init; }
}
