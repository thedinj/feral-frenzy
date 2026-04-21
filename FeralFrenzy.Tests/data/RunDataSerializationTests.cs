using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeralFrenzy.Core.Data.Engine;
using Xunit;

namespace FeralFrenzy.Tests.Data;

public class RunDataSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private static RunData BuildSampleRunData()
    {
        return new RunData(
            RunId: "run_20240815_001",
            Seed: 48291,
            SchemaVersion: "1.0",
            Segments: new List<SegmentData>
            {
                new SegmentData(
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
                    PlayerCountAtGeneration: 2),
            },
            HasSurpriseDestructible: true);
    }

    [Fact]
    public void Serialize_RunData_ProducesValidJson()
    {
        RunData runData = BuildSampleRunData();
        string json = JsonSerializer.Serialize(runData, Options);

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"runId\"", json);
        Assert.Contains("\"seed\"", json);
        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"segments\"", json);
        Assert.Contains("\"hasSurpriseDestructible\"", json);
    }

    [Fact]
    public void Deserialize_ValidJson_ProducesRunData()
    {
        RunData original = BuildSampleRunData();
        string json = JsonSerializer.Serialize(original, Options);
        RunData? deserialized = JsonSerializer.Deserialize<RunData>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.RunId, deserialized!.RunId);
        Assert.Equal(original.Seed, deserialized.Seed);
        Assert.Equal(original.HasSurpriseDestructible, deserialized.HasSurpriseDestructible);
    }

    [Fact]
    public void RoundTrip_RunData_IsIdentical()
    {
        RunData original = BuildSampleRunData();
        string json = JsonSerializer.Serialize(original, Options);
        RunData? deserialized = JsonSerializer.Deserialize<RunData>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.RunId, deserialized!.RunId);
        Assert.Equal(original.Seed, deserialized.Seed);
        Assert.Equal(original.SchemaVersion, deserialized.SchemaVersion);
        Assert.Equal(original.HasSurpriseDestructible, deserialized.HasSurpriseDestructible);
        Assert.Single(deserialized.Segments);
        Assert.Equal(original.Segments[0].SegmentId, deserialized.Segments[0].SegmentId);
        Assert.Equal(original.Segments[0].Type, deserialized.Segments[0].Type);
        Assert.Equal(original.Segments[0].Geometry, deserialized.Segments[0].Geometry);
        Assert.Equal(original.Segments[0].DifficultyBudget, deserialized.Segments[0].DifficultyBudget);
    }

    [Fact]
    public void Deserialize_ExampleFromSchema_Succeeds()
    {
        const string schemaJson = """
            {
              "runId": "run_20240815_001",
              "seed": 48291,
              "schemaVersion": "1.0",
              "hasSurpriseDestructible": true,
              "segments": [
                {
                  "segmentId": "seg_001",
                  "chapterKey": "chapter_cretaceous",
                  "type": "Opening",
                  "geometry": "Open",
                  "ceilingPresent": false,
                  "destructible": "None",
                  "difficultyBudget": 0.15,
                  "hazardClass": "Enemy",
                  "platformMotivation": "Tactical",
                  "sightline": "Long",
                  "rewardNodes": [
                    { "type": "Positive", "powerUpKey": "powerup_rapid_fire" }
                  ],
                  "enemyRoster": ["enemy_raptor_rider", "enemy_ptero_bomber"],
                  "uniqueMechanicTag": "mechanic_dino_riding",
                  "playerCountAtGeneration": 2
                }
              ]
            }
            """;

        RunData? result = JsonSerializer.Deserialize<RunData>(schemaJson, Options);

        Assert.NotNull(result);
        Assert.Equal("run_20240815_001", result!.RunId);
        Assert.Equal(48291, result.Seed);
        Assert.Equal("1.0", result.SchemaVersion);
        Assert.True(result.HasSurpriseDestructible);
        Assert.Single(result.Segments);
        Assert.Equal("seg_001", result.Segments[0].SegmentId);
        Assert.Equal(SegmentType.Opening, result.Segments[0].Type);
        Assert.Equal(GeometryProfile.Open, result.Segments[0].Geometry);
        Assert.Equal(HazardClass.Enemy, result.Segments[0].HazardClass);
        Assert.Single(result.Segments[0].RewardNodes);
        Assert.Equal(RewardNodeType.Positive, result.Segments[0].RewardNodes[0].Type);
    }
}
