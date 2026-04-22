using System.Collections.Generic;
using System.Linq;
using FeralFrenzy.Core.Data.Content;
using FeralFrenzy.Godot.Autoloads;
using Godot;

namespace FeralFrenzy.Godot.Importer;

public static class GodotParallaxBuilder
{
    // Returns a Node2D container holding one Parallax2D per layer definition.
    // Godot 4.3+ replaced ParallaxBackground+ParallaxLayer with the single Parallax2D node.
    public static Node2D Build(
        IReadOnlyList<FFParallaxLayerDefinition> definitions,
        AssetRegistry registry)
    {
        Node2D container = new Node2D();

        foreach (FFParallaxLayerDefinition def in definitions.OrderBy(d => d.ZIndex))
        {
            Parallax2D layer = new Parallax2D();
            layer.ScrollScale = new Vector2(def.ScrollSpeedX, def.ScrollSpeedY);
            layer.RepeatSize = new Vector2(
                def.RepeatX ? 320f : 0f,
                def.RepeatY ? 180f : 0f);
            layer.ZIndex = def.ZIndex;

            Texture2D? texture = registry.Load<Texture2D>(def.SpriteKey);
            if (texture is not null)
            {
                Sprite2D sprite = new Sprite2D();
                sprite.Texture = texture;
                sprite.Centered = false;
                layer.AddChild(sprite);
            }

            container.AddChild(layer);
        }

        return container;
    }
}
