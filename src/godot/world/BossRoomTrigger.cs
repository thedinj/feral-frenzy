using FeralFrenzy.Core.Data.Engine;
using FeralFrenzy.Godot.Autoloads;
using FeralFrenzy.Godot.Characters;
using FeralFrenzy.Godot.Constants;
using Godot;

namespace FeralFrenzy.Godot.World;

public partial class BossRoomTrigger : Area2D
{
    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (_triggered || body is not PlayerController)
        {
            return;
        }

        _triggered = true;
        SpawnBoss();

        GetNode<GameStateManager>(AutoloadPaths.GameStateManager)
            .TransitionTo<BossIntroState>(
                new BossFightPayload("villain_rex", "chapter_cretaceous"));
    }

    private void SpawnBoss()
    {
        PackedScene? scene = GetNode<AssetRegistry>(AutoloadPaths.AssetRegistry)
            .GetScene(AssetKeys.SceneEnemyBoss);

        if (scene is null)
        {
            return;
        }

        Node2D boss = scene.Instantiate<Node2D>();
        boss.GlobalPosition = new Vector2(160f, 155f);

        if (LevelController.Instance is not null)
        {
            LevelController.Instance.AddToEntities(boss);
        }
    }
}
