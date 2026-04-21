using FeralFrenzy.Core.Constants;
using Xunit;

namespace FeralFrenzy.Tests;

public class SolvabilityConstantsTests
{
    [Fact]
    public void ReferenceCharacterKey_IsCharCroc()
    {
        Assert.Equal("char_croc", SolvabilityConstants.ReferenceCharacterKey);
    }

    [Fact]
    public void GuaranteedHeavyDestructibleChapter_IsChapterDeadStation()
    {
        Assert.Equal("chapter_dead_station", SolvabilityConstants.GuaranteedHeavyDestructibleChapter);
    }

    [Fact]
    public void MaxDifficultyRampPerSegment_IsBetweenZeroAndOne()
    {
        Assert.True(SolvabilityConstants.MaxDifficultyRampPerSegment > 0f);
        Assert.True(SolvabilityConstants.MaxDifficultyRampPerSegment < 1f);
    }
}
