using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeralFrenzy.Core.Data.Engine;
using Xunit;

namespace FeralFrenzy.Tests.Data;

public class SegmentDataSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private static SegmentData BuildSampleSegment()
    {
        return new SegmentData(
            SegmentId: "seg_001",
            ChapterKey: "chapter_cretaceous",
            Type: SegmentType.Opening,
            Geometry: GeometryProfile.Open,
            CeilingPresent: false,
            Destructible: DestructibleLevel.None,
            DifficultyBudget: 0.15f,
            HazardClass: HazardClass.Enemy,
            PlatformMotivation: PlatformMotivation.Tactical,
            Sightline: SightlineRating.Long,
            RewardNodes: new List<RewardNode>
            {
                new RewardNode(RewardNodeType.Positive, "powerup_rapid_fire"),
            },
            EnemyRoster: new List<string> { "enemy_raptor_rider", "enemy_ptero_bomber" },
            UniqueMechanicTag: "mechanic_dino_riding",
            PlayerCountAtGeneration: 2);
    }

    [Fact]
    public void RoundTrip_SegmentData_IsIdentical()
    {
        SegmentData original = BuildSampleSegment();
        string json = JsonSerializer.Serialize(original, Options);
        SegmentData? deserialized = JsonSerializer.Deserialize<SegmentData>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.SegmentId, deserialized!.SegmentId);
        Assert.Equal(original.ChapterKey, deserialized.ChapterKey);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.Geometry, deserialized.Geometry);
        Assert.Equal(original.DifficultyBudget, deserialized.DifficultyBudget);
        Assert.Equal(original.HazardClass, deserialized.HazardClass);
        Assert.Equal(original.UniqueMechanicTag, deserialized.UniqueMechanicTag);
        Assert.Equal(original.PlayerCountAtGeneration, deserialized.PlayerCountAtGeneration);
    }

    [Fact]
    public void Serialize_EnumValues_AreStringsNotIntegers()
    {
        SegmentData segment = BuildSampleSegment();
        string json = JsonSerializer.Serialize(segment, Options);

        Assert.Contains("\"Opening\"", json);
        Assert.Contains("\"Open\"", json);
        Assert.Contains("\"None\"", json);
        Assert.Contains("\"Enemy\"", json);
        Assert.Contains("\"Tactical\"", json);
        Assert.Contains("\"Long\"", json);
        Assert.DoesNotContain(": 0,", json);
        Assert.DoesNotContain(": 1,", json);
    }

    [Fact]
    public void Serialize_PropertyNames_AreCamelCase()
    {
        SegmentData segment = BuildSampleSegment();
        string json = JsonSerializer.Serialize(segment, Options);

        Assert.Contains("\"segmentId\"", json);
        Assert.Contains("\"chapterKey\"", json);
        Assert.Contains("\"ceilingPresent\"", json);
        Assert.Contains("\"difficultyBudget\"", json);
        Assert.Contains("\"rewardNodes\"", json);
        Assert.Contains("\"enemyRoster\"", json);
        Assert.Contains("\"uniqueMechanicTag\"", json);
        Assert.Contains("\"playerCountAtGeneration\"", json);
    }
}
