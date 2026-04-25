using System;
using System.Collections.Generic;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class AssetRegistry : Node
{
    private readonly Dictionary<string, string> _manifest = new Dictionary<string, string>();
    private readonly Dictionary<string, float> _loopPoints = new Dictionary<string, float>();

    public override void _Ready()
    {
        LoadManifest();
    }

    public T? Load<T>(string key)
        where T : Resource
    {
        if (!_manifest.TryGetValue(key, out string? path))
        {
            GD.PushWarning($"AssetRegistry: key '{key}' not found in manifest.");
            return null;
        }

        return GD.Load<T>(path);
    }

    public PackedScene? GetScene(string key) => Load<PackedScene>(key);

    public float GetLoopPoint(string key, float defaultValue = 0f)
    {
        return _loopPoints.TryGetValue(key, out float point) ? point : defaultValue;
    }

    private void LoadManifest()
    {
        using FileAccess? file = FileAccess.Open(
            "res://data/assets_manifest.json",
            FileAccess.ModeFlags.Read);

        if (file is null)
        {
            throw new InvalidOperationException(
                "AssetRegistry: assets_manifest.json not found.");
        }

        Variant parsed = Json.ParseString(file.GetAsText());
        var root = parsed.AsGodotDictionary();
        var assets = root["assets"].AsGodotDictionary();

        foreach (var kvp in assets)
        {
            string key = kvp.Key.AsString();

            if (kvp.Value.VariantType == Variant.Type.Dictionary)
            {
                var meta = kvp.Value.AsGodotDictionary();
                _manifest[key] = meta["path"].AsString();

                if (meta.ContainsKey("loopPoint"))
                {
                    _loopPoints[key] = (float)meta["loopPoint"].AsDouble();
                }
            }
            else
            {
                _manifest[key] = kvp.Value.AsString();
            }
        }

        GD.Print($"AssetRegistry: loaded {_manifest.Count} asset entries.");
        ValidateScenes();
    }

    private void ValidateScenes()
    {
        foreach (KeyValuePair<string, string> entry in _manifest)
        {
            if (!entry.Key.StartsWith("scene_", StringComparison.Ordinal))
            {
                continue;
            }

            if (GD.Load<PackedScene>(entry.Value) is null)
            {
                throw new InvalidOperationException(
                    $"AssetRegistry: scene '{entry.Key}' at path '{entry.Value}' failed to load. Fix the manifest before running.");
            }
        }
    }
}
