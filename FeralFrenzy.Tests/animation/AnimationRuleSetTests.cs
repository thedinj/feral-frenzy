using FeralFrenzy.Core.Animation;
using FeralFrenzy.Core.Data.Content;
using Xunit;

namespace FeralFrenzy.Tests.Animation;

public class AnimationRuleSetTests
{
    private static AnimationInput Moving() => new AnimationInput(
        IsMoving: true, IsOnFloor: true, IsOnWall: false,
        IsJumping: false, IsSliding: false, IsAttacking: false,
        IsDead: false, TookHit: false, VelocityY: 0f, VelocityX: 5f);

    private static AnimationInput Still() => new AnimationInput(
        IsMoving: false, IsOnFloor: true, IsOnWall: false,
        IsJumping: false, IsSliding: false, IsAttacking: false,
        IsDead: false, TookHit: false, VelocityY: 0f, VelocityX: 0f);

    [Fact]
    public void Evaluate_FirstRuleMatches_ReturnsFirstTarget()
    {
        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Walk),
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Attack),
            });

        FFSimpleEnemyState result = ruleSet.Evaluate(FFSimpleEnemyState.Idle, Moving());

        Assert.Equal(FFSimpleEnemyState.Walk, result);
    }

    [Fact]
    public void Evaluate_NoRuleMatches_ReturnsDefault()
    {
        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Walk),
            });

        FFSimpleEnemyState result = ruleSet.Evaluate(FFSimpleEnemyState.Idle, Still());

        Assert.Equal(FFSimpleEnemyState.Idle, result);
    }

    [Fact]
    public void Evaluate_SecondRuleMatches_WhenFirstDoesNot()
    {
        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsAttacking, FFSimpleEnemyState.Attack),
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Walk),
            });

        FFSimpleEnemyState result = ruleSet.Evaluate(FFSimpleEnemyState.Idle, Moving());

        Assert.Equal(FFSimpleEnemyState.Walk, result);
    }

    [Fact]
    public void Evaluate_CurrentStatePassedToCondition()
    {
        FFSimpleEnemyState capturedCurrent = FFSimpleEnemyState.Death;

        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>(
                    (c, _) => { capturedCurrent = c; return false; },
                    FFSimpleEnemyState.Walk),
            });

        ruleSet.Evaluate(FFSimpleEnemyState.Attack, Still());

        Assert.Equal(FFSimpleEnemyState.Attack, capturedCurrent);
    }
}
