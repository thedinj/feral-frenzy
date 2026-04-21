using Godot;

namespace FeralFrenzy.Godot.Autoloads;

public partial class AssetRegistry : Node
{
    // Phase 0 stub — returns null for all keys; full implementation: Phase 1, loads from data/assets_manifest.json
    public T? Load<T>(string key)
        where T : Resource
    {
        GD.PushWarning($"AssetRegistry: key '{key}' requested but registry not yet initialized.");
        return null;
    }

    public PackedScene? GetScene(string key) => Load<PackedScene>(key);
}
