namespace FeralFrenzy.Core.Data.Content;

public record FFParallaxLayerDefinition(
    string SpriteKey,
    float ScrollSpeedX,
    float ScrollSpeedY,
    bool RepeatX,
    bool RepeatY,
    int ZIndex);
