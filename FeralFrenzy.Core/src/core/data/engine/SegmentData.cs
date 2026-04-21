using System.Collections.Generic;

namespace FeralFrenzy.Core.Data.Engine;

public record SegmentData(
    string SegmentId,
    string ChapterKey,
    SegmentType Type,
    GeometryProfile Geometry,
    bool CeilingPresent,
    DestructibleLevel Destructible,
    float DifficultyBudget,
    HazardClass HazardClass,
    PlatformMotivation PlatformMotivation,
    SightlineRating Sightline,
    List<RewardNode> RewardNodes,
    List<string> EnemyRoster,
    string UniqueMechanicTag,
    int PlayerCountAtGeneration);
