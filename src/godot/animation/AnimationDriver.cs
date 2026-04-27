using System;
using System.Collections.Generic;
using FeralFrenzy.Core.Animation;
using Godot;

namespace FeralFrenzy.Godot.Animation;

public partial class AnimationDriver<TState> : AnimationDriverBase
where TState : struct, Enum
{
    private AnimationStateMachine<TState> _stateMachine = null!;
    private Dictionary<TState, string> _clipNames = null!;
    private Func<AnimationInput> _inputBuilder = null!;
    private AnimatedSprite2D? _sprite;
    private AnimationPlayer? _animPlayer;
    private TState _currentState;

    internal void Initialize(
        AnimationStateMachine<TState> stateMachine,
        Dictionary<TState, string> clipNames,
        Func<AnimationInput> inputBuilder,
        AnimatedSprite2D? sprite,
        AnimationPlayer? animPlayer)
    {
        _stateMachine = stateMachine;
        _clipNames = clipNames;
        _inputBuilder = inputBuilder;
        _sprite = sprite;
        _animPlayer = animPlayer;

        if (_sprite is not null)
        {
            _sprite.AnimationFinished += OnSpriteAnimationFinished;
        }

        if (_animPlayer is not null)
        {
            _animPlayer.AnimationFinished += OnAnimationPlayerFinished;
        }
    }

    public override void Tick()
    {
        AnimationInput input = _inputBuilder();
        _currentState = _stateMachine.Update(input);

        if (!_stateMachine.JustTransitioned)
        {
            return;
        }

        if (!_clipNames.TryGetValue(_currentState, out string? clip))
        {
            return;
        }

        if (_sprite is not null)
        {
            // Only play if the SpriteFrames knows this animation, to avoid console errors
            // with placeholder frames that don't have all clips yet.
            if (_sprite.SpriteFrames is not null && _sprite.SpriteFrames.HasAnimation(clip))
            {
                _sprite.Play(clip);
            }
        }

        _animPlayer?.Play(clip);
    }

    private void OnSpriteAnimationFinished()
        => _stateMachine.NotifyFinished(_currentState);

    private void OnAnimationPlayerFinished(StringName _)
        => _stateMachine.NotifyFinished(_currentState);
}
