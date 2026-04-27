using System;
using System.Text.Json;
using FeralFrenzy.Core.Data.Content;
using Godot;

namespace FeralFrenzy.Godot.Animation;

public static class SpriteFramesBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public static SpriteFrames Build(Texture2D spritesheet, FFSpriteContract contract)
    {
        SpriteFrames frames = new SpriteFrames();
        frames.RemoveAnimation("default");

        foreach (FFSpriteAnimation anim in contract.Animations)
        {
            frames.AddAnimation(anim.Name);
            frames.SetAnimationLoop(anim.Name, anim.Loop);
            frames.SetAnimationSpeed(anim.Name, anim.Fps);

            foreach (FFSpriteFrame frame in anim.Frames)
            {
                AtlasTexture atlas = new AtlasTexture();
                atlas.Atlas = spritesheet;
                atlas.Region = new Rect2(
                    frame.X * contract.FrameWidth,
                    frame.Y * contract.FrameHeight,
                    contract.FrameWidth,
                    contract.FrameHeight);
                frames.AddFrame(anim.Name, atlas);
            }
        }

        return frames;
    }

    /// <summary>
    /// Loads contract JSON from the given path.
    /// Convention: contract lives adjacent to the spritesheet with a .json extension.
    /// </summary>
    public static FFSpriteContract LoadContract(string contractPath)
    {
        using FileAccess? file = FileAccess.Open(contractPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new InvalidOperationException(
                $"SpriteFramesBuilder: contract not found at {contractPath}");
        }

        string json = file.GetAsText();
        return JsonSerializer.Deserialize<FFSpriteContract>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"SpriteFramesBuilder: failed to deserialize contract at {contractPath}");
    }
}
