using System.Collections.Generic;
using FeralFrenzy.Core.Data.Engine;

namespace FeralFrenzy.Core.Data.Content;

public record FFChapterDefinition(
    string ChapterKey,
    string DisplayName,
    GeometryProfile PreferredGeometry,
    SightlineRating DefaultSightline,
    bool CeilingsPreferred,
    float BaseHazardBudgetRatio,
    List<string> EnemyPool,
    List<string> HazardPool,
    List<string> MechanicPool,
    string VillainKey,
    string TilesetKey,
    List<FFParallaxLayerDefinition> ParallaxLayers);
