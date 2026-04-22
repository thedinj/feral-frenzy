using System;
using System.Collections.Generic;
using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class AssetRegistry : Node
{
    private readonly Dictionary<string, string> _manifest = new Dictionary<string, string>();

    public override void _Ready()
    {
        LoadManifest();
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
            _manifest[kvp.Key.AsString()] = kvp.Value.AsString();
        }

        GD.Print($"AssetRegistry: loaded {_manifest.Count} asset entries.");
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
}
