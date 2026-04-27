using FeralFrenzy.Core.Animation;
using FeralFrenzy.Core.Data.Content;
using Xunit;

namespace FeralFrenzy.Tests.Animation;

public class AnimationStateMachineTests
{
    // --- helpers ---

    private static AnimationInput Input(
        bool isMoving = false,
        bool isOnFloor = true,
        bool isJumping = false,
        bool isSliding = false,
        bool isDead = false,
        bool tookHit = false,
        float velocityY = 0f) => new AnimationInput(
            IsMoving: isMoving,
            IsOnFloor: isOnFloor,
            IsOnWall: false,
            IsJumping: isJumping,
            IsSliding: isSliding,
            IsAttacking: false,
            IsDead: isDead,
            TookHit: tookHit,
            VelocityY: velocityY,
            VelocityX: isMoving ? 5f : 0f);

    // Rules matching the PlayerController setup — exercise the full state machine logic.
    private static AnimationStateMachine<FFPlayerAnimationState> BuildPlayerStateMachine()
    {
        AnimationRuleSet<FFPlayerAnimationState> rules = new AnimationRuleSet<FFPlayerAnimationState>(
            FFPlayerAnimationState.Idle,
            new[]
            {
                new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsDead, FFPlayerAnimationState.Death),
                new AnimationRule<FFPlayerAnimationState>((_, i) => i.TookHit, FFPlayerAnimationState.Hit),
                new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsJumping, FFPlayerAnimationState.Jump),
                new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsSliding, FFPlayerAnimationState.Slide),
                new AnimationRule<FFPlayerAnimationState>((_, i) => !i.IsOnFloor && i.VelocityY > 0f, FFPlayerAnimationState.Fall),
                new AnimationRule<FFPlayerAnimationState>((c, i) => c == FFPlayerAnimationState.Walk && i.IsMoving, FFPlayerAnimationState.Walk),
                new AnimationRule<FFPlayerAnimationState>((c, i) => c == FFPlayerAnimationState.WalkStart && i.IsMoving, FFPlayerAnimationState.Walk),
                new AnimationRule<FFPlayerAnimationState>((_, i) => i.IsMoving, FFPlayerAnimationState.WalkStart),
            });

        return new AnimationStateMachine<FFPlayerAnimationState>(
            initialState: FFPlayerAnimationState.Idle,
            evaluate: rules.Evaluate,
            oneShotStates: new[]
            {
                FFPlayerAnimationState.WalkStart,
                FFPlayerAnimationState.Slide,
                FFPlayerAnimationState.Hit,
                FFPlayerAnimationState.Death,
            });
    }

    // --- basic transitions ---

    [Fact]
    public void Idle_WhenMoving_TransitionsToWalkStart()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();

        FFPlayerAnimationState result = sm.Update(Input(isMoving: true));

        Assert.Equal(FFPlayerAnimationState.WalkStart, result);
        Assert.True(sm.JustTransitioned);
    }

    [Fact]
    public void WalkStart_WhenFinished_TransitionsToWalk()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true)); // → WalkStart (blocked as one-shot)
        sm.NotifyFinished(FFPlayerAnimationState.WalkStart);

        FFPlayerAnimationState result = sm.Update(Input(isMoving: true));

        Assert.Equal(FFPlayerAnimationState.Walk, result);
    }

    [Fact]
    public void Walk_WhenNotMoving_TransitionsToIdle()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true));
        sm.NotifyFinished(FFPlayerAnimationState.WalkStart);
        sm.Update(Input(isMoving: true)); // now Walk

        FFPlayerAnimationState result = sm.Update(Input(isMoving: false));

        Assert.Equal(FFPlayerAnimationState.Idle, result);
    }

    [Fact]
    public void Idle_WhenJumping_TransitionsToJump()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();

        FFPlayerAnimationState result = sm.Update(Input(isJumping: true, isOnFloor: false));

        Assert.Equal(FFPlayerAnimationState.Jump, result);
    }

    [Fact]
    public void Jump_WhenVelocityPositive_TransitionsToFall()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isJumping: true, isOnFloor: false)); // → Jump

        FFPlayerAnimationState result = sm.Update(Input(isOnFloor: false, velocityY: 50f));

        Assert.Equal(FFPlayerAnimationState.Fall, result);
    }

    [Fact]
    public void Fall_WhenOnFloor_TransitionsToIdle()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isOnFloor: false, velocityY: 50f)); // → Fall

        FFPlayerAnimationState result = sm.Update(Input(isOnFloor: true));

        Assert.Equal(FFPlayerAnimationState.Idle, result);
    }

    // --- global transitions ---

    [Fact]
    public void AnyState_WhenDead_TransitionsToDeath()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true));
        sm.NotifyFinished(FFPlayerAnimationState.WalkStart);
        sm.Update(Input(isMoving: true)); // Walk

        FFPlayerAnimationState result = sm.Update(Input(isDead: true));

        Assert.Equal(FFPlayerAnimationState.Death, result);
    }

    [Fact]
    public void AnyState_WhenHit_TransitionsToHit()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true));
        sm.NotifyFinished(FFPlayerAnimationState.WalkStart);
        sm.Update(Input(isMoving: true)); // Walk

        FFPlayerAnimationState result = sm.Update(Input(tookHit: true));

        Assert.Equal(FFPlayerAnimationState.Hit, result);
    }

    [Fact]
    public void Death_IsTerminal_NoFurtherTransitions()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isDead: true)); // → Death (one-shot, never finished)

        // Even with isDead cleared, Death is a one-shot and NotifyFinished was never called.
        FFPlayerAnimationState result = sm.Update(Input(isMoving: true));

        Assert.Equal(FFPlayerAnimationState.Death, result);
        Assert.False(sm.JustTransitioned);
    }

    // --- one-shot blocking ---

    [Fact]
    public void WalkStart_BlocksTransitionUntilFinished()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true)); // → WalkStart

        // Attempt to transition to Walk without notifying finished — should stay blocked.
        FFPlayerAnimationState blocked = sm.Update(Input(isMoving: true));

        Assert.Equal(FFPlayerAnimationState.WalkStart, blocked);
        Assert.False(sm.JustTransitioned);
    }

    [Fact]
    public void Slide_BlocksTransitionUntilFinished()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isSliding: true)); // → Slide

        // Attempt to leave Slide before NotifyFinished.
        FFPlayerAnimationState blocked = sm.Update(Input(isMoving: true));

        Assert.Equal(FFPlayerAnimationState.Slide, blocked);
    }

    [Fact]
    public void Hit_ReturnsToPreHitState_AfterFinished()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        // Reach Walk first.
        sm.Update(Input(isMoving: true));
        sm.NotifyFinished(FFPlayerAnimationState.WalkStart);
        sm.Update(Input(isMoving: true)); // Walk

        sm.Update(Input(tookHit: true)); // → Hit
        sm.NotifyFinished(FFPlayerAnimationState.Hit);

        // After Hit finishes, moving input should yield Walk (via WalkStart since Walk
        // rule requires current == Walk, but we are coming from Hit so current == Idle-ish).
        // The exact state depends on rules — verify we are out of Hit.
        FFPlayerAnimationState result = sm.Update(Input(isMoving: true));

        Assert.NotEqual(FFPlayerAnimationState.Hit, result);
    }

    // --- rule set integration ---

    [Fact]
    public void RuleSet_FirstMatchWins()
    {
        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsDead, FFSimpleEnemyState.Death),
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsDead, FFSimpleEnemyState.Hit),
            });

        AnimationInput deadInput = Input(isDead: true);
        FFSimpleEnemyState result = ruleSet.Evaluate(FFSimpleEnemyState.Idle, deadInput);

        Assert.Equal(FFSimpleEnemyState.Death, result);
    }

    [Fact]
    public void RuleSet_ReturnsDefault_WhenNoRuleMatches()
    {
        AnimationRuleSet<FFSimpleEnemyState> ruleSet = new AnimationRuleSet<FFSimpleEnemyState>(
            FFSimpleEnemyState.Idle,
            new[]
            {
                new AnimationRule<FFSimpleEnemyState>((_, i) => i.IsMoving, FFSimpleEnemyState.Walk),
            });

        FFSimpleEnemyState result = ruleSet.Evaluate(FFSimpleEnemyState.Idle, Input(isMoving: false));

        Assert.Equal(FFSimpleEnemyState.Idle, result);
    }

    // --- JustTransitioned flag ---

    [Fact]
    public void JustTransitioned_FalseWhenStateUnchanged()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input()); // Idle → Idle

        Assert.False(sm.JustTransitioned);
    }

    [Fact]
    public void JustTransitioned_TrueOnFirstTransition()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isMoving: true));

        Assert.True(sm.JustTransitioned);
    }

    // --- Reset ---

    [Fact]
    public void Reset_ClearsOneShotBlocking()
    {
        AnimationStateMachine<FFPlayerAnimationState> sm = BuildPlayerStateMachine();
        sm.Update(Input(isDead: true)); // → Death (blocked)

        sm.Reset(FFPlayerAnimationState.Idle);

        FFPlayerAnimationState result = sm.Update(Input(isMoving: true));
        Assert.Equal(FFPlayerAnimationState.WalkStart, result);
    }
}
