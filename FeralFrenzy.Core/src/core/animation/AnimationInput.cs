namespace FeralFrenzy.Core.Animation;

/// <summary>
/// Facts about an entity's physical and behavioral state this frame.
/// Set by the controller after all game logic resolves.
/// The animation system observes — it never reads raw input.
/// </summary>
public record AnimationInput(
    bool IsMoving,
    bool IsOnFloor,
    bool IsOnWall,
    bool IsJumping,
    bool IsSliding,
    bool IsAttacking,
    bool IsDead,
    bool TookHit,
    float VelocityY,
    float VelocityX
);
