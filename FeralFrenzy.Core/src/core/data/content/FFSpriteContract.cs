using System.Collections.Generic;

namespace FeralFrenzy.Core.Data.Content;

public record FFSpriteContract(
    string EntityKey,
    int FrameWidth,
    int FrameHeight,
    IReadOnlyList<FFSpriteAnimation> Animations
);

public record FFSpriteAnimation(
    string Name,
    bool Loop,
    float Fps,
    IReadOnlyList<FFSpriteFrame> Frames
);

public record FFSpriteFrame(int X, int Y);
