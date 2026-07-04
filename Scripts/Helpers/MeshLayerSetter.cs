namespace Behide.Helpers;

using Godot;


[Tool]
public partial class MeshLayerSetter : EditorScenePostImport
{
    public override GodotObject _PostImport(Node scene)
    {
        if (!GetSourceFile().Contains("prop", StringComparison.InvariantCultureIgnoreCase)) return scene;
        Iterate(scene);
        return scene;
    }

    private static void Iterate(Node? node)
    {
        if (node is null) return;
        if (node is MeshInstance3D mesh) mesh.Layers = 2; // Props layer
        foreach (var child in node.GetChildren()) Iterate(child);
    }
}
